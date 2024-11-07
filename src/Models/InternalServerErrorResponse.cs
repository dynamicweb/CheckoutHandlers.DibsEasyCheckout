using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;

[DataContract]
internal sealed class InternalServerErrorResponse
{
    [DataMember(Name = "message")]
    public string Message { get; set; }

    [DataMember(Name = "Code")]
    public string Code { get; set; }

    [DataMember(Name = "Source")]
    public string Source { get; set; }
}
