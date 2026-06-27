using GestorInventario.Application.DTOS.Paypal.Requests.POST;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Order;
using GestorInventario.Domain.Models;
using GestorInventario.enums.Pedido;
using GestorInventario.enums.Productos;

using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Utilities;

namespace GestorInventario.Application.Services.External_Sevices;

public class PaypalOrderTrackingService : IPaypalOrderTrackingService
{
    private readonly ILogger<PaypalOrderTrackingService> _logger;
    private readonly IPayPalHttpClient _paypal;

    public PaypalOrderTrackingService(
        ILogger<PaypalOrderTrackingService> logger,
        IPayPalHttpClient paypal)
    {
        _logger = logger;
        _paypal = paypal;
    }

    public async Task<OperationResult<(string TrackingNumber, string TrackingUrl)>> 
        AddTrackingAsync(
            string payPalOrderId,
            string captureId,
            Carrier carrier,
            BarcodeType barcode,
            List<TrackingItemDto> items)
    {
        try
        {
            var trackingNumber = GenerarNumeroSeguimiento();

            var trackingInfo = new PayPalTrackingInfoRequestDto
            {
                CaptureId = captureId,
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                NotifyPayer = true,
                Items = items.Select(i => new TrackingItems
                {
                    Name = i.Name,
                    Sku = i.Sku,
                    Quantity = i.Quantity,
                    Upc = new Upc
                    {
                        Type = barcode,
                        Code = i.BarcodeCode
                    },
                    ImageUrl = i.ImageUrl,
                    Url = i.Url
                }).ToList()
            };

            await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                $"v2/checkout/orders/{payPalOrderId}/track",
                trackingInfo,
                async resp =>
                {
                    var errBody = await resp.Content.ReadAsStringAsync();
                    throw new InvalidOperationException(
                        $"Error al establecer el seguimiento: {resp.StatusCode} - {errBody}");
                });

            return OperationResult<(string, string)>.Ok(
                "Seguimiento registrado en PayPal",
                (trackingNumber, "URL NO ESPECIFICADA"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar seguimiento en PayPal");
            throw new InvalidOperationException("No se pudo agregar el seguimiento.", ex);
        }
    }

    private string GenerarNumeroSeguimiento()
    {
        string prefijo = "PKG";
        string fecha = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string aleatorio = Random.Shared.Next(1000, 9999).ToString();
        return $"{prefijo}-{fecha}-{aleatorio}";
    }
}