using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class Contact
{
    [DataMember(Name = "firstName", EmitDefaultValue = false)]
    public string FirstName { get; set; }

    [DataMember(Name = "lastName", EmitDefaultValue = false)]
    public string LastName { get; set; }
}
