﻿using Models.Models;
using Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Repositories.Interfaces;
using System.Runtime.InteropServices;

namespace Services.Services
{
    public class StockService : IStockService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        //private readonly ILogger<StockService> _logger;
        public StockService(IMemoryCache memoryCache, IStockRepository stockRepository)
        {
            _memoryCache = memoryCache;
            _stockRepository = stockRepository;
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
                    if (_memoryCache.TryGetValue<StockTechInfoModel>($"Tech{i}", out StockTechInfoModel? stock) && stock != null && stock.StockDetails != null)
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
                                Id = stock.Id,
                                Name = stock.Name,
                                StockDetails = stockDetails.Take(1).ToList()
                            };
                            result.Add(model);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            return result;
        }



        private delegate bool GetStocksBySpecificStrategy(List<StockTechDetailModel> stockDetails);
        #region JumpEmpty
        private bool JumpEmptyStrategy(List<StockTechDetailModel> stockDetails)
        {
            for (int j = 1; j < 10; j++)
            {
                if (stockDetails[j].l >= stockDetails[j + 1].h)
                {
                    var periodStocks = stockDetails.Take(j).ToList();
                    var topClose = periodStocks.Select(x => x.c).Max();
                    var lowClose = periodStocks.Select(x => x.c).Min();
                    if (topClose <= stockDetails[j].h && lowClose >= stockDetails[j + 1].h)
                    {
                        return true;
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
            if (bottomClose >= lastBottomClose && mh5/ml5 <= 1.02)
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
                        var hasNegativeEpsYoy = stockEpss.Take(2).Any(x=>x.Yoy < 0);
                        var stockRevenues = stock.StockRevenues;
                        var hasNegativeRevenueYoy = stockRevenues.Take(3).Any(x=>x.Yoy < 0);
                        if (!hasNegativeEps && !hasNegativeEpsYoy && !hasNegativeRevenueYoy)
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
