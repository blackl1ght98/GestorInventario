using GestorInventario.Application.Classes;
using GestorInventario.Domain.Models.ViewModels.Paypal;


namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalService
    {
      
       
      
        Task<string> GetAccessTokenAsync();  
        Task<string> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency, int trialDays = 0, decimal trialAmount = 0.00m);
        Task<HttpResponseMessage> CreateProductAsync(string productName, string productDescription, string productType, string productCategory);
        Task<string> DesactivarPlan(string productId, string planId);
        Task<string> MarcarDesactivadoProducto(string id);
        Task<string> EditarProducto(string id, string name, string description);
        Task<string> Subscribirse(string id, string returnUrl, string cancelUrl, string planName);
        Task<dynamic> ObtenerDetallesSuscripcion(string subscription_id);
        Task<dynamic> ObtenerDetallesPlan(string id);
        Task<dynamic> ObtenerDetallesPagoEjecutado(string id);
        Task<(string ProductsResponse, bool HasNextPage)> GetProductsAsync(int page = 1, int pageSize = 10);
      
        Task<string> CancelarSuscripcion(string subscription_id, string reason);
      
        Task<string> RefundSaleAsync(int pedidoId, string currency);
       
        Task<string> CreateOrderAsync(Checkout pagar);
        Task<string> GetAccessTokenAsync(string clientId, string clientSecret);
        Task<(List<Plan> plans, bool HasNextPage)> GetSubscriptionPlansAsync(int page = 1, int pageSize = 10);
    }
}
