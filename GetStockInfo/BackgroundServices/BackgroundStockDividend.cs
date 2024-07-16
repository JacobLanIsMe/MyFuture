using Caches.Interfaces;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GetStockInfo.BackgroundServices
{
    public class BackgroundStockDividend : BackgroundService
    {
        private readonly ICacheStockDividend _cacheStockDividend;
        private readonly ILogger _logger;
        public BackgroundStockDividend(ICacheStockDividend cacheStockDividend, ILogger logger)
        {
            _cacheStockDividend = cacheStockDividend;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                //取得所有的 StockInfo，並寫入
                await _cacheStockDividend.SetStockDividendCache();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
        }
    }
}
