using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalService
    {
        Task<string> GetAccessTokenAsync(string clientId, string clientSecret);
        Task<string> CreateOrderWithPaypalAsync(Checkout pagar);
        Task<(string CaptureId, string Total, string Currency)> CapturarPagoAsync(string orderId);
        Task<CheckoutDetails> ObtenerDetallesPagoEjecutadoV2(string id);
        Task<string> RefundSaleAsync(int pedidoId, string currency);
        Task<HttpResponseMessage> CreateProductAsync(string productName, string productDescription, string productType, string productCategory);
        Task<string> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency, int trialDays = 0, decimal trialAmount = 0.00m);
        Task<string> Subscribirse(string id, string returnUrl, string cancelUrl, string planName);
        Task<string> DesactivarPlan( string planId);
        Task<string> ActivarPlan(string planId);
        Task<string> CancelarSuscripcion(string subscription_id, string reason);
        Task<string> SuspenderSuscripcion(string subscription_id,string reason);
        Task<string> ActivarSuscripcion(string subscription_id, string reason);
        Task<string> EditarProducto(string id, string name, string description);

        Task<string> UpdatePricingPlanAsync(string planId, decimal? trialAmount, decimal regularAmount, string currency);
        Task<PaypalSubscriptionResponse> ObtenerDetallesSuscripcion(string subscription_id);  
        Task<PaypalPlanResponse> ObtenerDetallesPlan(string id);
        Task<(PaypalProductListResponse ProductsResponse, bool HasNextPage)> GetProductsAsync(int page = 1, int pageSize = 10);
       
    
        Task<(List<PaypalPlanResponse> plans, bool HasNextPage)> GetSubscriptionPlansAsyncV2(int page = 1, int pageSize = 6);
        Task<string> CreateProductAndNotifyAsync(string productName, string productDescription, string productType, string productCategory);
        


    }
}
