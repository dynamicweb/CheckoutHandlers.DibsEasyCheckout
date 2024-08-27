using Dynamicweb.Core;
using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;
using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;
using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Ecommerce.Units;
using Dynamicweb.Ecommerce.Variants;
using Dynamicweb.Environment.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Service;

internal sealed class DibsService
{
    public string ApiUrl { get; set; }

    public string SecretKey { get; set; }

    public DibsService(string secretKey, string apiUrl)
    {
        SecretKey = secretKey;
        ApiUrl = apiUrl;
    }

    public CreatePaymentResponse CreatePayment(Order order, CreatePaymentParameters parameters)
    {
        PaymentRequest request = ConvertOrder(order, parameters);

        string response = DibsRequest.SendRequest(ApiUrl, SecretKey, new()
        {
            CommandType = ApiCommand.CreatePayment,
            Data = request
        });

        return Converter.Deserialize<CreatePaymentResponse>(response);
    }

    public DibsPaymentResponse GetPayment(string paymentId)
    {

        string response = DibsRequest.SendRequest(ApiUrl, SecretKey, new()
        {
            CommandType = ApiCommand.GetPayment,
            OperatorId = paymentId
        });

        return Converter.Deserialize<DibsPaymentResponse>(response);
    }

    public CapturePaymentResponse CapturePayment(string paymentId, long amount)
    {
        string response = DibsRequest.SendRequest(ApiUrl, SecretKey, new()
        {
            CommandType = ApiCommand.CapturePayment,
            OperatorId = paymentId,
            Data = new CapturePaymentRequest
            {
                Amount = amount
            }
        });

        return Converter.Deserialize<CapturePaymentResponse>(response);
    }

    public RefundPaymentResponse RefundPayment(string paymentId, long amount)
    {
        string response = DibsRequest.SendRequest(ApiUrl, SecretKey, new()
        {
            CommandType = ApiCommand.RefundPayment,
            OperatorId = paymentId,
            Data = new RefundPaymentRequest
            {
                Amount = amount
            }
        });

        return Converter.Deserialize<RefundPaymentResponse>(response);
    }

    public void CancelPayment(string paymentId, long amount)
    {
        string response = DibsRequest.SendRequest(ApiUrl, SecretKey, new()
        {
            CommandType = ApiCommand.CancelPayment,
            OperatorId = paymentId,
            Data = new CancelPaymentRequest
            {
                Amount = amount
            }
        });
    }

    private static PaymentRequest ConvertOrder(Order order, CreatePaymentParameters parameters)
    {
        long linesTotalPIP = 0;
        var lineItems = new List<LineItem>(order.OrderLines.Select(x => ConvertOrderLine(x, ref linesTotalPIP)));
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
            lineItems.Add(new()
            {
                Reference = "RoundingError",
                Name = "RoundingError",
                Quantity = 1.0,
                UnitPrice = roundingError,
                Unit = "unit",
                TaxRate = 0,
                TaxAmount = 0,
                GrossTotalAmount = roundingError,
                NetTotalAmount = roundingError
            });
        }

        ConsumerType supportedConsumerTypes = null;
        if (parameters.SupportB2b)
        {
            var defaultConsumerType = "B2C";
            if (parameters.SetB2bAsDefault)
            {
                defaultConsumerType = "B2B";
            }
            supportedConsumerTypes = new()
            {
                Default = defaultConsumerType,
                SupportedTypes = new[] { "B2C", "B2B" }
            };
        }

        string checkoutUrl = !string.IsNullOrEmpty(parameters.ReceiptUrl) ? parameters.ReceiptUrl : parameters.ApprovetUrl;
        bool isRedirectMode = parameters.WindowMode is WindowModes.Redirect;

        var payment = new PaymentRequest
        {
            Order = new()
            {
                Items = lineItems,
                Amount = order.Price.PricePIP,
                Currency = order.CurrencyCode,
                Reference = order.Id
            },
            Checkout = new()
            {
                Url = isRedirectMode ? null : checkoutUrl,
                ReturnUrl = isRedirectMode ? checkoutUrl : null,
                TermsUrl = GetTermsUrl(parameters.BaseUrl, parameters.TermsPage),
                IntegrationType = isRedirectMode ? "hostedPaymentPage" : "EmbeddedCheckout",
                MerchantHandlesConsumerData = parameters.PrefillCustomerAddress,
                Consumer = parameters.PrefillCustomerAddress ? GetConsumerData(order) : null,
                ConsumerType = supportedConsumerTypes
            },
            Notifications = new()
            {
                Webhooks =
                [
                    new()
                    {
                        EventName = "payment.checkout.completed",
                        Url = parameters.ApprovetUrl + "&callback=true",
                        Authorization = "myAuthorizationKey"
                    }
                ]
            }
        };
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

