using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;

[DataContract]
internal sealed class CancelPaymentRequest
{
    [DataMember(Name = "amount")]
    public long Amount { get; set; }
}
