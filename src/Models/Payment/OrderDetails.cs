using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-payments-paymentid-get
[DataContract]
internal sealed class OrderDetails
{
    [DataMember(Name = "amount")]
    public long Amount { get; set; }

    [DataMember(Name = "currency")]
    public string Currency { get; set; }

    [DataMember(Name = "reference")]
    public string Reference { get; set; }
}
