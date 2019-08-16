using Essensoft.AspNetCore.Payment.Alipay;
using Essensoft.AspNetCore.Payment.Alipay.Domain;
using Essensoft.AspNetCore.Payment.Alipay.Notify;
using Essensoft.AspNetCore.Payment.Alipay.Request;
using log4net;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Threading.Tasks;
using WebApi.Filter;
using WebApi.Models.PayModel;
using WebApi.Models.Web;

namespace WebApi.Controllers
{
    /// <summary>
    /// 支付宝Api文档：https://docs.open.alipay.com/api_1
    /// </summary>
    public class AlipayController : Controller
    {
        private ILog _log = LogManager.GetLogger(Startup.LoggerRepository.Name, typeof(HttpGlobalExceptionFilter));

        // 支付宝请求客户端(用于处理请求与其响应)
        private readonly IAlipayClient _client;

        // 支付宝通知客户端(用于解析异步通知或同步跳转)
        private readonly IAlipayNotifyClient _notifyClient;

        public AlipayController(IAlipayClient client, IAlipayNotifyClient notifyClient)
        {
            this._client = client;
            this._notifyClient = notifyClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 当面付-扫码支付
        /// </summary>
        /// <param name="out_trade_no">商户订单号,64个字符以内、可包含字母、数字、下划线；需保证在商户端不重复</param>
        /// <param name="subject">订单标题</param>
        /// <param name="total_amount">订单总金额，单位为元，精确到小数点后两位，取值范围[0.01,100000000]。</param>
        /// <param name="body">订单描述</param>
        /// <param name="notify_url">设置异步通知URL</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> PreCreate(string out_trade_no, string subject, string total_amount, string body, string notify_url = "")
        {
            var model = new AlipayTradePrecreateModel
            {
                OutTradeNo = out_trade_no,
                Subject = subject,
                TotalAmount = total_amount,
                Body = body
            };
            var req = new AlipayTradePrecreateRequest();
            req.SetBizModel(model);
            req.SetNotifyUrl(notify_url);

            var response = await _client.ExecuteAsync(req);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);
        }

        /// <summary>
        /// 当面付-二维码/条码/声波支付
        /// </summary>
        /// <param name="out_trade_no">商户订单号,64个字符以内、可包含字母、数字、下划线；需保证在商户端不重复</param>
        /// <param name="subject">订单标题</param>
        /// <param name="total_amount">订单总金额，单位为元，精确到小数点后两位，取值范围[0.01,100000000]。</param>
        /// <param name="body">订单描述</param>
        /// <param name="scene">支付场景条码支付，取值：bar_code声波支付，取值：wave_code</param>
        /// <param name="auth_code">支付授权码，25~30开头的长度为16~24位的数字，实际字符串长度以开发者获取的付款码长度为准 </param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Pay(string out_trade_no, string subject, string total_amount, string body, string scene, string auth_code)
        {
            var model = new AlipayTradePayModel
            {
                OutTradeNo = out_trade_no,
                Subject = subject,
                Scene = scene,
                AuthCode = auth_code,
                TotalAmount = total_amount,
                Body = body
            };
            var req = new AlipayTradePayRequest();
            req.SetBizModel(model);

            var response = await _client.ExecuteAsync(req);
            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);
        }

