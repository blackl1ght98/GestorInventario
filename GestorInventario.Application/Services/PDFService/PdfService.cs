using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Renderer;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Services.PDFService
{
    public class PdfService : IPdfService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPayPalInvoiceRenderer _invoiceRenderer;

        public PdfService(
            IPaymentRepository paymentRepository,
            IPayPalInvoiceRenderer invoiceRenderer)
        {
            _paymentRepository = paymentRepository;
            _invoiceRenderer = invoiceRenderer;
        }

        public async Task<OperationResult<byte[]>> GenerarFacturaPagoEjecutadoAsync(string pagoId)
        {
            var detalle = await _paymentRepository.ObtenerDetallesPagoPorIDAsync(pagoId);
            if (detalle == null)
                return OperationResult<byte[]>.Fail("No se encontró el detalle de pago");

            var pdfBytes = _invoiceRenderer.Render(detalle);
            return OperationResult<byte[]>.Ok("Factura generada correctamente", pdfBytes);
        }
    }
}
