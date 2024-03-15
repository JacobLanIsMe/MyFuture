using Caches.Interfaces;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GetStockInfo.BackgroundServices
{
    public class BackgroundStockTech : BackgroundService
    {
        private readonly ILogger _logger;  
        private readonly ICacheStockTech _cacheStockTech;
        public BackgroundStockTech(ILogger logger, ICacheStockTech cacheStockTech)
        {
            _logger = logger;
            _cacheStockTech = cacheStockTech;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //取得所有的 StockInfo，並寫入
                    await _cacheStockTech.SetStockTechCache();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.ToString());
                }
                await Task.Delay(TimeSpan.FromHours(2));
            }
        }
    }
}
