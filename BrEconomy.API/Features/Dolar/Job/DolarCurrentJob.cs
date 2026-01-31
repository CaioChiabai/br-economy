using BrEconomy.API.Features.Selic.Job;
using BrEconomy.API.Features.Shared;
using Microsoft.Extensions.Caching.Distributed;

namespace BrEconomy.API.Features.Dolar.Job
{
    public class DolarCurrentJob : GenericIndicatorUpdateJob
    {
        protected override string Name => "DOLAR";
        protected override string Url => "dados/serie/bcdata.sgs.1/dados/ultimos/1?formato=json";
        protected override string CacheKey => "indicador:dolar:current";

        public DolarCurrentJob(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IDistributedCache cache,
        ILogger logger)
        : base(serviceProvider, httpClientFactory, cache, logger)
        {
        }
    }
}

