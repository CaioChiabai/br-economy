using BrEconomy.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BrEconomy.API.Features.Dollar
{
    [ApiController]
    [Route("api/v1/indicators/dolar")]
    public class DolarController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly AppDbContext _context;

        public DolarController(IDistributedCache cache, AppDbContext context)
        {
            _cache = cache;
            _context = context;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
            var cached = await _cache.GetStringAsync("indicador:dolar:current");
            if (!string.IsNullOrEmpty(cached))
            {
                return Ok(JsonSerializer.Deserialize<object>(cached));
            }

            // Fallback: banco (só se Redis estiver fora)
            var indicator = await _context.EconomicIndicators
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "DOLAR");

            return indicator is null
                ? NotFound("Dolar indisponível.")
                : Ok(new { Valor = indicator.Value, Data = indicator.ReferenceDate });
        }
    }
}
