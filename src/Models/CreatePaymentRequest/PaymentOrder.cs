using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class PaymentOrder
{
    [DataMember(Name = "items")]
    public IEnumerable<LineItem> Items { get; set; }

    [DataMember(Name = "amount")]
    public long Amount { get; set; }

    [DataMember(Name = "currency")]
    public string Currency { get; set; }

    [DataMember(Name = "reference", EmitDefaultValue = false)]
    public string Reference { get; set; }
}
