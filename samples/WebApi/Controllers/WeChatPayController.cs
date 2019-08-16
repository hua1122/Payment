using Essensoft.AspNetCore.Payment.WeChatPay;
using Essensoft.AspNetCore.Payment.WeChatPay.Request;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Filter;
using WebApi.Models.Web;

namespace WebApi.Controllers
{
    //微信Api文档：https://pay.weixin.qq.com/wiki/doc/api/index.html
    public class WeChatPayController : Controller
    {
        private ILog _log = LogManager.GetLogger(Startup.LoggerRepository.Name, typeof(HttpGlobalExceptionFilter));

        private readonly IWeChatPayClient _client;

        public WeChatPayController(IWeChatPayClient client)
        {
            this._client = client;
        }

        public IActionResult Index()
        {
            return View();
        }


        /// <summary>
        /// 刷卡支付
        /// </summary>
        /// <param name="out_trade_no">商户系统内部订单号，要求32个字符内，只能是数字、大小写字母_-|*且在同一个商户号下唯一。详见商户订单号</param>
        /// <param name="total_fee">订单总金额，单位为分，只能为整数</param>
        /// <param name="body">商品描述</param>
        /// <param name="spbill_create_ip">终端IP。支持IPV4和IPV6两种格式的IP地址。调用微信支付API的机器IP</param>
        /// <param name="auth_code">扫码支付授权码，设备读取用户微信中的条码或者二维码信息（注：用户付款码条形码规则：18位纯数字，以10、11、12、13、14、15开头）</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> MicroPay(string out_trade_no, int total_fee, string body, string spbill_create_ip, string auth_code)
        {
            var request = new WeChatPayMicroPayRequest
            {
                Body = body,
                OutTradeNo = out_trade_no,
                TotalFee = total_fee,
                SpbillCreateIp = spbill_create_ip,
                AuthCode = auth_code
            };
            var response = await _client.ExecuteAsync(request);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);                
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);
        }

        /// <summary>
        /// 公众号支付
        /// </summary>
        /// <param name="body">商品描述</param>
        /// <param name="out_trade_no">商户系统内部订单号，要求32个字符内，只能是数字、大小写字母_-|* 且在同一个商户号下唯一。</param>
        /// <param name="total_fee">订单总金额，单位为分</param>
        /// <param name="spbill_create_ip">支持IPV4和IPV6两种格式的IP地址。调用微信支付API的机器IP</param>
        /// <param name="notify_url">异步接收微信支付结果通知的回调地址，通知url必须为外网可访问的url，不能携带参数。</param>
        /// <param name="trade_type">JSAPI -JSAPI支付 NATIVE -Native支付 APP -APP支付</param>
        /// <param name="openid">trade_type=JSAPI时（即JSAPI支付），此参数必传，此参数为微信用户在商户对应appid下的唯一标识。</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> PubPay(string body, string out_trade_no, int total_fee, string spbill_create_ip, string notify_url, string trade_type, string openid)
        {
            var request = new WeChatPayUnifiedOrderRequest
            {
                Body = body,
                OutTradeNo = out_trade_no,
                TotalFee = total_fee,
                SpbillCreateIp = spbill_create_ip,
                NotifyUrl = notify_url,
                TradeType = trade_type,
                OpenId = openid
            };
            var response = await _client.ExecuteAsync(request);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {                
                var req = new WeChatPayH5CallPaymentRequest
                {
                    Package = "prepay_id=" + response.PrepayId
                };
                var parameter = await _client.ExecuteAsync(req);
                // 将参数(parameter)给 公众号前端 让他在微信内H5调起支付(https://pay.weixin.qq.com/wiki/doc/api/jsapi.php?chapter=7_7&index=6)
                result.Add("parameter", JsonConvert.SerializeObject(parameter));
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);                
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);

        }

        /// <summary>
        /// 扫码支付
        /// </summary>
        /// <param name="body">商品描述</param>
        /// <param name="out_trade_no">商户系统内部订单号，要求32个字符内，只能是数字、大小写字母_-|* 且在同一个商户号下唯一。</param>
        /// <param name="total_fee">订单总金额，单位为分</param>
        /// <param name="spbill_create_ip">支持IPV4和IPV6两种格式的IP地址。调用微信支付API的机器IP</param>
        /// <param name="notify_url">异步接收微信支付结果通知的回调地址，通知url必须为外网可访问的url，不能携带参数。</param>
        /// <param name="trade_type">JSAPI -JSAPI支付 NATIVE -Native支付 APP -APP支付</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> QrCodePay(string body, string out_trade_no, int total_fee, string spbill_create_ip, string notify_url, string trade_type)
        {
            var request = new WeChatPayUnifiedOrderRequest
            {
                Body = body,
                OutTradeNo = out_trade_no,
                TotalFee = total_fee,
                SpbillCreateIp = spbill_create_ip,
                NotifyUrl = notify_url,
                TradeType = trade_type
            };
            var response = await _client.ExecuteAsync(request);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {
                responseModel.code = HTTPResponseCode.Successful;
                result.Add("qrcode", response.CodeUrl);
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);                
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);

        }

        /// <summary>
        /// APP支付
        /// </summary>
        /// <param name="body">商品描述</param>
        /// <param name="out_trade_no">商户系统内部订单号，要求32个字符内，只能是数字、大小写字母_-|* 且在同一个商户号下唯一。</param>
        /// <param name="total_fee">订单总金额，单位为分</param>
        /// <param name="spbill_create_ip">支持IPV4和IPV6两种格式的IP地址。调用微信支付API的机器IP</param>
        /// <param name="notify_url">异步接收微信支付结果通知的回调地址，通知url必须为外网可访问的url，不能携带参数。</param>
        /// <param name="trade_type">JSAPI -JSAPI支付 NATIVE -Native支付 APP -APP支付</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> AppPay(string body, string out_trade_no, int total_fee, string spbill_create_ip, string notify_url, string trade_type)
        {
            var request = new WeChatPayUnifiedOrderRequest
            {
                Body = body,
                OutTradeNo = out_trade_no,
                TotalFee = total_fee,
                SpbillCreateIp = spbill_create_ip,
                NotifyUrl = notify_url,
                TradeType = trade_type
            };
            var response = await _client.ExecuteAsync(request);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {                
                var req = new WeChatPayAppCallPaymentRequest
                {
                    PrepayId = response.PrepayId
                };
                var parameter = await _client.ExecuteAsync(req);
                // 将参数(parameter)给 ios/android端 让他调起微信APP(https://pay.weixin.qq.com/wiki/doc/api/app/app.php?chapter=8_5)
                result.Add("parameter", JsonConvert.SerializeObject(parameter));
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);                
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);

        }

        /// <summary>
        /// H5支付
        /// </summary>
        /// <param name="body">商品描述</param>
        /// <param name="out_trade_no">商户系统内部订单号，要求32个字符内，只能是数字、大小写字母_-|* 且在同一个商户号下唯一。</param>
        /// <param name="total_fee">订单总金额，单位为分</param>
        /// <param name="spbill_create_ip">支持IPV4和IPV6两种格式的IP地址。调用微信支付API的机器IP</param>
        /// <param name="notify_url">异步接收微信支付结果通知的回调地址，通知url必须为外网可访问的url，不能携带参数。</param>
        /// <param name="trade_type">JSAPI -JSAPI支付 NATIVE -Native支付 APP -APP支付</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> H5Pay(string body, string out_trade_no, int total_fee, string spbill_create_ip, string notify_url, string trade_type)
        {
            var request = new WeChatPayUnifiedOrderRequest
            {
                Body = body,
                OutTradeNo = out_trade_no,
                TotalFee = total_fee,
                SpbillCreateIp = spbill_create_ip,
                NotifyUrl = notify_url,
                TradeType = trade_type
            };
            var response = await _client.ExecuteAsync(request);

            // mweb_url为拉起微信支付收银台的中间页面，可通过访问该url来拉起微信客户端，完成支付,mweb_url的有效期为5分钟。
            //return Redirect(response.MwebUrl);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {
                // mweb_url为拉起微信支付收银台的中间页面，可通过访问该url来拉起微信客户端，完成支付,mweb_url的有效期为5分钟。
                result.Add("mweb_url", response.MwebUrl);
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);                
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);

        }

        /// <summary>
        /// 小程序支付
        /// </summary>
        /// <param name="body">商品描述</param>
        /// <param name="out_trade_no">商户系统内部订单号，要求32个字符内，只能是数字、大小写字母_-|* 且在同一个商户号下唯一。</param>
        /// <param name="total_fee">订单总金额，单位为分</param>
        /// <param name="spbill_create_ip">支持IPV4和IPV6两种格式的IP地址。调用微信支付API的机器IP</param>
        /// <param name="notify_url">异步接收微信支付结果通知的回调地址，通知url必须为外网可访问的url，不能携带参数。</param>
        /// <param name="trade_type">JSAPI -JSAPI支付 NATIVE -Native支付 APP -APP支付</param>
        /// <param name="openid">trade_type=JSAPI时（即JSAPI支付），此参数必传，此参数为微信用户在商户对应appid下的唯一标识。</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> LiteAppPay(string body, string out_trade_no, int total_fee, string spbill_create_ip, string notify_url, string trade_type, string openid)
        {
            var request = new WeChatPayUnifiedOrderRequest
            {
                Body = body,
                OutTradeNo = out_trade_no,
                TotalFee = total_fee,
                SpbillCreateIp = spbill_create_ip,
                NotifyUrl = notify_url,
                TradeType = trade_type,
                OpenId = openid
            };
            var response = await _client.ExecuteAsync(request);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {
                var req = new WeChatPayLiteAppCallPaymentRequest
                {
                    Package = "prepay_id=" + response.PrepayId
                };
                var parameter = await _client.ExecuteAsync(req);
                // 将参数(parameter)给 小程序前端 让他调起支付API(https://pay.weixin.qq.com/wiki/doc/api/wxa/wxa_api.php?chapter=7_7&index=5)
                result.Add("parameter", JsonConvert.SerializeObject(parameter));
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);
        }

        /// <summary>
        /// 查询订单
        /// </summary>
        /// <param name="transaction_id">微信的订单号，建议优先使用 </param>
        /// <param name="out_trade_no">商户订单号</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> OrderQuery(string transaction_id, string out_trade_no)
        {
            var request = new WeChatPayOrderQueryRequest
            {
                TransactionId = transaction_id,
                OutTradeNo = out_trade_no
            };
            var response = await _client.ExecuteAsync(request);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);
        }

        /// <summary>
        /// 撤销订单
        /// </summary>
        /// <param name="transaction_id">微信的订单号，建议优先使用 </param>
        /// <param name="out_trade_no">商户订单号</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Reverse(string transaction_id, string out_trade_no)
        {
            var request = new WeChatPayReverseRequest
            {
                TransactionId = transaction_id,
                OutTradeNo = out_trade_no
            };
            var response = await _client.ExecuteAsync(request, "wechatpayCertificateName");
            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);
        }

        /// <summary>
        /// 关闭订单
        /// </summary>
        /// <param name="out_trade_no">商户订单号</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CloseOrder(string out_trade_no)
        {
            var request = new WeChatPayCloseOrderRequest
            {
                OutTradeNo = out_trade_no
            };
            var response = await _client.ExecuteAsync(request);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS")
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);
        }

        /// <summary>
        /// 申请退款
        /// </summary>
        /// <param name="out_refund_no">商户系统内部的退款单号，商户系统内部唯一，只能是数字、大小写字母_-|*@ ，同一退款单号多次请求只退一笔。</param>
        /// <param name="transaction_id">微信生成的订单号，在支付通知中有返回</param>
        /// <param name="out_trade_no">商户系统内部订单号，要求32个字符内，只能是数字、大小写字母_-|*@ ，且在同一个商户号下唯一。</param>
        /// <param name="total_fee">订单总金额，单位为分，只能为整数</param>
        /// <param name="refund_fee">退款总金额，订单总金额，单位为分，只能为整数</param>
        /// <param name="refund_desc">退款原因。若商户传入，会在下发给用户的退款消息中体现退款原因</param>
        /// <param name="notify_url">异步接收微信支付退款结果通知的回调地址，通知URL必须为外网可访问的url，不允许带参数如果参数中传了notify_url，则商户平台上配置的回调地址将不会生效。</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Refund(string out_refund_no,string transaction_id,string out_trade_no,int total_fee,int refund_fee,string refund_desc,string notify_url)
        {
            var request = new WeChatPayRefundRequest
            {
                OutRefundNo = out_refund_no,
                TransactionId = transaction_id,
                OutTradeNo = out_trade_no,
                TotalFee = total_fee,
                RefundFee = refund_fee,
                RefundDesc = refund_desc,
                NotifyUrl = notify_url
            };
            var response = await _client.ExecuteAsync(request, "wechatpayCertificateName");
            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);
        }

        /// <summary>
        /// 查询退款
        /// </summary>
        /// <param name="refund_id">微信生成的退款单号，在申请退款接口有返回 </param>
        /// <param name="out_refund_no">商户退款单号</param>
        /// <param name="transaction_id">微信订单号</param>
        /// <param name="out_trade_no">商户订单号</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> RefundQuery(string refund_id,string out_refund_no, string transaction_id, string out_trade_no)
        {
            var request = new WeChatPayRefundQueryRequest
            {
                RefundId = refund_id,
                OutRefundNo = out_refund_no,
                TransactionId = transaction_id,
                OutTradeNo = out_trade_no
            };
            var response = await _client.ExecuteAsync(request);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);
        }

        /// <summary>
        /// 下载对账单
        /// </summary>
        /// <param name="bill_date">下载对账单的日期，格式：20140603 </param>
        /// <param name="bill_type">ALL（默认值），返回当日所有订单信息（不含充值退款订单）SUCCESS，返回当日成功支付的订单（不含充值退款订单）REFUND，返回当日退款订单（不含充值退款订单）RECHARGE_REFUND，返回当日充值退款订单</param>
        /// <param name="tar_type">非必传参数，固定值：GZIP，返回格式为.gzip的压缩包账单。不传则默认为数据流形式。</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DownloadBill(string bill_date, string bill_type, string tar_type)
        {
            var request = new WeChatPayDownloadBillRequest
            {
                BillDate = bill_date,
                BillType = bill_type,
                TarType = tar_type
            };
            var response = await _client.ExecuteAsync(request);
            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS")
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);
        }

        /// <summary>
        /// 下载资金账单
        /// </summary>
        /// <param name="bill_date">下载对账单的日期，格式：20140603</param>
        /// <param name="account_type">资金账户类型</param>
        /// <param name="tar_type">压缩账单</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DownloadFundFlow(string bill_date, string account_type, string tar_type)
        {
            var request = new WeChatPayDownloadFundFlowRequest
            {
                BillDate = bill_date,
                AccountType = account_type,
                TarType = tar_type
            };
            var response = await _client.ExecuteAsync(request, "wechatpayCertificateName");

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);
        }

        /// <summary>
        /// 获取RSA加密公钥
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetPublicKey()
        {
            var request = new WeChatPayGetPublicKeyRequest();
            var response = await _client.ExecuteAsync(request, "wechatpayCertificateName");

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("return_code", response.ReturnCode);
            result.Add("return_msg", response.ReturnMsg);

            if (response.ReturnCode == "SUCCESS" && response.ResultCode == "SUCCESS")
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            else
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                result.Add("err_code", response.ErrCode);
                result.Add("err_code_des", response.ErrCodeDes);
            }
            responseModel.message = JsonConvert.SerializeObject(result);
            return Json(responseModel);
        }

    }
}