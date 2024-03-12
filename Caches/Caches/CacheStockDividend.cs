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
            List<string> stockIds = _stockRepository.GetStockIds(); // 取得所有的 stockId
            HttpClient client = _httpClientFactory.CreateClient();
            List<StockDividendModel> results = new List<StockDividendModel>();
            foreach (var stockId in stockIds)
            {
                try
                {
                    string url = $"https://tw.stock.yahoo.com/quote/{stockId}.TW/dividend";
                    var responseMsg = await client.GetAsync(url);
                    if (responseMsg.IsSuccessStatusCode)
                    {
                        var response = await responseMsg.Content.ReadAsStringAsync();
                        HtmlParser parser = new HtmlParser();
                        var document = await parser.ParseDocumentAsync(response);
                        string name = document.QuerySelector("div#main-0-QuoteHeader-Proxy>div>div>h1").InnerHtml;
                        if (string.IsNullOrEmpty(name)) throw new Exception($"Cannot retrieve the name of stock {stockId} when getting the dividend of stock {stockId}"); ;
                        StockDividendModel stock = new StockDividendModel
                        {
                            StockId = stockId,
                            Name = name,
                            DividendList = new List<StockDevidendDetailModel>()
                        };
                        var data = document.QuerySelectorAll("div#layout-col1 div.table-body-wrapper li");
                        foreach (var i in data)
                        {
                            var allDiv = i.QuerySelectorAll("div>div");
                            var yearString = allDiv.Skip(2).FirstOrDefault().InnerHtml;
                            var allSpan = i.QuerySelectorAll("span");
                            var cashDividendString = allSpan.FirstOrDefault().InnerHtml;
                            var stockDividendString = allSpan.Skip(1).FirstOrDefault().InnerHtml;
                            int year = 0;
                            if (!Int32.TryParse(yearString.Split('Q')[0], out year) && !Int32.TryParse(yearString.Split('H')[0], out year)) continue;
                            StockDevidendDetailModel stockDevidendDetailModel = new StockDevidendDetailModel();
                            stockDevidendDetailModel.Year = year;
                            stockDevidendDetailModel.CashDividend = double.TryParse(cashDividendString, out double cashDividend) ? cashDividend : 0;
                            stockDevidendDetailModel.StockDividend = double.TryParse(stockDividendString, out double stockDividend) ? stockDividend : 0;
                            stock.DividendList.Add(stockDevidendDetailModel);
                        }
                        results.Add(stock);
                        _logger.Information($"Stock: {stockId} gets its dividend completed");
                    }
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
