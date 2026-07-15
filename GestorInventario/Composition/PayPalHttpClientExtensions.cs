using GestorInventario.Application.Services.ExternalServices;
using GestorInventario.Interfaces.Application.ExternalServices;
using System.Net.Http.Headers;

namespace GestorInventario.Composition
{
    public static class PayPalHttpClientExtensions
    {
        public static IServiceCollection AddPayPalHttpClient(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            var baseUrl = configuration["PayPal:BaseUrl"] ?? Environment.GetEnvironmentVariable("BaseUrl");

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
