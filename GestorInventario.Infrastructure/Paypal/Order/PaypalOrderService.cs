using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Checkout;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Domain.Common;
using GestorInventario.Infrastructure.ExternalServices;
using GestorInventario.Infrastructure.ExternalServices.Common;
using GestorInventario.Infrastructure.Helpers.Formaters;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.Paypal.Order
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
        public async Task<OperationResult<CreateOrderResultDto>> CreateOrderWithPaypalAsync(CheckoutDto pagar)
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
                        throw new InvalidOperationException(
                            $"Error al crear orden: {resp.StatusCode} - {body}");
                    });

                // PayPalOrderResponse solo existe en Infrastructure
                var createdPayment = JsonConvert.DeserializeObject<PayPalOrderResponse>(responseBody);
                if (createdPayment == null || string.IsNullOrEmpty(createdPayment.Id))
                    return OperationResult<CreateOrderResultDto>.Fail(
                        "Respuesta de creación de orden vacía o inválida");

                var approvalUrl = createdPayment.Links?
                    .FirstOrDefault(x => x.Rel is "payer-action" or "approve")
                    ?.Href;

                if (string.IsNullOrEmpty(approvalUrl))
                    return OperationResult<CreateOrderResultDto>.Fail(
                        "PayPal no devolvió URL de aprobación");

                var dto = new CreateOrderResultDto
                {
                    OrderId = createdPayment.Id,
                    ApprovalUrl = approvalUrl,
                    Status = createdPayment.Status
                };

                return OperationResult<CreateOrderResultDto>.Ok(dto, "URL a la que redirigir");
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<CreateOrderResultDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult<CreateOrderResultDto>.Fail(
                    $"Ocurrió un error inesperado: {ex.Message}");
            }
        }
        private CreateOrderRequest BuildOrder(CheckoutDto pagar)
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
                        Value = PayPalFormatter.FormatearPayPal(precioUnitario),
                        CurrencyCode = pagar.Currency
                    },
                    Tax = new Money
                    {
                        Value = PayPalFormatter.FormatearPayPal(impuestoPorUnidad),
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
                            Value = PayPalFormatter.FormatearPayPal(pagar.TotalAmount), // <--- Usa el Total del Request
                            Breakdown = new AmountBreakdown
                            {
                                ItemTotal = new Money
                                {
                                    CurrencyCode = pagar.Currency,
                                    Value = PayPalFormatter.FormatearPayPal(pagar.Subtotal) // <--- Usa el Subtotal del Request
                                },
                                TaxTotal = new Money
                                {
                                    CurrencyCode = pagar.Currency,
                                    Value = PayPalFormatter.FormatearPayPal(pagar.Iva) // <--- Usa el Iva del Request
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
        public async Task<OperationResult<PayPalOrderDetailsDto>> CapturarPagoAsync(string orderId)
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
                
               var dto = PaypalMappingService.MapToOrderDetailsDto(paypalResponse);
                return OperationResult<PayPalOrderDetailsDto>.Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<PayPalOrderDetailsDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult<PayPalOrderDetailsDto>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region Obtener detalles del pago  paypal   
        public async Task<OperationResult<PayPalOrderDetailsDto>> ObtenerDetallesPagoEjecutadoAsync(string id)
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

                var paypalResponse = JsonConvert.DeserializeObject<OrderDetailsResponse>(responseBody)
                                     ?? throw new InvalidOperationException("No se pudo deserializar la respuesta de PayPal");

                var dto = PaypalMappingService.MapToOrderDetailsDto(paypalResponse);
                return OperationResult<PayPalOrderDetailsDto>.Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<PayPalOrderDetailsDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult<PayPalOrderDetailsDto>.Fail($"Error inesperado: {ex.Message}");
            }
        }
           

        }
        #endregion

       
    }

