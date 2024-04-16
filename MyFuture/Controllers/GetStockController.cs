using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.Models;
using Services.Interfaces;

namespace MyFuture.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetStockController : ControllerBase
    {
        private readonly IStockService _stockService;
        public GetStockController(IStockService stockService)
        {
            _stockService = stockService;
        }
        [HttpGet("GetJumpEmptyStocks")]
        public async Task<ApiDataResponseModel> GetJumpEmptyStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetJumpEmptyStocks();
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetBullishPullbackStocks")]
        public async Task<ApiDataResponseModel> GetBullishPullbackStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetBullishPullbackStocks();
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetOrganizedStocks")]
        public async Task<ApiDataResponseModel> GetOrganizedStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetOrganizedStocks();
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetFinanceIncreasingStocks")]
        public async Task<ApiDataResponseModel> GetFinanceIncreasingStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetFinanceIncreasingStocks();
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetHighYieldStocks")]
        public async Task<ApiDataResponseModel> GetHighYieldStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetHighYieldStocks();
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
    }
}
