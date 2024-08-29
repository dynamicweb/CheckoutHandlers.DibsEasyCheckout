namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Service;

internal sealed class CommandConfiguration
{
    /// <summary>
    /// Dibs command. See operation urls in <see cref="DibsRequest"/> and <see cref="ApiCommand"/>
    /// </summary>
    public ApiCommand CommandType { get; set; }

    /// <summary>
    /// Command operator id, like https://.../v1/.../{OperatorId}
    /// </summary>
    public string OperatorId { get; set; }

    /// <summary>
    /// Data to serialize
    /// </summary>
    public object Data { get; set; }
}
