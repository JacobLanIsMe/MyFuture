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
        public ApiDataResponseModel GetJumpEmptyStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = _stockService.GetJumpEmptyStocks();
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetBullishPullbackStocks")]
        public ApiDataResponseModel GetBullishPullbackStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = _stockService.GetBullishPullbackStocks();
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetEpsIncreasingStocks")]
        public ApiDataResponseModel GetEpsIncreasingStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = _stockService.GetEpsIncreasingStocks();
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
