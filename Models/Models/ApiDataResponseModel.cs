using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class ApiDataResponseModel
    {
        public object? Data { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMsg { get; set; }
        public string? ErrorMsgDetail { get; set; }
        public void SetSuccess()
        {
            IsSuccess = true;
        }
        public void SetError(string errorMsg, string errorMsgDetail)
        {
            IsSuccess = false;
            ErrorMsg = errorMsg;
            ErrorMsgDetail = errorMsgDetail;
        }
    }
}
