using BrEconomy.API.Features.Selic.Job;
using BrEconomy.API.Features.Shared;
using Microsoft.Extensions.Caching.Distributed;

namespace BrEconomy.API.Features.CDI.Job
{
    public class CdiYtdJob : GenericIndicatorUpdateJob
    {
        protected override string Name => "CDI_YTD";
        protected override string Url => "dados/serie/bcdata.sgs.4391/dados/ultimos/1?formato=json";
        protected override string CacheKey => "indicador:cdi:ytd";

        public CdiYtdJob(
            IServiceProvider serviceProvider,
            IHttpClientFactory httpClientFactory,
            IDistributedCache cache,
            ILogger logger)
            : base(serviceProvider, httpClientFactory, cache, logger)
        {
        }
    }
}
