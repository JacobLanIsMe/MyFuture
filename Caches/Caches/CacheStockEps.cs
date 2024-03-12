using AngleSharp.Html.Parser;
using Caches.Interfaces;
using Models.Models;
using MongoDbProvider;
using Repositories.Interfaces;
using Serilog;

namespace Caches.Caches
{
    public class CacheStockEps : ICacheStockEps
    {
        // private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        private readonly IMongoDbService _mongoDbService;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public CacheStockEps(/* IMemoryCache memoryCache,  */IStockRepository stockRepository, IMongoDbService mongoDbService, ILogger logger, IHttpClientFactory httpClientFactory)
        {
            // _memoryCache = memoryCache;
            _stockRepository = stockRepository;
            _mongoDbService = mongoDbService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }
        public async Task SetStockEpsCache()
        {
            List<string> stockIds = _stockRepository.GetStockIds();
            HttpClient client = _httpClientFactory.CreateClient();
            List<StockEpsModel> results = new List<StockEpsModel>();
            foreach (var stockId in stockIds)
            {
                try
                {
                    var (name, eps) = await GetStockNameAndEPS(stockId, client);
                    StockEpsModel stock = new StockEpsModel
                    {
                        StockId = stockId,
                        Name = name,
                        EpsList = eps
                    };
                    if (string.IsNullOrEmpty(stock.Name)) throw new Exception($"Cannot retrieve the name of stock {stockId} when getting the EPS of stock {stockId}");
                    // _memoryCache.Set($"Finance{stockId}", stock);
                    results.Add(stock);
                    _logger.Information($"Stock: {stockId} gets its EPS completed");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Getting the EPS of Stock: {stockId} occurred error {ex.ToString()}");
                }
            }
            try
            {
                _logger.Information("Writing stock EPS into Mongodb started");
                await _mongoDbService.DeleteAndInsertManyData<StockEpsModel>("EPS", results);
                _logger.Information("Writing stock EPS into Mongodb completed");
            }
            catch(Exception ex)
            {
                _logger.Error($"Writing stock EPS into Mongodb error. {ex.ToString()}");
            }
        }
        private async Task<(string? name, List<StockEpsDetailModel> eps)> GetStockNameAndEPS(string stockId, HttpClient client)
        {
            string url = $"https://tw.stock.yahoo.com/quote/{stockId}.TW/eps";
            var responseMsg = await client.GetAsync(url);
            string? name = null;
            List<StockEpsDetailModel> details = new List<StockEpsDetailModel>();
            if (!responseMsg.IsSuccessStatusCode) throw new Exception($"Cannot receive successful response when getting the EPS of stock {stockId}");
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
            return (name, details);
        }
    }
}
