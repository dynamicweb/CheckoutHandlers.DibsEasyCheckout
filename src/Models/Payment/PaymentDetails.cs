using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-payments-paymentid-get
[DataContract]
internal sealed class PaymentDetails
{
    [DataMember(Name = "paymentType")]
    public string PaymentType { get; set; }

    [DataMember(Name = "paymentMethod")]
    public string PaymentMethod { get; set; }

    [DataMember(Name = "cardDetails")]
    public CardDetails CardDetails { get; set; }

    [DataMember(Name = "invoiceDetails")]
    public InvoiceDetails InvoiceDetails { get; set; }
}
