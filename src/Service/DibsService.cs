using Dynamicweb.Core;
using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;
using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;
using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;
using Dynamicweb.Ecommerce.Orders;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Service;

internal sealed class DibsService
{
    public string ApiUrl { get; set; }

    public string SecretKey { get; set; }

    public Order Order { get; set; }

    public DibsService(Order order, string secretKey, string apiUrl)
    {
        Order = order;
        SecretKey = secretKey;
        ApiUrl = apiUrl;
    }

    public CreatePaymentResponse CreatePayment(CreatePaymentParameters parameters)
    {
        var createRequestService = new DibsCreateRequestService(Order);
        PaymentRequest request = createRequestService.CreatePaymentRequest(parameters);

        string response = DibsRequest.SendRequest(Order, ApiUrl, SecretKey, new()
        {
            CommandType = ApiCommand.CreatePayment,
            Data = request
        });

        return Converter.Deserialize<CreatePaymentResponse>(response);
    }

    public DibsPaymentResponse GetPayment(string paymentId)
    {

        string response = DibsRequest.SendRequest(Order, ApiUrl, SecretKey, new()
        {
            CommandType = ApiCommand.GetPayment,
            OperatorId = paymentId
        });

        return Converter.Deserialize<DibsPaymentResponse>(response);
    }

    public CapturePaymentResponse CapturePayment(string paymentId, long amount)
    {
        string response = DibsRequest.SendRequest(Order, ApiUrl, SecretKey, new()
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
        string response = DibsRequest.SendRequest(Order, ApiUrl, SecretKey, new()
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
        string response = DibsRequest.SendRequest(Order, ApiUrl, SecretKey, new()
        {
            CommandType = ApiCommand.CancelPayment,
            OperatorId = paymentId,
            Data = new CancelPaymentRequest
            {
                Amount = amount
            }
        });
    }
}
