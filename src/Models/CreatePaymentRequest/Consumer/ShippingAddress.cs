using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class ShippingAddress
{
    [DataMember(Name = "addressLine1")]
    public string AddressLine1 { get; set; }

    [DataMember(Name = "addressLine2")]
    public string AddressLine2 { get; set; }

    [DataMember(Name = "postalCode")]
    public string PostalCode { get; set; }

    [DataMember(Name = "city")]
    public string City { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }
}
