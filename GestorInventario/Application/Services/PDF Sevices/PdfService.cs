
using GestorInventario.Application.Services.Generic_Services;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GestorInventario.Application.Services
{
    public class PdfService:IPdfService
    {
        private readonly GestorInventarioContext _context;

        public PdfService(GestorInventarioContext context)
        {
            _context = context;
        }
       

        public async Task<OperationResult<byte[]>> GenerarFacturaPagoEjecutadoAsync(string pagoId)
        {
            var detalle = await _context.PayPalPaymentDetails
                .Include(d => d.PayPalPaymentItems)
                .Include(d=>d.PayPalPaymentShippings)
                .FirstOrDefaultAsync(d => d.Id == pagoId);

            if (detalle == null)
                return OperationResult<byte[]>.Fail("No se encontró el detalle de pago");

            var document = new FacturaPagoEjecutadoPdf(detalle);
            var pdfBytes = document.GeneratePdf();

            return OperationResult<byte[]>.Ok("Factura generada correctamente", pdfBytes);
        }
    }
}
