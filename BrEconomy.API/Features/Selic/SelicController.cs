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
        var cached = await _cache.GetStringAsync("indicador:selic");
        if (!string.IsNullOrEmpty(cached))
        {
            return Ok(JsonSerializer.Deserialize<object>(cached));
        }

        // Fallback: banco (só se Redis estiver fora)
        var indicator = await _context.EconomicIndicators
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == "SELIC");

        return indicator is null
            ? NotFound("Selic indisponível.")
            : Ok(new { Valor = indicator.Value, Data = indicator.ReferenceDate });
    }
}