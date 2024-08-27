using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class Contact
{
    [DataMember(Name = "firstName")]
    public string FirstName { get; set; }

    [DataMember(Name = "lastName")]
    public string LastName { get; set; }
}
