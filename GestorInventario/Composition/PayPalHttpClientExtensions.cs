using GestorInventario.Application.Services.Paypal.PaypalApi;
using System.Net.Http.Headers;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi;

namespace GestorInventario.Composition
{
    public static class PayPalHttpClientExtensions
    {
        public static IServiceCollection AddPayPalHttpClient(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            var baseUrl = configuration["PayPal:BaseUrl"] ?? Environment.GetEnvironmentVariable("PAYPAL_BASEURL");

            services.AddHttpClient<IPayPalHttpClient, PayPalHttpClient>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });

            return services;
        }
    }
}
