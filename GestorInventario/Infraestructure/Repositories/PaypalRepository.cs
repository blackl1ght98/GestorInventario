
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;

using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;



namespace GestorInventario.Infraestructure.Repositories
{
    public class PaypalRepository : IPaypalRepository
    {
        public readonly GestorInventarioContext _context;    
        private readonly ILogger<PaypalRepository> _logger;     
       
  
        public PaypalRepository(GestorInventarioContext context, ILogger<PaypalRepository> logger)
        {
            _context = context;
           
            _logger = logger;          
         
        }

        public async Task<List<SubscriptionDetail>> ObtenerSuscriptcionesActivas(string planId)
        {
            return await _context.SubscriptionDetails
                .Where(s => s.PlanId == planId && s.Status != "CANCELLED")
                .ToListAsync();
        }
        public async Task<PlanDetail> ObtenerPlanPorIdAsync(string planId)
        {
            var plan= await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);
            return plan;
        }
   
        public async Task<List<UserSubscription>> SusbcripcionesUsuario(string planId)
        {
            return await _context.UserSubscriptions
                .Where(us => us.PaypalPlanId == planId)
                .ToListAsync();
        }
        public IQueryable<SubscriptionDetail> ObtenerSubscripciones()
        {
            return _context.SubscriptionDetails
                           .OrderBy(s => s.SubscriptionId)     
                           .AsQueryable();
        }
        public  IQueryable<UserSubscription> ObtenerSubscripcionesUsuario(int usuarioId)
        {
            var queryable = _context.UserSubscriptions.Include(x=>x.Subscription)
                   .Include(x => x.User)
                   .Where(x => x.UserId == usuarioId);
            return queryable;
        }

        public async Task<OperationResult<PlanDetail>> AgregarPlanAsync(PlanDetail plan)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(plan);
                return OperationResult<PlanDetail>.Ok("Plan creado", plan);
            });
        }

        public async Task<OperationResult<PlanDetail>> ActualizarPlanAsync(PlanDetail plan)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(plan);
                return OperationResult<PlanDetail>.Ok("Plan actualizado", plan);
            });
        }
        public async Task<OperationResult<Rembolso>> AgregarRembolsoAsync(Rembolso rembolso)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(rembolso);
                return OperationResult<Rembolso>.Ok("Plan creado", rembolso);
            });
        }
        public async Task<OperationResult<Rembolso>> ActualizarRembolsoAsync(Rembolso rembolso)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(rembolso);
                return OperationResult<Rembolso>.Ok("Plan creado", rembolso);
            });
        }
        public async Task<OperationResult<SubscriptionDetail>> AgregarDetallesSubscripcionAsync(SubscriptionDetail subscripcion)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(subscripcion);
                return OperationResult<SubscriptionDetail>.Ok("Detalles de subscripcion creada", subscripcion);
            });
        }
        public async Task<OperationResult<SubscriptionDetail>> ActualizarDetallesSubscripcionAsync(SubscriptionDetail subscripcion)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(subscripcion);
                return OperationResult<SubscriptionDetail>.Ok("Detalles de subscripcion creada", subscripcion);
            });
        }
        public async Task<OperationResult<UserSubscription>> AgregarSubscripcionUsuarioAsync(UserSubscription subscripcion)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(subscripcion);
                return OperationResult<UserSubscription>.Ok("Detalles de subscripcion creada", subscripcion);
            });
        }
        public async Task<Rembolso> ObtenRembolsoAsync(string numeroPedido) => await _context.Rembolsos.FirstOrDefaultAsync(x => x.NumeroPedido == numeroPedido);
        public async Task<SubscriptionDetail> ObtenerSubscriptionIdAsync(string subscriptionId) => await _context.SubscriptionDetails.FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
        public async Task<UserSubscription> ObtenerSubscricionUsuarioAsync(int userId, string subscriptionId) => await _context.UserSubscriptions.FirstOrDefaultAsync(us => us.UserId == userId && us.SubscriptionId == subscriptionId);
        // Método para crear la lista de categorías a partir de la enumeración
        public List<string> GetCategoriesFromEnum()
        {
            return Enum.GetNames(typeof(Category)).ToList();
        }
     
     

    }

}