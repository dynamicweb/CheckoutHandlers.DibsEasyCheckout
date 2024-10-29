using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;

[DataContract]
internal sealed class PaymentCheckoutCompletedData
{
    [DataMember(Name = "paymentId")]
    public string PaymentId { get; set; }
}
