using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;

[DataContract]
internal sealed class BadRequestResponse
{
    [DataMember(Name = "errors")]
    public Dictionary<string, IEnumerable<string>> Errors { get; set; }
}
