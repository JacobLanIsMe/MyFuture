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
                await _mongoDbService.DropAndInsertManyData<StockEpsModel>("EPS", results);
                _logger.Information("Writing stock EPS into Mongodb completed");
            }
            catch(Exception ex)
            {
                _logger.Error($"Writing stock EPS into Mongodb error. {ex.ToString()}");
            }
        }
        private async Task<(string? name, List<StockEpsDetailModel> eps)> GetStockNameAndEPS(string stockId, HttpClient client)
        {
            string url = $"https://histock.tw/stock/{stockId}/%E6%AF%8F%E8%82%A1%E7%9B%88%E9%A4%98";
            var responseMsg = await client.GetAsync(url);
            if (!responseMsg.IsSuccessStatusCode) throw new Exception($"Cannot receive successful response when getting the EPS of stock {stockId}");
            var response = await responseMsg.Content.ReadAsStringAsync();
            HtmlParser parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(response);
            string name = document.QuerySelector("div.info-left a") == null ? string.Empty : document.QuerySelector("div.info-left a").InnerHtml;
            List<StockEpsDetailModel> details = new List<StockEpsDetailModel>();
            
            var data = document.QuerySelectorAll("table.tb-stock tr");
            if (data == null || data.Count() < 6) throw new Exception($"Stock {stockId} does not have EPS information");
            var yearHeader = data.FirstOrDefault().QuerySelectorAll("th").Skip(1);
            List<int> years = new List<int>();
            foreach (var i in yearHeader)
            {
                string yearString = i.InnerHtml;
                if (!Int32.TryParse(yearString, out int year)) continue;
                years.Add(year);
            }
            if (years.Count == 0) throw new Exception($"Stock {stockId} does not have EPS information");
            for (int i = 1; i <= 4; i++)
            {
                for (int j = 0; j < years.Count; j++)
                {
                    var epsString = data.Skip(i).FirstOrDefault().QuerySelectorAll("td").Skip(j).FirstOrDefault().InnerHtml;
                    if (!double.TryParse(epsString, out double eps)) continue;
                    StockEpsDetailModel model = new StockEpsDetailModel
                    {
                        Year = years[j],
                        Quarter = i,
                        Eps = eps
                    };
                    details.Add(model);
                }
            }
            details = details.OrderByDescending(x=>x.Year).ThenByDescending(x=>x.Quarter).ToList();
            for (int i = 0; i < details.Count - 1; i++)
            {
                if (details[i].Eps == 0 || details[i+1].Eps == 0)
                {
                    details[i].Qoq = 0;
                    continue;
                }
                double qoq = Math.Abs(((details[i].Eps / details[i + 1].Eps) - 1) * 100);
                details[i].Qoq = details[i].Eps > details[i + 1].Eps ? qoq * 1 : qoq * -1;
            }
            for (int i = 1; i <= 4; i++)
            {
                for (int j = 0; j < years.Count - 1; j++)
                {
                    StockEpsDetailModel? lastData = details.Where(x => x.Year == years[j] && x.Quarter == i).FirstOrDefault();
                    StockEpsDetailModel? thisData = details.Where(x => x.Year == years[j+1] && x.Quarter == i).FirstOrDefault();
                    if (lastData == null || thisData == null) continue;
                    if (thisData.Eps == 0 || lastData.Eps == 0)
                    {
                        thisData.Yoy = 0;
                        continue;
                    }
                    double yoy = Math.Abs(((thisData.Eps / lastData.Eps) - 1) * 100);
                    thisData.Yoy = thisData.Eps > lastData.Eps ? yoy * 1 : yoy * -1;
                }
            }
            
            return (name, details);
        }
    }
}
