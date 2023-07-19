using Models.Models;
using Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Repositories.Interfaces;

namespace Services.Services
{
    public class StockService : IStockService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        public StockService(IMemoryCache memoryCache, IStockRepository stockRepository)
        {
            _memoryCache = memoryCache;
            _stockRepository = stockRepository;
        }
        public List<StockInfoModel> GetJumpEmptyStocks()
        {
            List<string> stockIds = _stockRepository.GetStockIds();
            List<StockInfoModel> selectedStocks = GetJumpEmptyStocksFromCache(stockIds);
            return selectedStocks;
        }

        private List<StockInfoModel> GetJumpEmptyStocksFromCache(List<string> stockIds)
        {
            List<StockInfoModel> result = new List<StockInfoModel>();
            foreach (var i in stockIds)
            {
                if (_memoryCache.TryGetValue<StockInfoModel>(i, out StockInfoModel? stock) && stock != null && stock.StockDetails != null && stock.StockDetails.Count > 5)
                {
                    List<StockDetailModel> stockDetails = stock.StockDetails.OrderByDescending(x => x.t).ToList();
                    for (int j = 1; j < 10; j++)
                    {
                        if (stockDetails[j].l >= stockDetails[j + 1].h)
                        {
                            var periodStocks = stockDetails.Take(j).ToList();
                            var topClose = periodStocks.Select(x => x.c).Max();
                            var lowClose = periodStocks.Select(x => x.l).Min();
                            if (topClose <= stockDetails[j].h && lowClose >= stockDetails[j+1].h)
                            {
                                StockInfoModel model = new StockInfoModel
                                {
                                    Id = stock.Id,
                                    Name = stock.Name,
                                    StockDetails = stockDetails.Take(1).ToList()
                                };
                                result.Add(model);
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public async Task SetStockInfoCache(string stockId)
        {
            HttpClient client = new HttpClient();
            string url = $"https://tw.quote.finance.yahoo.net/quote/q?type=ta&perd=d&mkt=10&sym={stockId}&v=1&callback=jQuery111306311117094962886_1574862886629&_=1574862886630";
            var responseMsg = await client.GetAsync(url);
            string? detail = null;
            string? name = null;
            StockInfoModel stock = new StockInfoModel();
            List<StockDetailModel> stockDetails = new List<StockDetailModel>();

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
            if (!string.IsNullOrEmpty(name))
            {
                _memoryCache.Set(stockId, stock);
            }
        }
    }
}
