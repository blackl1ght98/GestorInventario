using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.enums;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalService
    {
        Task<string> GetAccessTokenAsync(string clientId, string clientSecret);
        Task<string> CreateOrderWithPaypalAsync(CheckoutDto pagar);
        Task<(string CaptureId, string Total, string Currency)> CapturarPagoAsync(string orderId);
        Task<CheckoutDetailsDto> ObtenerDetallesPagoEjecutadoV2(string id);
        Task<string> RefundSaleAsync(int pedidoId, string currency);
        Task<CreateProductResponseDto> CreateProductAsync(string productName, string productDescription, string productType, string productCategory);
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
        Task<PaypalPlanResponseDto> ObtenerDetallesPlan(string id);
        Task<(PaypalProductListResponseDto ProductsResponse, bool HasNextPage)> GetProductsAsync(int page = 1, int pageSize = 10);
        Task<(List<PaypalPlanResponseDto> plans, bool HasNextPage)> GetSubscriptionPlansAsyncV2(int page = 1, int pageSize = 6);
        Task<CreateProductResponseDto> CreateProductAndNotifyAsync(string productName, string productDescription, string productType, string productCategory);
        Task<string> SeguimientoPedido(int pedidoId, Carrier carrier, BarcodeType barcode);
        Task<string> RefundPartialAsync(int pedidoId, string currency, string motivo);

    }
}
