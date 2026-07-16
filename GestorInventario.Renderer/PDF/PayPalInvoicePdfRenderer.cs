using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Renderer;
using QuestPDF.Fluent;


namespace GestorInventario.Renderer.PDF
{
    public class PayPalInvoicePdfRenderer : IPayPalInvoiceRenderer
    {
        public byte[] Render(PayPalPaymentDetail data)
        {
            return new FacturaPagoEjecutadoPdf(data).GeneratePdf();
        }
    }
}
