using BrEconomy.API.Data;
using BrEconomy.API.Domain.Entities;
using BrEconomy.API.Features.Selic.Job;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrEconomy.API.Features.Shared
{
    public abstract class GenericIndicatorUpdateJob : BackgroundService
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly IDistributedCache _cache;
        protected readonly ILogger _logger;

        protected abstract string Name { get; }
        protected abstract string Url { get; }
        protected abstract string CacheKey { get; }
        protected virtual TimeSpan UpdateInterval => TimeSpan.FromHours(24);
        protected virtual TimeSpan InitialDelay => TimeSpan.FromSeconds(2);

        protected GenericIndicatorUpdateJob(
            IServiceProvider serviceProvider,
            IHttpClientFactory httpClientFactory,
            IDistributedCache cache,
            ILogger<GenericIndicatorUpdateJob> logger)
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        private record BcbDto(
            [property: JsonPropertyName("data")] string date,
            [property: JsonPropertyName("valor")] string value
        );

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try 
                { 
                    var client = _httpClientFactory.CreateClient("BancoCentral");
                    var response = await client.GetAsync(Url, stoppingToken);
                    response.EnsureSuccessStatusCode();

                    var content =  await response.Content.ReadAsStringAsync(stoppingToken);
                    var dataBcb = JsonSerializer.Deserialize<List<BcbDto>>(content)?.FirstOrDefault();

                    if (dataBcb is null)
                    {
                        _logger.LogWarning("Resposta do BCB está vazia ou inválida");
                    }
                    else if (!double.TryParse(
                        dataBcb.value,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double value))
                    {
                        _logger.LogWarning($"Não foi possível converter o valor: {dataBcb.value}");
                    }
                    else
                    {
                        await SalvarNoBanco(value, dataBcb.date);
                        await SalvarNoCache(value, dataBcb.date);

                        _logger.LogInformation($"{Name} atualizado com sucesso: {value} na data {dataBcb.date}", Name);
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
                    _logger.LogError(ex, "Erro inesperado ao atualizar");
                }
                finally
                {
                    _logger.LogInformation("Próxima atualização em 24 horas...");
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
            }
        }

        private async Task SalvarNoBanco(double value, string referenceDate)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var indicator = await context.EconomicIndicators
                .FirstOrDefaultAsync(e => e.Name == Name);

            if (indicator is null)
            {
                _logger.LogInformation("Criando novo indicador econômico: {Name}", Name);
                indicator = new EconomicIndicator {  Name = Name };
                context.EconomicIndicators.Add(indicator);
            }

            indicator.Value = (decimal)value;

            if (!DateTime.TryParseExact(
                referenceDate,
                "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dataParsed))
            {
                var mensagem = $"Data inválida retornada pelo BCB: '{referenceDate}'. Abortando atualização.";
                _logger.LogError(mensagem);
                throw new InvalidOperationException(mensagem);
            }

            indicator.ReferenceDate = DateTime.SpecifyKind(dataParsed, DateTimeKind.Utc);
            indicator.LastUpdated = DateTime.UtcNow;

            await context.SaveChangesAsync();
        } 

        private async Task SalvarNoCache(double value, string referenceDate)
        {
            var cacheData = new
            {
                Value = value,
                Date = referenceDate,
                LastVerification = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(cacheData);

            var opcoes = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(25)
            };

            await _cache.SetStringAsync(CacheKey, json, opcoes);
            _logger.LogInformation($"{Name} salva no cache Redis Cache.", Name);
        }
    }
}
