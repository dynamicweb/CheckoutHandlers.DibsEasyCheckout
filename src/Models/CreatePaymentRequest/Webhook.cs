using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class Webhook
{
    [DataMember(Name = "eventName")]
    public string EventName { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "authorization", EmitDefaultValue = false)]
    public string Authorization { get; set; }
}
