using QuestPDF.Infrastructure;


namespace GestorInventario.Renderer.PDF
{
    public static class PayPalInvoiceRendererBootstrap
    {
        public static void Initialize()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
    }
}
