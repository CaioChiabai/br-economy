using BrEconomy.API.Features.Selic.Job;
using BrEconomy.API.Features.Shared;
using Microsoft.Extensions.Caching.Distributed;

namespace BrEconomy.API.Features.IPCA.Job
{
    public class IpcaYtdJob : GenericIndicatorUpdateJob
    {
        protected override string Name => "IPCA_YTD";
        protected override string Url => "dados/serie/bcdata.sgs.433/dados/ultimos/1?formato=json";
        protected override string CacheKey => "indicador:ipca:ytd";

        public IpcaYtdJob(
            IServiceProvider serviceProvider,
            IHttpClientFactory httpClientFactory,
            IDistributedCache cache,
            ILogger logger)
            : base(serviceProvider, httpClientFactory, cache, logger)
        {
        }
    }
}
