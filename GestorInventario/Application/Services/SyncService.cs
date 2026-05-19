using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using System.Globalization;

namespace GestorInventario.Application.Services
{
    public class SyncService: ISyncService
    {
        private readonly IPaypalSubscriptionService _paypalSubscriptionService;
        private readonly IPaypalRepository _paypalRepository;

        public SyncService(IPaypalSubscriptionService paypalSubscriptionService, IPaypalRepository paypalRepository)
        {
            _paypalSubscriptionService = paypalSubscriptionService;
            _paypalRepository = paypalRepository;
        }

        public async Task<OperationResult<int>> SyncPlansFromPayPalAsync(int pagina)
        {
            var result = await _paypalSubscriptionService.GetSubscriptionPlansAsync(pagina, 10);
            if (!result.Success)
                return OperationResult<int>.Fail(result.Message);

            var planesPayPal = result.Data;
            int actualizados = 0;
            int creados = 0;

            foreach (var planDto in planesPayPal.plans)
            {
                var existingPlan = await _paypalRepository.ObtenerPlanPorIdAsync(planDto.Id);

                if (existingPlan != null)
                {
                    // ── Comparación campo a campo (igual que SubscriptionDetail) ──
                    bool hasChanges = !(
                        existingPlan.Name == planDto.Name &&
                        existingPlan.Description == planDto.Description &&
                        existingPlan.Status == planDto.Status &&
                        existingPlan.ProductId == planDto.ProductId &&

                        // Ciclos de prueba
                        existingPlan.TrialIntervalUnit == planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "TRIAL")?.Frequency?.IntervalUnit &&
                        existingPlan.TrialIntervalCount == planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "TRIAL")?.Frequency?.IntervalCount &&
                        existingPlan.TrialTotalCycles == planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "TRIAL")?.TotalCycles &&

                        // Ciclos regulares
                        existingPlan.RegularIntervalUnit == planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "REGULAR")?.Frequency?.IntervalUnit &&
                        existingPlan.RegularIntervalCount == planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "REGULAR")?.Frequency?.IntervalCount &&
                        existingPlan.RegularTotalCycles == planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "REGULAR")?.TotalCycles &&

                        // Moneda
                        existingPlan.CurrencyCode == planDto.BillingCycles?.FirstOrDefault()?.PricingScheme?.FixedPrice?.CurrencyCode
                    );

                    // Comparar precios decimales por si cambiaron
                    var trialPrice = planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "TRIAL")?.PricingScheme?.FixedPrice?.Value;
                    var regularPrice = planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "REGULAR")?.PricingScheme?.FixedPrice?.Value;

                    if (trialPrice != null && decimal.TryParse(trialPrice, CultureInfo.InvariantCulture, out var tPrice))
                        hasChanges = hasChanges || existingPlan.TrialFixedPrice != tPrice;

                    if (regularPrice != null && decimal.TryParse(regularPrice, CultureInfo.InvariantCulture, out var rPrice))
                        hasChanges = hasChanges || existingPlan.RegularFixedPrice != rPrice;

                    if (hasChanges)
                    {
                        // ── Actualizar campos directamente (sin mapper intermedio) ──
                        existingPlan.Name = planDto.Name;
                        existingPlan.Description = planDto.Description;
                        existingPlan.Status = planDto.Status;
                        existingPlan.ProductId = planDto.ProductId;

                        var trialCycle = planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "TRIAL");
                        var regularCycle = planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "REGULAR")
                                         ?? planDto.BillingCycles?.LastOrDefault();

                        if (trialCycle != null)
                        {
                            existingPlan.TrialIntervalUnit = trialCycle.Frequency?.IntervalUnit;
                            existingPlan.TrialIntervalCount = trialCycle.Frequency?.IntervalCount;
                            existingPlan.TrialTotalCycles = trialCycle.TotalCycles;
                            if (trialCycle.PricingScheme?.FixedPrice?.Value != null)
                                existingPlan.TrialFixedPrice = decimal.Parse(trialCycle.PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture);
                        }

                        if (regularCycle != null)
                        {
                            existingPlan.RegularIntervalUnit = regularCycle.Frequency?.IntervalUnit;
                            existingPlan.RegularIntervalCount = regularCycle.Frequency?.IntervalCount;
                            existingPlan.RegularTotalCycles = regularCycle.TotalCycles;
                            if (regularCycle.PricingScheme?.FixedPrice?.Value != null)
                                existingPlan.RegularFixedPrice = decimal.Parse(regularCycle.PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture);
                        }

                        existingPlan.CurrencyCode = planDto.BillingCycles?.FirstOrDefault()?.PricingScheme?.FixedPrice?.CurrencyCode;

                        await _paypalRepository.ActualizarPlanAsync(existingPlan);
                        actualizados++;

                    }
                }
                else
                {
                    // ── CREAR: el plan existe en PayPal pero no en BD ──
                    var nuevoPlan = new PlanDetail
                    {
                        Id = Guid.NewGuid().ToString(),
                        PaypalPlanId = planDto.Id,
                        Name = planDto.Name,
                        Description = planDto.Description,
                        Status = planDto.Status,
                        ProductId = planDto.ProductId
                    };

                    var trialCycle = planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "TRIAL");
                    var regularCycle = planDto.BillingCycles?.FirstOrDefault(c => c.TenureType == "REGULAR")
                                     ?? planDto.BillingCycles?.LastOrDefault();

                    if (trialCycle != null)
                    {
                        nuevoPlan.TrialIntervalUnit = trialCycle.Frequency?.IntervalUnit;
                        nuevoPlan.TrialIntervalCount = trialCycle.Frequency?.IntervalCount;
                        nuevoPlan.TrialTotalCycles = trialCycle.TotalCycles;
                        if (trialCycle.PricingScheme?.FixedPrice?.Value != null)
                            nuevoPlan.TrialFixedPrice = decimal.Parse(trialCycle.PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture);
                    }

                    if (regularCycle != null)
                    {
                        nuevoPlan.RegularIntervalUnit = regularCycle.Frequency?.IntervalUnit;
                        nuevoPlan.RegularIntervalCount = regularCycle.Frequency?.IntervalCount;
                        nuevoPlan.RegularTotalCycles = regularCycle.TotalCycles;
                        if (regularCycle.PricingScheme?.FixedPrice?.Value != null)
                            nuevoPlan.RegularFixedPrice = decimal.Parse(regularCycle.PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture);
                    }

                    nuevoPlan.CurrencyCode = planDto.BillingCycles?.FirstOrDefault()?.PricingScheme?.FixedPrice?.CurrencyCode;

                    await _paypalRepository.AgregarPlanAsync(nuevoPlan);
                    creados++;
                }
            }

            return OperationResult<int>.Ok(
                $"Sincronización completada. {actualizados} actualizado(s), {creados} creado(s).",
                actualizados + creados);
        }
    }
}
