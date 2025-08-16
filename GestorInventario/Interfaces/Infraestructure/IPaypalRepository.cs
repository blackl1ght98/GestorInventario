using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalRepository
    {
        Task<List<SubscriptionDetail>> ObtenerSuscriptcionesActivas(string planId);
        Task<List<UserSubscription>> SusbcripcionesUsuario(string planId);
        Task<SubscriptionDetail> ObtenerSubscripcion(string subscription_id);
        Task SavePlanPriceUpdateAsync(string planId, UpdatePricingPlan planPriceUpdate);
        Task SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails);
        Task UpdatePlanStatusAsync(string planId, string status);
        Task<(Pedido Pedido, decimal TotalAmount)> GetPedidoWithDetailsAsync(int pedidoId);
      
        Task UpdatePedidoStatusAsync(int pedidoId, string status, string refundId);
        Task UpdatePlanStatusInDatabase(string planId, string status);
        Task<PlanDetail> ObtenerPlan(string planId);
        Task<(DetallePedido Detalle, decimal PrecioProducto)> GetProductoDePedidoAsync(int detallePedidoId);
        Task<(Pedido Pedido, List<DetallePedido> Detalles)> GetPedidoConDetallesAsync(int pedidoId);
        Task AddInfoTrackingOrder(int pedidoId, string tracking, string url, string carrier);
        List<BillingCycle> MapBillingCycles(List<BillingCycle> billingCycles);
        Frequency MapFrequency(Frequency frequency);
        PricingScheme MapPricingScheme(PricingScheme pricingScheme);
        Money MapMoney(Money money);
        Taxes MapTaxes(Taxes taxes);
        List<string> GetCategoriesFromEnum();

    }
}
