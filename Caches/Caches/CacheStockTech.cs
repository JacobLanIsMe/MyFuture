using Models.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Caches.Interfaces;
using Repositories.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Caches.Caches
{
    public class CacheStockTech : ICacheStockTech
    {
        private readonly IDistributedCache _cache;
        private readonly IStockRepository _stockRepository;
        public CacheStockTech(IDistributedCache cache, IStockRepository stockRepository)
        {
            _cache = cache;
            _stockRepository = stockRepository;
        }
        public async Task SetStockTechCache()
        {
            List<string> stockIds = _stockRepository.GetStockIds(); // 取得所有的 stockId
            #region 取得所有的 StockInfo，並寫入 Cache
            foreach (var stockId in stockIds)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    string url = $"https://tw.quote.finance.yahoo.net/quote/q?type=ta&perd=d&mkt=10&sym={stockId}&v=1&callback=jQuery111306311117094962886_1574862886629&_=1574862886630";
                    var responseMsg = await client.GetAsync(url);
                    string? detail = null;
                    string? name = null;
                    StockTechInfoModel stock = new StockTechInfoModel();
                    List<StockTechDetailModel> stockDetails = new List<StockTechDetailModel>();

                    if (responseMsg.IsSuccessStatusCode)
                    {
                        var data = await responseMsg.Content.ReadAsStringAsync();
                        detail = data.Split("\"ta\":")[1].Split(",\"ex\"")[0];
                        name = data.Split("\"name\":\"")[1].Split('\"')[0];
                    }
                    stockDetails = JsonSerializer.Deserialize<List<StockTechDetailModel>>(detail);
                    stock.StockDetails = stockDetails;
                    stock.Name = name;
                    stock.Id = stockId;
                    if (!string.IsNullOrEmpty(name))
                    {
                        string stockString = JsonSerializer.Serialize(stock);
                        _cache.SetString($"Tech{stockId}", stockString);
                    }
                }
                catch (Exception ex)
                {

                }
            }
            #endregion
        }
    }
}
