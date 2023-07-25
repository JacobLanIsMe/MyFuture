using Caches.Interfaces;
using Repositories.Interfaces;

namespace MyFuture.BackgroundServices
{
    public class GetStockTech : BackgroundService
    {
        //private readonly ILogger<GetStockTech> _logger;
        private readonly IServiceProvider _serviceProvider;
        public GetStockTech(/*ILogger<GetAllStockInfo> logger, */IServiceProvider serviceProvider)
        {
            //_logger = logger;
            _serviceProvider = serviceProvider;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //DateTime start = DateTime.Now;
                //_logger.LogInformation($"Start {start.ToString("d")}");
                try
                {
                    List<string> stockIds = new List<string>();
                    #region 取得所有的 StockId
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var stockRepository = scope.ServiceProvider.GetRequiredService<IStockRepository>();
                        stockIds = stockRepository.GetStockIds();
                    }
                    #endregion
                    #region 取得所有的 StockInfo，並寫入 Cache
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var cacheStockTech = scope.ServiceProvider.GetRequiredService<ICacheStockTech>();
                        foreach (var i in stockIds)
                        {
                            try
                            {
                                await cacheStockTech.SetStockTechCache(i);
                                //_logger.LogInformation($"Extract {i} success");
                            }
                            catch (Exception ex)
                            {
                                //_logger.LogInformation($"Extract {i} fail, {ex.Message}");
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {

                }
                //DateTime end = DateTime.Now;
                //var period = (end - start).ToString("c");
                //_logger.LogInformation($"End {end}");
                //_logger.LogInformation($"Period {period}");
                await Task.Delay(TimeSpan.FromMinutes(60));
            }
        }
    }
}
