using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class Shipping
{
    [DataMember(Name = "enableBillingAddress", EmitDefaultValue = false)]
    public bool EnableBillingAddress { get; set; }
}
