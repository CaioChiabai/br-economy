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

    private record BcbDto(string data, string valor);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. Busca dados no Banco Central
                var client = _httpClientFactory.CreateClient("BancoCentral");

                // Código 432 = Meta Selic
                var response = await client.GetAsync("dados/serie/bcdata.sgs.432/dados/ultimos/1?formato=json", stoppingToken);
                response.EnsureSuccessStatusCode(); // Lança exceção se não for 2xx

                var conteudo = await response.Content.ReadAsStringAsync(stoppingToken);
                var dadosBcb = JsonSerializer.Deserialize<List<BcbDto>>(conteudo)?.FirstOrDefault();

                if (dadosBcb == null)
                {
                    _logger.LogWarning("Resposta do BCB está vazia ou inválida");
                }
                else if (!double.TryParse(dadosBcb.valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double valorSelic))
                {
                    _logger.LogWarning($"Não foi possível converter o valor: {dadosBcb.valor}");
                }
                else
                {
                    await SalvarNoBanco(valorSelic, dadosBcb.data);

                    await SalvarNoCache(valorSelic, dadosBcb.data);

                    _logger.LogInformation($"Selic atualizada: {valorSelic}% (Data: {dadosBcb.data})");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de rede ao buscar dados do BCB. Tentando novamente em 24h");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout ao buscar dados do BCB (mais de 30s). Tentando novamente em 24h");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao deserializar resposta do BCB. Formato JSON inválido");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar Selic");
            }
            finally
            {
                _logger.LogInformation("Próxima atualização em 24 horas...");
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }

    private async Task SalvarNoBanco(double valor, string dataReferencia)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var indicador = await context.EconomicIndicators
            .FirstOrDefaultAsync(i => i.Name == "SELIC");

        var isNovoIndicador = indicador == null;
        if (isNovoIndicador)
        {
            _logger.LogInformation("Criando primeiro registro da SELIC no banco...");
            indicador = new EconomicIndicator { Name = "SELIC" };
            context.EconomicIndicators.Add(indicador);
        }

        indicador.Value = (decimal)valor;
        
        if (!DateTime.TryParse(dataReferencia, out var dataParsed))
        {
            var mensagem = $"Data inválida retornada pelo BCB: '{dataReferencia}'. Abortando atualização.";
            _logger.LogError(mensagem);
            throw new InvalidOperationException(mensagem);
        }
        
        indicador.ReferenceDate = DateTime.SpecifyKind(dataParsed, DateTimeKind.Utc);
        indicador.LastUpdated = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        _logger.LogInformation($"Selic salva no PostgreSQL (ID: {indicador.Id}, Ação: {(isNovoIndicador ? "Criado" : "Atualizado")})");
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

        // Configurações do cache: expira em 25 horas (1h a mais que o ciclo de atualização)
        var opcoes = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(25)
        };

        await _cache.SetStringAsync("indicador:selic", json, opcoes);
        _logger.LogInformation("Selic salva no Redis Cache (Expira em: 25h)");
    }
}