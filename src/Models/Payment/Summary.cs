using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-payments-paymentid-get
[DataContract]
internal sealed class Summary
{
    [DataMember(Name = "reservedAmount")]
    public long ReservedAmount { get; set; }

    [DataMember(Name = "chargedAmount")]
    public long ChargedAmount { get; set; }

    [DataMember(Name = "refundedAmount")]
    public long RefundedAmount { get; set; }

    [DataMember(Name = "cancelledAmount")]
    public long CancelledAmount { get; set; }
}
