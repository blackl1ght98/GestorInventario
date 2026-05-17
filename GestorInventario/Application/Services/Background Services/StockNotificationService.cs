using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class StockNotificationService : IStockNotificationService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IEmailService _emailService;
        private readonly IAdminRepository _adminNotifierRepository;
        private readonly ILogger<StockNotificationService> _logger;

        public StockNotificationService(
            IProductoRepository productoRepository,
            IEmailService emailService,
            IAdminRepository adminNotifierRepository,
            ILogger<StockNotificationService> logger)
        {
            _productoRepository = productoRepository;
            _emailService = emailService;
            _adminNotifierRepository = adminNotifierRepository;
            _logger = logger;
        }

        public async Task VerificarYNotificarStockBajoAsync(CancellationToken stoppingToken = default)
        {
            try
            {
                // 1. Traer administradores desde la base de datos
                var adminEmails = await _adminNotifierRepository
                    .ObtenerEmailsAdministradoresAsync(stoppingToken);

                if (!adminEmails.Any())
                {
                    _logger.LogWarning(
                        "No hay usuarios con rol Administrador en la base de datos para notificar stock.");
                    return;
                }

                _logger.LogInformation(
                    "Notificaciones de stock dirigidas a {Count} administrador(es).",
                    adminEmails.Count);

                // 2. Traer productos con stock bajo
                var productos =  _productoRepository.ObtenerTodosLosProductos();
                var productosBajoStock = productos.Where(p => p.Cantidad < 10).ToList();

                if (!productosBajoStock.Any())
                {
                    _logger.LogInformation("No hay productos con stock bajo.");
                    return;
                }

                _logger.LogInformation(
                    "Detectados {Count} productos con stock bajo.",
                    productosBajoStock.Count);

                // 3. Enviar un email por producto a cada admin
                foreach (var producto in productosBajoStock)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    var lowStockData = new LowStockEmailData
                    {
                        NombreProducto = producto.NombreProducto,
                        Cantidad = producto.Cantidad
                    };

                    foreach (var email in adminEmails)
                    {
                        try
                        {
                            await _emailService.SendEmailAsyncLowStock(
                                new EmailDto { ToEmail = email },
                                lowStockData);

                         
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Fallo al notificar {Producto} a {Email}",
                                producto.NombreProducto, email);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Verificación de stock cancelada por token.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general en el servicio de notificación de stock.");
            }
        }
    }
}
