using Models.Models;
using System.Text.Json;
using Caches.Interfaces;
using Repositories.Interfaces;
using MongoDbProvider;
using Serilog;

namespace Caches.Caches
{
    public class CacheStockTech : ICacheStockTech
    {
        // private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        private readonly IMongoDbService _mongoDbService;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public CacheStockTech(/* IMemoryCache memoryCache,  */IStockRepository stockRepository, IMongoDbService mongoDbService, ILogger logger, IHttpClientFactory httpClientFactory)
        {
            // _memoryCache = memoryCache;
            _stockRepository = stockRepository;
            _mongoDbService = mongoDbService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }
        public async Task SetStockTechCache()
        {
            //List<string> stockIds = _stockRepository.GetStockIds(); // 取得所有的 stockId
            List<string> stockIds = new List<string> { "2330" };
            HttpClient client = _httpClientFactory.CreateClient();
            List<StockTechInfoModel> results = new List<StockTechInfoModel>();
            foreach (var stockId in stockIds)
            {
                try
                {
                    string url = $"https://tw.quote.finance.yahoo.net/quote/q?type=ta&perd=d&mkt=10&sym={stockId}&v=1&callback=jQuery111306311117094962886_1574862886629&_=1574862886630";
                    var responseMsg = await client.GetAsync(url);
                    string? detail = null;
                    string? name = null;
                    StockTechInfoModel stock = new StockTechInfoModel();
                    List<StockTechDetailModel> stockDetails = new List<StockTechDetailModel>();
                    if (!responseMsg.IsSuccessStatusCode) throw new Exception($"Cannot receive successful response when getting the tech of stock {stockId}");
                    var data = await responseMsg.Content.ReadAsStringAsync();
                    detail = data.Split("\"ta\":")[1].Split("});")[0].Split(",\"ex\"")[0];
                    name = data.Split("\"name\":\"")[1].Split('\"')[0];
                    stockDetails = JsonSerializer.Deserialize<List<StockTechDetailModel>>(detail);
                    stock.StockDetails = stockDetails;
                    stock.Name = name;
                    stock.StockId = stockId;
                    if (string.IsNullOrEmpty(name)) throw new Exception($"Cannot retrieve the name of stock {stockId} when getting the tech of stock {stockId}");
                    results.Add(stock);
                    _logger.Information($"Stock: {stockId} gets its stock tech completed");
                }
                catch(Exception ex)
                {
                    _logger.Error($"Getting the tech of Stock: {stockId} occurred error {ex.ToString()}");
                }
            }
            try
            {
                _logger.Information("Writing stock tech into Mongodb started");
                await _mongoDbService.DeleteAndInsertManyData<StockTechInfoModel>("Tech", results);
                _logger.Information("Writing stock tech into Mongodb completed");
            }
            catch(Exception ex)
            {
                _logger.Error($"Writing stock tech into Mongodb error. {ex.ToString()}");
            }
        }
    }
}
