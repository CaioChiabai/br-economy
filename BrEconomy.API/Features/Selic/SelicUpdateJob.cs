using BrEconomy.API.Features.Shared;
using Microsoft.Extensions.Caching.Distributed;

namespace BrEconomy.API.Features.Selic;

public class SelicUpdateJob : GenericIndicatorUpdateJob
{
    protected override string Name => "SELIC";
    protected override string Url => "dados/serie/bcdata.sgs.432/dados/ultimos/1?formato=json";
    protected override string CacheKey => "indicador:selic";

    public SelicUpdateJob(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IDistributedCache cache,
        ILogger<SelicUpdateJob> logger)
        : base(serviceProvider, httpClientFactory, cache, logger)
    {
    }
}