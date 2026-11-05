using GestorInventario.Application.Interfaces.Renderer;
using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Application.Interfaces.Services;
using GestorInventario.Application.Mappers;
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
                return OperationResult<byte[]>.Fail();
            var facturaDto = FacturaMapper.ToDto(detalle);
            var pdfBytes = _invoiceRenderer.Render(facturaDto);
            return OperationResult<byte[]>.Ok(pdfBytes, "Factura generada correctamente");
        }
    }
}