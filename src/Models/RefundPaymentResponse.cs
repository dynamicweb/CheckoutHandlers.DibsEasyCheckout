using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-charges-chargeid-refunds-post-responses-201-refundid
[DataContract]
internal sealed class RefundPaymentResponse
{
    [DataMember(Name = "refundId")]
    public string RefundId { get; set; }
}
