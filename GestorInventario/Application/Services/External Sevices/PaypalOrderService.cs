using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs.Checkout;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.External_Sevices
{
    public class PaypalOrderService: IPaypalOrderService
    {
        private readonly ILogger<PaypalOrderService> _logger;
        private readonly IPaypalRepository _repo;
        private readonly IPayPalHttpClient _paypal;
    
        public PaypalOrderService(ILogger<PaypalOrderService> logger,
           IPayPalHttpClient paypal, IPaypalRepository repo)
        {
            _logger = logger;
           
            _paypal = paypal;
            _repo = repo;
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
            var jsonResponse = JsonConvert.DeserializeObject<PayPalOrderResponse>(responseBody);


            return responseBody;
        }
        private CreateOrderRequest BuildOrder(CheckoutDto pagar)
        {
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
                        Value = pagar.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),
                        Breakdown = new AmountBreakdown
                        {
                            ItemTotal = new Money
                            {
                                CurrencyCode = pagar.Currency,
                                Value = pagar.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)
                            },
                            TaxTotal = new Money
                            {
                                CurrencyCode = pagar.Currency,
                                Value = "0.00"
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
                    Items = pagar.Items.ConvertAll(item => new Item
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Quantity = item.Quantity.ToString(),
                        UnitAmount = new Money
                        {
                            Value = item.Price.ToString("F2", CultureInfo.InvariantCulture),
                            CurrencyCode = pagar.Currency
                        },
                        Tax = new Money
                        {
                            Value = "0.00",
                            CurrencyCode = pagar.Currency
                        },
                        Sku = item.Sku
                    }),
                    Shipping = new Shipping
                    {
                        Name = new DTOs.Response_paypal.POST.ShippingName
                        {
                            FullName = pagar.NombreCompleto
                        },
                        Address = new DTOs.Response_paypal.POST.ShippingAddress
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
        public async Task<(string CaptureId, string Total, string Currency)> CapturarPagoAsync(string orderId)
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


                var paypalResponse = JsonConvert.DeserializeObject<PaypalCaptureOrderResponseDto>(responseBody);

                var capture = paypalResponse?.PurchaseUnits?
                    .FirstOrDefault()?.Payments?
                    .Captures?.FirstOrDefault();

                if (capture == null || string.IsNullOrEmpty(capture.Id) ||
                    string.IsNullOrEmpty(capture.Amount?.Value) ||
                    string.IsNullOrEmpty(capture.Amount?.CurrencyCode))
                {
                    throw new Exception("No se pudo extraer la información de la captura del pago.");
                }

                return (capture.Id, capture.Amount.Value, capture.Amount.CurrencyCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al capturar el pago de PayPal");
                throw new InvalidOperationException("No se pudo capturar el pago de PayPal", ex);
            }
        }

        #endregion

        #region Obtener detalles del pago v2 paypal   
        public async Task<CheckoutDetailsDto> ObtenerDetallesPagoEjecutadoV2(string id)
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
            var subscriptionDetails = JsonConvert.DeserializeObject<CheckoutDetailsDto>(responseBody);
            if (subscriptionDetails == null)
            {
                throw new ArgumentNullException("No se puede obtener los detalles de la subscripcion");
            }
            return subscriptionDetails;

        }
        #endregion

      
    }
}
