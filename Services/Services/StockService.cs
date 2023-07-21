using Models.Models;
using Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Repositories.Interfaces;
using System.ComponentModel.Design;

namespace Services.Services
{
    public class StockService : IStockService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        //private readonly ILogger<StockService> _logger;
        public StockService(IMemoryCache memoryCache, IStockRepository stockRepository)
        {
            _memoryCache = memoryCache;
            _stockRepository = stockRepository;
        }
        public List<StockInfoModel> GetJumpEmptyStocks()
        {
            List<string> stockIds = _stockRepository.GetStockIds();
            List<StockInfoModel> selectedStocks = GetStockBySpecificStrategy(stockIds, JumpEmptyStrategy);
            return selectedStocks;
        }
        public List<StockInfoModel> GetBullishPullbackStocks()
        {
            List<string> stockIds = _stockRepository.GetStockIds();
            List<StockInfoModel> selectedStocks = GetStockBySpecificStrategy(stockIds, BullishPullbackStrategy);
            return selectedStocks;
        }
        private List<StockInfoModel> GetStockBySpecificStrategy(List<string> stockIds, GetStocksBySpecificStrategy strategy)
        {
            List<StockInfoModel> result = new List<StockInfoModel>();
            foreach (var i in stockIds)
            {
                if (_memoryCache.TryGetValue<StockInfoModel>(i, out StockInfoModel? stock) && stock != null && stock.StockDetails != null)
                {
                    var q = strategy(stock);
                    if (q != null)
                    {
                        result.Add(q);
                    }
                }
            }
            return result;
        }



        private delegate StockInfoModel? GetStocksBySpecificStrategy(StockInfoModel stock);
        #region JumpEmpty
        private StockInfoModel? JumpEmptyStrategy(StockInfoModel stock)
        {
            if (stock.StockDetails.Count <= 5)
            {
                return null;
            }
            List<StockDetailModel> stockDetails = stock.StockDetails.OrderByDescending(x => x.t).ToList();
            var averageVolumn = stockDetails.Take(5).Select(x => x.v).Average();
            if (averageVolumn >= 500)
            {
                for (int j = 1; j < 10; j++)
                {
                    if (stockDetails[j].l >= stockDetails[j + 1].h)
                    {
                        var periodStocks = stockDetails.Take(j).ToList();
                        var topClose = periodStocks.Select(x => x.c).Max();
                        var lowClose = periodStocks.Select(x => x.l).Min();
                        if (topClose <= stockDetails[j].h && lowClose >= stockDetails[j + 1].h)
                        {
                            StockInfoModel model = new StockInfoModel
                            {
                                Id = stock.Id,
                                Name = stock.Name,
                                StockDetails = stockDetails.Take(1).ToList()
                            };
                            return model;
                        }
                    }
                }
            }
            return null;
        }
        #endregion
        #region BullishPullback
        private StockInfoModel? BullishPullbackStrategy(StockInfoModel stock)
        {
            try
            {
                if (stock.StockDetails.Count < 60)
                {
                    return null;
                }
                List<StockDetailModel> stockDetails = stock.StockDetails.OrderByDescending(x => x.t).ToList();
                double averageVolumn = stockDetails.Take(5).Select(x => x.v).Average();
                var last40Days = stockDetails.Take(40).ToList();
                if (averageVolumn >= 500)
                {
                    double topClose = last40Days.Select(x => x.c).Max(); // 找到近40天的最高收盤價
                    int topDayIndex = last40Days.FindIndex(x => x.c == topClose); // 找到近40天的最高收盤價的位置
                    double lastTopClose = stockDetails.Skip(topDayIndex + 1).Take(20).Select(x => x.c).Max(); // 找到前一個峰值的收盤價
                    int lastTopDayIndex = stockDetails.Skip(topDayIndex + 1).Take(20).ToList().FindIndex(x => x.c == lastTopClose); // 找出第二個峰值的位置
                    double bottomClose = stockDetails.Take(topDayIndex).Select(x => x.c).Min(); // 找到最近的底部收盤價
                    double lastBottomClose = stockDetails.Skip(topDayIndex + 1).Take(lastTopDayIndex).Select(x => x.c).Min(); // 找出兩個峰值間的最低值
                    if (topClose > lastTopClose && bottomClose >= lastBottomClose)
                    {
                        StockInfoModel model = new StockInfoModel
                        {
                            Id = stock.Id,
                            Name = stock.Name,
                            StockDetails = stockDetails.Take(1).ToList()
                        };
                        return model;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                //throw new Exception($"StockId: {stock.Id} error");
                return null;
            }
            
        }
        #endregion


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
