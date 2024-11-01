using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Ecommerce.Units;
using Dynamicweb.Ecommerce.Variants;
using Dynamicweb.Environment.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Service;

internal sealed class DibsCreateRequestService
{
    public Order Order { get; set; }

    public DibsCreateRequestService(Order order)
    {
        Order = order;
    }

    public PaymentRequest CreatePaymentRequest(CreatePaymentParameters parameters)
    {
        bool prefillCustomerAddress = parameters.PrefillCustomerAddress;
        bool isRedirectMode = parameters.WindowMode is WindowModes.Redirect;
        string checkoutUrl = !string.IsNullOrEmpty(parameters.ReceiptUrl) ? parameters.ReceiptUrl : parameters.ApprovetUrl;

        Consumer consumer = null;
        if (prefillCustomerAddress)
        {
            consumer = TryToGetConsumer();
            if (consumer is null)
                prefillCustomerAddress = false;
        }

        var payment = new PaymentRequest
        {
            Order = new()
            {
                Items = ConvertOrderLines(Order.OrderLines),
                Amount = Order.Price.PricePIP,
                Currency = Order.CurrencyCode,
                Reference = Order.Id
            },
            Checkout = new()
            {
                Url = isRedirectMode ? null : checkoutUrl,
                ReturnUrl = isRedirectMode ? checkoutUrl : null,
                TermsUrl = GetTermsUrl(parameters.BaseUrl, parameters.TermsPage),
                IntegrationType = isRedirectMode ? "HostedPaymentPage" : "EmbeddedCheckout",
                MerchantHandlesConsumerData = prefillCustomerAddress,
                Consumer = consumer,
                ConsumerType = GetConsumerType(parameters.SupportB2b, parameters.SetB2bAsDefault),
                Shipping = parameters.EnableBillingAddress ? new() { EnableBillingAddress = true } : null
            },
            Notifications = new()
            {
                Webhooks =
                [
                    new()
                    {
                        EventName = "payment.checkout.completed",
                        Url = $"{parameters.ApprovetUrl}&callback=true",
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

    private string GetTermsUrl(string baseUrl, string termsPage)
    {
        if (!string.IsNullOrWhiteSpace(termsPage) && !LinkHelper.IsLinkInternal(termsPage))
            return termsPage;

        var baseUri = new Uri(baseUrl);
        var pagePath = $"/{termsPage.TrimStart('/')}";

        return baseUrl.Replace(baseUri.PathAndQuery, pagePath);
    }

    private void Log(string message)
    {
        if (Order is null)
            return;

        Services.OrderDebuggingInfos.Save(Order, message, typeof(DibsEasyCheckout).FullName, DebuggingInfoType.Undefined);
    }

    #region LineItems

    private IEnumerable<LineItem> ConvertOrderLines(IEnumerable<OrderLine> orderLines)
    {
        long linesTotalPIP = 0;

        List<LineItem> lineItems = orderLines.Select(x => ConvertOrderLine(x, ref linesTotalPIP)).ToList();
        if (Order.ShippingFee.Price >= 0.01)
            lineItems.Add(GetLineItem(Order.ShippingMethodId, "ShippingFee", 1.0, Order.ShippingFee, Order.ShippingFee, "unit", ref linesTotalPIP));
        if (Order.PaymentFee.Price >= 0.01)
            lineItems.Add(GetLineItem(Order.PaymentMethodId, "PaymentFee", 1.0, Order.PaymentFee, Order.PaymentFee, "unit", ref linesTotalPIP));

        if (linesTotalPIP != Order.Price.PricePIP)
        {
            long roundingError = Order.Price.PricePIP - linesTotalPIP;
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

        return lineItems;
    }


    private LineItem ConvertOrderLine(OrderLine orderLine, ref long linesTotalPIP)
    {
        string unitName = "pcs";
        if (!string.IsNullOrWhiteSpace(orderLine.UnitId))
        {
            Product product = orderLine.Product;
            VariantOption unit = GetStockUnitsSorted(product?.Id, product?.VariantId).FirstOrDefault(variantOption => variantOption.Id.Equals(orderLine.UnitId, StringComparison.OrdinalIgnoreCase));
            if (unit is not null)
                unitName = Services.VariantOptions.GetVariantOption(unit.Id)?.GetName(orderLine.Order.LanguageId);
        }

        return GetLineItem(orderLine.Id, orderLine.ProductName, orderLine.Quantity, orderLine.Price, orderLine.UnitPrice, unitName, ref linesTotalPIP);
    }

    private IEnumerable<VariantOption> GetStockUnitsSorted(string productId, string variantId)
    {
        if (string.IsNullOrEmpty(productId))
            return [];

        var result = new Dictionary<string, VariantOption>();
        foreach (var stockUnit in Services.StockService.GetStockUnits(productId, variantId))
        {
            if (result.ContainsKey(stockUnit.UnitId))
                continue;

            if (Services.Units.GetUnit(stockUnit.UnitId) is Unit unit)
            {
                var option = new VariantOption
                {
                    Id = unit.Id,
                    GroupId = unit.Id
                };
                result.Add(stockUnit.UnitId, option);
            }
        }

        return result.Values
            .OrderBy(variantOption => variantOption.SortOrder)
            .ThenBy(variantOption => variantOption.Id);
    }


    private LineItem GetLineItem(string reference, string name, double quantity, PriceInfo linePrice, PriceInfo unitPrice, string unitName, ref long linesTotalPIP)
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

    private string EncodeAndTruncateText(string name)
    {
        name = WebUtility.HtmlEncode(name);
        if (name.Length > 128)
            name = name.Substring(0, 128);

        return name;
    }

    #endregion

    #region Consumer

    private ConsumerType GetConsumerType(bool supportB2B, bool setB2BAsDefault)
    {
        if (!supportB2B)
        {
            //B2C is used by Dibs as default setting
            return null;
        }

        return new()
        {
            Default = setB2BAsDefault ? "B2B" : "B2C",
            SupportedTypes = ["B2C", "B2B"]
        };
    }

    private Consumer TryToGetConsumer()
    {
        var consumer = new Consumer
        {
            Email = Order.CustomerEmail
        };

        HandleAddress(consumer);
        HandleCustomerCompanyOrPerson(consumer);

        if (!IsConsumerHasData())
        {
            Log("The Prefill customer address is not applied because the required customer information is missing. For prefill, there should be at least one of following data: customer email, shipping address, billing address, customer first name/last name, customer company.");
            return null;
        }

        return consumer;

        void HandleAddress(Consumer consumer)
        {
            Address shippingAddress = GetShippingAddress();
            Address billingAddress = GetBillingAddress();

            if (shippingAddress is null)
                Log("The Dibs shipping address could not be prefilled, because some of the following data was missing from the order: delivery country code, delivery address, delivery postal code, and delivery city.");
            if (billingAddress is null)
                Log("The Dibs billing address could not be prefilled, because some of the following data was missing from the order: customer country code, customer address, customer zip, customer city");

            consumer.ShippingAddress = shippingAddress;
            consumer.BillingAddress = billingAddress;
        }

        void HandleCustomerCompanyOrPerson(Consumer consumer)
        {
            Contact contact = GetContact();
            if (contact is null)
                Log("The Dibs customer name could not be pre-filled, because of customer first name or customer last name was not set.");

            if (!string.IsNullOrWhiteSpace(Order.CustomerCompany))
            {
                consumer.Company = new()
                {
                    Name = Order.CustomerCompany,
                    Contact = contact
                };
            }
            else
                consumer.PrivatePerson = contact;
        }

        bool IsConsumerHasData()
        {
            bool isEmailFilled = !string.IsNullOrWhiteSpace(consumer.Email);
            bool isShippingAddressFilled = consumer.ShippingAddress is not null;
            bool isBillingAddressFilled = consumer.BillingAddress is not null;
            bool isCompanyFilled = consumer.Company is not null;
            bool isPrivatePersonFilled = consumer.PrivatePerson is not null;

            return isEmailFilled || isShippingAddressFilled || isBillingAddressFilled || isCompanyFilled || isPrivatePersonFilled;
        }
    }

    private Address GetShippingAddress()
    {
        string deliveryCountryCode = Services.Countries.GetCountry(Order.DeliveryCountryCode)?.Code3;
        bool isDeliveryAddressFilled = !string.IsNullOrWhiteSpace(deliveryCountryCode) &&
                               !string.IsNullOrWhiteSpace(Order.DeliveryAddress) &&
                               !string.IsNullOrWhiteSpace(Order.DeliveryZip) &&
                               !string.IsNullOrWhiteSpace(Order.DeliveryCity);

        if (!isDeliveryAddressFilled)
            return null;

        var shippingAddress = new Address
        {
            AddressLine1 = Order.DeliveryAddress,
            PostalCode = Order.DeliveryZip,
            City = Order.DeliveryCity,
            Country = deliveryCountryCode
        };

        if (!string.IsNullOrWhiteSpace(Order.DeliveryAddress2))
            shippingAddress.AddressLine2 = Order.DeliveryAddress2;

        return shippingAddress;
    }

    private Address GetBillingAddress()
    {
        string customerCountryCode = Services.Countries.GetCountry(Order.CustomerCountryCode)?.Code3;
        bool iscustomerAddressFilled = !string.IsNullOrWhiteSpace(customerCountryCode) &&
                               !string.IsNullOrWhiteSpace(Order.CustomerAddress) &&
                               !string.IsNullOrWhiteSpace(Order.CustomerZip) &&
                               !string.IsNullOrWhiteSpace(Order.CustomerCity);

        if (!iscustomerAddressFilled)
            return null;

        var shippingAddress = new Address
        {
            AddressLine1 = Order.CustomerAddress,
            PostalCode = Order.CustomerZip,
            City = Order.CustomerCity,
            Country = customerCountryCode
        };

        if (!string.IsNullOrWhiteSpace(Order.CustomerAddress2))
            shippingAddress.AddressLine2 = Order.CustomerAddress2;

        return shippingAddress;
    }

    private Contact GetContact()
    {
        string firstName = string.IsNullOrWhiteSpace(Order.CustomerFirstName) ? Order.CustomerName : Order.CustomerFirstName;
        string lastName = string.IsNullOrWhiteSpace(Order.CustomerSurname) ? Order.CustomerMiddleName : Order.CustomerSurname;

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return null;

        return new()
        {
            FirstName = firstName,
            LastName = lastName
        };
    }

    #endregion
}
