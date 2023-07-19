using Models.Models;
using Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Services.Services
{
    public class StockService : IStockService
    {
        private readonly IMemoryCache _memoryCache;
        public StockService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        public async Task GetJumpEmptyStocks()
        {

        }
        public async Task<StockInfoModel> SetStockInfoCache(string stockId)
        {
            HttpClient client = new HttpClient();
            string url = $"https://tw.quote.finance.yahoo.net/quote/q?type=ta&perd=d&mkt=10&sym={stockId}&v=1&callback=jQuery111306311117094962886_1574862886629&_=1574862886630";
            var responseMsg = await client.GetAsync(url);
            string? detail = null;
            string? name = null;
            StockInfoModel stock = new StockInfoModel();
            List<StockDetailModel> stockDetails = new List<StockDetailModel>();
            string cacheKey = $"{DateTime.Now.Date}_{stockId}";
            if (responseMsg.IsSuccessStatusCode)
            {
                var data = await responseMsg.Content.ReadAsStringAsync();
                detail = data.Split("\"ta\":")[1].Split(",\"ex\"")[0];
                name = data.Split("\"name\":\"")[1].Split('\"')[0];
            }
            stockDetails = JsonSerializer.Deserialize<List<StockDetailModel>>(detail);
            stock.StockDetails = stockDetails;
            stock.Name = name;
            stock.Id = stockId;
            _memoryCache.Set(cacheKey, stock, TimeSpan.FromMinutes(1));
            return stock;
        }
    }
}
