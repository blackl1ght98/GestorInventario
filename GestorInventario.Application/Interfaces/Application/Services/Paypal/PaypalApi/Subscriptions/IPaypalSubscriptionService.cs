using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Subscriptions
{
    public interface IPaypalSubscriptionService
    {
        Task<OperationResult<string>> CreateProductAsync(string productName, string productDescription, string productType, string productCategory);
        Task<OperationResult<string>> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency, string intervalUnit, int trialDays = 0, decimal trialAmount = 0.00m);
        Task<OperationResult<PlanDetailsDto>> ObtenerDetallesPlan(string id);
        Task<OperationResult<ProductsPageDto>> GetProductsAsync(int page = 1, int pageSize = 10);

        Task<OperationResult<SubscriptionPlansPageDto>> GetSubscriptionPlansAsync(
            int page = 1, int pageSize = 6);
        Task<OperationResult<string>> EditarProducto(string id, string name, string description);

        Task<OperationResult<PlanPriceUpdateResultDto>> UpdatePricingPlanAsync(
            string planId,
            decimal? trialAmount,
            decimal regularAmount,
            string currency);
        Task<OperationResult<string>> Subscribirse(string id, string returnUrl, string cancelUrl, string planName);
        Task<OperationResult<PaypalSubscriptionDto>> ObtenerDetallesSuscripcion(string subscriptionId);
        Task<OperationResult<(string planId, string planStatus)>> DesactivarPlan(string planId);
        Task<OperationResult<(string planId, string planStatus)>> ActivarPlan(string planId);
        Task<OperationResult<(string subId, string subStatus)>> CancelarSuscripcion(string subscriptionId, string reason);
        Task<OperationResult<(string subId, string subStatus)>> SuspenderSuscripcion(string subscriptionId, string reason);
        Task<OperationResult<(string subId, string subStatus)>> ActivarSuscripcion(string subscriptionId, string reason);
      
    }
}
