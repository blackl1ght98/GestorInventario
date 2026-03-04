using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Interfaces.Application;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace GestorInventario.Application.Services.External_Sevices
{
    public class PayPalHttpClient: IPayPalHttpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PayPalHttpClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string> GetAccessTokenAsync(string clientId, string clientSecret)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var tokenUrl = "v1/oauth2/token";
            var byteArray = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
            var authHeader = Convert.ToBase64String(byteArray);

            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            });

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonConvert.DeserializeObject<TokenResponsePayPalDto>(responseString);
            if (responseJson?.AccessToken == null)
            {
                throw new InvalidOperationException("No se pudo obtener el token de acceso.");
            }

            return responseJson.AccessToken;
        }
        private (string clientId, string clientSecret) GetPaypalCredentials()
        {
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            if (clientId == null || clientSecret == null)
            {
                throw new InvalidOperationException("No se puede obtener el cliente id o secreto de cliente");
            }
            return (clientId, clientSecret);
        }
        public async Task<T> ExecutePayPalRequestAsync<T>(
          HttpMethod method,
          string endpoint,
          object? content = null,
          string? rawJsonBody = null,
          Func<HttpResponseMessage, Task>? onError = null)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (clientId, clientSecret) = GetPaypalCredentials();
            var token = await GetAccessTokenAsync(clientId, clientSecret);

            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("No se pudo obtener token de PayPal");

            var request = new HttpRequestMessage(method, endpoint)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
            };

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (rawJsonBody != null)
            {
                request.Content = new StringContent(rawJsonBody, Encoding.UTF8, "application/json");
            }
            else if (content != null)
            {
                var json = JsonConvert.SerializeObject(content);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (onError != null) await onError(response);
                throw new HttpRequestException($"PayPal error {response.StatusCode} → {body}");
            }

            if (typeof(T) == typeof(string))
                return (T)(object)body;

            var result = JsonConvert.DeserializeObject<T>(body);
            return result ?? throw new InvalidOperationException("Respuesta deserializada es null");
        }
        // 1. Para POST/PATCH con objeto (el más común para creación/actualización)
        public async Task<T> ExecutePayPalRequestAsync<T>(
            HttpMethod method,              // POST, PATCH, etc.
            string endpoint,
            object content,                 // DTO o anónimo → se serializa
            Func<HttpResponseMessage, Task>? onError = null)
        {
            return await ExecutePayPalRequestAsync<T>(method, endpoint, content, null, onError);
        }

        // 2. Para POST/PATCH con body JSON crudo (casos raros como "{}" especial)
        public async Task<T> ExecutePayPalRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            string rawJsonBody,
            Func<HttpResponseMessage, Task>? onError = null)
        {
            return await ExecutePayPalRequestAsync<T>(method, endpoint, null, rawJsonBody, onError);
        }

        // 3. Para GET / DELETE / HEAD (sin body nunca)
        public async Task<T> ExecutePayPalRequestAsync<T>(
            HttpMethod method,              // GET, DELETE, etc.
            string endpoint,
            Func<HttpResponseMessage, Task>? onError = null)
        {

            return await ExecutePayPalRequestAsync<T>(method, endpoint, null, null, onError);
        }

    }
}
