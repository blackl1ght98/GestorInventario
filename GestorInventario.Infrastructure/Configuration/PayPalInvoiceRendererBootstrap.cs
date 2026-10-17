using QuestPDF.Infrastructure;

namespace GestorInventario.Infrastructure.Configuration
{
    public static class PayPalInvoiceRendererBootstrap
    {
        public static void Initialize()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
    }
}
