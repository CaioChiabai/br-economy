using BrEconomy.API.Features.Selic.Job;
using BrEconomy.API.Features.Shared;
using Microsoft.Extensions.Caching.Distributed;

namespace BrEconomy.API.Features.CDI.Job
{
    public class Cdi12MJob : GenericIndicatorUpdateJob
    {
        protected override string Name => "CDI_12M";
        protected override string Url => "dados/serie/bcdata.sgs.4392/dados/ultimos/1?formato=json";
        protected override string CacheKey => "indicador:cdi:12m";

        public Cdi12MJob(
            IServiceProvider serviceProvider,
            IHttpClientFactory httpClientFactory,
            IDistributedCache cache,
            ILogger logger)
            : base(serviceProvider, httpClientFactory, cache, logger)
        {
        }
    }
}
