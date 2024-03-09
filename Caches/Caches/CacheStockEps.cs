using AngleSharp.Html.Parser;
using Caches.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Models.Models;
using MongoDB.Driver;
using MongoDbProvider;
using Repositories.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caches.Caches
{
    public class CacheStockEps : ICacheStockEps
    {
        // private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        private readonly IMongoDbService _mongoDbService;
        private readonly ILogger<CacheStockEps> _logger;
        public CacheStockEps(/* IMemoryCache memoryCache,  */IStockRepository stockRepository, IMongoDbService mongoDbService, ILogger<CacheStockEps> logger)
        {
            // _memoryCache = memoryCache;
            _stockRepository = stockRepository;
            _mongoDbService = mongoDbService;
            _logger = logger;
        }
        public async Task SetStockEpsCache()
        {
            List<string> stockIds = _stockRepository.GetStockIds();
            List<StockEpsModel> results = new List<StockEpsModel>();
            foreach (var stockId in stockIds)
            {
                try
                {
                    var (name, eps) = await GetStockNameAndEPS(stockId);
                    StockEpsModel stock = new StockEpsModel
                    {
                        StockId = stockId,
                        Name = name,
                        EpsList = eps
                    };
                    if (!string.IsNullOrEmpty(stock.Name))
                    {
                        // _memoryCache.Set($"Finance{stockId}", stock);
                        results.Add(stock);
                        _logger.LogInformation($"Stock: {stockId} gets its EPS completed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Getting the EPS of Stock: {stockId} occurred error {ex.ToString()}");
                }
            }
            try
            {
                _logger.LogInformation("Writing stock EPS into Mongodb started");
                MongoClient mongoClient = _mongoDbService.GetMongoClient();
                var collection = mongoClient.GetDatabase("MyFuture").GetCollection<StockEpsModel>("EPS");
                var filter = Builders<StockEpsModel>.Filter.Empty;
                await collection.DeleteManyAsync(filter);
                await collection.InsertManyAsync(results);
                _logger.LogInformation("Writing stock EPS into Mongodb completed");
            }
            catch(Exception ex)
            {
                _logger.LogError($"Writing stock EPS into Mongodb error. {ex.ToString()}");
            }
        }
        private async Task<(string? name, List<StockEpsDetailModel> eps)> GetStockNameAndEPS(string stockId)
        {
            HttpClient client = new HttpClient();
            string url = $"https://tw.stock.yahoo.com/quote/{stockId}.TW/eps";
            var responseMsg = await client.GetAsync(url);
            string? name = null;
            List<StockEpsDetailModel> details = new List<StockEpsDetailModel>();
            if (responseMsg.IsSuccessStatusCode)
            {
                var response = await responseMsg.Content.ReadAsStringAsync();
                HtmlParser parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(response);
                name = document.QuerySelector("div#main-0-QuoteHeader-Proxy>div>div>h1").InnerHtml;
                var data = document.QuerySelectorAll("div#layout-col1 div.table-body-wrapper li");
                foreach (var i in data)
                {
                    var yearAndQuarter = i.QuerySelector("div>div>div").InnerHtml;
                    var yearAndQuarterArray = yearAndQuarter.Split(" Q");
                    var finance = i.QuerySelectorAll("span");
                    if (Int32.TryParse(yearAndQuarterArray[0], out int year) && Int32.TryParse(yearAndQuarterArray[1], out int quarter) && double.TryParse(finance[0].InnerHtml, out double eps) && double.TryParse(finance[1].InnerHtml.TrimEnd('%'), out double qoq) && double.TryParse(finance[2].InnerHtml.TrimEnd('%'), out double yoy))
                    {
                        StockEpsDetailModel model = new StockEpsDetailModel()
                        {
                            Year = year,
                            Quarter = quarter,
                            Eps = eps,
                            Qoq = qoq,
                            Yoy = yoy
                        };
                        details.Add(model);
                    };
                }
            }
            return (name, details);
        }
        
    }
}