        /// <summary>
        /// APP支付
        /// </summary>
        /// <param name="out_trade_no">商户订单号,64个字符以内、可包含字母、数字、下划线；需保证在商户端不重复</param>
        /// <param name="subject">订单标题</param>
        /// <param name="total_amount">订单总金额，单位为元，精确到小数点后两位，取值范围[0.01,100000000]。</param>
        /// <param name="body">订单描述</param>
        /// <param name="product_code">/// 销售产品码，与支付宝签约的产品码名称。  注：目前仅支持FAST_INSTANT_TRADE_PAY</param>
        /// <param name="notify_url">设置异步通知URL</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> AppPay(string out_trade_no, string subject, string total_amount, string body, string product_code, string notify_url = "")
        {
            var model = new AlipayTradeAppPayModel
            {
                OutTradeNo = out_trade_no,
                Subject = subject,
                ProductCode = product_code,
                TotalAmount = total_amount,
                Body = body
            };
            var req = new AlipayTradeAppPayRequest();
            req.SetBizModel(model);
            req.SetNotifyUrl(notify_url);

            var response = await _client.SdkExecuteAsync(req);
            //将response.Body给 ios/android端 由其去调起支付宝APP(https://docs.open.alipay.com/204/105296/ https://docs.open.alipay.com/204/105295/)
            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                data = response.Body
            };

            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);
        }

        /// <summary>
        /// 手机网站支付
        /// </summary>
        /// <param name="out_trade_no">商户订单号,64个字符以内、可包含字母、数字、下划线；需保证在商户端不重复</param>
        /// <param name="subject">订单标题</param>
        /// <param name="total_amount">订单总金额，单位为元，精确到小数点后两位，取值范围[0.01,100000000]。</param>
        /// <param name="body">订单描述</param>
        /// <param name="product_code">/// 销售产品码，与支付宝签约的产品码名称。  注：目前仅支持FAST_INSTANT_TRADE_PAY</param>
        /// <param name="notify_url">设置异步通知URL</param>
        /// <param name="return_url">设置同步跳转URL</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> WapPay(string out_trade_no, string subject, string total_amount, string body, string product_code, string notify_url="", string return_url="")
        {
            var model = new AlipayTradeWapPayModel
            {
                Body = body,
                Subject = subject,
                TotalAmount = total_amount,
                OutTradeNo = out_trade_no,
                ProductCode = product_code
            };
            var req = new AlipayTradeWapPayRequest();
            req.SetBizModel(model);
            req.SetNotifyUrl(notify_url);
            req.SetReturnUrl(return_url);

            var response = await _client.PageExecuteAsync(req);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message = string.Empty,
                data = response.Body
            };
            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);

            //return Content(response.Body, "text/html", Encoding.UTF8);
        }

        public async Task<IActionResult> PagePayBeta(AlipayTradePagePayViewModel viewModel)
        {
            var model = new AlipayTradePagePayModel
            {
                Body = viewModel.Body,
                Subject = viewModel.Subject,
                TotalAmount = viewModel.TotalAmount,
                OutTradeNo = viewModel.OutTradeNo,
                ProductCode = viewModel.ProductCode
            };
            var req = new AlipayTradePagePayRequest();
            req.SetBizModel(model);
            req.SetNotifyUrl(viewModel.NotifyUrl);
            req.SetReturnUrl(viewModel.ReturnUrl);

            var response = await _client.PageExecuteAsync(req);


            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message = string.Empty,
                data = response.Body
            };
            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);
        }

        /// <summary>
        /// 电脑网站支付
        /// </summary>
        /// <param name="out_trade_no">商户订单号,64个字符以内、可包含字母、数字、下划线；需保证在商户端不重复</param>
        /// <param name="subject">订单标题</param>
        /// <param name="total_amount">订单总金额，单位为元，精确到小数点后两位，取值范围[0.01,100000000]。</param>
        /// <param name="body">订单描述</param>
        /// <param name="product_code">/// 销售产品码，与支付宝签约的产品码名称。  注：目前仅支持FAST_INSTANT_TRADE_PAY</param>
        /// <param name="notify_url">设置异步通知URL</param>
        /// <param name="return_url">设置同步跳转URL</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> PagePay(string out_trade_no, string subject, string total_amount, string body, string product_code, string notify_url = "", string return_url = "")
        {
            // 组装模型
            var model = new AlipayTradePagePayModel()
            {
                Body = body,
                Subject = subject,
                TotalAmount = total_amount,
                OutTradeNo = out_trade_no,
                ProductCode = product_code,
            };

            var req = new AlipayTradePagePayRequest();

            // 设置请求参数
            req.SetBizModel(model);

            // 设置异步通知URL
            req.SetNotifyUrl(notify_url);

            // 设置同步跳转URL
            req.SetReturnUrl(return_url);

            // 页面请求处理 传入 'GET' 返回的 response.Body 为 URL, 'POST' 返回的 response.Body 为 HTML.
            //var response = await _client.PageExecuteAsync(req, null, "GET");
            // 重定向到支付宝电脑网页支付页面.
            //return Redirect(response.Body);
            var response = await _client.PageExecuteAsync(req);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message = string.Empty,
                data = response.Body
            };
            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);

            //return Content(response.Body, "text/html", Encoding.UTF8);
        }

        /// <summary>
        /// 交易查询
        /// </summary>
        /// <param name="out_trade_no">订单支付时传入的商户订单号,和支付宝交易号不能同时为空。trade_no,out_trade_no如果同时存在优先取trade_no</param>
        /// <param name="trade_no">支付宝交易号，和商户订单号不能同时为空 </param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Query(string out_trade_no, string trade_no)
        {
           

            var model = new AlipayTradeQueryModel
            {
                OutTradeNo = out_trade_no,
                TradeNo = trade_no
            };

            var req = new AlipayTradeQueryRequest();
            req.SetBizModel(model);
            
            var response = await _client.ExecuteAsync(req);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message = string.Empty,
                data = response.Body
            };
            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);
        }

        /// <summary>
        /// 交易退款
        /// </summary>
        /// <param name="out_trade_no">订单支付时传入的商户订单号,和支付宝交易号不能同时为空。trade_no,out_trade_no如果同时存在优先取trade_no</param>
        /// <param name="trade_no">支付宝交易号，和商户订单号不能同时为空 </param>
        /// <param name="refund_amount">需要退款的金额，该金额不能大于订单金额,单位为元，支持两位小数 </param>
        /// <param name="out_request_no">标识一次退款请求，同一笔交易多次退款需要保证唯一，如需部分退款，则此参数必传。 </param>
        /// <param name="refund_reason">退款的原因说明（示例：正常退款）</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Refund(string out_trade_no, string trade_no, string refund_amount, string out_request_no, string refund_reason)
        {
            var model = new AlipayTradeRefundModel
            {
                OutTradeNo = out_trade_no,
                TradeNo = trade_no,
                RefundAmount = refund_amount,
                OutRequestNo = out_request_no,
                RefundReason = refund_reason
            };

            var req = new AlipayTradeRefundRequest();
            req.SetBizModel(model);

            var response = await _client.ExecuteAsync(req);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message = string.Empty,
                data = response.Body
            };
            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);
        }

        /// <summary>
        /// 退款查询
        /// </summary>
        /// <param name="out_trade_no">订单支付时传入的商户订单号,和支付宝交易号不能同时为空。trade_no,out_trade_no如果同时存在优先取trade_no</param>
        /// <param name="trade_no">支付宝交易号，和商户订单号不能同时为空 </param>
        /// <param name="out_request_no">请求退款接口时，传入的退款请求号，如果在退款请求时未传入，则该值为创建交易时的外部交易号  </param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> RefundQuery(string out_trade_no, string trade_no, string out_request_no)
        {
            var model = new AlipayTradeFastpayRefundQueryModel
            {
                OutTradeNo = out_trade_no,
                TradeNo = trade_no,
                OutRequestNo = out_request_no
            };

            var req = new AlipayTradeFastpayRefundQueryRequest();
            req.SetBizModel(model);

            var response = await _client.ExecuteAsync(req);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message = string.Empty,
                data = response.Body
            };
            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);
        }

        /// <summary>
        /// 单笔转账到支付宝账户
        /// </summary>
        /// <param name="out_biz_no">商户转账唯一订单号。发起转账来源方定义的转账单据ID，用于将转账回执通知给来源方。不同来源方给出的ID可以重复，同一个来源方必须保证其ID的唯一性。只支持半角英文、数字，及“-”、“_”。 </param>
        /// <param name="payee_type">收款方账户类型。可取值：1、ALIPAY_USERID：支付宝账号对应的支付宝唯一用户号。以2088开头的16位纯数字组成。2、ALIPAY_LOGONID：支付宝登录号，支持邮箱和手机号格式。 </param>
        /// <param name="payee_account">收款方账户。与payee_type配合使用。付款方和收款方不能是同一个账户。</param>
        /// <param name="amount">转账金额，单位：元。只支持2位小数，小数点前最大支持13位，金额必须大于等于0.1元。最大转账金额以实际签约的限额为准。</param>
        /// <param name="payer_show_name">付款方姓名（最长支持100个英文/50个汉字）。显示在收款方的账单详情页。如果该字段不传，则默认显示付款方的支付宝认证姓名或单位名称。 </param>
        /// <param name="payee_real_name">收款方真实姓名（最长支持100个英文/50个汉字）。如果本参数不为空，则会校验该账户在支付宝登记的实名是否与收款方真实姓名一致。</param>
        /// <param name="remark">转账备注（支持200个英文/100个汉字）。当付款方为企业账户，且转账金额达到（大于等于）50000元，remark不能为空。收款方可见，会展示在收款用户的收支详情中。</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Transfer(string out_biz_no, string payee_type, string payee_account, string amount, string payer_show_name, string payee_real_name ,string remark)
        {
            var model = new AlipayFundTransToaccountTransferModel
            {
                OutBizNo = out_biz_no,
                PayeeType = payee_type,
                PayeeAccount = payee_account,
                Amount = amount,                
                PayerShowName= payer_show_name,
                PayeeRealName = payee_real_name,
                Remark = remark
            };
            var req = new AlipayFundTransToaccountTransferRequest();
            req.SetBizModel(model);
            var response = await _client.ExecuteAsync(req);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message = string.Empty,
                data = response.Body
            };
            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);
        }

        /// <summary>
        /// 查询转账订单
        /// </summary>
        /// <param name="out_biz_no">商户转账唯一订单号：发起转账来源方定义的转账单据ID。和支付宝转账单据号不能同时为空。当和支付宝转账单据号同时提供时，将用支付宝转账单据号进行查询，忽略本参数。 </param>
        /// <param name="order_id">支付宝转账单据号：和商户转账唯一订单号不能同时为空。当和商户转账唯一订单号同时提供时，将用本参数进行查询，忽略商户转账唯一订单号。 </param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> TransQuery(string out_biz_no, string order_id)
        {
            var model = new AlipayFundTransOrderQueryModel
            {
                OutBizNo = out_biz_no,
                OrderId = order_id
            };

            var req = new AlipayFundTransOrderQueryRequest();
            req.SetBizModel(model);
            var response = await _client.ExecuteAsync(req);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message = string.Empty,
                data = response.Body
            };
            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);
        }

        /// <summary>
        /// 查询对账单下载地址
        /// </summary>
        /// <param name="bill_type">账单类型，商户通过接口或商户经开放平台授权后其所属服务商通过接口可以获取以下账单类型：trade、signcustomer；trade指商户基于支付宝交易收单的业务账单；signcustomer是指基于商户支付宝余额收入及支出等资金变动的帐务账单； </param>
        /// <param name="bill_date">账单时间：日账单格式为yyyy-MM-dd，月账单格式为yyyy-MM。 </param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> BillDownloadurlQuery(string bill_type,string bill_date)
        {
            var model = new AlipayDataDataserviceBillDownloadurlQueryModel
            {
                BillDate = bill_type,
                BillType = bill_date
            };

            var req = new AlipayDataDataserviceBillDownloadurlQueryRequest();
            req.SetBizModel(model);
            var response = await _client.ExecuteAsync(req);

            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message=string.Empty,
                data = response.Body
            };
            if (response.IsError)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = response.Msg;
            }
            else
            {
                responseModel.code = HTTPResponseCode.Successful;
            }
            return Json(responseModel);
        }

        /// <summary>
        /// 电脑网站支付 - 同步跳转 常用于展示订单支付状态页，建议在异步通知统一做业务处理，而不是在此处.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> PagePayReturn()
        {
            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message = string.Empty
            };

            try
            {
                var notify = await _notifyClient.ExecuteAsync<AlipayTradePagePayReturn>(Request);
                responseModel.data = notify.OutTradeNo;
                responseModel.code = HTTPResponseCode.Successful;
                return Json(responseModel);
            }
            catch (Exception ex)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = ex.Message;
                return Json(responseModel);
            }
        }

        /// <summary>
        /// 手机网站支付 - 同步跳转
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> WapPayReturn()
        {
            ResponseModel responseModel = new ResponseModel()
            {
                code = HTTPResponseCode.PartialContent,
                message = string.Empty
            };

            try
            {
                var notify = await _notifyClient.ExecuteAsync<AlipayTradeWapPayReturn>(Request);
                responseModel.data = notify.OutTradeNo;
                responseModel.code = HTTPResponseCode.Successful;
                return Json(responseModel);
            }
            catch(Exception ex)
            {
                responseModel.code = HTTPResponseCode.BadRequest;
                responseModel.message = ex.Message;
                return Json(responseModel);
            }
        }

    }
}