using GestorInventario.Application.DTOS;
using GestorInventario.Application.Interfaces.Renderer;
using GestorInventario.Domain.Models;
using QuestPDF.Fluent;

namespace GestorInventario.Infrastructure.Renderers.PDF
{
    public class PayPalInvoicePdfRenderer : IPayPalInvoiceRenderer
    {
        public byte[] Render(FacturaPdfDto dto )
        {
            return new FacturaPagoEjecutadoPdf(dto).GeneratePdf();
        }
    }
}
