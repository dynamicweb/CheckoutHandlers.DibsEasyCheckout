using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class ConsumerType
{
    [DataMember(Name = "default")]
    public string Default { get; set; }

    [DataMember(Name = "supportedTypes")]
    public IEnumerable<string> SupportedTypes { get; set; }
}
