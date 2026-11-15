using System.Net.Http.Headers;
using GestorInventario.Application.Interfaces.Infrastructure.Paypal;
using GestorInventario.Infrastructure.Paypal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Infrastructure.Configuration
{
    public static class PayPalHttpClientExtensions
    {
        public static IServiceCollection AddPayPalHttpClient(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            var envConfig = new EnvironmentConfig(configuration);
            var baseUrl = envConfig.PaypalBaseUrl;

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
