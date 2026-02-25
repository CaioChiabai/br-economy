using BrEconomy.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BrEconomy.API.Features.IPCA
{
    [ApiController]
    [Route("api/v1/indicators/ipca")]
    public class IpcaController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly AppDbContext _context;

        public IpcaController(IDistributedCache cache, AppDbContext context)
        {
            _cache = cache;
            _context = context;
        }

        [HttpGet("ytd")]
        public async Task<IActionResult> GetYtd()
        {
            var cached = await _cache.GetStringAsync("indicador:ipca:ytd");
            if (!string.IsNullOrEmpty(cached))
            {
                Response.Headers["Data-Source"] = "cache";
                return Ok(JsonSerializer.Deserialize<object>(cached));
            }

            // Fallback: banco (só se Redis estiver fora)
            var indicator = await _context.EconomicIndicators
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "IPCA_YTD");

            Response.Headers["Data-Source"] = "database";

            return indicator is null
                ? NotFound("IPCA indisponível.")
                : Ok(new { Valor = indicator.Value, Data = indicator.ReferenceDate });
        }

        [HttpGet("12m")]
        public async Task<IActionResult> Get12M()
        {
            var cached = await _cache.GetStringAsync("indicador:ipca:12m");
            if (!string.IsNullOrEmpty(cached))
            {
                Response.Headers["Data-Source"] = "cache";
                return Ok(JsonSerializer.Deserialize<object>(cached));
            }

            // Fallback: banco (só se Redis estiver fora)
            var indicator = await _context.EconomicIndicators
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "IPCA_12M");

            Response.Headers["Data-Source"] = "database";

            return indicator is null
                ? NotFound("IPCA indisponível.")
                : Ok(new { Valor = indicator.Value, Data = indicator.ReferenceDate });
        }
    }
}
