using Amazon.Runtime.Documents.Internal.Transform;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Caches.Interfaces;
using Models.Models;
using MongoDbProvider;
using Repositories.Interfaces;
using Serilog;
using System.Reflection.Metadata.Ecma335;

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
            string url = $"https://histock.tw/stock/{stockId}/%E8%B2%A1%E5%8B%99%E5%A0%B1%E8%A1%A8";
            var responseMsg = await client.GetAsync(url);
            if (!responseMsg.IsSuccessStatusCode) throw new Exception($"Cannot receive successful response when getting the revenue of stock {stockId}");
            var response = await responseMsg.Content.ReadAsStringAsync();
            HtmlParser parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(response);
            string name = document.QuerySelector("div.info-left a") == null ? string.Empty : document.QuerySelector("div.info-left a").InnerHtml;
            List<StockRevenueDetailModel> details = new List<StockRevenueDetailModel>();
            var data = document.QuerySelectorAll("table.tb-stock tr").Skip(2);
            foreach (var i in data)
            {
                var epsYearInfo = i.QuerySelectorAll("td");
                if (epsYearInfo == null || epsYearInfo.Count() < 5) continue;
                var yearAndMonthString = epsYearInfo.FirstOrDefault().InnerHtml;
                string[] yearAndMonthArray = yearAndMonthString.Split('/');
                if (!Int32.TryParse(yearAndMonthArray[0], out int year) || !Int32.TryParse(yearAndMonthArray[1], out int month)) continue;

                string revenueString = epsYearInfo.Skip(1).FirstOrDefault().InnerHtml.Replace(','.ToString(), "");
                string lastYearRevenueString = epsYearInfo.Skip(2).FirstOrDefault().InnerHtml.Replace(','.ToString(), "");
                string momString = epsYearInfo.Skip(3).FirstOrDefault().QuerySelector("span") == null ? string.Empty : epsYearInfo.Skip(3).FirstOrDefault().QuerySelector("span").InnerHtml.TrimEnd('%');
                string yoyString = epsYearInfo.Skip(4).FirstOrDefault().QuerySelector("span") == null ? string.Empty : epsYearInfo.Skip(4).FirstOrDefault().QuerySelector("span").InnerHtml.TrimEnd('%');
              
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
