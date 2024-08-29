namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Service;

internal enum ApiCommand
{
    /// <summary>
    /// Creates a payment.
    /// POST /payments
    /// </summary>
    CreatePayment,

    /// <summary>
    /// Retrieves a payment object.
    /// GET /payments/{operatorId}"
    /// </summary>
    GetPayment,

    /// <summary>
    /// POST /payments/{operatorId}/cancels
    /// </summary>
    CancelPayment,

    /// <summary>
    /// Captures payment
    /// POST /payments/{operatorId}/charges
    /// </summary>
    CapturePayment,


    /// <summary>
    /// POST /charges/{operatorId}/refunds
    /// </summary>
    RefundPayment
}
