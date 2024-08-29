using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.Payment;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-payments-paymentid-get
[DataContract]
internal sealed class DibsPayment
{
    [DataMember(Name = "paymentId")]
    public string PaymentId { get; set; }

    [DataMember(Name = "created")]
    public string Created { get; set; }

    [DataMember(Name = "terminated")]
    public string Terminated { get; set; }

    [DataMember(Name = "myReference")]
    public string Reference { get; set; }

    [DataMember(Name = "paymentDetails")]
    public PaymentDetails PaymentDetails { get; set; }

    [DataMember(Name = "orderDetails")]
    public OrderDetails OrderDetails { get; set; }

    [DataMember(Name = "summary")]
    public Summary Summary { get; set; }
}
