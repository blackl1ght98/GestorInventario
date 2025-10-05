using System.Net.Http.Headers;

namespace GestorInventario.MetodosExtension.Metodos_program.cs
{
    public static class HttpClientPayPal
    {
        public static IServiceCollection AddHttpClientPayPal(this IServiceCollection services)
        {
            services.AddHttpClient("PayPal", client =>
            {
                client.BaseAddress = new Uri("https://api-m.sandbox.paypal.com/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
            return services;
        }
    }
}
