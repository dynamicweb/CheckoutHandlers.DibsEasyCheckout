using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-payments-post
[DataContract]
internal sealed class CreatePaymentResponse
{
    [DataMember(Name = "paymentId")]
    public string PaymentId { get; set; }

    [DataMember(Name = "hostedPaymentPageUrl")]
    public string HostedPaymentPageUrl { get; set; }
}
