using System.Text.Json;
using BrEconomy.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace BrEconomy.API.Features.Selic;

[ApiController]
[Route("api/v1/indicators/selic")]
public class SelicController : ControllerBase
{
    private readonly IDistributedCache _cache;
    private readonly AppDbContext _context;

    public SelicController(IDistributedCache cache, AppDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        const string cacheKey = "indicador:selic";

        // 1. Tenta pegar do Redis (Rápido: < 5ms)
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            // Se achou no cache, retorna direto. Nem encosta no Banco.
            return Ok(JsonSerializer.Deserialize<object>(cachedData));
        }

        // 2. Se não tem no Redis, busca no Postgres (Segurança)
        var indicador = await _context.EconomicIndicators
            .AsNoTracking() // Otimização para leitura
            .FirstOrDefaultAsync(x => x.Name == "SELIC");

        if (indicador == null)
        {
            return NotFound("Dados da Selic ainda não foram carregados.");
        }

        // 3. Opcional: Se achou no banco mas não no cache, salva no cache agora (Self-Healing)
        // Isso garante que a próxima requisição seja rápida.
        var response = new
        {
            Valor = indicador.Value,
            Data = indicador.ReferenceDate,
            Fonte = "Banco de Dados (Cache Miss)"
        };

        // Salva no Redis para a próxima vez
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response));

        return Ok(response);
    }
}