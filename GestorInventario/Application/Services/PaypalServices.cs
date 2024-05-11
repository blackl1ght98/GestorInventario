using Newtonsoft.Json;
using PayPal.Api;
using System.Globalization;

namespace GestorInventario.Application.Services
{
    public class PaypalServices : IPaypalService
    {
        private readonly APIContext _apiContext;
        private readonly Payment _payment;
        private readonly IConfiguration _configuration;

        public PaypalServices(IConfiguration configuration)
        {
            _configuration = configuration;

            var clientId = configuration["Paypal:ClientId"];
            var clientSeecret = configuration["Paypal:ClientSecret"];
            var mode = configuration["Paypal:Mode"];
            var config = new Dictionary<string, string>
            {
                {"mode",mode },
                {"clientId",clientId },
                {"clientSecret", clientSeecret}
            };
            var accessToken = new OAuthTokenCredential(clientId, clientSeecret, config).GetAccessToken();
            _apiContext = new APIContext(accessToken);
            _payment = new Payment
            {
                intent = "sale",
                payer = new Payer { payment_method = "paypal" }
            };

        }
        //public async Task<Payment> CreateOrderAsync(decimal amount, string returnUrl, string cancelUrl, string currency)
        //{
        //    var apiContext = new APIContext(new OAuthTokenCredential(_configuration["Paypal:ClientId"], _configuration["Paypal:ClientSecret"]).GetAccessToken());
        //    var itemList = new ItemList()
        //    {
        //        items = new List<Item>()
        //        {
        //            new Item()
        //            {
        //                name="Membership Free",
        //                currency=currency,
        //                price= amount.ToString("0.00"),
        //                quantity="1",
        //                sku="membership"
        //            }

        //        }
        //    };
        //    var transaction = new Transaction()
        //    {
        //        amount = new Amount()
        //        {
        //            currency = currency,
        //            total = amount.ToString("0.00"),
        //            details = new Details()
        //            {
        //                subtotal = amount.ToString("0.00")
        //            },

        //        },

        //        item_list = itemList,
        //        description = "Membership Free"

        //    };
        //    var payment = new Payment()
        //    {
        //        intent = "sale",
        //        payer = new Payer() { payment_method = "paypal" },
        //        redirect_urls = new RedirectUrls()
        //        {
        //            return_url = returnUrl,
        //            cancel_url = cancelUrl,
        //        },
        //        transactions = new List<Transaction>() { transaction }
        //    };
        //    var settings = new JsonSerializerSettings
        //    {
        //        Culture = CultureInfo.InvariantCulture
        //    };
        //    var createdPayment = payment.Create(apiContext);
        //    return createdPayment;
        //}
        public async Task<Payment> CreateOrderAsync(List<Item> items,decimal amount, string returnUrl, string cancelUrl, string currency)
        {
            var apiContext = new APIContext(new OAuthTokenCredential(_configuration["Paypal:ClientId"], _configuration["Paypal:ClientSecret"]).GetAccessToken());
            var itemList = new ItemList()
            {
                items = items
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
                description = "Membership Free"

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
    }
}