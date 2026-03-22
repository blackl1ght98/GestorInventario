using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using System.Globalization;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class SubscriptionService:ISubscriptionService
    {
        private readonly GestorInventarioContext _context;
        private readonly IPaypalService _paypalSevice;
        private readonly IPaypalRepository _paypalRepository;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(GestorInventarioContext context, IPaypalService paypalSevice, IPaypalRepository paypalRepository, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _paypalSevice = paypalSevice;
            _paypalRepository = paypalRepository;
            _logger = logger;
        }

        public async Task<SubscriptionDetail> CreateSubscriptionDetailAsync(dynamic subscriptionDetails, string planId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                // Check if plan exists
                var plan = await _paypalRepository.ObtenerPlan(planId);
                if (plan == null)
                {
                    var planResponse = await _paypalSevice.ObtenerDetallesPlan(planId);
                    var planDetails = new PaypalPlanDetailsDto
                    {
                        Id = planResponse.Id,
                        ProductId = planResponse.ProductId,
                        Name = planResponse.Name,
                        Description = planResponse.Description,
                        Status = planResponse.Status,
                        PaymentPreferences = new PaymentPreferencesDto
                        {
                            AutoBillOutstanding = planResponse.PaymentPreferences.AutoBillOutstanding,
                            SetupFee = planResponse.PaymentPreferences.SetupFee != null
                                ? new FixedPriceDto
                                {
                                    Value = planResponse.PaymentPreferences.SetupFee.Value.ToString(CultureInfo.InvariantCulture),
                                    CurrencyCode = planResponse.PaymentPreferences.SetupFee.CurrencyCode
                                }
                                : null,
                            SetupFeeFailureAction = planResponse.PaymentPreferences.SetupFeeFailureAction,
                            PaymentFailureThreshold = planResponse.PaymentPreferences.PaymentFailureThreshold
                        },
                        Taxes = planResponse.Taxes != null
                            ? new TaxesDto
                            {
                                Percentage = planResponse.Taxes.Percentage.ToString(CultureInfo.InvariantCulture),
                                Inclusive = planResponse.Taxes.Inclusive
                            }
                            : null,
                        BillingCycles = planResponse.BillingCycles.Select(b => new BillingCycleDto
                        {
                            TenureType = b.TenureType,
                            Sequence = b.Sequence,
                            Frequency = new FrequencyDto
                            {
                                IntervalUnit = b.Frequency.IntervalUnit,
                                IntervalCount = b.Frequency.IntervalCount
                            },
                            TotalCycles = b.TotalCycles,
                            PricingScheme = new PricingSchemeDto
                            {
                                FixedPrice = b.PricingScheme.FixedPrice != null
                                    ? new FixedPriceDto
                                    {
                                        Value = b.PricingScheme.FixedPrice.Value,
                                        CurrencyCode = b.PricingScheme.FixedPrice.CurrencyCode
                                    }
                                    : null
                            }
                        }).ToArray()
                    };

                    await _paypalRepository.SavePlanDetailsAsync(planId, planDetails);
                    plan = await _paypalRepository.ObtenerPlan(planId);
                }

                var minSqlDate = new DateTime(1753, 1, 1);

                var detallesSuscripcion = new SubscriptionDetail
                {
                    SubscriptionId = subscriptionDetails.Id ?? string.Empty,
                    PlanId = subscriptionDetails.PlanId ?? string.Empty,
                    Status = subscriptionDetails.Status ?? string.Empty,
                    StartTime = subscriptionDetails.StartTime ?? minSqlDate,
                    StatusUpdateTime = subscriptionDetails.StatusUpdateTime ?? minSqlDate,
                    SubscriberName = $"{subscriptionDetails.Subscriber?.Name?.GivenName ?? string.Empty} {subscriptionDetails.Subscriber?.Name?.Surname ?? string.Empty}".Trim(),
                    SubscriberEmail = subscriptionDetails.Subscriber?.EmailAddress ?? string.Empty,
                    PayerId = subscriptionDetails.Subscriber?.PayerId ?? string.Empty,
                    OutstandingBalance = subscriptionDetails.BillingInfo?.OutstandingBalance?.Value != null
                        ? Convert.ToDecimal(subscriptionDetails.BillingInfo.OutstandingBalance.Value)
                        : 0,
                    OutstandingCurrency = subscriptionDetails.BillingInfo?.OutstandingBalance?.CurrencyCode ?? string.Empty,
                    NextBillingTime = subscriptionDetails.BillingInfo?.NextBillingTime ?? minSqlDate,
                    LastPaymentTime = subscriptionDetails.BillingInfo?.LastPayment?.Time ?? minSqlDate,
                    LastPaymentAmount = subscriptionDetails.BillingInfo?.LastPayment?.Amount?.Value != null
                        ? Convert.ToDecimal(subscriptionDetails.BillingInfo.LastPayment.Amount.Value)
                        : 0,
                    LastPaymentCurrency = subscriptionDetails.BillingInfo?.LastPayment?.Amount?.CurrencyCode ?? string.Empty,
                    FinalPaymentTime = subscriptionDetails.BillingInfo?.FinalPaymentTime ?? minSqlDate,
                    CyclesCompleted = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].CyclesCompleted
                        : 0,
                    CyclesRemaining = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].CyclesRemaining
                        : 0,
                    TotalCycles = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].TotalCycles
                        : 0,
                    TrialIntervalUnit = plan?.TrialIntervalUnit,
                    TrialIntervalCount = plan?.TrialIntervalCount ?? 0,
                    TrialTotalCycles = plan?.TrialTotalCycles ?? 0,
                    TrialFixedPrice = plan?.TrialFixedPrice ?? 0
                };

                // Calcular la fecha del próximo pago después del período de prueba
                if (detallesSuscripcion.Status == "ACTIVE" && detallesSuscripcion.CyclesCompleted == 1 && detallesSuscripcion.CyclesRemaining == 0)
                {
                    detallesSuscripcion.NextBillingTime = detallesSuscripcion.StartTime.AddDays((double)detallesSuscripcion.TrialIntervalCount * (double)detallesSuscripcion.TrialTotalCycles + 1);
                }
                await transaction.CommitAsync();
                return detallesSuscripcion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear detalles de suscripción para planId {planId}");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
