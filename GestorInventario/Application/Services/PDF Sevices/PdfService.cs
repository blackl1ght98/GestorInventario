
using GestorInventario.Application.Services.Generic_Services;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using QuestPDF.Fluent;

namespace GestorInventario.Application.Services
{
    public class PdfService:IPdfService
    {
        private readonly IPaymentRepository _paymentRepository;

        public PdfService(IPaymentRepository repository)
        {
            _paymentRepository = repository;
        }
       
        public async Task<OperationResult<byte[]>> GenerarFacturaPagoEjecutadoAsync(string pagoId)
        {
            var detalle = await _paymentRepository.ObtenerDetallesPagoPorIDAsync(pagoId);

            if (detalle == null)
                return OperationResult<byte[]>.Fail("No se encontró el detalle de pago");

            var document = new FacturaPagoEjecutadoPdf(detalle);
            var pdfBytes = document.GeneratePdf();

            return OperationResult<byte[]>.Ok("Factura generada correctamente", pdfBytes);
        }
    }
}
