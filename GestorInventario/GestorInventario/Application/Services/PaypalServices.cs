using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using Newtonsoft.Json;
using PayPal.Api;
using System.Globalization;

namespace GestorInventario.Application.Services
{
    public class PaypalServices : IPaypalService
    {
        // Estas son las variables privadas que se utilizarán en la clase.
        private readonly APIContext _apiContext;
        private readonly Payment _payment;
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly ILogger<PaypalServices> _logger;
     
        // Este es el constructor de la clase. Se llama automáticamente cuando se crea una nueva instancia de la clase.
        public PaypalServices(IConfiguration configuration, GestorInventarioContext context, ILogger<PaypalServices> logger )
        {
            // Aquí se asigna la configuración pasada al constructor a la variable privada _configuration.
            _configuration = configuration;
            _context= context;
            _logger = logger;
            // Aquí se obtienen los valores de configuración de PayPal del archivo de configuración.
            var clientId = configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSeecret = configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var mode = configuration["Paypal:Mode"] ?? Environment.GetEnvironmentVariable("Paypal_Mode");
            // Aquí se crea un diccionario con la configuración de PayPal.
            var config = new Dictionary<string, string>
            {
                {"mode",mode },
                {"clientId",clientId },
                {"clientSecret", clientSeecret}
            };
            // Aquí se obtiene el token de acceso de PayPal utilizando las credenciales del cliente.
            var accessToken = new OAuthTokenCredential(clientId, clientSeecret, config).GetAccessToken();
            // Aquí se crea una nueva instancia de APIContext con el token de acceso. APIContext se utiliza para realizar llamadas a la API de PayPal
            _apiContext = new APIContext(accessToken);
            // Aquí se crea una nueva instancia de Payment con la intención de "sale" y el método de pago "paypal".
            _payment = new Payment
            {
                intent = "sale",
                payer = new Payer { payment_method = "paypal" }
            };
            
        }
        public async Task<Payment> CreateDonation(decimal amount, string returnUrl, string cancelUrl, string currency)
        {
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSeecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var apiContext = new APIContext(new OAuthTokenCredential(clientId, clientSeecret).GetAccessToken());
            var itemList = new ItemList()
            {
                items = new List<Item>()
                {
                    new Item()
                    {
                        name="Donacion",
                        currency=currency,
                        price= amount.ToString("0.00"),
                        quantity="1",
                        sku="donacion"
                    }

                }
            };
            var transaction = new Transaction()
            {
                amount = new Amount()
                {
                    currency = currency,
                    total = amount.ToString("0.00"),
                    details = new Details()
                    {
                        subtotal = amount.ToString("0.00")
                    },

                },

                item_list = itemList,
                description = "Donacion"

            };
            var payment = new Payment()
            {
                intent = "sale",
                payer = new Payer() { payment_method = "paypal" },
                redirect_urls = new RedirectUrls()
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl,
                },
                transactions = new List<Transaction>() { transaction }
            };
            var settings = new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture
            };
            var createdPayment = payment.Create(apiContext);
            return createdPayment;
        }
        // Este es el método que se utiliza para crear un pedido en PayPal.
        public async Task<Payment> CreateOrderAsync(List<Item> items,decimal amount, string returnUrl, string cancelUrl, string currency)
        {
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSeecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            // Aquí se crea una nueva instancia de APIContext con el token de acceso.
            var apiContext = new APIContext(new OAuthTokenCredential(clientId, clientSeecret).GetAccessToken());
            // Aquí se crea una nueva instancia de ItemList con los items pasados al método.
            var itemList = new ItemList()
            {
                items = items
            };
            // Aquí se crea una nueva instancia de Transaction con la cantidad, la lista de items y la descripción.
            var transaction = new Transaction()
            {
                amount = new Amount()
                {
                    currency = currency,
                    total = amount.ToString("0.00"),
                    details = new Details()
                    {
                        subtotal = amount.ToString("0.00")
                    },

                },

                item_list = itemList,
                description = "Aquisicion de productos"

            };
            // Aquí se crea una nueva instancia de Payment con la intención de "sale", el pagador, las URL de redirección y las transacciones.
            var payment = new Payment()
            {
                intent = "sale",
                payer = new Payer() { payment_method = "paypal" },
                redirect_urls = new RedirectUrls()
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl,
                },
                transactions = new List<Transaction>() { transaction }
            };
            var settings = new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture
            };
            // Aquí se crea el pago en PayPal y se devuelve el pago creado.
            var createdPayment = payment.Create(apiContext);
            return createdPayment;
        }

        public async Task<Refund> RefundSaleAsync(int pedidoId, decimal refundAmount = 0, string currency = "EUR")
        {
            try
            {
                // Recuperar el pedido de la base de datos
                var pedido = await _context.Pedidos.FindAsync(pedidoId);

                if (pedido == null || string.IsNullOrEmpty(pedido.SaleId))
                {
                    throw new ArgumentException("Pedido no encontrado o SaleId no disponible.");
                }
                var saleId = pedido.SaleId;
                // Configuración de PayPal
                var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
                var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
                var apiContext = new APIContext(new OAuthTokenCredential(clientId, clientSecret).GetAccessToken());
                // Crear la solicitud de reembolso
                var refundRequest = new RefundRequest();
                if (refundAmount > 0)
                {
                    refundRequest.amount = new Amount
                    {
                        total = refundAmount.ToString("0.00"),
                        currency = currency
                    };
                }
                // Ejecutar el reembolso utilizando RefundTransaction
                var refund = new Refund();
                var sale = new Sale() { id = saleId };
                var response = sale.Refund(apiContext, refund);
                // Actualizar el estado del pedido a "Reembolsado"
                pedido.EstadoPedido = "Reembolsado";
                _context.Update(pedido);
                await _context.SaveChangesAsync();

                return refund;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el reembolso");
                throw new InvalidOperationException("No se pudo realizar el reembolso", ex);
            }
        }





    }
}