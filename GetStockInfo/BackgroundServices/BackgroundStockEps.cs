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
    public class BackgroundStockEps : BackgroundService
    {
        private readonly ILogger<BackgroundStockEps> _logger;
        private readonly ICacheStockEps _cacheStockEps;
        public BackgroundStockEps(ILogger<BackgroundStockEps> logger, ICacheStockEps cacheStockEps)
        {
            _logger = logger;
            _cacheStockEps = cacheStockEps;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //取得所有的 StockInfo，並寫入 Cache
                    await _cacheStockEps.SetStockEpsCache();
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
