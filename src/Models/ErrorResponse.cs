using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;

[DataContract]
internal sealed class ErrorResponse
{
    [DataMember(Name = "errors")]
    public Dictionary<string, IEnumerable<string>> Errors { get; set; }
}
