using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-payments-paymentid-get
[DataContract]
internal sealed class InvoiceDetails
{
    [DataMember(Name = "invoiceNumber")]
    public string InvoiceNumber { get; set; }
}
