namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Service;

internal sealed class CreatePaymentParameters
{
    public WindowModes WindowMode { get; set; }

    public bool SupportB2b { get; set; }

    public bool SetB2bAsDefault { get; set; }

    public bool PrefillCustomerAddress { get; set; }

    public bool EnableBillingAddress { get; set; }

    public string ReceiptUrl { get; set; }

    public string ApprovetUrl { get; set; }

    public string BaseUrl { get; set; }

    public string TermsPage { get; set; }
}
