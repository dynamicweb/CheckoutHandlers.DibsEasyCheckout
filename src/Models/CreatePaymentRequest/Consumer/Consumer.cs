using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class Consumer
{
    [DataMember(Name = "email", EmitDefaultValue = false)]
    public string Email { get; set; }

    [DataMember(Name = "shippingAddress", EmitDefaultValue = false)]
    public Address ShippingAddress { get; set; }

    [DataMember(Name = "billingAddress", EmitDefaultValue = false)]
    public Address BillingAddress { get; set; }

    [DataMember(Name = "privatePerson", EmitDefaultValue = false)]
    public Contact PrivatePerson { get; set; }

    [DataMember(Name = "company", EmitDefaultValue = false)]
    public Company Company { get; set; }
}
