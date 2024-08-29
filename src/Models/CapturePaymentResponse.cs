using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-payments-paymentid-charges-post-responses
[DataContract]
internal sealed class CapturePaymentResponse
{
    [DataMember(Name = "chargeId")]
    public string ChargeId { get; set; }

    [DataMember(Name = "invoice")]
    public InvoiceDetails Invoice { get; set; }
}
