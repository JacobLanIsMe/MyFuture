using Microsoft.Extensions.Caching.Memory;
using Repositories.Interfaces;
using Services.Interfaces;

namespace MyFuture.BackgroundServices
{
    public class GetAllStockInfo : BackgroundService
    {
        private readonly ILogger<GetAllStockInfo> _logger;
        private readonly IServiceProvider _serviceProvider;
        public GetAllStockInfo(/*ILogger<GetAllStockInfo> logger, */IServiceProvider serviceProvider)
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
                        var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
                        foreach (var i in stockIds)
                        {
                            try
                            {
                                await stockService.SetStockInfoCache(i);
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
