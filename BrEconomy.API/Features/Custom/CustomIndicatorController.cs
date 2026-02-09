using BrEconomy.API.Features.Custom.Models;
using BrEconomy.API.Features.Custom.Services;
using Microsoft.AspNetCore.Mvc;

namespace BrEconomy.API.Features.Custom;

/// <summary>
/// Busca inteligente de indicadores econômicos usando linguagem natural
/// </summary>
[ApiController]
[Route("api/v1/indicators/custom")]
[Produces("application/json")]
public class CustomIndicatorController : ControllerBase
{
    private readonly IGroqService _groqService;
    private readonly ICustomIndicatorService _customIndicatorService;
    private readonly ILogger<CustomIndicatorController> _logger;

    private static readonly List<string> SupportedIndicators = new() { "SELIC", "IPCA", "CDI", "DÓLAR" };
    private static readonly List<string> Examples = new()
    {
        "Qual a taxa SELIC atual?",
        "Taxa acumulada do CDI nos últimos 5 anos",
        "Cotação do dólar hoje"
    };

    public CustomIndicatorController(
        IGroqService groqService,
        ICustomIndicatorService customIndicatorService,
        ILogger<CustomIndicatorController> logger)
    {
        _groqService = groqService;
        _customIndicatorService = customIndicatorService;
        _logger = logger;
    }

    /// <summary>
    /// Busca indicadores econômicos usando linguagem natural
    /// </summary>
    /// <param name="request">Pergunta sobre indicadores econômicos</param>
    /// <returns>Dados do indicador solicitado</returns>
    /// <remarks>
    /// Indicadores disponíveis: SELIC, IPCA, CDI, DÓLAR
    /// 
    /// Exemplos:
    /// - "Qual a SELIC hoje?"
    /// - "CDI acumulado nos últimos 5 anos"
    /// - "Inflação do ano"
    /// </remarks>
    [HttpPost("search")]
    [ProducesResponseType(typeof(SimpleIndicatorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PeriodAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IndicatorNotAvailableResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromBody] CustomSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query não pode ser vazia" });
        }

        _logger.LogInformation("Nova busca personalizada: {Query}", request.Query);

        try
        {
            var interpretation = await _groqService.InterpretIndicatorRequestAsync(request.Query);

            // Validar se o indicador é suportado
            if (!interpretation.IsValid)
            {
                _logger.LogWarning("Indicador não suportado ou consulta inválida: {Query}", request.Query);
                return BadRequest(new IndicatorNotAvailableResponse(
                    RequestedIndicator: interpretation.Indicator,
                    Message: interpretation.Suggestion ?? "Não consegui entender sua solicitação",
                    AvailableIndicators: SupportedIndicators,
                    Examples: Examples
                ));
            }

            _logger.LogInformation("IA interpretou como: {Indicator} (código {Code}, período: {Period})", 
                interpretation.Indicator, interpretation.BcbCode, interpretation.Period);

            var result = await _customIndicatorService.FetchIndicatorAsync(interpretation);

            _logger.LogInformation("Busca concluída: {Indicator} = {Value}", 
                result.Indicator, result.Value);

            return Ok(FormatResponse(result, interpretation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar busca personalizada");
            return StatusCode(500, new { error = "Erro interno ao processar sua solicitação" });
        }
    }

    private object FormatResponse(IndicatorResult result, IndicatorInterpretation interpretation)
    {
        var unit = GetUnit(interpretation.Indicator);

        // Consulta simples (valor atual)
        if (interpretation.Period == "last")
        {
            return new SimpleIndicatorResponse(
                Indicator: result.Indicator,
                Value: result.Value,
                Unit: unit,
                Date: result.Date,
                Context: $"{result.Indicator} em {result.Date}"
            );
        }

        // Análise de período (ytd, 12m, custom)
        if (interpretation.Period == "custom" && interpretation.AggregationType == "accumulated")
        {
            var years = interpretation.RequestedPeriodYears ?? 1;
            var investmentReturn = 100 * (1 + result.Value / 100);
            
            return new PeriodAnalysisResponse(
                Indicator: result.Indicator,
                PeriodDescription: result.Description,
                Summary: new AnalysisSummary(
                    MainValue: result.Value,
                    MainValueLabel: $"Taxa acumulada em {years} {(years == 1 ? "ano" : "anos")}",
                    AverageValue: null,
                    InitialValue: null,
                    FinalValue: null,
                    InitialDate: result.Period?.Split(" a ")[0],
                    FinalDate: result.Date,
                    DataPointsCount: result.DataPointsCount ?? 0,
                    PracticalExample: $"R$ 100 → R$ {investmentReturn:F2}"
                ),
                AdditionalInfo: $"Baseado em {result.DataPointsCount} meses de dados oficiais do Banco Central"
            );
        }

        // Resposta padrão para outros casos
        return new
        {
            indicator = result.Indicator,
            value = result.Value,
            unit = unit,
            date = result.Date,
            period = result.Description,
            dataPointsAnalyzed = result.DataPointsCount
        };
    }

    private string GetUnit(string indicator) => indicator.ToUpperInvariant() switch
    {
        "SELIC" => "% a.a.",
        "CDI" => "% a.a.",
        "IPCA" => "%",
        "DÓLAR" => "R$",
        _ => "unidade"
    };
}
