using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Repositories
{
    public interface IPaypalRepository
    {
        //Consultas
        Task<List<SubscriptionDetails>> ObtenerSuscriptcionesActivas(string planId);
        IQueryable<SubscriptionDetails> ObtenerSubscripciones();
        IQueryable<UserSubscription> ObtenerSubscripcionesUsuario(int usuarioId);
        Task<PlanDetails> ObtenerPlanPorIdAsync(string planId);
        Task<SubscriptionDetails> ObtenerSubscriptionIdAsync(string subscriptionId);
        Task<Rembolso> ObtenRembolsoAsync(string numeroPedido);
        Task<UserSubscription> ObtenerSubscricionUsuarioAsync(int userId, string subscriptionId);
        Task<List<UserSubscription>> ObtenerSusbcripcionesUsuario(string planId);
        IQueryable<PlanDetails> ObtenerPlanes();

    
        //Operaciones
        Task<OperationResult<PlanDetails>> AgregarPlanAsync(PlanDetails plan);
        Task<OperationResult<Rembolso>> AgregarRembolsoAsync(Rembolso rembolso);
        Task<OperationResult<Rembolso>> ActualizarRembolsoAsync(Rembolso rembolso);     
        Task<OperationResult<SubscriptionDetails>> AgregarDetallesSubscripcionAsync(SubscriptionDetails subscripcion);
        Task<OperationResult<SubscriptionDetails>> ActualizarDetallesSubscripcionAsync(SubscriptionDetails subscripcion);
        Task<OperationResult<PlanDetails>> ActualizarPlanAsync(PlanDetails plan);
        Task<OperationResult<UserSubscription>> AgregarSubscripcionUsuarioAsync(UserSubscription subscripcion);
      
        List<string> GetCategoriesFromEnum();
        Task<PayPalPaymentDetails> ObtenerDetallePagoPorId(string paymentId);
        
            
    }
}
