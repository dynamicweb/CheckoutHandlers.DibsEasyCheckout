using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class Company
{
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "contact")]
    public Contact Contact { get; set; }
}
