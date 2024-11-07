using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

[DataContract]
internal sealed class PaymentCheckout
{
    [DataMember(Name = "url", EmitDefaultValue = false)]
    public string Url { get; set; }

    [DataMember(Name = "returnUrl", EmitDefaultValue = false)]
    public string ReturnUrl { get; set; }

    [DataMember(Name = "termsUrl")]
    public string TermsUrl { get; set; }

    [DataMember(Name = "integrationType", EmitDefaultValue = false)]
    public string IntegrationType { get; set; }

    [DataMember(Name = "merchantHandlesConsumerData", EmitDefaultValue = false)]
    public bool MerchantHandlesConsumerData { get; set; }

    [DataMember(Name = "consumerType", EmitDefaultValue = false)]
    public ConsumerType ConsumerType { get; set; }

    [DataMember(Name = "consumer", EmitDefaultValue = false)]
    public Consumer Consumer { get; set; }

    [DataMember(Name = "shipping", EmitDefaultValue = false)]
    public Shipping Shipping { get; set; }
}
