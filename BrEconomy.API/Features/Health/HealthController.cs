using BrEconomy.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;

namespace BrEconomy.API.Features.Health;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        AppDbContext context,
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<HealthController> logger)
    {
        _context = context;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private record HealthCheckResult(string Status, string ResponseTime, string Message, object? Details);
    private record HealthResponse(string Status, DateTime Timestamp, string ResponseTime, ServicesHealth Services);
    private record ServicesHealth(HealthCheckResult Database, HealthCheckResult Cache, HealthCheckResult BancoCentralApi);


    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var databaseHealth = await CheckDatabaseHealth();
        var cacheHealth = await CheckCacheHealth();
        var bcbHealth = await CheckBancoCentralHealth();

        stopwatch.Stop();

        var todosServicosOk = 
            databaseHealth.Status == "Healthy" &&
            cacheHealth.Status == "Healthy" &&
            bcbHealth.Status == "Healthy";

        var resultado = new HealthResponse(
            Status: todosServicosOk ? "Healthy" : "Unhealthy",
            Timestamp: DateTime.UtcNow,
            ResponseTime: $"{stopwatch.ElapsedMilliseconds}ms",
            Services: new ServicesHealth(databaseHealth, cacheHealth, bcbHealth)
        );

        if (todosServicosOk)
        {
            _logger.LogInformation("Health check executado com sucesso. Todos os serviços estão operacionais.");
            return Ok(resultado);
        }
        else
        {
            _logger.LogWarning("Health check detectou problemas em um ou mais serviços.");
            return StatusCode(503, resultado); // 503 Service Unavailable
        }
    }

    private async Task<HealthCheckResult> CheckDatabaseHealth()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                var count = await _context.EconomicIndicators.CountAsync();
                
                stopwatch.Stop();
                
                return new HealthCheckResult(
                    Status: "Healthy",
                    ResponseTime: $"{stopwatch.ElapsedMilliseconds}ms",
                    Message: "PostgreSQL conectado e operacional",
                    Details: new
                    {
                        IndicatorsCount = count,
                        Database = _context.Database.GetDbConnection().Database
                    }
                );
            }
            else
            {
                stopwatch.Stop();
                return new HealthCheckResult(
                    Status: "Unhealthy",
                    ResponseTime: $"{stopwatch.ElapsedMilliseconds}ms",
                    Message: "Não foi possível conectar ao PostgreSQL",
                    Details: null
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar saúde do PostgreSQL");
            return new HealthCheckResult(
                Status: "Unhealthy",
                ResponseTime: "N/A",
                Message: $"Erro ao conectar ao PostgreSQL: {ex.Message}",
                Details: null
            );
        }
    }

    private async Task<HealthCheckResult> CheckCacheHealth()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var testKey = "health-check-test";
            var testValue = DateTime.UtcNow.ToString();

            await _cache.SetStringAsync(testKey, testValue, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            });

            var retrievedValue = await _cache.GetStringAsync(testKey);

            stopwatch.Stop();

            if (retrievedValue == testValue)
            {
                // Verifica se tem a chave da Selic
                var selicCache = await _cache.GetStringAsync("indicador:selic:current");
                
                return new HealthCheckResult(
                    Status: "Healthy",
                    ResponseTime: $"{stopwatch.ElapsedMilliseconds}ms",
                    Message: "Redis conectado e operacional",
                    Details: new
                    {
                        CanReadWrite = true,
                        SelicCacheExists = selicCache != null
                    }
                );
            }
            else
            {
                return new HealthCheckResult(
                    Status: "Unhealthy",
                    ResponseTime: $"{stopwatch.ElapsedMilliseconds}ms",
                    Message: "Redis não retornou o valor esperado",
                    Details: null
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar saúde do Redis");
            return new HealthCheckResult(
                Status: "Unhealthy",
                ResponseTime: "N/A",
                Message: $"Erro ao conectar ao Redis: {ex.Message}",
                Details: null
            );
        }
    }

    private async Task<HealthCheckResult> CheckBancoCentralHealth()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var client = _httpClientFactory.CreateClient("BancoCentral");

            // Faz uma requisição simples para ver se a API está respondendo
            var response = await client.GetAsync("dados/serie/bcdata.sgs.432/dados/ultimos/1?formato=json");

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new HealthCheckResult(
                    Status: "Healthy",
                    ResponseTime: $"{stopwatch.ElapsedMilliseconds}ms",
                    Message: "API do Banco Central respondendo normalmente",
                    Details: new
                    {
                        StatusCode = (int)response.StatusCode,
                        BaseUrl = client.BaseAddress?.ToString()
                    }
                );
            }
            else
            {
                return new HealthCheckResult(
                    Status: "Unhealthy",
                    ResponseTime: $"{stopwatch.ElapsedMilliseconds}ms",
                    Message: $"API do Banco Central retornou status {(int)response.StatusCode}",
                    Details: new
                    {
                        StatusCode = (int)response.StatusCode
                    }
                );
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao verificar saúde da API do Banco Central");
            return new HealthCheckResult(
                Status: "Unhealthy",
                ResponseTime: "N/A",
                Message: $"Erro de rede ao conectar com API do BCB: {ex.Message}",
                Details: null
            );
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout ao verificar saúde da API do Banco Central");
            return new HealthCheckResult(
                Status: "Unhealthy",
                ResponseTime: "N/A",
                Message: "Timeout ao conectar com API do BCB (mais de 30s)",
                Details: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao verificar saúde da API do Banco Central");
            return new HealthCheckResult(
                Status: "Unhealthy",
                ResponseTime: "N/A",
                Message: $"Erro inesperado: {ex.Message}",
                Details: null
            );
        }
    }
}