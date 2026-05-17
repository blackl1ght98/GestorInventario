using GestorInventario.Application.DTOs.Email;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.ViewModels.Pedidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Infraestructure.Controllers.PedidosControllers
{
    public class FacturasController : Controller
    {
        private readonly IPdfService _pdfService;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IEmailService _emailService;
        private readonly ILogger<FacturasController> _logger;

        public FacturasController(IPdfService pdfService, ICurrentUserAccessor currentUserAccessor, IEmailService emailService, ILogger<FacturasController> logger)
        {
            _pdfService = pdfService;
            _currentUserAccessor = currentUserAccessor;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> DownloadInvoice(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogError("Intento de manipulacion de id");
                return RedirectToAction("Error", "Home");
            }

            var resultado = await _pdfService.GenerarFacturaPagoEjecutadoAsync(id);

            if (!resultado.Success)
            {
                TempData["ErrorMessage"] = resultado.Message ?? "No se pudo generar la factura.";
                return RedirectToAction("DetallesPagoEjecutado", new { id });
            }

            var pdfBytes = resultado.Data;
            var fileName = $"Factura_Pago_{id}.pdf";


            return File(pdfBytes, "application/pdf", fileName);
        }
        [HttpGet]
        [Authorize] // Opcional, según quién pueda enviar facturas
        public async Task<IActionResult> SendInvoiceByEmail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogError("Intento de manipulacion de id");
                return RedirectToAction("Error", "Home");
            }

            // Obtener email del cliente       
            var emailDestinatario = _currentUserAccessor.GetCurrentUserEmail();

            if (string.IsNullOrEmpty(emailDestinatario))
            {
                TempData["ErrorMessage"] = "No se encontró email del cliente.";
                return RedirectToAction("DetallesPagoEjecutado", new { id });
            }

            // Preparar modelo para la plantilla del email
            var emailModel = new FacturaViewmodel
            {
                IdPago = id,
                EnlaceDescarga = Url.Action(nameof(DownloadInvoice), "Pedidos", new { id }, Request.Scheme)

            };

            var emailDto = new EmailDto { ToEmail = emailDestinatario };
            await _emailService.SendEmailAsyncFactura(emailDto, id);
            TempData["SuccessMessage"] = "Factura enviada por email correctamente.";
            return RedirectToAction("Index", "Pedidos");
            
        }
    }
}
