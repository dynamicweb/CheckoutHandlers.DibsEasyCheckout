using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class Notifications
{
    [DataMember(Name = "webhooks")]
    public IEnumerable<Webhook> Webhooks { get; set; }
}
