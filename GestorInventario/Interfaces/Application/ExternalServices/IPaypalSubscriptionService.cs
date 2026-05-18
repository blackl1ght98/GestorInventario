using GestorInventario.Application.DTOs;

using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Subscription;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalSubscriptionService
    {
        Task<OperationResult<string>> CreateProductAsync(string productName, string productDescription, string productType, string productCategory);
        Task<OperationResult<string>> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency, string intervalUnit, int trialDays = 0, decimal trialAmount = 0.00m);
        Task<OperationResult<PaypalPlanDetailsDto>> ObtenerDetallesPlan(string id);
        Task<OperationResult<(PaypalProductListResponseDto ProductsResponse, bool HasNextPage)>> GetProductsAsync(int page = 1, int pageSize = 10);
        Task<OperationResult<(List<PaypalPlanResponseDto> plans, bool HasNextPage)>> GetSubscriptionPlansAsync(int page = 1, int pageSize = 6);
        Task<OperationResult<string>> EditarProducto(string id, string name, string description);
        Task<OperationResult<(string, UpdatePricingPlanDto)>> UpdatePricingPlanAsync(string planId, decimal? trialAmount, decimal regularAmount, string currency);
        Task<OperationResult<string>> Subscribirse(string id, string returnUrl, string cancelUrl, string planName);
        Task<PaypalSubscriptionResponse> ObtenerDetallesSuscripcion(string subscription_id);
        Task<string> DesactivarPlan(string planId);
        Task<string> ActivarPlan(string planId);
        Task<string> CancelarSuscripcion(string subscription_id, string reason);
        Task<string> SuspenderSuscripcion(string subscription_id, string reason);
        Task<string> ActivarSuscripcion(string subscription_id, string reason);
      
    }
}
