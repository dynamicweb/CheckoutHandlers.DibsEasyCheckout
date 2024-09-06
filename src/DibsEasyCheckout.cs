using Dynamicweb.Configuration;
using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;
using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;
using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Service;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Orders.Gateways;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.Frontend;
using Dynamicweb.Rendering;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout
{
    /// <summary>
    /// DibsCheckout Payment Window Checkout Handler
    /// </summary>
    [
        AddInName("Dibs/Nets Easy checkout"),
        AddInDescription("Dibs/Nets Easy Checkout handler")
    ]
    public class DibsEasyCheckout : CheckoutHandler, IParameterOptions, IRemoteCapture, ICancelOrder, IFullReturn
    {
        private const string PostTemplateFolder = "eCom7/CheckoutHandler/DibsEasy/Form";
        private const string ErrorTemplateFolder = "eCom7/CheckoutHandler/DibsEasy/Error";

        #region Addin parameters

        [AddInParameter("Test Secret key"), AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true; ")]
        public string TestSecretKey { get; set; }

        [AddInParameter("Test Checkout key "), AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true; ")]
        public string TestCheckoutKey { get; set; }

        [AddInParameter("Live Secret key"), AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true; ")]
        public string LiveSecretKey { get; set; }

        [AddInParameter("Live Checkout key "), AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true; ")]
        public string LiveCheckoutKey { get; set; }

        [AddInParameter("Language"), AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true; none=false; SortBy=Value;")]
        public string Language { get; set; } = "en-GB";

        [AddInParameter("Terms page"), AddInParameterEditor(typeof(PageSelectEditor), "")]
        public string TermsPage { get; set; }

        internal WindowModes windowMode = WindowModes.Embedded;
        private string postTemplate = "eCom7/CheckoutHandler/DibsEasy/Form/EmbededDibs.html";
        private string errorTemplate = "eCom7/CheckoutHandler/DibsEasy/Error/checkouthandler_error.html";

        [AddInLabel("Window Mode"), AddInParameter("WindowMode"), AddInParameterEditor(typeof(RadioParameterEditor), "")]
        public string WindowMode
        {
            get { return windowMode.ToString(); }
            set { Enum.TryParse(value, out windowMode); }
        }

        [AddInParameter("Post template"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=Templates/{PostTemplateFolder}")]
        public string PostTemplate
        {
            get
            {
                return TemplateHelper.GetTemplateName(postTemplate);
            }
            set => postTemplate = value;
        }
        [AddInParameter("Error template"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=Templates/{ErrorTemplateFolder}")]
        public string ErrorTemplate
        {
            get
            {
                return TemplateHelper.GetTemplateName(errorTemplate);
            }
            set => errorTemplate = value;
        }
        [AddInParameter("Auto capture"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Activates Auto-capture for orders – this causes all authorized orders to be immediately captured. Please note that in many countries it is illegal to capture an order before any goods have been shipped.;")]
        public bool AutoCapture { get; set; }

        [AddInParameter("Render inline form"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Makes it possible to use this form inline Use the Ecom:Cart.PaymentInlineForm tag in the cart flow to render the form inline.;")]
        public bool RenderInline { get; set; }

        [AddInParameter("Prefill customer address"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Posts information from previous steps to DIBS Easy instead of requiring them to be filled out again. If enabled you should ensure that the customer can't leave the customer billing fields empty when checking out or else DIBS easy will return errors.;")]
        public bool PrefillCustomerAddress { get; set; }

        [AddInParameter("Support B2B"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Enables a \"Business checkout\" radio button in the DIBS easy payment window.;")]
        public bool SupportB2b { get; set; }

        [AddInParameter("Set B2B as default"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=If \"Support B2B\" is enabled and \"Set B2B as default\" then B2B fields are rendered by default in the DIBS easy payment window. The added B2B address fields vary from country to country.;")]
        public bool SetB2bAsDefault { get; set; }

        [AddInParameter("Test mode"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=When enabled, the test credentials are used instead of the live credentials.;")]
        public bool TestMode { get; set; } = true;

        #endregion


        /// <summary>
        /// Starts order checkout procedure
        /// </summary>
        /// <param name="order">Order to be checked out</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>String representation of template output</returns>
        public override OutputResult BeginCheckout(Order order, CheckoutParameters parameters)
        {
            LogEvent(order, "Checkout started");


            var formTemplate = new Template(TemplateHelper.GetTemplatePath(PostTemplate, PostTemplateFolder));
            return RenderPaymentForm(order, formTemplate, false, parameters?.ReceiptUrl);
        }

        protected override string GetBaseUrl(Order order, bool headless = false)
        {
            if (Context.Current is null)
            {
                return string.Empty;
            }

            // Write the port if it is not default
            bool disablePortNumber = SystemConfiguration.Instance.GetValue("/Globalsettings/System/http/DisableBaseHrefPort") == "True";
            string portString = Context.Current.Request.Url.IsDefaultPort || disablePortNumber ? string.Empty : string.Format(":{0}", Context.Current.Request.Url.Port);
            string pageId = string.IsNullOrWhiteSpace(Context.Current.Request["ID"]) ? string.Empty : string.Format("ID={0}&", Context.Current.Request["ID"]);

            if (headless)
            {
                var baseUrl = SystemConfiguration.Instance.GetValue("/Globalsettings/System/http/BaseUrl");
                if (string.IsNullOrEmpty(baseUrl))
                {
                    var scheme = Context.Current.Request.Headers.Get("X-Forwarded-Proto") ?? Context.Current.Request.Url.Scheme;
                    var host = Context.Current.Request.Headers.Get("X-Forwarded-Host") ?? Context.Current.Request.Url.Host;
                    baseUrl = $"{scheme}://{host}{portString}";
                }

                var url = $"{baseUrl}/dwapi/ecommerce/carts/{order.Secret}/callback";
                return url;
            }

            // Base url
            return string.Format("{0}://{1}{2}/Default.aspx?{3}{4}={5}", Context.Current.Request.Url.Scheme, Context.Current.Request.Url.Host, portString, pageId, OrderIdRequestName, order.Id);
        }

        #region payment request

        /// <summary>
        /// Redirects user to to Dibs CheckoutHandler step
        /// </summary>
        /// <param name="order">Order for processing</param>
        /// <returns>String representation of template output</returns>
        public override OutputResult HandleRequest(Order order)
        {
            try
            {
                LogEvent(null, "Redirected to Dibs CheckoutHandler");

                string action = Context.Current.Request["action"];
                switch (action)
                {
                    case "Approve":
                    case "ApproveInvoce":
                        return StateOk(order);

                    default:
                        string msg = string.Format("Unknown Dibs state: '{0}'", action);
                        LogError(order, msg);
                        return PrintErrorTemplate(order, msg);
                }
            }
            catch (ThreadAbortException)
            {
                return ContentOutputResult.Empty;
            }
            catch (Exception ex)
            {
                LogError(order, ex, "Unhandled exception with message: {0}", ex.Message);
                return PrintErrorTemplate(order, ex.Message);
            }
        }


        #endregion
        private OutputResult PrintErrorTemplate(Order order, string msg)
        {
            LogEvent(order, "Printing error template");

            order.TransactionAmount = 0;
            order.TransactionStatus = "Failed";
            order.Errors.Add(msg);
            Services.Orders.Save(order);

            Services.Orders.DowngradeToCart(order);
            order.TransactionStatus = "";
            Common.Context.SetCart(order);

            if (string.IsNullOrWhiteSpace(ErrorTemplate))
            {
                return PassToCart(order);
            }

            var errorTemplate = new Template(TemplateHelper.GetTemplatePath(ErrorTemplate, ErrorTemplateFolder));
            errorTemplate.SetTag("CheckoutHandler:ErrorMessage", msg);

            return new ContentOutputResult
            {
                Content = Render(order, errorTemplate)
            };
        }

        private OutputResult StateOk(Order order)
        {
            LogEvent(order, "State ok");

            if (Converter.ToBoolean(Context.Current.Request["paymentFailed"]))
            {
                order.TransactionStatus = "Failed";
                throw new Exception("Dibs payment failed.");
            }
            var paymentId = Context.Current.Request["paymentId"];
            UpdateOrderInfo(paymentId, order);

            return PassToCart(order);
        }

        #region Dibs API

        private void UpdateOrderInfo(string paymentId, Order order)
        {
            string errMessage = "Called create charge, but order is not set complete.";
            if (order.Complete)
            {
                return;
            }

            try
            {
                LogEvent(order, "Getting payment from Dibs by paymentId");

                var service = new DibsService(GetSecretKey(), GetApiUrl());
                DibsPaymentResponse paymentResponse = service.GetPayment(paymentId);

                if (paymentResponse != null)
                {
                    DibsPayment paymentInfo = paymentResponse.Payment;
                    LogEvent(order, "Payment succeeded with transaction number {0}", paymentId);

                    var paymentType = paymentInfo.PaymentDetails.PaymentType;
                    order.TransactionCardType = paymentInfo.PaymentDetails.PaymentMethod;

                    if (paymentType.ToLower() == "card")
                    {
                        order.TransactionCardNumber = HideCardNumber(paymentInfo.PaymentDetails.CardDetails.MaskedPan);
                    }
                    else // invoice
                    {
                        var invoiceDetails = paymentInfo.PaymentDetails.InvoiceDetails;
                        if (invoiceDetails != null)
                        {
                            order.TransactionCardNumber = HideCardNumber(invoiceDetails.InvoiceNumber);
                        }
                    }
                    var transactionAmount = paymentInfo.Summary.ReservedAmount;
                    var paymentAmount = paymentInfo.OrderDetails.Amount;
                    order.TransactionAmount = transactionAmount / 100d;
                    if (paymentAmount != transactionAmount)
                    {
                        order.TransactionStatus = "Failed";
                        errMessage = string.Format("Transaction amount doesn't match with order amount.");
                    }
                    else
                    {
                        order.TransactionStatus = "Succeeded";
                        bool updateReference = order.Id.StartsWith("CART", StringComparison.Ordinal);
                        SetOrderComplete(order, paymentId);

                        if (updateReference)
                        {
                            string url = GetApprovetUrl(GetBaseUrl(order));
                            service.UpdatePaymentReference(paymentId, order.Id, url);
                        }

                        if (AutoCapture)
                        {
                            try
                            {
                                LogEvent(order, "Start autocapture request.");
                                ChargePayment(order, paymentId, paymentAmount);
                                order.CaptureAmount = paymentAmount / 100d;
                                order.CaptureInfo.Message = "Autocapture successful";

                                Services.Orders.Save(order);
                            }
                            catch (Exception ex)
                            {
                                LogError(order, ex, $"Payment succeed, but autocapture request failed. {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    order.TransactionStatus = "Failed";
                    errMessage = string.Format("Error on finalizing payment. Cannot get payment info from Dibs.");
                }
            }
            finally
            {
                CheckoutDone(order);
            }

            if (!order.Complete)
            {
                throw new Exception(errMessage);
            }
        }

        private void ChargePayment(Order order, string paymentId, long chargeAmount)
        {
            var service = new DibsService(GetSecretKey(), GetApiUrl());
            CapturePaymentResponse charge = service.CapturePayment(paymentId, chargeAmount);

            if (!string.IsNullOrWhiteSpace(charge?.ChargeId))
            {
                order.TransactionNumber = charge.ChargeId;
                LogEvent(order, "Capturing order", DebuggingInfoType.CaptureResult);
                order.CaptureInfo = new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Success, "Capture successful");
            }
            else
            {
                throw new Exception("Charge request returned no charge info.");
            }
        }

        private string GetSecretKey()
        {
            return TestMode ? TestSecretKey : LiveSecretKey;
        }

        private string GetApiUrl()
        {
            var url = "api.dibspayment.eu";
            if (TestMode)
            {
                url = $"test.{url}";
            }
            url = $"https://{url}";
            return url;
        }

        private static string GetApprovetUrl(string baseUrl)
        {
            var baseUri = new UriBuilder(baseUrl);
            var queryToAppend = "action=Approve";
            if (baseUri.Query != null && baseUri.Query.Length > 1)
                baseUri.Query = baseUri.Query[1..] + "&" + queryToAppend;
            else
                baseUri.Query = queryToAppend;

            return baseUri.Uri.ToString();
        }

        #endregion

        #region IRemoteCapture

        /// <summary>
        /// Send capture request to transaction service
        /// </summary>
        /// <param name="order">Order to be captured</param>
        /// <returns>Response from transaction service</returns>
        public OrderCaptureInfo Capture(Order order)
        {
            var amount = order.Price.PricePIP;

            // Check order
            if (order == null)
            {
                LogError(null, "Order not set");
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, "Order not set");
            }
            else if (string.IsNullOrEmpty(order.Id))
            {
                LogError(null, "Order id not set");
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, "Order id not set");
            }
            else if (string.IsNullOrEmpty(order.TransactionNumber))
            {
                LogError(null, "Transaction number not set");
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, "Transaction number not set");
            }
            else if (amount > order.Price.PricePIP)
            {
                LogError(null, "Amount to capture should be less or equal to order total");
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, "Amount to capture should be less or equal to order total");
            }

            try
            {
                LogEvent(order, "Start autocapture request.");
                ChargePayment(order, order.TransactionNumber, amount);

                double capturedAmount = amount / 100d;

                if (order.Price.PricePIP == amount)
                    LogEvent(order, string.Format("Message=\"{0}\" Amount=\"{1:f2}\"", "Capture successful", capturedAmount), DebuggingInfoType.CaptureResult);
                else
                {
                    LogEvent(order, string.Format("Message=\"{0}\" Amount=\"{1:f2}\"", "Split capture (final)", capturedAmount), DebuggingInfoType.CaptureResult);
                    order.CaptureInfo.Message = "Split capture successful";
                }

                return order.CaptureInfo;
            }
            catch (Exception ex)
            {
                LogError(order, ex, "Unexpected error during capture: {0}", ex.Message);
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, ex.Message);
            }
        }

        /// <summary>
        /// Shows if capture supported
        /// </summary>
        /// <param name="order">This object is not used in current implementation</param>
        /// <returns>This method always return 'true' value</returns>
        public bool CaptureSupported(Order order)
        {
            return true;
        }

        /// <summary>
        /// Shows if partial capture of the order supported
        /// </summary>
        /// <param name="order">Not used</param>
        /// <returns>Always returns true</returns>
        public bool SplitCaptureSupported(Order order)
        {
            return false;
        }

        #endregion

        #region IFullReturn

        public void FullReturn(Order order)
        {
            ProceedReturn(order);
        }

        private void ProceedReturn(Order order)
        {
            // Check order
            if (order == null)
            {
                LogError(null, "Order not set");
                return;
            }

            if (order.CaptureInfo == null || order.CaptureInfo.State != OrderCaptureInfo.OrderCaptureState.Success || order.CaptureAmount <= 0.00)
            {
                OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, "Order must be captured before return.", order.CaptureAmount, order);
                LogError(null, "Order must be captured before return.");
                return;
            }

            var errorMessage = Refund(order);
            if (string.IsNullOrEmpty(errorMessage))
            {
                OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.FullyReturned, "Dibs has refunded payment.", order.CaptureAmount, order);
            }
            else
            {
                OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, errorMessage, order.CaptureAmount, order);
            }
        }

        private string Refund(Order order)
        {
            string errorMessage;
            try
            {

                if (string.IsNullOrEmpty(order.Id))
                {
                    errorMessage = "Order id not set";
                    LogError(null, errorMessage);
                    return errorMessage;
                }

                if (string.IsNullOrEmpty(order.TransactionNumber))
                {
                    errorMessage = "Transaction number not set";
                    LogError(null, errorMessage);
                    return errorMessage;
                }

                long refundAmount = PriceHelper.ConvertToPIP(order.Currency, order.CaptureAmount);
                var service = new DibsService(GetSecretKey(), GetApiUrl());
                RefundPaymentResponse returnResponse = service.RefundPayment(order.TransactionNumber, refundAmount);

                LogEvent(order, "Remote return id: {0}", returnResponse.RefundId);
                LogEvent(order, "Return successful", DebuggingInfoType.ReturnResult);
                return null;
            }
            catch (Exception ex)
            {
                errorMessage = $"Unexpected error during return: {ex.Message}";
                LogError(order, ex, errorMessage);
                return errorMessage;
            }
        }

        #endregion

        #region ICancelOrder

        public bool CancelOrder(Order order)
        {
            return Cancel(order);
        }

        private bool Cancel(Order order)
        {
            try
            {

                if (string.IsNullOrEmpty(order.Id))
                {
                    LogError(null, "Order id not set");
                    return false;
                }

                if (string.IsNullOrEmpty(order.TransactionNumber))
                {
                    LogError(null, "Transaction number not set");
                    return false;
                }

                var service = new DibsService(GetSecretKey(), GetApiUrl());
                service.CancelPayment(order.TransactionNumber, order.Price.PricePIP);
                LogEvent(order, "Order has been cancelled.");
                return true;
            }
            catch (Exception ex)
            {
                LogError(order, ex, $"Unexpected error during cancel: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region IParameterOptions

        /// <summary>
        /// Gets options according to behavior mode
        /// </summary>
        /// <param name="behaviorMode"></param>
        /// <returns>Key-value pairs of settings</returns>
        public IEnumerable<ParameterOption> GetParameterOptions(string parameterName)
        {
            try
            {
                switch (parameterName)
                {
                    case "Language":
                        return new List<ParameterOption>
                        {
                            new("English", "en-GB"),
                            new("Swedish", "sv-SE"),
                            new("Norwegian Bokmål", "nb-NO"),
                            new("Danish", "da-DK")
                        };

                    case "WindowMode":
                        return new List<ParameterOption>
                        {
                            new("Redirect", WindowModes.Redirect),
                            new("Embedded", WindowModes.Embedded)
                        };

                    default:
                        throw new ArgumentException(string.Format("Unknown dropdown name: '{0}'", parameterName));
                }

            }
            catch (Exception ex)
            {
                LogError(null, ex, "Unhandled exception with message: {0}", ex.Message);
                return null;
            }
        }

        #endregion

        #region RenderInlineForm

        public override string RenderInlineForm(Order order)
        {
            if (RenderInline)
            {
                LogEvent(order, "Render inline form");
                var formTemplate = new Template(TemplateHelper.GetTemplatePath(PostTemplate, PostTemplateFolder));
                var outputResult = RenderPaymentForm(order, formTemplate);

                if (outputResult is ContentOutputResult paymentFormData)
                    return paymentFormData.Content;
                if (outputResult is RedirectOutputResult)
                    return "Unhandled exception. Please see logs to find the problem.";
            }

            return string.Empty;
        }

        private OutputResult RenderPaymentForm(Order order, Template formTemplate, bool headless = false, string? receiptUrl = null)
        {
            try
            {
                string baseUrl = GetBaseUrl(order);
                string approvetUrl = GetApprovetUrl(baseUrl);

                var service = new DibsService(GetSecretKey(), GetApiUrl());
                var newPayment = service.CreatePayment(order, new()
                {
                    BaseUrl = baseUrl,
                    PrefillCustomerAddress = PrefillCustomerAddress,
                    ReceiptUrl = receiptUrl,
                    ApprovetUrl = approvetUrl,
                    SetB2bAsDefault = SetB2bAsDefault,
                    SupportB2b = SupportB2b,
                    TermsPage = TermsPage,
                    WindowMode = windowMode
                });

                LogEvent(order, "Payment created.");

                var key = LiveCheckoutKey;
                var src = "checkout.dibspayment.eu/v1/checkout.js?v=1";
                if (TestMode)
                {
                    key = TestCheckoutKey;
                    src = $"test.{src}";
                }
                src = $"https://{src}";

                var hostedPageUrl = string.Empty;
                if (!string.IsNullOrWhiteSpace(newPayment.HostedPaymentPageUrl))
                {
                    hostedPageUrl = $"{newPayment.HostedPaymentPageUrl}&language={Language}";
                }

                var formValues = new Dictionary<string, string>
                {
                    { "hostedPaymentPageUrl", hostedPageUrl },
                    { "paymentId", newPayment.PaymentId },
                    { "checkoutKey", key },
                    { "checkoutSrc", src },
                    { "language", Language },
                    { "invoiceApproveUrl", approvetUrl }
                };

                foreach (var formValue in formValues)
                {
                    formTemplate.SetTag(string.Format("DibsEasy.{0}", formValue.Key), formValue.Value);
                }

                // Render and return
                return new ContentOutputResult
                {
                    Content = Render(order, formTemplate)
                };
            }
            catch (Exception ex)
            {
                LogError(order, ex, "Unhandled exception with message: {0}", ex.Message);
                return PrintErrorTemplate(order, ex.Message);
            }
        }

        #endregion
    }
}
