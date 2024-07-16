using Caches.Interfaces;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GetStockInfo.BackgroundServices
{
    public class BackgroundStockRevenue : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ICacheStockRevenue _cacheStockRevenue;
        public BackgroundStockRevenue(ILogger logger, ICacheStockRevenue cacheStockRevenue)
        {
            _logger = logger;
            _cacheStockRevenue = cacheStockRevenue;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                //取得所有的 StockInfo，並寫入 Cache
                await _cacheStockRevenue.SetStockRevenueCache();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
        }
    }
}
