using AngleSharp.Html.Parser;
using Caches.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Models.Models;
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
        private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        public CacheStockFinance(IMemoryCache memoryCache, IStockRepository stockRepository)
        {
            _memoryCache = memoryCache;
            _stockRepository = stockRepository;
        }
        public async Task SetStockFinanceCache()
        {
            List<string> stockIds = _stockRepository.GetStockIds();
            foreach (var stockId in stockIds)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    string url = $"https://tw.stock.yahoo.com/quote/{stockId}.TW/eps";
                    var responseMsg = await client.GetAsync(url);
                    if (responseMsg.IsSuccessStatusCode)
                    {
                        var response = await responseMsg.Content.ReadAsStringAsync();
                        HtmlParser parser = new HtmlParser();
                        var document = await parser.ParseDocumentAsync(response);
                        var name = document.QuerySelector("div#main-0-QuoteHeader-Proxy>div>div>h1").InnerHtml;
                        var data = document.QuerySelectorAll("div#layout-col1 div.table-body-wrapper li");
                        List<StockFinanceDetailModel> details = new List<StockFinanceDetailModel>();
                        foreach (var i in data)
                        {
                            var quarter = i.QuerySelector("div>div>div").InnerHtml;
                            var finance = i.QuerySelectorAll("span");
                            if (double.TryParse(finance[0].InnerHtml, out double eps) && double.TryParse(finance[1].InnerHtml.TrimEnd('%'), out double qoq) && double.TryParse(finance[2].InnerHtml.TrimEnd('%'), out double yoy))
                            {
                                StockFinanceDetailModel model = new StockFinanceDetailModel()
                                {
                                    quarter = quarter,
                                    eps = eps,
                                    qoq = qoq,
                                    yoy = yoy
                                };
                                details.Add(model);
                            };
                            
                        }
                        StockFinanceInfoModel stock = new StockFinanceInfoModel();
                        stock.Id = stockId;
                        stock.Name = name;
                        stock.StockDetails = details;
                        if (!string.IsNullOrEmpty(name))
                        {
                            _memoryCache.Set($"Finance{stockId}", stock);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
