using AngleSharp.Html.Parser;
using Caches.Interfaces;
using Models.Models;
using MongoDbProvider;
using Repositories.Interfaces;
using Serilog;

namespace Caches.Caches
{
    public class CacheStockDividend : ICacheStockDividend
    {
        private readonly IStockRepository _stockRepository;
        private readonly IMongoDbService _mongoDbService;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public CacheStockDividend(IStockRepository stockRepository, IMongoDbService mongoDbService, ILogger logger, IHttpClientFactory httpClientFactory)
        {
            _stockRepository = stockRepository;
            _mongoDbService = mongoDbService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SetStockDividendCache()
        {
            //List<string> stockIds = _stockRepository.GetStockIds(); // 取得所有的 stockId
            List<string> stockIds = new List<string> { "2330" };
            HttpClient client = _httpClientFactory.CreateClient();
            List<StockDividendModel> results = new List<StockDividendModel>();
            foreach (var stockId in stockIds)
            {
                try
                {
                    string url = $"https://histock.tw/stock/{stockId}/%E9%99%A4%E6%AC%8A%E9%99%A4%E6%81%AF";
                    var responseMsg = await client.GetAsync(url);
                    if (!responseMsg.IsSuccessStatusCode) throw new Exception($"Cannot receive successful response when getting the dividend of stock {stockId}");
                    var response = await responseMsg.Content.ReadAsStringAsync();
                    HtmlParser parser = new HtmlParser();
                    var document = await parser.ParseDocumentAsync(response);
                    string name = document.QuerySelector("div.info-left a") == null ? string.Empty : document.QuerySelector("div.info-left a").InnerHtml;
                    if (string.IsNullOrEmpty(name)) throw new Exception($"Cannot retrieve the name of stock {stockId} when getting the dividend of stock {stockId}"); ;
                    StockDividendModel stock = new StockDividendModel
                    {
                        StockId = stockId,
                        Name = name,
                        DividendList = new List<StockDevidendDetailModel>()
                    };
                    var data = document.QuerySelectorAll("table.tb-stock tr").Skip(2);
                    if (data == null || data.Count() == 0) throw new Exception($"Stock {stockId} does not have dividend information");
                    foreach (var i in data)
                    {
                        var allTd = i.QuerySelectorAll("td");
                        if (allTd == null || allTd.Count() < 7) continue;
                        var yearString = allTd.FirstOrDefault().InnerHtml;
                        var cashDividendString = allTd.Skip(6).FirstOrDefault().InnerHtml;
                        var stockDividendString = allTd.Skip(5).FirstOrDefault().InnerHtml;
                        if (!Int32.TryParse(yearString, out int year)) continue;
                        StockDevidendDetailModel stockDevidendDetailModel = new StockDevidendDetailModel();
                        stockDevidendDetailModel.Year = year;
                        stockDevidendDetailModel.CashDividend = double.TryParse(cashDividendString, out double cashDividend) ? cashDividend : 0;
                        stockDevidendDetailModel.StockDividend = double.TryParse(stockDividendString, out double stockDividend) ? stockDividend : 0;
                        stock.DividendList.Add(stockDevidendDetailModel);
                    }
                    results.Add(stock);
                    _logger.Information($"Stock: {stockId} gets its dividend completed");
                }
                catch(Exception ex) 
                {
                    _logger.Error($"Getting the dividend of Stock: {stockId} occurred error {ex.ToString()}");
                }
            }

            try
            {
                _logger.Information("Writing stock dividend into Mongodb started");
                await _mongoDbService.DeleteAndInsertManyData<StockDividendModel>("Dividend", results);
                _logger.Information("Writing stock dividend into Mongodb completed");
            }
            catch(Exception ex)
            {
                _logger.Error($"Writing stock dividend into Mongodb error. {ex.ToString()}");
            }
        }
    }
}
