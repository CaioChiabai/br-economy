using BrEconomy.API.Features.Selic.Job;
using BrEconomy.API.Features.Shared;
using Microsoft.Extensions.Caching.Distributed;

namespace BrEconomy.API.Features.IPCA.Job
{
    public class Ipca12MJob : GenericIndicatorUpdateJob
    {
        protected override string Name => "IPCA_12M";
        protected override string Url => "dados/serie/bcdata.sgs.13522/dados/ultimos/1?formato=json";
        protected override string CacheKey => "indicador:ipca:12m";

        public Ipca12MJob(
            IServiceProvider serviceProvider,
            IHttpClientFactory httpClientFactory,
            IDistributedCache cache,
            ILogger<GenericIndicatorUpdateJob> logger)
            : base(serviceProvider, httpClientFactory, cache, logger)
        {
        }
    }
}
