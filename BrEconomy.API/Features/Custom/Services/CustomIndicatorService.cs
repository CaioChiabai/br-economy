using System.Globalization;
using System.Text.Json;
using BrEconomy.API.Features.Custom.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace BrEconomy.API.Features.Custom.Services;

public class CustomIndicatorService : ICustomIndicatorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CustomIndicatorService> _logger;

    public CustomIndicatorService(
        IHttpClientFactory httpClientFactory,
        IDistributedCache cache,
        ILogger<CustomIndicatorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IndicatorResult> FetchIndicatorAsync(IndicatorInterpretation interpretation)
    {
        // Para períodos custom, não usar cache pois as datas podem variar
        if (interpretation.Period != "custom")
        {
            var cacheKey = $"custom:{interpretation.Indicator.ToLowerInvariant()}:{interpretation.Period}";
            var cached = await _cache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("Cache hit: {Key}", cacheKey);
                return JsonSerializer.Deserialize<IndicatorResult>(cached)!;
            }
        }

        _logger.LogInformation("Buscando dados do BCB para {Indicator} (período: {Period})", 
            interpretation.Indicator, interpretation.Period);

        var client = _httpClientFactory.CreateClient("BancoCentral");
        string url;
        
        if (interpretation.Period == "custom" && !string.IsNullOrEmpty(interpretation.StartDate))
        {
            // Período customizado - buscar por intervalo de datas
            url = $"https://api.bcb.gov.br/dados/serie/bcdata.sgs.{interpretation.BcbCode}/dados?formato=json&dataInicial={interpretation.StartDate}&dataFinal={interpretation.EndDate}";
            _logger.LogInformation("Buscando período customizado: {Start} até {End}", interpretation.StartDate, interpretation.EndDate);
        }
        else
        {
            // Período padrão - buscar último valor
            url = $"https://api.bcb.gov.br/dados/serie/bcdata.sgs.{interpretation.BcbCode}/dados/ultimos/1?formato=json";
        }

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var elements = doc.RootElement.EnumerateArray().ToList();

        if (interpretation.Period == "custom" && elements.Count > 1)
        {
            // Múltiplos valores - agregar conforme solicitado
            return ProcessCustomPeriod(interpretation, elements);
        }
        else
        {
            // Valor único
            var element = elements[0];
            var result = new IndicatorResult(
                Indicator: interpretation.Indicator,
                Description: interpretation.Description,
                Value: ParseValue(element.GetProperty("valor").GetString()!),
                Date: element.GetProperty("data").GetString()!,
                Source: "bcb",
                Timestamp: DateTime.UtcNow
            );

            // Salvar no cache apenas para períodos não-custom
            if (interpretation.Period != "custom")
            {
                var cacheKey = $"custom:{interpretation.Indicator.ToLowerInvariant()}:{interpretation.Period}";
                var serialized = JsonSerializer.Serialize(result);
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
                _logger.LogInformation("Salvo no cache: {Key}", cacheKey);
            }

            return result;
        }
    }

    private IndicatorResult ProcessCustomPeriod(IndicatorInterpretation interpretation, List<JsonElement> elements)
    {
        var dataPoints = elements.Select(e => new DataPoint(
            Date: e.GetProperty("data").GetString()!,
            Value: ParseValue(e.GetProperty("valor").GetString()!)
        )).ToList();

        double finalValue;
        string periodDescription;
        var years = interpretation.RequestedPeriodYears ?? CalculateYearsBetween(dataPoints.First().Date, dataPoints.Last().Date);

        switch (interpretation.AggregationType?.ToLowerInvariant())
        {
            case "accumulated":
                // Calcular acumulado baseado no tipo de indicador
                finalValue = CalculateAccumulatedRate(
                    dataPoints.Select(d => d.Value).ToList(),
                    interpretation.BcbCode,
                    interpretation.Indicator
                );
                
                periodDescription = $"Análise de {years} {(years == 1 ? "ano" : "anos")} ({dataPoints.Count} pontos)";
                
                _logger.LogInformation("Taxa acumulada {Years} anos: {Value}% ({Count} pontos)", 
                    years, finalValue, dataPoints.Count);
                break;

            case "average":
                // Calcular média das taxas
                finalValue = dataPoints.Average(d => d.Value);
                periodDescription = $"Média de {years} {(years == 1 ? "ano" : "anos")}";
                _logger.LogInformation("Média calculada: {Value}% a.a.", finalValue);
                break;

            case "last":
            default:
                // Último valor do período
                finalValue = dataPoints.Last().Value;
                periodDescription = $"Último valor do período";
                _logger.LogInformation("Último valor: {Value}% a.a.", finalValue);
                break;
        }

        // NÃO retornar dataPoints - apenas metadados
        return new IndicatorResult(
            Indicator: interpretation.Indicator,
            Description: periodDescription,
            Value: finalValue,
            Date: dataPoints.Last().Date,
            Source: "bcb",
            Timestamp: DateTime.UtcNow,
            Period: $"{dataPoints.First().Date} a {dataPoints.Last().Date}",
            DataPointsCount: dataPoints.Count,
            DataPoints: null // Removido para economia de dados
        );
    }

    private double CalculateAccumulatedRate(List<double> rates, int bcbCode, string indicator)
    {
        if (rates.Count == 0) return 0;

        _logger.LogInformation("Calculando taxa acumulada para {Indicator} (código {Code}) com {Count} valores", 
            indicator, bcbCode, rates.Count);

        // SELIC (432): valores são diários quando há muitos pontos
        if (bcbCode == 432)
        {
            double accumulated = 1.0;
            
            // Se tem muitos pontos (>100), provavelmente são dados diários
            if (rates.Count > 100)
            {
                // Taxa diária = (1 + taxa_anual)^(1/252) - 1, onde 252 são dias úteis
                foreach (var annualRate in rates)
                {
                    double dailyRate = Math.Pow(1 + annualRate / 100.0, 1.0 / 252.0) - 1;
                    accumulated *= (1 + dailyRate);
                }
            }
            else
            {
                // Poucos pontos, são dados mensais
                foreach (var annualRate in rates)
                {
                    double monthlyRate = annualRate / 12.0;
                    accumulated *= (1 + monthlyRate / 100.0);
                }
            }
            
            var result = Math.Round((accumulated - 1) * 100, 2);
            _logger.LogInformation("SELIC acumulada: {Result}%", result);
            return result;
        }

        // CDI (4392), IPCA (433): valores mensais anualizados
        double accumulatedMonthly = 1.0;
        foreach (var annualRate in rates)
        {
            double monthlyRate = annualRate / 12.0;
            accumulatedMonthly *= (1 + monthlyRate / 100.0);
        }
        
        return Math.Round((accumulatedMonthly - 1) * 100, 2);
    }

    private int CalculateYearsBetween(string startDate, string endDate)
    {
        var start = DateTime.ParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        var end = DateTime.ParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        return (int)Math.Round((end - start).TotalDays / 365.25);
    }

    private double ParseValue(string value)
    {
        return double.Parse(value.Replace(",", "."), CultureInfo.InvariantCulture);
    }
}
