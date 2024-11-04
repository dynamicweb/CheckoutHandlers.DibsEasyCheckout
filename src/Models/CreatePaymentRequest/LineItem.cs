using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models.CreatePaymentRequest;

//see https://developer.nexigroup.com/nexi-checkout/en-EU/api/payment-v1/#v1-payments-post-body-order-items
[DataContract]
internal sealed class LineItem
{
    [DataMember(Name = "reference")]
    public string Reference { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "quantity")]
    public double Quantity { get; set; }

    [DataMember(Name = "unitPrice")]
    public long UnitPrice { get; set; }

    [DataMember(Name = "unit")]
    public string Unit { get; set; }

    [DataMember(Name = "taxRate", EmitDefaultValue = false)]
    public long TaxRate { get; set; }

    [DataMember(Name = "taxAmount", EmitDefaultValue = false)]
    public long TaxAmount { get; set; }

    [DataMember(Name = "grossTotalAmount")]
    public long GrossTotalAmount { get; set; }

    [DataMember(Name = "netTotalAmount")]
    public long NetTotalAmount { get; set; }
}