    private static Consumer GetConsumerData(Order order)
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

        return new()
        {
            Email = Converter.ToString(order.CustomerEmail),
            ShippingAddress = new()
            {
                AddressLine1 = isDeliveryAddressFilled ? order.DeliveryAddress : order.CustomerAddress,
                AddressLine2 = isDeliveryAddressFilled ? order.DeliveryAddress2 : order.CustomerAddress2,
                PostalCode = isDeliveryAddressFilled ? order.DeliveryZip : order.CustomerZip,
                City = isDeliveryAddressFilled ? order.DeliveryCity : order.CustomerCity,
                Country = countryCode3
            },
            PrivatePerson = string.IsNullOrEmpty(order.CustomerCompany) ? new()
            {
                FirstName = firstName,
                LastName = lastName
            } : null,
            Company = !string.IsNullOrEmpty(order.CustomerCompany) ? new()
            {
                Name = order.CustomerCompany,
                Contact = new()
                {
                    FirstName = firstName,
                    LastName = lastName
                }
            } : null
        };
    }

    private static LineItem ConvertOrderLine(OrderLine orderline, ref long linesTotalPIP)
    {
        var unitName = "pcs";
        if (!string.IsNullOrWhiteSpace(orderline.UnitId))
        {
            Product product = orderline.Product;
            var unit = GetStockUnitsSorted(product?.Id, product?.VariantId).FirstOrDefault(u => u.Id == orderline.UnitId);
            if (unit != null)
            {
                unitName = Services.VariantOptions.GetVariantOption(unit.Id)?.GetName(orderline.Order.LanguageId);
            }
        }
        return GetLineItem(orderline.Id, orderline.ProductName, orderline.Quantity, orderline.Price, orderline.UnitPrice, unitName, ref linesTotalPIP);
    }

    private static IEnumerable<VariantOption> GetStockUnitsSorted(string productId, string variantId)
    {
        if (string.IsNullOrEmpty(productId))
            return Enumerable.Empty<VariantOption>();

        var check = new HashSet<string>();
        var result = new List<VariantOption>();
        foreach (var stockUnit in Services.StockService.GetStockUnits(productId, variantId))
        {
            if (!check.Add(stockUnit.UnitId))
            {
                continue;
            }

            Unit unit = Services.Units.GetUnit(stockUnit.UnitId);

            if (unit is not null)
            {
                result.Add(new() { Id = unit.Id, GroupId = unit.Id });
            }
        }

        return result.OrderBy(v => v.SortOrder).ThenBy(v => v.Id);
    }


    private static LineItem GetLineItem(string reference, string name, double quantity, PriceInfo linePrice, PriceInfo unitPrice, string unitName, ref long linesTotalPIP)
    {
        linesTotalPIP += PriceHelper.ConvertToPIP(linePrice.Currency, linePrice.Price);
        return new()
        {
            Reference = reference,
            Name = EncodeAndTruncateText(name),
            Quantity = quantity,
            UnitPrice = PriceHelper.ConvertToPIP(unitPrice.Currency, unitPrice.PriceWithoutVAT),
            Unit = EncodeAndTruncateText(unitName),
            TaxRate = PriceHelper.ConvertToPIP(linePrice.Currency, linePrice.VATPercent),
            TaxAmount = PriceHelper.ConvertToPIP(linePrice.Currency, linePrice.VAT),
            GrossTotalAmount = PriceHelper.ConvertToPIP(linePrice.Currency, linePrice.PriceWithVAT),
            NetTotalAmount = PriceHelper.ConvertToPIP(linePrice.Currency, linePrice.PriceWithoutVAT)
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

    private static string GetTermsUrl(string baseUrl, string termsPage)
    {
        if (!string.IsNullOrWhiteSpace(termsPage) && !LinkHelper.IsLinkInternal(termsPage))
        {
            return termsPage;
        }
        else
        {
            var baseUri = new Uri(baseUrl);
            var pagePath = $"/{termsPage.TrimStart('/')}";
            return baseUrl.Replace(baseUri.PathAndQuery, pagePath);
        }
    }
}
