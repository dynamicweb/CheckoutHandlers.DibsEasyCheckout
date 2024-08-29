using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-payments-paymentid-get
[DataContract]
internal sealed class CardDetails
{
    [DataMember(Name = "maskedPan")]
    public string MaskedPan { get; set; }

    [DataMember(Name = "expiryDate")]
    public string ExpiryDate { get; set; }
}
