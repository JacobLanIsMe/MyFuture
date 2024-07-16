using Caches.Interfaces;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GetStockInfo.BackgroundServices
{
    public class BackgroundStockEps : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ICacheStockEps _cacheStockEps;
        public BackgroundStockEps(ILogger logger, ICacheStockEps cacheStockEps)
        {
            _logger = logger;
            _cacheStockEps = cacheStockEps;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                //取得所有的 StockInfo，並寫入 Cache
                await _cacheStockEps.SetStockEpsCache();
            }
            catch (Exception ex) 
            {
                _logger.Error(ex.ToString());
            }
        }
    }
}
