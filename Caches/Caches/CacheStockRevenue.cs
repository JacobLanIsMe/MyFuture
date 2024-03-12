using AngleSharp.Html.Parser;
using Caches.Interfaces;
using Models.Models;
using MongoDbProvider;
using Repositories.Interfaces;
using Serilog;

namespace Caches.Caches
{
    public class CacheStockRevenue : ICacheStockRevenue
    {
        private readonly IStockRepository _stockRepository;
        private readonly IMongoDbService _mongoDbService;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public CacheStockRevenue(IStockRepository stockRepository, IMongoDbService mongoDbService, ILogger logger, IHttpClientFactory httpClientFactory)
        {
            _stockRepository = stockRepository;
            _mongoDbService = mongoDbService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }
        public async Task SetStockRevenueCache()
        {
            List<string> stockIds = _stockRepository.GetStockIds();
            HttpClient client = _httpClientFactory.CreateClient();
            List<StockRevenueModel> results = new List<StockRevenueModel>();
            foreach (var stockId in stockIds)
            {
                try
                {
                    var (name, revenue) = await GetStockNameAndRevenue(stockId, client);
                    StockRevenueModel stock = new StockRevenueModel
                    {
                        StockId = stockId,
                        Name = name,
                        RevenueList = revenue
                    };
                    if (string.IsNullOrEmpty(stock.Name)) throw new Exception($"Cannot retrieve the name of stock {stockId} when getting the revenue of stock {stockId}");
                    results.Add(stock);
                    _logger.Information($"Stock: {stockId} gets its revenue completed");
                }
                catch(Exception ex)
                {
                    _logger.Error($"Getting the revenue of Stock: {stockId} occurred error {ex.ToString()}");
                }
            }
            try
            {
                _logger.Information("Writing stock revenue into Mongodb started");
                await _mongoDbService.DeleteAndInsertManyData<StockRevenueModel>("Revenue", results);
                _logger.Information("Writing stock revenue into Mongodb completed");
            }
            catch(Exception ex)
            {
                _logger.Error($"Writing stock revenue into Mongodb error. {ex.ToString()}");
            }
        }
        private async Task<(string? name, List<StockRevenueDetailModel> revenue)> GetStockNameAndRevenue(string stockId, HttpClient client)
        {
            string url = $"https://tw.stock.yahoo.com/quote/{stockId}.TW/revenue";
            var responseMsg = await client.GetAsync(url);
            string? name = null;
            List<StockRevenueDetailModel> details = new List<StockRevenueDetailModel>();
            if (!responseMsg.IsSuccessStatusCode) throw new Exception($"Cannot receive successful response when getting the revenue of stock {stockId}");
            var response = await responseMsg.Content.ReadAsStringAsync();
            HtmlParser parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(response);
            name = document.QuerySelector("div#main-0-QuoteHeader-Proxy>div>div>h1").InnerHtml;
            var data = document.QuerySelectorAll("div#main-3-QuoteFinanceRevenue-Proxy section#qsp-revenue-table div.table-body-wrapper>ul>li");
            foreach (var i in data)
            {
                var yearAndMonthString = i.QuerySelector("div>div>div").InnerHtml;
                string[] yearAndMonthArray = yearAndMonthString.Split('/');
                if (!Int32.TryParse(yearAndMonthArray[0], out int year) || !Int32.TryParse(yearAndMonthArray[1], out int month)) continue;
                var revenueInfo = i.QuerySelectorAll("li span");
                var revenueString = revenueInfo[0].InnerHtml.Replace(",", "");
                var momString = revenueInfo[1].InnerHtml.TrimEnd('%');
                var lastYearRevenueString = revenueInfo[2].InnerHtml.Replace(",", "");
                var yoyString = revenueInfo[3].InnerHtml.TrimEnd('%');
                if (double.TryParse(revenueString, out double revenue) && double.TryParse(momString, out double mom) && double.TryParse(lastYearRevenueString, out double lastYearRevenue) && double.TryParse(yoyString, out double yoy))
                {
                    StockRevenueDetailModel model = new StockRevenueDetailModel
                    {
                        Year = year,
                        Month = month,
                        Revenue = revenue,
                        Mom = mom,
                        LastYearRevenue = lastYearRevenue,
                        Yoy = yoy
                    };
                    details.Add(model);
                }
            }
            return (name, details);
        }
    }

}
