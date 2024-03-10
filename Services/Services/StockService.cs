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
        private MongoClient mongoClient;
        //private readonly ILogger<StockService> _logger;
        public StockService(/* IMemoryCache memoryCache,  */IStockRepository stockRepository, IMongoDbService mongoDbService, IConfiguration config)
        {
            // _memoryCache = memoryCache;
            _stockRepository = stockRepository;
            _mongoDbservice = mongoDbService;
            mongoClient = _mongoDbservice.GetMongoClient();
        }
        public async Task<List<StockTechInfoModel>> GetJumpEmptyStocks()
        {
            List <StockTechInfoModel> selectedStocks = await GetStockBySpecificStrategy(JumpEmptyStrategy);
            return selectedStocks;
        }
        public async Task<List<StockTechInfoModel>> GetBullishPullbackStocks()
        {
            List<StockTechInfoModel> selectedStocks = await GetStockBySpecificStrategy(BullishPullbackStrategy);
            return selectedStocks;
        }
        private async Task<List<StockTechInfoModel>> GetStockBySpecificStrategy(GetStocksBySpecificStrategy strategy)
        {
            var techCollection = mongoClient.GetDatabase("MyFuture").GetCollection<StockTechInfoModel>("Tech");
            List<StockTechInfoModel> allData = await _mongoDbservice.GetAllData<StockTechInfoModel>(techCollection);
            List<StockTechInfoModel> results = new List<StockTechInfoModel>();
            foreach (var i in allData)
            {
                try
                {
                    List<StockTechDetailModel> stockDetails = i.StockDetails.OrderByDescending(x => x.t).ToList();
                    var mv5 = stockDetails.Take(5).Select(x => x.v).Average();
                    double bias60 = default;
                    if (stockDetails.Count >= 60)
                    {
                        double ma60 = stockDetails.Take(60).Select(x => x.c).Average();
                        bias60 = stockDetails.First().c / ma60;
                    }
                    bool isMatchStrategy = false;
                    if (mv5 >= 200 && bias60 != default && bias60 <= 1.1)
                    {
                        isMatchStrategy = strategy(stockDetails);
                    }
                    if (isMatchStrategy)
                    {
                        results.Add(i);
                    }
                }
                catch(Exception ex)
                {

                }
            }
            return results;
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

        public async Task<List<StockFinanceInfoModel>> GetFinanceIncreasingStocks()
        {
            var database = mongoClient.GetDatabase("MyFuture");
            var epsCollection = database.GetCollection<StockEpsModel>("EPS");
            var revenueCollection = database.GetCollection<StockRevenueModel>("Revenue");
            var getEpsTask = _mongoDbservice.GetAllData<StockEpsModel>(epsCollection);
            var getRevenueTask = _mongoDbservice.GetAllData<StockRevenueModel>(revenueCollection);
            List<StockEpsModel> epsInfos = await getEpsTask;
            List<StockRevenueModel> revenueInfos = await getRevenueTask;
            List<StockFinanceInfoModel> results = new List<StockFinanceInfoModel>();
            foreach (var i in epsInfos)
            {
                try
                {
                    if (i.EpsList == null || i.EpsList.Count < 0) continue;
                    bool hasNegativeEps = i.EpsList.Take(8).Any(x => x.Eps < 0);
                    bool hasNegativeEpsYoy = i.EpsList.Take(2).Any(x => x.Yoy < 0);
                    if (hasNegativeEps || hasNegativeEpsYoy) continue;
                    var revenueStock = revenueInfos.Where(x => x.StockId == i.StockId).FirstOrDefault();
                    if (revenueStock == null || revenueStock.RevenueList == null || revenueStock.RevenueList.Count < 3) continue;
                    var hasNotTwoDigitGrowth = revenueStock.RevenueList.Take(3).Any(x => x.Yoy < 10);
                    if (hasNotTwoDigitGrowth) continue;
                    StockFinanceInfoModel model = new StockFinanceInfoModel
                    {
                        StockId = i.StockId,
                        Name = i.Name
                    };
                    results.Add(model);
                }
                catch(Exception ex)
                {

                }
            }
            return results;
        }
    }
}
