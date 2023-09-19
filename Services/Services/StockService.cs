using Models.Models;
using Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Repositories.Interfaces;
using System.Runtime.InteropServices;
using MongoDbProvider;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Collections.ObjectModel;

namespace Services.Services
{
    public class StockService : IStockService
    {
        // private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        private readonly IMongoDbService _mongoDbservice;
        private IMongoCollection<StockTechInfoModel> techCollection;
        //private readonly ILogger<StockService> _logger;
        public StockService(/* IMemoryCache memoryCache,  */IStockRepository stockRepository, IMongoDbService mongoDbService, IConfiguration config)
        {
            // _memoryCache = memoryCache;
            _stockRepository = stockRepository;
            _mongoDbservice = mongoDbService;
            string connString = config.GetConnectionString("Mongo");
            if (string.IsNullOrEmpty(connString))
            {
                throw new Exception("connString is missing.");
            }
            MongoClient mongoClient = new MongoClient(connString);
            techCollection = mongoClient.GetDatabase("MyFuture").GetCollection<StockTechInfoModel>("StockTech");
        }
        public List<StockTechInfoModel> GetJumpEmptyStocks()
        {
            List<StockTechInfoModel> selectedStocks = GetStockBySpecificStrategy(JumpEmptyStrategy);
            return selectedStocks;
        }
        public List<StockTechInfoModel> GetBullishPullbackStocks()
        {
            List<StockTechInfoModel> selectedStocks = GetStockBySpecificStrategy(BullishPullbackStrategy);
            return selectedStocks;
        }
        private List<StockTechInfoModel> GetStockBySpecificStrategy(GetStocksBySpecificStrategy strategy)
        {
            List<string> stockIds = _stockRepository.GetStockIds();
            List<StockTechInfoModel> result = new List<StockTechInfoModel>();
            foreach (var i in stockIds)
            {
                try
                {
                    // if (_memoryCache.TryGetValue<StockTechInfoModel>($"Tech{i}", out StockTechInfoModel? stock) && stock != null && stock.StockDetails != null)
                    var filter = Builders<StockTechInfoModel>.Filter.Eq(r=>r.StockId, i);
                    StockTechInfoModel stock = techCollection.Find(filter).FirstOrDefault();
                    if (stock != null)
                    {
                        List<StockTechDetailModel> stockDetails = stock.StockDetails.OrderByDescending(x => x.t).ToList();
                        var mv5 = stockDetails.Take(5).Select(x => x.v).Average();
                        #region 取得季線乖離率
                        double bias60 = default;
                        if (stockDetails.Count >= 60)
                        {
                            double ma60 = stockDetails.Take(60).Select(x => x.c).Average();
                            bias60 = stockDetails.First().c / ma60;
                        }
                        #endregion
                        bool isMatchStrategy = false;
                        if (mv5 >= 200 && bias60 != default && bias60 <= 1.1)
                        {
                            isMatchStrategy = strategy(stockDetails);
                        }

                        if (isMatchStrategy)
                        {
                            StockTechInfoModel model = new StockTechInfoModel
                            {
                                StockId = stock.StockId,
                                Name = stock.Name,
                                StockDetails = stockDetails.Take(1).ToList()
                            };
                            result.Add(model);
                        }
                    }
                }
                catch {}
            }
            return result;
        }



