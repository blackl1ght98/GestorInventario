using GestorInventario.Application.Interfaces.Services.Paypal.PaypalApi;
using GestorInventario.Application.Interfaces.Services.Paypal.PaypalApi.Order;
using GestorInventario.Application.Services.Common;
using GestorInventario.Shared.ExternalServices.Paypal.API.Requests.POST;
using GestorInventario.Shared.ExternalServices.Paypal.API.Responses.GET.Order;
using GestorInventario.Shared.ExternalServices.Paypal.API.Responses.POST.Order;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Services.Paypal.PaypalApi.Order
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
        public async Task<OperationResult<string>> CreateOrderWithPaypalAsync(CheckoutRequest pagar)
        {
            try
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
                var createdPayment = JsonConvert.DeserializeObject<PayPalOrderResponse>(responseBody);
                var approvalUrl = createdPayment?.Links?.FirstOrDefault(x => x.Rel == "payer-action")?.Href;
                return OperationResult<string>.Ok(approvalUrl,"URL a la que redirigir");
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<string>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
           
        }
        private CreateOrderRequest BuildOrder(CheckoutRequest pagar)
        {
            var itemsParaPayPal = pagar.Items.ConvertAll(item =>
            {
                // El precio ya viene calculado desde el PaymentService
                decimal precioUnitario = item.Price;

                // Calculamos el impuesto unitario aquí mismo usando la utilidad estática
                // No necesitamos BD para esto, solo el precio.
                decimal impuestoPorUnidad = CalculadoraFiscal.CalcularIvaUnitario(precioUnitario);

                return new Item
                {
                    Name = item.Name,
                    Description = item.Description,
                    Quantity = item.Quantity,
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
                            Value = CalculadoraFiscal.FormatearPayPal(pagar.TotalAmount), // <--- Usa el Total del Request
                            Breakdown = new AmountBreakdown
                            {
                                ItemTotal = new Money
                                {
                                    CurrencyCode = pagar.Currency,
                                    Value = CalculadoraFiscal.FormatearPayPal(pagar.Subtotal) // <--- Usa el Subtotal del Request
                                },
                                TaxTotal = new Money
                                {
                                    CurrencyCode = pagar.Currency,
                                    Value = CalculadoraFiscal.FormatearPayPal(pagar.Iva) // <--- Usa el Iva del Request
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
                            Name = new OrderShippingName { FullName = pagar.NombreCompleto },
                            Address = new OrderShippingAddress
                            {
                                AddressLine1 = pagar.Line1,
                                AddressLine2 = pagar.Line2 ?? "",
                                City = pagar.Ciudad,
                                State = "ES",
                                PostalCode = pagar.CodigoPostal.ToString(),
                                CountryCode = "ES"
                            }
                        }
                    }
                },
                PaymentSource = new PaymentSource
                {
                    Paypal = new PayPalWallet
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
        public async Task<OperationResult<OrderDetailsResponse>> CapturarPagoAsync(string orderId)
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
                return OperationResult<OrderDetailsResponse>.Ok(paypalResponse);
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<OrderDetailsResponse>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult<OrderDetailsResponse>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region Obtener detalles del pago  paypal   
        public async Task<OperationResult<OrderDetailsResponse>> ObtenerDetallesPagoEjecutadoAsync(string id)
        {
            try
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
                    throw new InvalidOperationException("No se puede obtener los detalles de la subscripcion");
                }
                return OperationResult<OrderDetailsResponse>.Ok(subscriptionDetails);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
           

        }
        #endregion

      
    }
}
