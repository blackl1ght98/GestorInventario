using GestorInventario.Application.Services.External_Sevices;
using GestorInventario.Interfaces.Application.ExternalServices;
using System.Net.Http.Headers;

namespace GestorInventario.MetodosExtension
{
    public static class HttpClientPayPal
    {
        public static IServiceCollection AddHttpClientPayPal(this IServiceCollection services)
        {
          
            services.AddHttpClient<IPayPalHttpClient, PayPalHttpClient>(client =>
            {
                client.BaseAddress = new Uri("https://api-m.sandbox.paypal.com/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
            return services;
        }
    }
}
