using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalRepository
    {
        Task<List<SubscriptionDetail>> ObtenerSuscriptcionesActivas(string planId);
        Task<List<UserSubscription>> SusbcripcionesUsuario(string planId);
        Task<IQueryable<SubscriptionDetail>> ObtenerSubscripciones();
        Task<IQueryable<UserSubscription>> ObtenerSubscripcionesUsuario(int usuarioId);
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
        List<string> GetCategoriesFromEnum();
        List<BillingCycle> MapBillingCycles(List<BillingCycle> billingCycles);
        Taxes MapTaxes(Taxes taxes);
        Task RegistrarReembolsoParcialAsync(int pedidoId, int detalleId, string status, string refundId, decimal montoReembolsado, string motivo, string estadoVenta);
        Task SaveOrUpdateSubscriptionDetailsAsync(SubscriptionDetail subscriptionDetails);
        Task SaveUserSubscriptionAsync(int userId, string subscriptionId, string subscriberName, string planId);
        Task<SubscriptionDetail> CreateSubscriptionDetailAsync(dynamic subscriptionDetails, string planId, IPaypalService paypalService);
        Task UpdateSubscriptionStatusAsync(string subscriptionId, string status);
        Task<(bool Success, string Message)> EnviarEmailNotificacionRembolso(int pedidoId, decimal montoReembolsado, string motivo);
    }
}
