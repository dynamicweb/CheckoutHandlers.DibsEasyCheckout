using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class PaymentRequest
{
    [DataMember(Name = "order")]
    public PaymentOrder Order { get; set; }

    [DataMember(Name = "checkout")]
    public PaymentCheckout Checkout { get; set; }

    [DataMember(Name = "notifications", EmitDefaultValue = false)]
    public Notifications Notifications { get; set; }
}
