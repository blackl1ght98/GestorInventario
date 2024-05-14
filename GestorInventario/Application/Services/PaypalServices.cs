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
        // Este es el constructor de la clase. Se llama automáticamente cuando se crea una nueva instancia de la clase.
        public PaypalServices(IConfiguration configuration)
        {
            // Aquí se asigna la configuración pasada al constructor a la variable privada _configuration.
            _configuration = configuration;
            // Aquí se obtienen los valores de configuración de PayPal del archivo de configuración.
            var clientId = configuration["Paypal:ClientId"];
            var clientSeecret = configuration["Paypal:ClientSecret"];
            var mode = configuration["Paypal:Mode"];
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
            var apiContext = new APIContext(new OAuthTokenCredential(_configuration["Paypal:ClientId"], _configuration["Paypal:ClientSecret"]).GetAccessToken());
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
            // Aquí se crea una nueva instancia de APIContext con el token de acceso.
            var apiContext = new APIContext(new OAuthTokenCredential(_configuration["Paypal:ClientId"], _configuration["Paypal:ClientSecret"]).GetAccessToken());
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
    }
}