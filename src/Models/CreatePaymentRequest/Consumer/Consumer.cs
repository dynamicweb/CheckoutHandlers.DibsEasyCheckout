using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class Consumer
{
    [DataMember(Name = "email")]
    public string Email { get; set; }

    [DataMember(Name = "shippingAddress")]
    public ShippingAddress ShippingAddress { get; set; }

    [DataMember(Name = "privatePerson", EmitDefaultValue = false)]
    public Contact PrivatePerson { get; set; }

    [DataMember(Name = "company")]
    public Company Company { get; set; }
}
