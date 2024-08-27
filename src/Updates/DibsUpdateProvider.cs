using Dynamicweb.Updates;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Updates;

public sealed class DibsUpdateProvider : UpdateProvider
{
    private static Stream GetResourceStream(string name)
    {
        string resourceName = $"Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Updates.{name}";

        return Assembly.GetAssembly(typeof(DibsUpdateProvider)).GetManifestResourceStream(resourceName);
    }

    public override IEnumerable<Update> GetUpdates()
    {
        return new List<Update>()
        {
            new FileUpdate("39da8029-91d8-4a10-a5e3-e4cc38330188", this, "/Files/Templates/eCom7/CheckoutHandler/DibsEasy/Error/checkouthandler_error.cshtml", () => GetResourceStream("checkouthandler_error.cshtml")),
            new FileUpdate("2d813363-d092-47ef-b9f1-12aca0034508", this, "/Files/Templates/eCom7/CheckoutHandler/DibsEasy/Form/EmbededDibs.cshtml", () => GetResourceStream("EmbededDibs.cshtml")),
            new FileUpdate("8ac8cb90-18bb-40b0-a392-0acccc6df1ea", this, "/Files/Templates/eCom7/CheckoutHandler/DibsEasy/Form/HostedPaymentDibs.cshtml", () => GetResourceStream("HostedPaymentDibs.cshtml"))
        };
    }

    /*
     * IMPORTANT!
     * Use a generated GUID string as id for an update
     * - Execute command in C# interactive window: Guid.NewGuid().ToString()
     */
}

