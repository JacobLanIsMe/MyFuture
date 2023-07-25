using Caches.Interfaces;

namespace MyFuture.BackgroundServices
{
    public class BackgroundStockTech : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public BackgroundStockTech(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    #region 取得所有的 StockInfo，並寫入 Cache
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<ICacheStockTech>();
                        await service.SetStockTechCache();
                    }
                    #endregion
                }
                catch (Exception ex)
                {

                }
                await Task.Delay(TimeSpan.FromMinutes(60));
            }
        }
    }
}
