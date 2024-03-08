using Caches.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetStockInfo.BackgroundServices
{
    public class BackgroundStockTech : BackgroundService
    {
        private readonly ILogger<BackgroundStockTech> _logger;  
        private readonly ICacheStockTech _cacheStockTech;
        public BackgroundStockTech(ILogger<BackgroundStockTech> logger, ICacheStockTech cacheStockTech)
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
                    _logger.LogError(ex.ToString());
                }
                await Task.Delay(TimeSpan.FromMinutes(60));
            }
        }
    }
}
