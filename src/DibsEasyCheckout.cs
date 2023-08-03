using Dynamicweb.Configuration;
using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Orders.Gateways;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Environment.Helpers;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.Rendering;
using Dynamicweb.SystemTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout
{
    /// <summary>
    /// DibsCheckout Payment Window Checkout Handler
    /// </summary>
    [
        AddInName("Dibs/Nets Easy checkout"),
        AddInDescription("Dibs/Nets Easy Checkout handler")
    ]
    public class DibsEasyCheckout : CheckoutHandler, IDropDownOptions, IRemoteCapture, ICancelOrder, IFullReturn
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

        internal enum WindowModes { Redirect, Embedded }
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
        [AddInParameter("Auto capture"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool AutoCapture { get; set; }

        [AddInParameter("Render inline form"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool RenderInline { get; set; }

        [AddInParameter("Prefill customer address"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool PrefillCustomerAddress { get; set; }

        [AddInParameter("Support B2B"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool SupportB2b { get; set; }

        [AddInParameter("Set B2B as default"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool SetB2bAsDefault { get; set; }

        [AddInParameter("Test mode"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool TestMode { get; set; } = true;

        #endregion

        /// <summary>
		/// Starts order checkout procedure
        /// </summary>
		/// <param name="order">Order to be checked out</param>
		/// <returns>String representation of template output</returns>
        public override string StartCheckout(Order order) => StartCheckout(order);

        public override string StartCheckout(Order order, bool headless = false, string receiptUrl = "", string cancelUrl = "")
        {
            LogEvent(order, "Checkout started");


            var formTemplate = new Template(TemplateHelper.GetTemplatePath(PostTemplate, PostTemplateFolder));
            return RenderPaymentForm(order, formTemplate);
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
                    baseUrl = $"{Context.Current.Request.Url.Scheme}://{Context.Current.Request.Url.Host}{portString}";

                var url = $"{baseUrl}/dwapi/ecommerce/carts/callback?{OrderIdRequestName}={order.Id}";
                return url;
            }

            // Base url
            return string.Format("{0}://{1}{2}/Default.aspx?{3}{4}={5}", Context.Current.Request.Url.Scheme, Context.Current.Request.Url.Host, portString, pageId, OrderIdRequestName, order.Id);
    }

        #region payment request

        private object ConvertOrder(Order order, bool headless = false, string? receiptUrl = null)
        {
            long linesTotalPIP = 0;
            var lineItems = new List<object>(order.OrderLines.Select(x => ConvertOrderLine(x, ref linesTotalPIP)));
            if (order.ShippingFee.Price >= 0.01)
            {
                lineItems.Add(GetLineItem(order.ShippingMethodId, "ShippingFee", 1.0, order.ShippingFee, order.ShippingFee, "unit", ref linesTotalPIP));
            }
            if (order.PaymentFee.Price >= 0.01)
            {
                lineItems.Add(GetLineItem(order.PaymentMethodId, "PaymentFee", 1.0, order.PaymentFee, order.PaymentFee, "unit", ref linesTotalPIP));
            }

            var orderPriceWithoutVatPIP = order.Price.PricePIP;
            if (linesTotalPIP != orderPriceWithoutVatPIP)
            {
                var roundingError = orderPriceWithoutVatPIP - linesTotalPIP;
                lineItems.Add(new
                {
                    reference = "RoundingError",
                    name = "RoundingError",
                    quantity = 1.0,
                    unitPrice = roundingError,
                    unit = "unit",
                    taxRate = 0,
                    taxAmount = 0,
                    grossTotalAmount = roundingError,
                    netTotalAmount = roundingError
                });
            }

            object supportedConsumerTypes = null;
            if (SupportB2b)
            {
                var defaultConsumerType = "B2C";
                if (SetB2bAsDefault)
                {
                    defaultConsumerType = "B2B";
                }
                supportedConsumerTypes = new
                {
                    @default = defaultConsumerType,
                    supportedTypes = new[] { "B2C", "B2B" }
                };
            }

            var payment = new
            {
                order = new
                {
                    items = lineItems,
                    amount = order.Price.PricePIP,
                    currency = order.CurrencyCode,
                    reference = order.Id
                },
                checkout = new
                {
                    url = windowMode == WindowModes.Redirect ? null : receiptUrl ?? GetApprovetUrl(order),
                    returnUrl = windowMode == WindowModes.Redirect ? receiptUrl ?? GetApprovetUrl(order) : null,
                    termsurl = GetTermsUrl(order),
                    integrationType = windowMode == WindowModes.Redirect ? "hostedPaymentPage" : "EmbeddedCheckout",
                    merchantHandlesConsumerData = PrefillCustomerAddress,
                    consumer = PrefillCustomerAddress ? GetConsumerData(order) : null,
                    consumerType = supportedConsumerTypes
                },
                notifications = new
                {
                    webhooks = new[]
                    {
                        new{
                            eventName = "payment.checkout.completed",
                            url = GetApprovetUrl(order, headless) + "&callback=true",
                            authorization = "myAuthorizationKey"
                        }
                    }
                }
            };
            LogEvent(order, $"Serialized payment request: {Converter.SerializeCompact(payment)}");
            LogEvent(order, $"Serialized webhooks: {Converter.SerializeCompact(payment.notifications.webhooks)}");
            return payment;
        }

        //     "notifications": {
        //    "webhooks": [
        //        {
        //            "eventName": "payment.created",
        //            "url": "https://example.com/api/WebhookState",
        //            "authorization": "myAuthorizationKey"
        //        },
        //        {
        //            "eventName": "payment.checkout.completed",
        //            "url": "https://example.com/api/WebhookState",
        //            "authorization": "myAuthorizationKey"
        //        }
        //    ]
        //}

        private object GetConsumerData(Order order)
        {
            string firstName = Converter.ToString(order.CustomerFirstName);
            string lastName = Converter.ToString(order.CustomerSurname);
            var customerName = Converter.ToString(order.CustomerName).Trim();
            var isDeliveryAddressFilled = !string.IsNullOrEmpty(order.DeliveryCountryCode);
            var countryCode3 = Services.Countries.GetCountry(isDeliveryAddressFilled ? order.DeliveryCountryCode : order.CustomerCountryCode)?.Code3;

            var delimeterPosition = customerName.IndexOf(' ');
            if (string.IsNullOrWhiteSpace(firstName))
            {
                firstName = delimeterPosition > -1 ? customerName.Substring(0, delimeterPosition) : customerName;
            }
            if (string.IsNullOrWhiteSpace(lastName))
            {
                lastName = delimeterPosition > -1 ? customerName.Substring(delimeterPosition + 1) : customerName;
            }

            return new
            {
                email = Converter.ToString(order.CustomerEmail),
                shippingAddress = new
                {
                    addressLine1 = isDeliveryAddressFilled ? order.DeliveryAddress : order.CustomerAddress,
                    addressLine2 = isDeliveryAddressFilled ? order.DeliveryAddress2 : order.CustomerAddress2,
                    postalCode = isDeliveryAddressFilled ? order.DeliveryZip : order.CustomerZip,
                    city = isDeliveryAddressFilled ? order.DeliveryCity : order.CustomerCity,
                    country = countryCode3
                },
                privatePerson = string.IsNullOrEmpty(order.CustomerCompany) ? new
                {
                    firstName = firstName,
                    lastName = lastName
                } : null,
                company = !string.IsNullOrEmpty(order.CustomerCompany) ? new
                {
                    name = order.CustomerCompany,
                    contact = new
                    {
                        firstName = firstName,
                        lastName = lastName
                    }
                } : null
            };
        }

        private object ConvertOrderLine(OrderLine orderline, ref long linesTotalPIP)
        {
            var unitName = "pcs";
            if (!string.IsNullOrWhiteSpace(orderline.UnitId))
            {
                var unit = orderline.Product?.GetUnitList(orderline.Order.LanguageId).FirstOrDefault(u => u.Id == orderline.UnitId);
                if (unit != null)
                {
                    unitName = Services.VariantOptions.GetVariantOption(unit.Id)?.GetName(orderline.Order.LanguageId);
                }
            }
            return GetLineItem(orderline.Id, orderline.ProductName, orderline.Quantity, orderline.Price, orderline.UnitPrice, unitName, ref linesTotalPIP);
        }

        private static object GetLineItem(string reference, string name, double quantity, PriceInfo linePrice, PriceInfo unitPrice, string unitName, ref long linesTotalPIP)
        {
            linesTotalPIP += PriceHelper.ConvertToPIP(linePrice.Currency, linePrice.Price);
            return new
            {
                reference = reference,
                name = EncodeAndTruncateText(name),
                quantity = quantity,
                unitPrice = PriceHelper.ConvertToPIP(unitPrice.Currency, unitPrice.PriceWithoutVAT),
                unit = EncodeAndTruncateText(unitName),
                taxRate = PriceHelper.ConvertToPIP(linePrice.Currency, linePrice.VATPercent),
                taxAmount = PriceHelper.ConvertToPIP(linePrice.Currency, linePrice.VAT),
                grossTotalAmount = PriceHelper.ConvertToPIP(linePrice.Currency, linePrice.PriceWithVAT),
                netTotalAmount = PriceHelper.ConvertToPIP(linePrice.Currency, linePrice.PriceWithoutVAT)
            };
        }

        private static string EncodeAndTruncateText(string name)
        {
            //https://tech.dibspayment.com/easy/api/paymentapi "About the parameters"
            name = HttpUtility.HtmlEncode(name);
            if (name.Length > 128)
            {
                name = name.Substring(0, 128);
            }
            return name;
        }

        private string GetApprovetUrl(Order order, bool headless = false)
        {
            return string.Format("{0}&action={1}", GetBaseUrl(order, headless), "Approve");
        }

        private string GetTermsUrl(Order order)
        {
            if (!LinkHelper.IsLinkInternal(TermsPage))
            {
                return TermsPage;
            }
            else
            {
                var baseUrl = GetBaseUrl(order);
                var baseUri = new Uri(baseUrl);
                var pagePath = $"/{TermsPage.TrimStart('/')}";
                return baseUrl.Replace(baseUri.PathAndQuery, pagePath);
            }
        }

        /// <summary>
        /// Redirects user to to Dibs CheckoutHandler step
        /// </summary>
        /// <param name="order">Order for processing</param>
        /// <returns>String representation of template output</returns>
        public override string Redirect(Order order)
        {
            try
            {
                LogEvent(null, "Redirected to Dibs CheckoutHandler");

                string action = Context.Current.Request["action"];
                switch (action)
                {
                    case "Approve":
                        return StateOk(order);

                    case "ApproveInvoce":
                        return StateOk(order);

                    default:
                        string msg = string.Format("Unknown Dibs state: '{0}'", action);
                        LogError(order, msg);
                        return PrintErrorTemplate(order, msg);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogError(order, ex, "Unhandled exception with message: {0}", ex.Message);
                return PrintErrorTemplate(order, ex.Message);
            }
        }

        #endregion
        private string PrintErrorTemplate(Order order, string msg)
        {
            LogEvent(order, "Printing error template");

            order.TransactionAmount = 0;
            order.TransactionStatus = "Failed";
            order.Errors.Add(msg);
            Services.Orders.Save(order);

            Services.Orders.DowngradeToCart(order);
            order.CartV2StepIndex = 0;
            order.TransactionStatus = "";
            Dynamicweb.Ecommerce.Common.Context.SetCart(order);

            if (string.IsNullOrWhiteSpace(ErrorTemplate))
            {
                RedirectToCart(order);
            }

            var errorTemplate = new Template(TemplateHelper.GetTemplatePath(ErrorTemplate, ErrorTemplateFolder));
            errorTemplate.SetTag("CheckoutHandler:ErrorMessage", msg);

            return Render(order, errorTemplate);
        }

        private string StateOk(Order order)
        {
            LogEvent(order, "State ok");

            if (Converter.ToBoolean(Context.Current.Request["paymentFailed"]))
            {
                order.TransactionStatus = "Failed";
                throw new Exception("Dibs payment failed.");
            }
            string paymentId = Context.Current.Request["paymentId"];

            UpdateOrderInfo(paymentId, order);

            RedirectToCart(order);
            return null;
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
                var payment = ExecutePostRequest($"payments/{paymentId}", null, "GET");

                if (payment != null)
                {
                    var paymentInfo = (dynamic)payment["payment"];
                    LogEvent(order, "Payment succeeded with transaction number {0}", paymentId);

                    var paymentType = Converter.ToString(paymentInfo["paymentDetails"]["paymentType"]);
                    order.TransactionCardType = Converter.ToString(paymentInfo["paymentDetails"]["paymentMethod"]);

                    if (paymentType.ToLower() == "card")
                    {
                        order.TransactionCardNumber = HideCardNumber(Converter.ToString(paymentInfo["paymentDetails"]["cardDetails"]["maskedPan"]));
                    }
                    else // invoice
                    {
                        var invoiceDetails = paymentInfo["paymentDetails"]["invoiceDetails"];
                        if (invoiceDetails != null)
                        {
                            order.TransactionCardNumber = HideCardNumber(Converter.ToString(paymentInfo["paymentDetails"]["invoiceDetails"]["ocr"]));
                        }
                    }
                    var transactionAmount = Converter.ToInt32(paymentInfo["summary"]["reservedAmount"]);
                    var paymentAmount = Converter.ToInt32(paymentInfo["orderDetails"]["amount"]);
                    order.TransactionAmount = transactionAmount / 100d;
                    if (paymentAmount != transactionAmount)
                    {
                        order.TransactionStatus = "Failed";
                        errMessage = string.Format("Transaction amount doesn't match with order amount.");
                    }
                    else
                    {
                        order.TransactionStatus = "Succeeded";

                        SetOrderComplete(order, paymentId);

                        if (AutoCapture)
                        {

                            try
                            {
                                LogEvent(order, "Start autocapture request.");
                                ChargePayment(order, paymentId, paymentAmount);
                                order.CaptureAmount = paymentAmount / 100d;
                                order.CaptureInfo.Message = "Autocapture successful";

                                var service = new OrderService();
                                service.Save(order);
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
            var chargeRequest = new
            {
                amount = chargeAmount
            };
            var charge = ExecutePostRequest($"payments/{paymentId}/CHARGES", chargeRequest);
            if (charge != null || !charge.ContainsKey("chargeId") || string.IsNullOrWhiteSpace(Converter.ToString(charge["chargeId"])))
            {
                var chargedId = Converter.ToString(charge["chargeId"]);
                order.GatewayUniqueId = chargedId;
                LogEvent(order, "Capturing order", DebuggingInfoType.CaptureResult);
                order.CaptureInfo = new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Success, "Capture successful");
            }
            else
            {
                throw new Exception("Charge request returned no charge info.");
            }
        }

        private Dictionary<string, object> ExecutePostRequest(string function, object body, string method = "POST")
        {
            HttpWebRequest request = WebRequest.CreateHttp(string.Format("{0}/v1/{1}", GetApiUrl(), function));
            request.Method = method;
            request.Timeout = 90 * 1000;
            request.ContentType = "application/json"; // "application/json;charset=UTF-8"; //   
            request.Accept = "application/json, text/plain, */*";
            request.Headers.Set(HttpRequestHeader.Authorization, GetSecretKey());

            if (body != null)
            {
                var strBody = Converter.SerializeCompact(body);

                byte[] bytes = Encoding.UTF8.GetBytes(strBody);
                request.ContentLength = bytes.Length;
                using (Stream st = request.GetRequestStream())
                {
                    st.Write(bytes, 0, bytes.Length);
                }
            }

            string result = ExecuteRequest(request);

            return Converter.DeserializeCompact<Dictionary<string, object>>(result); ;
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

        private string ExecuteRequest(WebRequest request)
        {
            string result = null;
            try
            {
                using (var response = request.GetResponse())
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        result = streamReader.ReadToEnd();
                    }
                }
            }
            catch (WebException wexc)
            {
                if (wexc.Response != null)
                {
                    string response;
                    using (StreamReader sr = new StreamReader(wexc.Response.GetResponseStream(), Encoding.UTF8))
                    {
                        response = sr.ReadToEnd();
                    }
                    var json_error = Converter.DeserializeCompact<Dictionary<string, object>>(response);
                    object errors = null;
                    var errorMessage = json_error.TryGetValue("errors", out errors) ? errors.ToString() : $"{wexc.Message} {response}";
                    errorMessage = $"Payment request failed with following errors: {errorMessage}";
                    throw new Exception(errorMessage);
                }
                throw;
            }

            return result;
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

                if (order.Price.PricePIP == amount)
                {
                    LogEvent(order, "Capture successful", DebuggingInfoType.CaptureResult);
                }
                else
                {
                    LogEvent(order, String.Format("Message=\"{0}\" Amount=\"{1:f2}\"", "Split capture(final)", amount / 100f), DebuggingInfoType.CaptureResult);
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

                if (string.IsNullOrEmpty(order.TransactionNumber) || string.IsNullOrEmpty(order.GatewayUniqueId))
                {
                    errorMessage = "Transaction number not set";
                    LogError(null, errorMessage);
                    return errorMessage;
                }

                var refundRequest = new
                {
                    amount = PriceHelper.ConvertToPIP(order.Currency, order.CaptureAmount)
                };

                var returnResponce = ExecutePostRequest($"CHARGES/{order.GatewayUniqueId}/REFUNDS", refundRequest);
                LogEvent(order, "Remote return id: {0}", returnResponce["refundId"]);
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

                var cancelRequest = new
                {
                    amount = order.Price.PricePIP
                };

                var returnResponce = ExecutePostRequest($"PAYMENTS/{order.TransactionNumber}/CANCELS", cancelRequest);
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

        #region IDropDownOptions

        /// <summary>
        /// Gets options according to behavior mode
        /// </summary>
        /// <param name="behaviorMode"></param>
        /// <returns>Key-value pairs of settings</returns>
        public Hashtable GetOptions(string behaviorMode)
        {
            try
            {
                switch (behaviorMode)
                {
                    case "Language":
                        return new Hashtable {
                            { "en-GB", "English" },
                            { "sv-SE", "Swedish" },
                            { "nb-NO", "Norwegian Bokmål" },
                            { "da-DK", "Danish" }
                        };

                    case "WindowMode":
                        return new Hashtable
                                   {
                                       {WindowModes.Redirect.ToString(), "Redirect"},
                                       {WindowModes.Embedded.ToString(), "Embedded"}
                                   };

                    default:
                        throw new ArgumentException(string.Format("Unknown dropdown name: '{0}'", behaviorMode));
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
                return RenderPaymentForm(order, formTemplate);
            }

            return string.Empty;
        }

        private string RenderPaymentForm(Order order, Template formTemplate, bool headless = false, string? receiptUrl = null)
        {
            try
            {
                var paymentRequest = ConvertOrder(order, headless, receiptUrl);
                var newPayment = ExecutePostRequest("payments", paymentRequest);

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
                if (newPayment.TryGetValue("hostedPaymentPageUrl", out var url))
                {
                    hostedPageUrl = $"{url}&language={Language}";
                }

                var formValues = new Dictionary<string, string>
                {
                    { "hostedPaymentPageUrl", hostedPageUrl },
                    { "paymentId", newPayment["paymentId"].ToString() },
                    { "checkoutKey", key },
                    { "checkoutSrc", src },
                    { "language", Language },
                    { "invoiceApproveUrl", GetApprovetUrl(order, headless) }
                };

                foreach (var formValue in formValues)
                {
                    formTemplate.SetTag(string.Format("DibsEasy.{0}", formValue.Key), formValue.Value);
                }

                // Render and return
                return Render(order, formTemplate);
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
