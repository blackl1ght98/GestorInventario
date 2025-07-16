using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalService
    {
      
       
      
       

        Task<string> CreateOrderAsyncV2(Checkout pagar);
        

       Task<CheckoutDetails> ObtenerDetallesPagoEjecutadoV2(string id);
  
        Task<string> RefundSaleAsync(int pedidoId, string currency);
        Task<string> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency, int trialDays = 0, decimal trialAmount = 0.00m);
        Task<HttpResponseMessage> CreateProductAsync(string productName, string productDescription, string productType, string productCategory);
        Task<string> DesactivarPlan( string planId);
        Task<string> MarcarDesactivadoProducto(string id);
        Task<string> EditarProducto(string id, string name, string description);
        Task<string> Subscribirse(string id, string returnUrl, string cancelUrl, string planName);
        Task<PaypalSubscriptionResponse> ObtenerDetallesSuscripcion(string subscription_id);
   
        Task<PaypalPlanResponse> ObtenerDetallesPlan(string id);
        // Task<(string ProductsResponse, bool HasNextPage)> GetProductsAsync(int page = 1, int pageSize = 10);
        Task<(PaypalProductListResponse ProductsResponse, bool HasNextPage)> GetProductsAsync(int page = 1, int pageSize = 10);
        Task<string> CancelarSuscripcion(string subscription_id, string reason);
        Task<string> GetAccessTokenAsync(string clientId, string clientSecret);
       
    
        Task<(List<PaypalPlanResponse> plans, bool HasNextPage)> GetSubscriptionPlansAsyncV2(int page = 1, int pageSize = 6);
        Task<string> CreateProductAndNotifyAsync(string productName, string productDescription, string productType, string productCategory);


    }
}
