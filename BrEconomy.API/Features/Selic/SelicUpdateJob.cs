using BrEconomy.API.Data;
using BrEconomy.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BrEconomy.API.Features.Selic;

public class SelicUpdateJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SelicUpdateJob> _logger;

    public SelicUpdateJob(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IDistributedCache cache,
        ILogger<SelicUpdateJob> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔄 Iniciando atualização da Selic...");

                // 1. Busca dados no Banco Central
                var client = _httpClientFactory.CreateClient("BancoCentral");

                // Código 432 = Meta Selic
                var response = await client.GetAsync("dados/serie/bcdata.sgs.432/dados/ultimos/1?formato=json", stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    var conteudo = await response.Content.ReadAsStringAsync(stoppingToken);
                    var dadosBcb = JsonSerializer.Deserialize<List<BcbDto>>(conteudo)?.FirstOrDefault();

                    if (dadosBcb != null && double.TryParse(dadosBcb.valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double valorSelic))
                    {
                        // 2. Salva no Banco de Dados (Postgres)
                        await SalvarNoBanco(valorSelic, dadosBcb.data);

                        // 3. Atualiza o Cache (Redis)
                        await SalvarNoCache(valorSelic, dadosBcb.data);

                        _logger.LogInformation($"✅ Selic atualizada: {valorSelic}%");
                    }
                }
                else
                {
                    _logger.LogWarning($"⚠️ Falha ao buscar Selic. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro crítico ao atualizar Selic");
            }

            // Dorme por 24 horas antes de tentar de novo
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task SalvarNoBanco(double valor, string dataReferencia)
    {
        // Precisamos criar um escopo manual porque o DbContext é 'Scoped' e o Job é 'Singleton'
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var indicador = await context.EconomicIndicators
            .FirstOrDefaultAsync(i => i.Name == "SELIC");

        if (indicador == null)
        {
            indicador = new EconomicIndicator { Name = "SELIC" };
            context.EconomicIndicators.Add(indicador);
        }

        indicador.Value = (decimal)valor;
        indicador.ReferenceDate = DateTime.SpecifyKind(DateTime.Parse(dataReferencia), DateTimeKind.Utc); indicador.LastUpdated = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private async Task SalvarNoCache(double valor, string dataReferencia)
    {
        var cacheData = new
        {
            Valor = valor,
            Data = dataReferencia,
            UltimaVerificacao = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(cacheData);

        // Salva no Redis (chave "indicador:selic")
        await _cache.SetStringAsync("indicador:selic", json);
    }

    // Classe auxiliar interna para ler o JSON do governo
    private record BcbDto(string data, string valor);
}