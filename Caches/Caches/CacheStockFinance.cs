using AngleSharp.Html.Parser;
using Caches.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Models.Models;
using MongoDB.Driver;
using MongoDbProvider;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caches.Caches
{
    public class CacheStockFinance : ICacheStockFinance
    {
        // private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        private readonly IMongoDbService _mongoDbService;
        public CacheStockFinance(/* IMemoryCache memoryCache,  */IStockRepository stockRepository, IMongoDbService mongoDbService)
        {
            // _memoryCache = memoryCache;
            _stockRepository = stockRepository;
            _mongoDbService = mongoDbService;
        }
        public async Task SetStockFinanceCache()
        {
            List<string> stockIds = _stockRepository.GetStockIds();
            MongoClient mongoClient = _mongoDbService.GetMongoClient();
            var collection = mongoClient.GetDatabase("MyFuture").GetCollection<StockFinanceInfoModel>("StockFinance");
            foreach (var stockId in stockIds)
            {
                try
                {
                    StockFinanceInfoModel stock = new StockFinanceInfoModel
                    {
                        StockId = stockId
                    };
                    var stockEps = GetStockNameAndEPS(stock);
                    var stockRevenue = GetStockRevenue(stock);
                    var (name, eps) = await stockEps;
                    var revenue = await stockRevenue;
                    stock.Name = name;
                    stock.StockEpss = eps;
                    stock.StockRevenues = revenue;
                    if (!string.IsNullOrEmpty(stock.Name))
                    {
                        // _memoryCache.Set($"Finance{stockId}", stock);
                        var filter = Builders<StockFinanceInfoModel>.Filter.Eq(r=>r.StockId, stockId);
                        await _mongoDbService.InsertOrUpdateStock(collection, filter, stock);
                    }
                }
                catch (Exception ex)
                {
                    
                }
            }
        }
        private async Task<(string? name, List<StockEpsModel> eps)> GetStockNameAndEPS(StockFinanceInfoModel stock)
        {
            HttpClient client = new HttpClient();
            string url = $"https://tw.stock.yahoo.com/quote/{stock.Id}.TW/eps";
            var responseMsg = await client.GetAsync(url);
            string? name = null;
            List<StockEpsModel> details = new List<StockEpsModel>();
            if (responseMsg.IsSuccessStatusCode)
            {
                var response = await responseMsg.Content.ReadAsStringAsync();
                HtmlParser parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(response);
                name = document.QuerySelector("div#main-0-QuoteHeader-Proxy>div>div>h1").InnerHtml;
                var data = document.QuerySelectorAll("div#layout-col1 div.table-body-wrapper li");
                foreach (var i in data)
                {
                    var quarter = i.QuerySelector("div>div>div").InnerHtml;
                    var finance = i.QuerySelectorAll("span");
                    if (double.TryParse(finance[0].InnerHtml, out double eps) && double.TryParse(finance[1].InnerHtml.TrimEnd('%'), out double qoq) && double.TryParse(finance[2].InnerHtml.TrimEnd('%'), out double yoy))
                    {
                        StockEpsModel model = new StockEpsModel()
                        {
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
        private async Task<List<StockRevenueModel>> GetStockRevenue(StockFinanceInfoModel stock)
        {
            HttpClient client = new HttpClient();
            string url = $"https://tw.stock.yahoo.com/quote/{stock.Id}.TW/revenue";
            var responseMsg = await client.GetAsync(url);
            List<StockRevenueModel> details = new List<StockRevenueModel>();
            if (responseMsg.IsSuccessStatusCode)
            {
                var response = await responseMsg.Content.ReadAsStringAsync();
                HtmlParser parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(response);
                var data = document.QuerySelectorAll("div#main-3-QuoteFinanceRevenue-Proxy section#qsp-revenue-table div.table-body-wrapper>ul>li");
                foreach (var i in data)
                {
                    var month = i.QuerySelector("div>div>div").InnerHtml;
                    var revenueInfo = i.QuerySelectorAll("li span");
                    var revenueString = revenueInfo[0].InnerHtml.Replace(",", "");
                    var momString = revenueInfo[1].InnerHtml.TrimEnd('%');
                    var lastYearRevenueString = revenueInfo[2].InnerHtml.Replace(",", "");
                    var yoyString = revenueInfo[3].InnerHtml.TrimEnd('%');
                    if (double.TryParse(revenueString, out double revenue) && double.TryParse(momString, out double mom) && double.TryParse(lastYearRevenueString, out double lastYearRevenue) && double.TryParse(yoyString, out double yoy))
                    {
                        StockRevenueModel model = new StockRevenueModel
                        {
                            Month = month,
                            Revenue = revenue,
                            Mom = mom,
                            LastYearRevenue = lastYearRevenue,
                            Yoy = yoy
                        };
                        details.Add(model);
                    }
                }
            }
            return details;
        }
    }
}
