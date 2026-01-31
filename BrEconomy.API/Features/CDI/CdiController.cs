using BrEconomy.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BrEconomy.API.Features.CDI
{
    [ApiController]
    [Route("api/v1/indicators/cdi")]
    public class CdiController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly AppDbContext _context;

        public CdiController(IDistributedCache cache, AppDbContext context)
        {
            _cache = cache;
            _context = context;
        }

        [HttpGet("ytd")]
        public async Task<IActionResult> GetYtd()
        {
            var cached = await _cache.GetStringAsync("indicador:cdi:ytd");
            if (!string.IsNullOrEmpty(cached))
            {
                return Ok(JsonSerializer.Deserialize<object>(cached));
            }

            // Fallback: banco (só se Redis estiver fora)
            var indicator = await _context.EconomicIndicators
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "CDI_YTD");

            return indicator is null
                ? NotFound("CDI indisponível.")
                : Ok(new { Valor = indicator.Value, Data = indicator.ReferenceDate });
        }

        [HttpGet("12m")]
        public async Task<IActionResult> Get12M()
        {
            var cached = await _cache.GetStringAsync("indicador:cdi:12m");
            if (!string.IsNullOrEmpty(cached))
            {
                return Ok(JsonSerializer.Deserialize<object>(cached));
            }

            // Fallback: banco (só se Redis estiver fora)
            var indicator = await _context.EconomicIndicators
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "CDI_12M");

            return indicator is null
                ? NotFound("CDI indisponível.")
                : Ok(new { Valor = indicator.Value, Data = indicator.ReferenceDate });
        }
    }
}
