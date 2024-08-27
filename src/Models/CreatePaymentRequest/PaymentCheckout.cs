using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class PaymentCheckout
{
    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "returnUrl")]
    public string ReturnUrl { get; set; }

    [DataMember(Name = "termsUrl")]
    public string TermsUrl { get; set; }

    [DataMember(Name = "integrationType")]
    public string IntegrationType { get; set; }

    [DataMember(Name = "merchantHandlesConsumerData")]
    public bool MerchantHandlesConsumerData { get; set; }

    [DataMember(Name = "consumerType")]
    public ConsumerType ConsumerType { get; set; }

    [DataMember(Name = "consumer")]
    public Consumer Consumer { get; set; }
}
