using Models.Models;
using Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Repositories.Interfaces;
using System.Runtime.InteropServices;
using MongoDbProvider;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Collections.ObjectModel;
using Serilog;
using Models.Enums;
using System.Collections.Generic;

namespace Services.Services
{
    public class StockService : IStockService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IMongoDbService _mongoDbservice;
        private MongodbConfigModel _mongodbConfig;
        private readonly ILogger _logger;
        public StockService(IMemoryCache memoryCache, IMongoDbService mongoDbService, IConfiguration config, ILogger logger)
        {
            _memoryCache = memoryCache;
            _mongoDbservice = mongoDbService;
            _mongodbConfig = config.GetSection("Mongodb").Get<MongodbConfigModel>();
            _logger = logger;
        }
        public async Task<List<StockTechInfoModel>> GetJumpEmptyStocks()
        {
            if (_memoryCache.TryGetValue<List<StockTechInfoModel>>(EStrategy.GetJumpEmptyStocks.ToString(), out var cacheResult)) return cacheResult;
            List<StockTechInfoModel> selectedStocks = await GetStockBySpecificStrategy(JumpEmptyStrategy);
            _memoryCache.Set<List<StockTechInfoModel>>(EStrategy.GetJumpEmptyStocks.ToString(), selectedStocks, TimeSpan.FromMinutes(10));
            return selectedStocks;
        }
        public async Task<List<StockTechInfoModel>> GetBullishPullbackStocks()
        {
            if (_memoryCache.TryGetValue<List<StockTechInfoModel>>(EStrategy.GetBullishPullbackStocks.ToString(), out var cacheResult)) return cacheResult;
            List<StockTechInfoModel> selectedStocks = await GetStockBySpecificStrategy(BullishPullbackStrategy);
            _memoryCache.Set<List<StockTechInfoModel>>(EStrategy.GetBullishPullbackStocks.ToString(), selectedStocks, TimeSpan.FromMinutes(10));
            return selectedStocks;
        }
        private async Task<List<StockTechInfoModel>> GetStockBySpecificStrategy(GetStocksBySpecificStrategy strategy)
        {
            var techCollection = _mongoDbservice.GetMongoClient().GetDatabase(_mongodbConfig.Name).GetCollection<StockTechInfoModel>(EnumCollection.Tech.ToString());
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
            if (_memoryCache.TryGetValue<List<StockFinanceInfoModel>>(EStrategy.GetFinanceIncreasingStocks.ToString(), out var cacheResult)) return cacheResult;
            var database = _mongoDbservice.GetMongoClient().GetDatabase(_mongodbConfig.Name);
            var epsCollection = database.GetCollection<StockEpsModel>(EnumCollection.EPS.ToString());
            var revenueCollection = database.GetCollection<StockRevenueModel>(EnumCollection.Revenue.ToString());
            var getEpsTask = _mongoDbservice.GetAllData<StockEpsModel>(epsCollection);
            var getRevenueTask = _mongoDbservice.GetAllData<StockRevenueModel>(revenueCollection);
            List<StockEpsModel> epsInfos = await getEpsTask;
            List<StockRevenueModel> revenueInfos = await getRevenueTask;
            List<StockFinanceInfoModel> results = new List<StockFinanceInfoModel>();
            foreach (var i in epsInfos)
            {
                try
                {
                    if (i.EpsList == null || i.EpsList.Count < 8) continue;
                    bool hasNegativeEps = i.EpsList.Take(8).Any(x => x.Eps <= 0);
                    bool hasNegativeEpsYoy = i.EpsList.Take(2).Any(x => x.Yoy <= 0);
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
            _memoryCache.Set<List<StockFinanceInfoModel>>(EStrategy.GetFinanceIncreasingStocks.ToString(), results, TimeSpan.FromMinutes(10));
            return results;
        }
        public async Task<List<StockBaseModel>> GetHighYieldStocks()
        {
            if (_memoryCache.TryGetValue<List<StockBaseModel>>(EStrategy.GetHighYieldStocks.ToString(), out var cacheResult)) return cacheResult;
            _logger.Information("Getting stock information from MongoDB started");
            var database = _mongoDbservice.GetMongoClient().GetDatabase(_mongodbConfig.Name);
            var epsCollection = database.GetCollection<StockEpsModel>(EnumCollection.EPS.ToString());
            var revenueCollection = database.GetCollection<StockRevenueModel>(EnumCollection.Revenue.ToString());
            var dividendCollection = database.GetCollection<StockDividendModel>(EnumCollection.Dividend.ToString());
            var techCollection = database.GetCollection<StockTechInfoModel>(EnumCollection.Tech.ToString());
            var epsTask = _mongoDbservice.GetAllData<StockEpsModel>(epsCollection);
            var revenueTask = _mongoDbservice.GetAllData<StockRevenueModel>(revenueCollection);
            var dividendTask = _mongoDbservice.GetAllData<StockDividendModel>(dividendCollection);
            var techTask = _mongoDbservice.GetAllData<StockTechInfoModel>(techCollection);
            List<StockEpsModel> epsInfos = await epsTask;
            List<StockRevenueModel> revenueInfos = await revenueTask;
            List<StockDividendModel> dividendInfos = await dividendTask;
            List<StockTechInfoModel> techInfos = await techTask;
            _logger.Information("Getting stock information from MongoDB completed");
            int thisYear = DateTime.Now.Year;
            List<StockEpsModel> epsMatch = epsInfos.Where(x => x.EpsList.Where(y => y.Year == thisYear - 2).Count() == 4 && (x.EpsList.Where(y => y.Year == thisYear - 1 && y.Quarter < 4).Sum(y => y.Eps) >= x.EpsList.Where(y => y.Year == thisYear - 2).Sum(y => y.Eps))).ToList();
            List<string> revenueMatch = revenueInfos.Where(x => x.RevenueList.Where(y => y.Year == thisYear - 1 && y.Month > 9).Count() == 3 && x.RevenueList.Where(y => y.Year == thisYear - 2 && y.Month > 9).Count() == 3 && (x.RevenueList.Where(y => y.Year == thisYear - 1 && y.Month > 9).Sum(y => y.Revenue) > x.RevenueList.Where(y => y.Year == thisYear - 2 && y.Month > 9).Sum(y => y.Revenue))).Select(x => x.StockId).ToList();
            List<StockEpsModel> bothEpsAndRevenueMatch = epsMatch.Where(x => revenueMatch.Contains(x.StockId)).ToList();
            List<StockBaseModel> results = new List<StockBaseModel>();
            foreach (var i in bothEpsAndRevenueMatch)
            {
                try
                {
                    StockDividendModel dividendMatch = dividendInfos.Where(x => x.StockId == i.StockId).FirstOrDefault();
                    StockTechInfoModel techMatch = techInfos.Where(x => x.StockId == i.StockId).FirstOrDefault();
                    if (i.EpsList == null || i.EpsList.Count == 0 || dividendMatch == null || techMatch == null || techMatch.StockDetails == null || techMatch.StockDetails.Count == 0) continue;
                    List<double> payoutRatioList = new List<double>();
                    int count = 2;
                    for (int j = 2; j < 7; j++)
                    {
                        List<StockEpsDetailModel> yearEps = i.EpsList.Where(x => x.Year == thisYear - j).ToList();
                        List<StockDevidendDetailModel> yearDividend = dividendMatch.DividendList.Where(x => x.Year == thisYear - j).ToList();
                        if (yearEps == null || yearEps.Count == 0 || yearDividend == null || yearDividend.Count == 0) continue;
                        double eps = yearEps.Sum(x => x.Eps);
                        double dividend = yearDividend.Sum(x => x.CashDividend);
                        double payoutRatio = eps == 0 ? 0 : dividend / eps;
                        payoutRatioList.Add(payoutRatio);
                    }
                    if (payoutRatioList.Count == 0) continue;
                    double averagePayoutRatio = payoutRatioList.Average();
                    List<StockEpsDetailModel> lastYearEpsList = i.EpsList.Where(x => x.Year == thisYear - 1).ToList();
                    if (lastYearEpsList.Count == 0) continue;
                    double lastYearEps = lastYearEpsList.Sum(x => x.Eps);
                    double todayClose = techMatch.StockDetails.TakeLast(1).FirstOrDefault().c;
                    double predictYield = (lastYearEps * averagePayoutRatio) / todayClose;
                    if ((lastYearEpsList.Count == 4 && predictYield > 0.07) || (lastYearEpsList.Count < 4 && predictYield > 0.03))
                    {
                        results.Add(new StockBaseModel() { StockId = i.StockId, Name = i.Name });
                    }
                }
                catch(Exception ex) 
                {
                    throw new Exception($"Stock: {i.StockId} with {ex.ToString()}");
                }
            }
            _memoryCache.Set<List<StockBaseModel>>(EStrategy.GetHighYieldStocks.ToString(), results, TimeSpan.FromMinutes(10));
            return results;
        }
    }
}
