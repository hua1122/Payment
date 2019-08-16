using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi.Models.Web
{
    public enum HTTPResponseCode : int
    {
        #region OK

        [Description("请求成功")]
        Successful = 200,

        [Description("部分内容。服务器成功处理了部分请求")]
        PartialContent = 206,

        #endregion

        #region Client Error

        [Description("客户端请求的语法错误，服务器无法理解")]
        BadRequest = 400,

        [Description("服务器理解请求客户端的请求，但是拒绝执行此请求")]
        Forbidden = 403,


        #endregion

        #region ServerError

        [Description("服务器内部错误，无法完成请求")]
        ServerError = 500,

        #endregion

    }
    public class ResponseModel
    {
        public HTTPResponseCode code { get; set; }
        
        public string message { get; set; }

        public dynamic data { get; set; }
    }
}
