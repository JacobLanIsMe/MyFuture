using Microsoft.Extensions.Caching.Memory;
using Models.Models;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Text.Json;

namespace MyFuture.BackgroundServices
{
    public class GetJumpEmptyStocks : BackgroundService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<GetJumpEmptyStocks> _logger;
        private readonly IStockRepository _stockRepository;
        private readonly IStockService _stockService;
        public GetJumpEmptyStocks(IMemoryCache memoryCache, ILogger<GetJumpEmptyStocks> logger, IStockRepository stockRepository, IStockService stockService)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _stockRepository = stockRepository;
            _stockService = stockService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string today = DateTime.Now.ToString("d");
            string cacheKey;
            try
            {
                List<string> stockIds = _stockRepository.GetStockIds();
                List<StockInfoModel> stocks = new List<StockInfoModel>();
                foreach (var i in stockIds)
                {
                    cacheKey = $"{today}_{i}";
                    try
                    {
                        StockInfoModel? stock = null;
                        if (!_memoryCache.TryGetValue<StockInfoModel>(cacheKey, out stock))
                        {
                            stock = await _stockService.SetStockInfoCache(i);
                        }
                        if (stock == null)
                        {
                            throw new Exception($"Cannot find stockId, {i}");
                        }
                        stocks.Add(stock);
                        _logger.LogInformation($"Extract {cacheKey} success");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"Extract {cacheKey} fail, {ex.Message}");
                    }
                }   

            }
            catch (Exception ex)
            {

            }
            
           
            //await Task.Delay(1000 * 60);
        }
        
        
    }
}
