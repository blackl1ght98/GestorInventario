using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PaypalServiceRepository: IPaypalServiceRepository
    {
        private readonly GestorInventarioContext _context;

        public PaypalServiceRepository(GestorInventarioContext context)
        {
            _context = context;
        }

        public async Task SavePlanDetailsToDatabase(string createdPlanId, dynamic planRequest)
        {
            var planDetails = new PlanDetail
            {
                Id = Guid.NewGuid().ToString(),
                PaypalPlanId = createdPlanId,
                ProductId = planRequest.product_id,
                Name = planRequest.name,
                Description = planRequest.description,
                Status = planRequest.status,
                AutoBillOutstanding = planRequest.payment_preferences.auto_bill_outstanding,
                SetupFee = decimal.Parse(planRequest.payment_preferences.setup_fee.value, CultureInfo.InvariantCulture),
                SetupFeeFailureAction = planRequest.payment_preferences.setup_fee_failure_action,
                PaymentFailureThreshold = planRequest.payment_preferences.payment_failure_threshold,
                TaxPercentage = decimal.Parse(planRequest.taxes.percentage, CultureInfo.InvariantCulture),
                TaxInclusive = planRequest.taxes.inclusive
            };

            // Verificar si existe un ciclo de facturación de prueba
            if (planRequest.billing_cycles.Count > 1)
            {
                planDetails.TrialIntervalUnit = planRequest.billing_cycles[0].frequency.interval_unit;
                planDetails.TrialIntervalCount = planRequest.billing_cycles[0].frequency.interval_count;
                planDetails.TrialTotalCycles = planRequest.billing_cycles[0].total_cycles;
                planDetails.TrialFixedPrice = decimal.Parse(planRequest.billing_cycles[0].pricing_scheme.fixed_price.value, CultureInfo.InvariantCulture);

                // Información del ciclo regular
                planDetails.RegularIntervalUnit = planRequest.billing_cycles[1].frequency.interval_unit;
                planDetails.RegularIntervalCount = planRequest.billing_cycles[1].frequency.interval_count;
                planDetails.RegularTotalCycles = planRequest.billing_cycles[1].total_cycles;
                planDetails.RegularFixedPrice = decimal.Parse(planRequest.billing_cycles[1].pricing_scheme.fixed_price.value, CultureInfo.InvariantCulture);
            }
            else if (planRequest.billing_cycles.Count == 1)
            {
                // Solo hay ciclo regular
                planDetails.RegularIntervalUnit = planRequest.billing_cycles[0].frequency.interval_unit;
                planDetails.RegularIntervalCount = planRequest.billing_cycles[0].frequency.interval_count;
                planDetails.RegularTotalCycles = planRequest.billing_cycles[0].total_cycles;
                planDetails.RegularFixedPrice = decimal.Parse(planRequest.billing_cycles[0].pricing_scheme.fixed_price.value, CultureInfo.InvariantCulture);
            }

            _context.PlanDetails.Add(planDetails);
            await _context.SaveChangesAsync();
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
               
                throw;
            }
        }
        public async Task UpdatePedidoStatusAsync(int pedidoId, string status)
        {
            try
            {
                var pedido = await _context.Pedidos.FindAsync(pedidoId);
                if (pedido == null)
                    throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

                pedido.EstadoPedido = status;
                _context.Update(pedido);
                await _context.SaveChangesAsync();
                //_logger.LogInformation($"Estado del pedido {pedidoId} actualizado a {status}");
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, $"Error al actualizar el estado del pedido {pedidoId}");
                throw;
            }
        }
    }
}
