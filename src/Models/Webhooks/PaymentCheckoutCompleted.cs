using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;

//see: https://developer.nexigroup.com/nexi-checkout/en-EU/api/webhooks/#checkout-completed
[DataContract]
internal sealed class PaymentCheckoutCompleted
{
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "merchantId")]
    public int MerchantId { get; set; }

    [DataMember(Name = "timestamp")]
    public string Timestamp { get; set; }

    [DataMember(Name = "event")]
    public string Event { get; set; }

    [DataMember(Name = "data")]
    public PaymentCheckoutCompletedData Data { get; set; }
}
