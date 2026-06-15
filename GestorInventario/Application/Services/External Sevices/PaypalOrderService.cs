using GestorInventario.Application.DTOs.Checkout;
using GestorInventario.Application.DTOs.Paypal.Responses.GET.Order;
using GestorInventario.Application.DTOS.Paypal.Requests.POST;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Order;
using GestorInventario.Application.Services.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.External_Sevices
{
    public class PaypalOrderService: IPaypalOrderService
    {
        private readonly ILogger<PaypalOrderService> _logger;    
        private readonly IPayPalHttpClient _paypal;
    
        public PaypalOrderService(ILogger<PaypalOrderService> logger,
           IPayPalHttpClient paypal)
        {
            _logger = logger;           
            _paypal = paypal;
          
        }
        #region Crear orden 
        public async Task<string> CreateOrderWithPaypalAsync(CheckoutDto pagar)
        {
            var order = BuildOrder(pagar);
            var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                "v2/checkout/orders",
                order,
               async resp =>
               {
                   var body = await resp.Content.ReadAsStringAsync();
                   throw new InvalidOperationException($"Error al crear orden: {resp.StatusCode} - {body}");
               });
          
            return responseBody;
        }
        private CreateOrderRequest BuildOrder(CheckoutDto pagar)
        {
            decimal totalNeto = 0m;
            decimal totalImpuestos = 0m;

            var itemsParaPayPal = pagar.Items.ConvertAll(item =>
            {
                decimal precioUnitario = item.Price;
                int cantidad = int.Parse(item.Quantity);
                decimal impuestoPorUnidad = CalculadoraFiscal.CalcularIvaUnitario(precioUnitario);

                totalNeto += precioUnitario * cantidad;
                totalImpuestos += impuestoPorUnidad * cantidad;

                return new Item
                {
                    Name = item.Name,
                    Description = item.Description,
                    Quantity = cantidad.ToString(),
                    UnitAmount = new Money
                    {
                        Value = CalculadoraFiscal.FormatearPayPal(precioUnitario),
                        CurrencyCode = pagar.Currency
                    },
                    Tax = new Money
                    {
                        Value = CalculadoraFiscal.FormatearPayPal(impuestoPorUnidad),
                        CurrencyCode = pagar.Currency
                    },
                    Sku = item.Sku
                };
            });

            // ✅ Validación de coherencia: lo que calculamos debe coincidir con el checkout
            var (subtotalEsperado, ivaEsperado, totalEsperado) = CalculadoraFiscal.CalcularTotales(
                pagar.Items.Select(i => (i.Price, int.Parse(i.Quantity))));

            if (totalNeto != subtotalEsperado || totalImpuestos != ivaEsperado)
            {
                _logger.LogError("Descuadre fiscal: Neto={Neto} vs {Esperado}, IVA={Iva} vs {Esperado}",
                    totalNeto, subtotalEsperado, totalImpuestos, ivaEsperado);
                throw new InvalidOperationException("Descuadre en cálculo fiscal del pedido");
            }

            return new CreateOrderRequest
            {
                Intent = "CAPTURE",
                PurchaseUnits = new List<PurchaseUnit>
        {
            new PurchaseUnit
            {
                Amount = new Amount
                {
                    CurrencyCode = pagar.Currency,
                    Value = CalculadoraFiscal.FormatearPayPal(totalEsperado),
                    Breakdown = new AmountBreakdown
                    {
                        ItemTotal = new Money
                        {
                            CurrencyCode = pagar.Currency,
                            Value = CalculadoraFiscal.FormatearPayPal(totalNeto)
                        },
                        TaxTotal = new Money
                        {
                            CurrencyCode = pagar.Currency,
                            Value = CalculadoraFiscal.FormatearPayPal(totalImpuestos)
                        },
                        ShippingAmount = new Money
                        {
                            CurrencyCode = pagar.Currency,
                            Value = "0.00"
                        }
                    }
                },
                Description = "The payment transaction description.",
                InvoiceId = Guid.NewGuid().ToString(),
                Items = itemsParaPayPal,
                Shipping = new Shipping
                {
                    Name = new OrderShippingName
                    {
                        FullName = pagar.NombreCompleto
                    },
                    Address = new OrderShippingAddress
                    {
                        AddressLine1 = pagar.Line1,
                        AddressLine2 = pagar.Line2 ?? "",
                        City = pagar.Ciudad,
                        State = "ES",
                        PostalCode = pagar.CodigoPostal,
                        CountryCode = "ES"
                    }
                }
            }
        },
                PaymentSource = new PaymentSource
                {
                    Paypal = new Paypal
                    {
                        ExperienceContext = new ExperienceContext
                        {
                            PaymentMethodPreference = "IMMEDIATE_PAYMENT_REQUIRED",
                            ReturnUrl = pagar.ReturnUrl,
                            CancelUrl = pagar.CancelUrl
                        }
                    }
                }
            };
        }
        #endregion

        #region Capturar el pago (orden)
        public async Task<(string CaptureId, decimal Total, string Currency)> CapturarPagoAsync(string orderId)
        {
            try
            {

                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                   HttpMethod.Post,
                   $"v2/checkout/orders/{orderId}/capture",
                   rawJsonBody: "{}",
                    async resp =>
                    {
                        var body = await resp.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al capturar: {resp.StatusCode} - {body}");
                    });


                var paypalResponse = JsonConvert.DeserializeObject<OrderDetailsResponse>(responseBody);

                var capture = paypalResponse?.PurchaseUnits?
                    .FirstOrDefault()?.Payments?
                    .Captures?.FirstOrDefault();

                if (capture == null || string.IsNullOrEmpty(capture.Id) ||
                    string.IsNullOrEmpty(capture.Amount?.Value) ||
                    string.IsNullOrEmpty(capture.Amount?.CurrencyCode))
                {
                    throw new Exception("No se pudo extraer la información de la captura del pago.");
                }
                decimal amountValue= decimal.Parse(capture.Amount.Value, CultureInfo.InvariantCulture);
                return (capture.Id, amountValue, capture.Amount.CurrencyCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al capturar el pago de PayPal");
                throw new InvalidOperationException("No se pudo capturar el pago de PayPal", ex);
            }
        }

        #endregion

        #region Obtener detalles del pago v2 paypal   
        public async Task<OrderDetailsResponse> ObtenerDetallesPagoEjecutadoAsync(string id)
        {
            var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
             HttpMethod.Get,
             $"v2/checkout/orders/{id}",
             async resp =>
             {
                 var errBody = await resp.Content.ReadAsStringAsync();
                 throw new InvalidOperationException(
                     $"Error al obtener detalles de orden {id}: {resp.StatusCode} - {errBody}");
             });
            // Deserializamos la respuesta al DTO correspondiente
            var subscriptionDetails = JsonConvert.DeserializeObject<OrderDetailsResponse>(responseBody);
            if (subscriptionDetails == null)
            {
                throw new ArgumentNullException("No se puede obtener los detalles de la subscripcion");
            }
            return subscriptionDetails;

        }
        #endregion

      
    }
}
