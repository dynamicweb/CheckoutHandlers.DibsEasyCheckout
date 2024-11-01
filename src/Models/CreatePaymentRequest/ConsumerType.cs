using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class ConsumerType
{
    [DataMember(Name = "default", EmitDefaultValue = false)]
    public string Default { get; set; }

    [DataMember(Name = "supportedTypes", EmitDefaultValue = false)]
    public IEnumerable<string> SupportedTypes { get; set; }
}
