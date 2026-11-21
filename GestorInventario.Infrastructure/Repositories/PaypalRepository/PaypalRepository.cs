using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Domain.enums.Paypal;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Data;
using GestorInventario.Infrastructure.Extensions;
using GestorInventario.Shared.Utilities;
using Microsoft.EntityFrameworkCore;



namespace GestorInventario.Infrastructure.Repositories.PaypalRepository
{
    public class PaypalRepository : IPaypalRepository
    {
        public readonly GestorInventarioContext _context;    
          
       
  
        public PaypalRepository(GestorInventarioContext context)
        {
            _context = context;          
                   
         
        }
        public async Task<PayPalPaymentDetails> ObtenerDetallePagoPorId(string paymentId)
        {
            var paypal = await _context.PayPalPaymentDetails.Include(x => x.PayPalPaymentCaptures).Include(x => x.PayPalPaymentItems).Include(x => x.PayPalPaymentShipping).Include(x=>x.PayPalPaymentRefunds).FirstOrDefaultAsync(x => x.Id == paymentId);
            return paypal;
        }
        public async Task<List<SubscriptionDetails>> ObtenerSuscriptcionesActivas(string planId)
        {
            return await _context.SubscriptionDetails
                .Where(s => s.PlanId == planId && s.Status != "CANCELLED")
                .ToListAsync();
        }
        public async Task<PlanDetails> ObtenerPlanPorIdAsync(string planId)
        {
            var plan= await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);
            return plan;
        }

       
        public IQueryable<PlanDetails> ObtenerPlanes()
        {
            return _context.PlanDetails
                           .OrderBy(s => s.PaypalPlanId)
                           .AsQueryable();
        }
        public async Task<List<UserSubscription>> ObtenerSusbcripcionesUsuario(string planId)
        {
            return await _context.UserSubscription
                .Where(us => us.PaypalPlanId == planId)
                .ToListAsync();
        }
        public IQueryable<SubscriptionDetails> ObtenerSubscripciones()
        {
            return _context.SubscriptionDetails
                           .OrderBy(s => s.SubscriptionId)     
                           .AsQueryable();
        }
        public  IQueryable<UserSubscription> ObtenerSubscripcionesUsuario(int usuarioId)
        {
            var queryable = _context.UserSubscription.Include(x=>x.Subscription)
                   .Include(x => x.User)
                   .Where(x => x.UserId == usuarioId);
            return queryable;
        }
        public async Task<Rembolso> ObtenRembolsoAsync(string numeroPedido) => await _context.Rembolso.FirstOrDefaultAsync(x => x.NumeroPedido == numeroPedido);
        public async Task<SubscriptionDetails> ObtenerSubscriptionIdAsync(string subscriptionId) => await _context.SubscriptionDetails.FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
        public async Task<UserSubscription> ObtenerSubscricionUsuarioAsync(int userId, string subscriptionId) => await _context.UserSubscription.FirstOrDefaultAsync(us => us.UserId == userId && us.SubscriptionId == subscriptionId);
        public async Task<OperationResult<PlanDetails>> AgregarPlanAsync(PlanDetails plan)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(plan);
                return OperationResult<PlanDetails>.Ok( plan);
            });
        }

        public async Task<OperationResult<PlanDetails>> ActualizarPlanAsync(PlanDetails plan)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(plan);
                return OperationResult<PlanDetails>.Ok( plan);
            });
        }
        public async Task<OperationResult<Rembolso>> AgregarRembolsoAsync(Rembolso rembolso)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(rembolso);
                return OperationResult<Rembolso>.Ok( rembolso);
            });
        }
        public async Task<OperationResult<Rembolso>> ActualizarRembolsoAsync(Rembolso rembolso)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(rembolso);
                return OperationResult<Rembolso>.Ok(rembolso);
            });
        }
        public async Task<OperationResult<SubscriptionDetails>> AgregarDetallesSubscripcionAsync(SubscriptionDetails subscripcion)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(subscripcion);
                return OperationResult<SubscriptionDetails>.Ok( subscripcion);
            });
        }
        public async Task<OperationResult<SubscriptionDetails>> ActualizarDetallesSubscripcionAsync(SubscriptionDetails subscripcion)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(subscripcion);
                return OperationResult<SubscriptionDetails>.Ok( subscripcion);
            });
        }
        public async Task<OperationResult<UserSubscription>> AgregarSubscripcionUsuarioAsync(UserSubscription subscripcion)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(subscripcion);
                return OperationResult<UserSubscription>.Ok( subscripcion);
            });
        }
       
        
        // Método para crear la lista de categorías a partir de la enumeración
        public List<string> GetCategoriesFromEnum()
        {
            return Enum.GetNames(typeof(Category)).ToList();
        }
     
     

    }

}