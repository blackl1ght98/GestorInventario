
using GestorInventario.Application.DTOs.Response_paypal.POST;

using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;



namespace GestorInventario.Application.Services
{
    public class PaypalOrderTrackingService : IPaypalOrderTrackingService
    {       
        private readonly ILogger<PaypalOrderTrackingService> _logger;
     
        private readonly IPaypalRepository _repo;
        private readonly IPayPalHttpClient _paypal;
        private readonly CultureHelper _culture;
        
        public PaypalOrderTrackingService( ILogger<PaypalOrderTrackingService> logger,   
           IPayPalHttpClient paypal, IPaypalRepository repo, CultureHelper culture)
        {                
            _logger = logger;           
             _culture=culture;                  
            _paypal = paypal;    
            _repo = repo;
        }

        #region Seguimiento pedido
        public async Task<string> SeguimientoPedido(int pedidoId, Carrier carrier, BarcodeType barcode)
        {
            try
            {
                var (pedido, detalles) = await _repo.GetPedidoConDetallesAsync(pedidoId);
                if (pedido == null || detalles == null)
                {
                    throw new Exception("No se pudo obtener la información completa del pedido.");
                }
                var trackingInfo = CrearTrackingInfo(pedido, detalles, carrier, barcode);
                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                $"v2/checkout/orders/{pedido.OrderId}/track",
                trackingInfo,
               async resp =>
               {
                   var errBody = await resp.Content.ReadAsStringAsync();
                   throw new InvalidOperationException($"Error al establecer el seguimiento del pedido: {resp.StatusCode} - {errBody} ");
               });

                // Actualizar el estado del pedido usando UnitOfWork
                await _repo.AddInfoTrackingOrder(pedidoId, trackingInfo.TrackingNumber, "URL NO ESPECIFICADA", carrier.ToString());
                _logger.LogInformation("Seguimiento agregado exitosamente para el pedido {PedidoId}", pedidoId);
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar seguimiento para el pedido {PedidoId}", pedidoId);
                throw new InvalidOperationException($"No se pudo agregar el seguimiento para el pedido {pedidoId}.", ex);
            }
        }
        private PayPalTrackingInfoDto CrearTrackingInfo(Pedido pedido, IEnumerable<DetallePedido> detalles, Carrier carrier, BarcodeType barcode)
        {
            var trackingItems = detalles.Select(item => new TrackingItems
            {
                Name = item.Producto?.NombreProducto ?? "Producto no disponible",
                Sku = item.Producto?.Descripcion ?? "N/A",
                Quantity = item.Cantidad ?? 1,
                Upc = new Upc
                {
                    Type = barcode,
                    Code = item.Producto?.UpcCode ?? "N/A"
                },
                ImageUrl = item.Producto?.Imagen ?? string.Empty,
                Url = string.Empty
            }).ToList();

            return new PayPalTrackingInfoDto
            {
                CaptureId = pedido.CaptureId,
                TrackingNumber = GenerarNumeroSeguimiento(),
                Carrier = carrier,
                NotifyPayer = true,
                Items = trackingItems
            };
        }
        private string GenerarNumeroSeguimiento()
        {
            // Prefijo opcional para identificar el tipo de pedido
            string prefijo = "PKG";

            // Fecha y hora para hacerlo único
            string fecha = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            // Parte aleatoria para reducir riesgo de colisión
            string aleatorio = new Random().Next(1000, 9999).ToString();

            // Concatenamos todo
            return $"{prefijo}-{fecha}-{aleatorio}";
        }
        #endregion

       


      
       

    }

}