        private delegate bool GetStocksBySpecificStrategy(List<StockTechDetailModel> stockDetails);
        #region JumpEmpty
        private bool JumpEmptyStrategy(List<StockTechDetailModel> stockDetails)
        {
            double ma20 = stockDetails.Take(20).Select(x => x.c).Average();
            double ma60 = stockDetails.Take(60).Select(x => x.c).Average();
            double todayClose = stockDetails.First().c;
            if (todayClose >= ma20 || todayClose >= ma60)
            {
                for (int j = 2; j < 20; j++)
                {
                    if (stockDetails[j].o > stockDetails[j + 1].h && stockDetails[j].c > stockDetails[j + 1].h)
                    {
                        double volatility = stockDetails[j].h / stockDetails[j].l;
                        if (volatility <= 1.04)
                        {
                            var periodStocks = stockDetails.Take(j).ToList();
                            int overRangeCount = 0;
                            if (stockDetails[j].l >= stockDetails[j + 1].h)
                            {
                                overRangeCount = periodStocks.Where(x => (x.c > stockDetails[j].h || x.c < stockDetails[j + 1].h)).Count();
                            }
                            else
                            {
                                overRangeCount = periodStocks.Where(x => (x.c > stockDetails[j].h || x.c < stockDetails[j].l)).Count();
                            }
                            
                            int canOrverRangeCount = (int)(periodStocks.Count / 5);
                            if (overRangeCount <= canOrverRangeCount)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        #endregion
        #region BullishPullback
        private bool BullishPullbackStrategy(List<StockTechDetailModel> stockDetails)
        {
            var last40Days = stockDetails.Take(40).ToList();
            double topClose = last40Days.Select(x => x.c).Max(); // 找到近40天的最高收盤價
            int topDayIndex = last40Days.FindIndex(x => x.c == topClose); // 找到近40天的最高收盤價的位置
            //double lastTopClose = stockDetails.Skip(topDayIndex + 1).Take(20).Select(x => x.c).Max(); // 找到前一個峰值的收盤價
            //int lastTopDayIndex = stockDetails.Skip(topDayIndex + 1).Take(20).ToList().FindIndex(x => x.c == lastTopClose); // 找出第二個峰值的位置
            double bottomClose = stockDetails.Take(topDayIndex).Select(x => x.c).Min(); // 找到最近的底部收盤價
            //double lastBottomClose = stockDetails.Skip(topDayIndex + 1).Take(lastTopDayIndex).Select(x => x.c).Min(); // 找出兩個峰值間的最低值
            double lastBottomClose = stockDetails.Skip(topDayIndex + 1).Take(20).Select(x => x.c).Min(); // 找出近期高點前的 20 天的最低值
            var mh5 = stockDetails.Take(5).Select(x => x.c).Max(); // 找出最近五日收盤價的最高點
            var ml5 = stockDetails.Take(5).Select(x => x.c).Min(); // 找出最近五日收盤價的最低點
            if (bottomClose >= lastBottomClose && mh5 / ml5 <= 1.02)
            {
                return true;
            }
            //if (topClose > lastTopClose && bottomClose >= lastBottomClose && mh5/ml5 <= 1.02)
            //{
            //    return true;
            //}

            return false;
        }
        #endregion

        public List<StockFinanceInfoModel> GetFinanceIncreasingStocks()
        {
            List<string> stockIds = _stockRepository.GetStockIds();
            List<StockFinanceInfoModel> result = new List<StockFinanceInfoModel>();
            foreach (var i in stockIds)
            {
                try
                {
                    if (_memoryCache.TryGetValue<StockFinanceInfoModel>($"Finance{i}", out StockFinanceInfoModel? stock) && stock != null && stock.StockEpss != null)
                    {
                        var stockEpss = stock.StockEpss;
                        var hasNegativeEps = stockEpss.Take(8).Any(x => x.Eps < 0);
                        var hasNegativeEpsYoy = stockEpss.Take(2).Any(x => x.Yoy < 0);
                        var stockRevenues = stock.StockRevenues;
                        var hasNotTwoDigitGrowth = stockRevenues.Take(3).Any(x => x.Yoy < 10);
                        //var hasNegativeRevenueYoy = stockRevenues.Take(3).Any(x=>x.Yoy < 0);
                        if (!hasNegativeEps && !hasNegativeEpsYoy && !hasNotTwoDigitGrowth)
                        {
                            result.Add(stock);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            return result;
        }



    }
}
