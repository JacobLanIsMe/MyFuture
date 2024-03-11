using Caches.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GetStockInfo.BackgroundServices
{
    public class BackgroundStockDividend : BackgroundService
    {
        private readonly ICacheStockDividend _cacheStockDividend;
        private readonly ILogger<BackgroundStockDividend> _logger;
        public BackgroundStockDividend(ICacheStockDividend cacheStockDividend, ILogger<BackgroundStockDividend> logger)
        {
            _cacheStockDividend = cacheStockDividend;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //取得所有的 StockInfo，並寫入
                    //await _cacheStockDividend.SetStockDividendCache();
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
