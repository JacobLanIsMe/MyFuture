using Amazon.Runtime.Internal.Util;
using Caches.Caches;
using Caches.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GetStockInfo.BackgroundServices
{
    public class BackgroundStockRevenue : BackgroundService
    {
        private readonly ILogger<BackgroundStockRevenue> _logger;
        private readonly ICacheStockRevenue _cacheStockRevenue;
        public BackgroundStockRevenue(ILogger<BackgroundStockRevenue> logger, ICacheStockRevenue cacheStockRevenue)
        {
            _logger = logger;
            _cacheStockRevenue = cacheStockRevenue;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //取得所有的 StockInfo，並寫入 Cache
                    await _cacheStockRevenue.SetStockRevenueCache();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
                await Task.Delay(TimeSpan.FromMinutes(60));
            }
        }
    }
}
