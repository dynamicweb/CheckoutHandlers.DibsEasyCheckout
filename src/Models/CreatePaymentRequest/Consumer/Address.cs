using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class Address
{
    [DataMember(Name = "addressLine1", EmitDefaultValue = false)]
    public string AddressLine1 { get; set; }

    [DataMember(Name = "addressLine2", EmitDefaultValue = false)]
    public string AddressLine2 { get; set; }

    [DataMember(Name = "postalCode", EmitDefaultValue = false)]
    public string PostalCode { get; set; }

    [DataMember(Name = "city", EmitDefaultValue = false)]
    public string City { get; set; }

    [DataMember(Name = "country", EmitDefaultValue = false)]
    public string Country { get; set; }
}
