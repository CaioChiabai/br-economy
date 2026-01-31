using BrEconomy.API.Features.Shared;
using Microsoft.Extensions.Caching.Distributed;

namespace BrEconomy.API.Features.Selic.Job;

public class SelicCurrentJob : GenericIndicatorUpdateJob
{
    protected override string Name => "SELIC";
    protected override string Url => "dados/serie/bcdata.sgs.432/dados/ultimos/1?formato=json";
    protected override string CacheKey => "indicador:selic:current";

    public SelicCurrentJob(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IDistributedCache cache,
        ILogger<SelicCurrentJob> logger)
        : base(serviceProvider, httpClientFactory, cache, logger)
    {
    }
}