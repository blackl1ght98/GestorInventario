using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

        public async Task<List<UserSubscription>> SusbcripcionesUsuario(string planId)
        {
            return await _context.UserSubscriptions
                .Where(us => us.PaypalPlanId == planId)
                .ToListAsync();
        }

        public async Task<SubscriptionDetail> ObtenerSubscripcion(string subscription_id)
        {
            return await _context.SubscriptionDetails
                .FirstOrDefaultAsync(x => x.SubscriptionId == subscription_id);
        }

        public async Task SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails)
        {
            try
            {
                // Verificar si el plan ya existe
                var existingPlan = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);
                if (existingPlan != null)
                {
                    _logger.LogInformation($"El plan con ID {planId} ya existe en la base de datos.");
                    return;
                }

                var planDetail = new PlanDetail
                {
                    Id = Guid.NewGuid().ToString(),
                    PaypalPlanId = planId,
                    ProductId = planDetails.ProductId,
                    Name = planDetails.Name,
                    Description = planDetails.Description,
                    Status = planDetails.Status,
                    AutoBillOutstanding = planDetails.PaymentPreferences.AutoBillOutstanding,
                    SetupFee = planDetails.PaymentPreferences.SetupFee?.Value != null
                        ? decimal.Parse(planDetails.PaymentPreferences.SetupFee.Value, CultureInfo.InvariantCulture)
                        : 0,
                    SetupFeeFailureAction = planDetails.PaymentPreferences.SetupFeeFailureAction,
                    PaymentFailureThreshold = planDetails.PaymentPreferences.PaymentFailureThreshold,
                    TaxPercentage = planDetails.Taxes?.Percentage != null
                        ? decimal.Parse(planDetails.Taxes.Percentage, CultureInfo.InvariantCulture)
                        : 0,
                    TaxInclusive = planDetails.Taxes?.Inclusive ?? false
                };

                // Manejar ciclos de facturación
                if (planDetails.BillingCycles.Length > 1)
                {
                    planDetail.TrialIntervalUnit = planDetails.BillingCycles[0].Frequency.IntervalUnit;
                    planDetail.TrialIntervalCount = planDetails.BillingCycles[0].Frequency.IntervalCount;
                    planDetail.TrialTotalCycles = planDetails.BillingCycles[0].TotalCycles;
                    planDetail.TrialFixedPrice = planDetails.BillingCycles[0].PricingScheme.FixedPrice?.Value != null
                        ? decimal.Parse(planDetails.BillingCycles[0].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture)
                        : 0;

                    planDetail.RegularIntervalUnit = planDetails.BillingCycles[1].Frequency.IntervalUnit;
                    planDetail.RegularIntervalCount = planDetails.BillingCycles[1].Frequency.IntervalCount;
                    planDetail.RegularTotalCycles = planDetails.BillingCycles[1].TotalCycles;
                    planDetail.RegularFixedPrice = planDetails.BillingCycles[1].PricingScheme.FixedPrice?.Value != null
                        ? decimal.Parse(planDetails.BillingCycles[1].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture)
                        : 0;
                }
                else if (planDetails.BillingCycles.Length == 1)
                {
                    planDetail.RegularIntervalUnit = planDetails.BillingCycles[0].Frequency.IntervalUnit;
                    planDetail.RegularIntervalCount = planDetails.BillingCycles[0].Frequency.IntervalCount;
                    planDetail.RegularTotalCycles = planDetails.BillingCycles[0].TotalCycles;
                    planDetail.RegularFixedPrice = planDetails.BillingCycles[0].PricingScheme.FixedPrice?.Value != null
                        ? decimal.Parse(planDetails.BillingCycles[0].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture)
                        : 0;
                }

                _context.PlanDetails.Add(planDetail);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Detalles del plan {planId} guardados exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al guardar los detalles del plan {planId}");
                throw;
            }
        }

        public async Task UpdatePlanStatusAsync(string planId, string status)
        {
            var planDetails = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);
            if (planDetails != null)
            {
                planDetails.Status = status;
                _context.PlanDetails.Update(planDetails);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Estado del plan {planId} actualizado a {status}");
            }
        }

        public async Task<(Pedido Pedido, decimal TotalAmount)> GetPedidoWithDetailsAsync(int pedidoId)
        {
            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallePedidos)
                    .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.Id == pedidoId);

                if (pedido == null || string.IsNullOrEmpty(pedido.SaleId))
                    throw new ArgumentException("Pedido no encontrado o SaleId no disponible.");

                if (string.IsNullOrEmpty(pedido.Currency))
                    throw new ArgumentException("El código de moneda no está definido.");

                decimal totalAmount = pedido.DetallePedidos.Sum(d => d.Producto.Precio * (d.Cantidad ?? 0));

                return (pedido, totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el pedido {pedidoId}");
                throw;
            }
        }

        public async Task UpdatePedidoStatusAsync(int pedidoId, string status,string refundId)
        {
            try
            {
                var pedido = await _context.Pedidos.FindAsync(pedidoId);
                if (pedido == null)
                    throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

                pedido.EstadoPedido = status;
                pedido.RefundId = refundId;

                _context.Update(pedido);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Estado del pedido {pedidoId} actualizado a {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el estado del pedido {pedidoId}");
                throw;
            }
        }
        public async Task UpdatePlanStatusInDatabase(string planId, string status)
        {
            var planDetails = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);
            if (planDetails != null)
            {
                planDetails.Status = status;
                _context.PlanDetails.Update(planDetails);
                await _context.SaveChangesAsync();
            }
        }
      
      
        
    }
}