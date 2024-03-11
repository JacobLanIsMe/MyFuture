using Models.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Caches.Interfaces;
using Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDbProvider;
using Microsoft.Extensions.Logging;
using Amazon.Runtime;

namespace Caches.Caches
{
    public class CacheStockTech : ICacheStockTech
    {
        // private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        private readonly IMongoDbService _mongoDbService;
        private readonly ILogger<CacheStockTech> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public CacheStockTech(/* IMemoryCache memoryCache,  */IStockRepository stockRepository, IMongoDbService mongoDbService, ILogger<CacheStockTech> logger, IHttpClientFactory httpClientFactory)
        {
            // _memoryCache = memoryCache;
            _stockRepository = stockRepository;
            _mongoDbService = mongoDbService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }
        public async Task SetStockTechCache()
        {
            List<string> stockIds = _stockRepository.GetStockIds(); // 取得所有的 stockId
            List<StockTechInfoModel> results = new List<StockTechInfoModel>();
            HttpClient client = _httpClientFactory.CreateClient();
            foreach (var stockId in stockIds.Take(4))
            {
                try
                {
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
                    stock.StockId = stockId;
                    if (!string.IsNullOrEmpty(name))
                    {
                        // _memoryCache.Set($"Tech{stockId}", stock);
                        results.Add(stock);
                        _logger.LogInformation($"Stock: {stockId} gets its stock tech completed");
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Getting the tech of Stock: {stockId} occurred error {ex.ToString()}");
                }
            }
            try
            {
                _logger.LogInformation("Writing stock tech into Mongodb started");
                MongoClient mongoClient = _mongoDbService.GetMongoClient();
                var collection = mongoClient.GetDatabase("MyFuture").GetCollection<StockTechInfoModel> ("Tech");
                var filter = Builders<StockTechInfoModel>.Filter.Empty;
                await collection.DeleteManyAsync(filter);
                await collection.InsertManyAsync(results);
                _logger.LogInformation("Writing stock tech into Mongodb completed");
            }
            catch(Exception ex)
            {
                _logger.LogError($"Writing stock tech into Mongodb error. {ex.ToString()}");
            }
        }
    }
}
