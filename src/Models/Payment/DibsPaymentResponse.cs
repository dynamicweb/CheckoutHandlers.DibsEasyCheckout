using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-payments-paymentid-get-responses-200-payment
[DataContract]
internal sealed class DibsPaymentResponse
{
    [DataMember(Name = "payment")]
    public DibsPayment Payment { get; set; }
}
