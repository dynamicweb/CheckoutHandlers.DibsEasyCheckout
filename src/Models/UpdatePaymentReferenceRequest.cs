using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;

[DataContract]
internal sealed class UpdatePaymentReferenceRequest
{
    [DataMember(Name = "reference")]
    public string Reference { get; set; }

    [DataMember(Name = "checkoutUrl")]
    public string CheckoutUrl { get; set; }
}
