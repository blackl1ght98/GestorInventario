using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
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
        private readonly UtilityClass _utilityClass;
        private readonly IEmailService _emailService;
        public PaypalRepository(GestorInventarioContext context, ILogger<PaypalRepository> logger, UtilityClass utility, IEmailService email)
        {
            _context = context;
            _emailService = email;
            _logger = logger;
            _utilityClass = utility;
        }

        public async Task<List<SubscriptionDetail>> ObtenerSuscriptcionesActivas(string planId)
        {
            return await _context.SubscriptionDetails
                .Where(s => s.PlanId == planId && s.Status != "CANCELLED")
                .ToListAsync();
        }
        public async Task<PlanDetail> ObtenerPlan(string planId)
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
            var queryable = from p in _context.SubscriptionDetails select p;
            return queryable;
        }
        public  IQueryable<UserSubscription> ObtenerSubscripcionesUsuario(int usuarioId)
        {
            var queryable = _context.UserSubscriptions
                   .Include(x => x.User)
                   .Where(x => x.UserId == usuarioId);
            return queryable;
        }
        public async Task<(Pedido Pedido, decimal TotalAmount)> GetPedidoWithDetailsAsync(int pedidoId)
        {
            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallePedidos)
                    .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.Id == pedidoId);

                if (pedido == null || string.IsNullOrEmpty(pedido.CaptureId))
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
        public async Task<(DetallePedido Detalle, decimal PrecioProducto)> GetProductoDePedidoAsync(int detallePedidoId)
        {
            try
            {
                var detalle = await _context.DetallePedidos
                    .Include(dp => dp.Producto)
                    .Include(dp => dp.Pedido)
                    .FirstOrDefaultAsync(dp => dp.Id == detallePedidoId);

                if (detalle == null)
                    throw new ArgumentException("Detalle de pedido no encontrado");

                if (detalle.Producto == null)
                    throw new ArgumentException("Producto no encontrado en el detalle");

                return (detalle, detalle.Producto.Precio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el detalle de pedido {detallePedidoId}");
                throw;
            }
        }
        public async Task<(Pedido? Pedido, List<DetallePedido>? Detalles)> GetPedidoConDetallesAsync(int pedidoId)
        {
            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallePedidos)
                    .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.Id == pedidoId);

                if (pedido == null || string.IsNullOrEmpty(pedido.CaptureId))
                {
                    _logger.LogWarning($"Pedido {pedidoId} no encontrado o sin SaleId");
                    return (null, null);
                }

                if (string.IsNullOrEmpty(pedido.Currency))
                {
                    _logger.LogWarning($"Pedido {pedidoId} sin moneda definida");
                    return (null, null);
                }

                return (pedido, pedido.DetallePedidos?.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el pedido {pedidoId}");
                throw;
            }
        }
        public async Task SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

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
                await transaction.CommitAsync();
                _logger.LogInformation($"Detalles del plan {planId} guardados exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al guardar los detalles del plan {planId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdatePlanStatusAsync(string planId, string status)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try {
                var planDetails = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);
                if (planDetails != null)
                {
                    planDetails.Status = status;
                    _context.PlanDetails.Update(planDetails);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Estado del plan {planId} actualizado a {status}");
                }
                await transaction.CommitAsync();
            } catch(Exception ex) {
                _logger.LogError(ex,"Ocurrio un error inesperado"); 
                await transaction.RollbackAsync();
            }
            
        }      
        public async Task<OperationResult<string>> EnviarEmailNotificacionRembolso(int pedidoId, decimal montoReembolsado, string motivo)
        {
            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallePedidos)
                    .ThenInclude(d => d.Producto)
                    .Include(p => p.IdUsuarioNavigation)
                    .FirstOrDefaultAsync(p => p.Id == pedidoId);

                if (pedido == null)
                {
                    _logger.LogWarning("No se encontró el pedido con ID {PedidoId}", pedidoId);
                    return OperationResult<string>.Fail("Pedido no encontrado");
                }

                var usuarioPedido = pedido.IdUsuarioNavigation?.Email ?? "Email no disponible";
                var nombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto ?? "Cliente";

                // Extraer la lista de productos con detalles
                var productosConDetalles = pedido.DetallePedidos?
                    .Select(detalle => new PayPalPaymentItem
                    {
                        ItemName = detalle.Producto?.NombreProducto ?? "N/A",
                        ItemQuantity = detalle.Cantidad ?? 0, 
                        ItemPrice = detalle.Producto?.Precio ?? 0,
                        ItemCurrency = pedido.Currency ?? "USD",
                        ItemSku = detalle.Producto?.Descripcion ?? "N/A"
                    })
                    .ToList() ?? new List<PayPalPaymentItem>();

                // Crear DTO para el correo
                var correo = new EmailReembolsoAprobadoDto
                {
                    NumeroPedido = pedido.NumeroPedido,
                    NombreCliente = nombreCliente,
                    EmailCliente = usuarioPedido,
                    FechaRembolso = DateTime.UtcNow,
                    CantidadADevolver = montoReembolsado,
                    MotivoRembolso = motivo,
                    Productos = productosConDetalles
                };

                // Enviar el correo
                await _emailService.EnviarNotificacionReembolsoAsync(correo);
                return OperationResult<string>.Ok("Correo de notificación de reembolso enviado correctamente");
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación de reembolso para el pedido ID {PedidoId}", pedidoId);
                return OperationResult<string>.Fail("Error al enviar el correo");
              
            }
        }
       
        public async Task UpdatePedidoStatusAsync(int pedidoId, string status, string refundId, string estadoVenta)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pedido = await _context.Pedidos.Include(x=>x.IdUsuarioNavigation).FirstOrDefaultAsync(x=>x.Id==pedidoId);
                if (pedido == null)
                    throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

                pedido.EstadoPedido = status;
                pedido.RefundId = refundId;

               await _context.UpdateEntityAsync(pedido);

                var usuarioActual = _utilityClass.ObtenerUsuarioIdActual();

                // Crear o actualizar registro de reembolso
                var obtenerRembolso = await _context.Rembolsos.FirstOrDefaultAsync(x => x.NumeroPedido == pedido.NumeroPedido);

                if (obtenerRembolso == null)
                {
                    var rembolso = new Rembolso
                    {

                        NumeroPedido = pedido.NumeroPedido,
                        NombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto,
                        EmailCliente = pedido.IdUsuarioNavigation?.Email,
                        FechaRembolso = DateTime.UtcNow,
                        MotivoRembolso = "Rembolso solicitado por el usuario",
                        EstadoRembolso = "REMBOLSO APROVADO",
                        RembosoRealizado = true,
                        UsuarioId = usuarioActual,
                        PedidoId=pedido.Id,
                        EstadoVenta = estadoVenta
                    };
                    await _context.AddEntityAsync(rembolso);
                }
                else
                {
                    obtenerRembolso.EstadoRembolso = "REMBOLSO APROVADO";
                    obtenerRembolso.RembosoRealizado = true;
                    obtenerRembolso.EstadoVenta = estadoVenta;
                    obtenerRembolso.FechaRembolso = DateTime.UtcNow;


                    await _context.UpdateEntityAsync(obtenerRembolso);
                }
               
                await transaction.CommitAsync();
                _logger.LogInformation($"Estado del pedido {pedidoId} actualizado a {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el estado del pedido {pedidoId}");
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task RegistrarReembolsoParcialAsync(int pedidoId, int detalleId, string status, string refundId, decimal montoReembolsado, string motivo,string estadoVenta)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Obtener el pedido con los datos relacionados
                var pedido = await _context.Pedidos
                    .Include(p => p.IdUsuarioNavigation)
                    .Include(p => p.DetallePedidos)
                    .FirstOrDefaultAsync(p => p.Id == pedidoId);

                if (pedido == null)
                    throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

                // Obtener el detalle específico por ID
                var detalleReembolsado = pedido.DetallePedidos.FirstOrDefault(d => d.Id == detalleId);
                if (detalleReembolsado == null)
                    throw new ArgumentException($"Detalle con ID {detalleId} no encontrado.");

                // Evitar reembolsos duplicados
                if (detalleReembolsado.Rembolsado?? false)
                    throw new InvalidOperationException($"El detalle con ID {detalleId} ya ha sido reembolsado.");

                var usuarioActual = _utilityClass.ObtenerUsuarioIdActual();

                // Crear registro de reembolso
                var rembolso = new Rembolso
                {
                    NumeroPedido = pedido.NumeroPedido,
                    NombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto,
                    EmailCliente = pedido.IdUsuarioNavigation?.Email,
                    FechaRembolso = DateTime.UtcNow,
                    MotivoRembolso = motivo,
                    EstadoRembolso = status,
                    RembosoRealizado = true,
                    UsuarioId = usuarioActual,
                    EstadoVenta= estadoVenta
                   
                };

                await _context.AddEntityAsync(rembolso);

                // Marcar el detalle correcto como reembolsado
                detalleReembolsado.Rembolsado = true;
                await _context.UpdateEntityAsync(detalleReembolsado);
                await transaction.CommitAsync();
                _logger.LogInformation($"Reembolso registrado para pedido {pedidoId}, detalle {detalleId}. Estado: {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al registrar reembolso para pedido {pedidoId}, detalle {detalleId}");
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task AddInfoTrackingOrder(int pedidoId, string tracking, string url, string carrier)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pedido = await _context.Pedidos.FindAsync(pedidoId);
                if (pedido == null)
                    throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

                pedido.TrackingNumber = tracking;
                pedido.UrlTracking = url;
                pedido.Transportista = carrier;
                await _context.UpdateEntityAsync(pedido);              
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el estado del pedido {pedidoId}");
                await transaction.RollbackAsync();
                throw;
            }
        }
       
        public async Task UpdatePlanStatusInDatabase(string planId, string status)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var planDetails = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);
                if (planDetails != null)
                {
                    planDetails.Status = status;
                    await _context.UpdateEntityAsync(planDetails);
                   
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex) {
                _logger.LogError(ex,"Ocurrio un error inesperado");
                await transaction.RollbackAsync();
            }
           
        }

        public async Task SavePlanPriceUpdateAsync(string planId, UpdatePricingPlanDto planPriceUpdate)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Buscar el plan en la base de datos
                var planDetail = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);
                if (planDetail == null)
                {
                    _logger.LogWarning($"No se encontró el plan con ID {planId} en la base de datos.");
                    throw new ArgumentException($"No se encontró el plan con ID {planId}.");
                }

                // Verificar si el plan tiene un ciclo de prueba (basado en los campos Trial no nulos)
                bool hasTrial = !string.IsNullOrEmpty(planDetail.TrialIntervalUnit);

                // Procesar los esquemas de precios
                foreach (var pricingScheme in planPriceUpdate.PricingSchemes)
                {
                    int billingCycleSequence = pricingScheme.BillingCycleSequence;
                    string priceValue = pricingScheme.PricingScheme.FixedPrice.Value;

                    if (string.IsNullOrEmpty(priceValue))
                    {
                        _logger.LogWarning($"El precio proporcionado para el ciclo {billingCycleSequence} del plan {planId} es nulo o vacío.");
                        continue;
                    }

                    decimal price = decimal.Parse(priceValue, CultureInfo.InvariantCulture);

                    if (hasTrial && billingCycleSequence == 1)
                    {
                        // Actualizar el precio del ciclo de prueba
                        planDetail.TrialFixedPrice = price;
                        _logger.LogInformation($"Precio del ciclo de prueba para el plan {planId} actualizado a {price}.");
                    }
                    else if ((hasTrial && billingCycleSequence == 2) || (!hasTrial && billingCycleSequence == 1))
                    {
                        // Actualizar el precio del ciclo regular
                        planDetail.RegularFixedPrice = price;
                        _logger.LogInformation($"Precio del ciclo regular para el plan {planId} actualizado a {price}.");
                    }
                    else
                    {
                        _logger.LogWarning($"Ciclo de facturación {billingCycleSequence} no válido para el plan {planId}.");
                    }
                }

               
                await _context.UpdateEntityAsync(planDetail);              
                await transaction.CommitAsync();
                _logger.LogInformation($"Precios del plan {planId} actualizados exitosamente en la base de datos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al guardar los precios actualizados del plan {planId}");
                await transaction.RollbackAsync();
                throw;
            }
        }
        // Mapear listas de BillingCycle tipadas
        public List<BillingCycle> MapBillingCycles(List<BillingCycle> billingCycles)
        {
            if (billingCycles == null)
                return new List<BillingCycle>();

            // Si los tipos coinciden exactamente, solo devolvemos o clonamos (si quieres evitar referencias directas)
            return billingCycles.Select(cycle => new BillingCycle
            {
                TenureType = cycle.TenureType,
                Sequence = cycle.Sequence,
                TotalCycles = cycle.TotalCycles,
                Frequency = MapFrequency(cycle.Frequency),
                PricingScheme = MapPricingScheme(cycle.PricingScheme)
            }).ToList();
        }

        private Frequency MapFrequency(Frequency frequency)
        {
            if (frequency == null)
                return null;

            return new Frequency
            {
                IntervalUnit = frequency.IntervalUnit,
                IntervalCount = frequency.IntervalCount
            };
        }
        private PricingScheme MapPricingScheme(PricingScheme pricingScheme)
        {
            if (pricingScheme == null)
                return null;

            return new PricingScheme
            {
                Version = pricingScheme.Version,
                FixedPrice = MapMoney(pricingScheme.FixedPrice),
                CreateTime = pricingScheme.CreateTime,
                UpdateTime = pricingScheme.UpdateTime
            };
        }

        private Money MapMoney(Money money)
        {
            if (money == null)
                return null;

            return new Money
            {
                CurrencyCode = money.CurrencyCode,
                Value = money.Value
            };
        }

        public Taxes MapTaxes(Taxes taxes)
        {
            if (taxes == null)
                return null;

            return new Taxes
            {
                Percentage = taxes.Percentage,
                Inclusive = taxes.Inclusive
            };
        }
        // Método para crear la lista de categorías a partir de la enumeración
        public List<string> GetCategoriesFromEnum()
        {
            return Enum.GetNames(typeof(Category)).ToList();
        }
        public async Task<SubscriptionDetail> CreateSubscriptionDetailAsync(dynamic subscriptionDetails, string planId, IPaypalService paypalService)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                // Check if plan exists
                var plan = await ObtenerPlan(planId);
                if (plan == null)
                {
                    var planResponse = await paypalService.ObtenerDetallesPlan(planId);
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

                    await SavePlanDetailsAsync(planId, planDetails);
                    plan = await ObtenerPlan(planId);
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
        public async Task SaveOrUpdateSubscriptionDetailsAsync(SubscriptionDetail subscriptionDetails)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verificar si la suscripción ya existe
                var existingSubscription = await _context.SubscriptionDetails
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionDetails.SubscriptionId);

                if (existingSubscription != null)
                {
                    // Comparar los detalles para determinar si hay cambios
                    bool hasChanges = !(
                        existingSubscription.PlanId == subscriptionDetails.PlanId &&
                        existingSubscription.Status == subscriptionDetails.Status &&
                        existingSubscription.StartTime == subscriptionDetails.StartTime &&
                        existingSubscription.StatusUpdateTime == subscriptionDetails.StatusUpdateTime &&
                        existingSubscription.SubscriberName == subscriptionDetails.SubscriberName &&
                        existingSubscription.SubscriberEmail == subscriptionDetails.SubscriberEmail &&
                        existingSubscription.PayerId == subscriptionDetails.PayerId &&
                        existingSubscription.OutstandingBalance == subscriptionDetails.OutstandingBalance &&
                        existingSubscription.OutstandingCurrency == subscriptionDetails.OutstandingCurrency &&
                        existingSubscription.NextBillingTime == subscriptionDetails.NextBillingTime &&
                        existingSubscription.LastPaymentTime == subscriptionDetails.LastPaymentTime &&
                        existingSubscription.LastPaymentAmount == subscriptionDetails.LastPaymentAmount &&
                        existingSubscription.LastPaymentCurrency == subscriptionDetails.LastPaymentCurrency &&
                        existingSubscription.FinalPaymentTime == subscriptionDetails.FinalPaymentTime &&
                        existingSubscription.CyclesCompleted == subscriptionDetails.CyclesCompleted &&
                        existingSubscription.CyclesRemaining == subscriptionDetails.CyclesRemaining &&
                        existingSubscription.TotalCycles == subscriptionDetails.TotalCycles &&
                        existingSubscription.TrialIntervalUnit == subscriptionDetails.TrialIntervalUnit &&
                        existingSubscription.TrialIntervalCount == subscriptionDetails.TrialIntervalCount &&
                        existingSubscription.TrialTotalCycles == subscriptionDetails.TrialTotalCycles &&
                        existingSubscription.TrialFixedPrice == subscriptionDetails.TrialFixedPrice
                    );

                    if (hasChanges)
                    {
                        await _context.UpdateEntityAsync(subscriptionDetails);                     
                        _logger.LogInformation($"Suscripción actualizada: {subscriptionDetails.SubscriptionId}");
                    }
                }
                else
                {
                    await _context.AddEntityAsync(subscriptionDetails);                 
                    _logger.LogInformation($"Suscripción creada: {subscriptionDetails.SubscriptionId}");
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al guardar o actualizar detalles de suscripción {subscriptionDetails.SubscriptionId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task SaveUserSubscriptionAsync(int userId, string subscriptionId, string subscriberName, string planId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verificar si la relación ya existe
                var existingRelation = await _context.UserSubscriptions
                    .FirstOrDefaultAsync(us => us.UserId == userId && us.SubscriptionId == subscriptionId);

                if (existingRelation == null)
                {
                    var userSubscription = new UserSubscription
                    {
                        UserId = userId,
                        SubscriptionId = subscriptionId,
                        NombreSusbcriptor = subscriberName,
                        PaypalPlanId = planId
                    };

                    await _context.AddEntityAsync(userSubscription);
                    
                    _logger.LogInformation($"Relación UserSubscription creada para usuario {userId}, suscripción {subscriptionId}");
                    await transaction.CommitAsync( );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al guardar UserSubscription para usuario {userId}, suscripción {subscriptionId}");
                await transaction.RollbackAsync( );
                throw;
            }
        }
      
        public async Task UpdateSubscriptionStatusAsync(string subscriptionId, string status)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var subscription = await _context.SubscriptionDetails
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

                if (subscription == null)
                {
                    _logger.LogWarning($"No se encontró la suscripción con ID {subscriptionId}");
                    return;
                }

                
                if (status == "ACTIVE" && subscription.Status != "CANCELLED" && subscription.Status != "SUSPENDED")
                {
                    _logger.LogInformation($"No se puede activar la suscripción {subscriptionId} porque no está en estado CANCELLED o SUSPENDED (estado actual: {subscription.Status})");
                    return;
                }
               
                else if (status == "SUSPENDED" && subscription.Status != "ACTIVE")
                {
                    _logger.LogInformation($"No se puede suspender la suscripción {subscriptionId} porque no está en estado ACTIVE (estado actual: {subscription.Status})");
                    return;
                }
               
                else if (status == "CANCELLED" && subscription.Status != "ACTIVE" && subscription.Status != "SUSPEND")
                {
                    _logger.LogInformation($"No se puede cancelar la suscripción {subscriptionId} porque no está en estado ACTIVE o SUSPEND (estado actual: {subscription.Status})");
                    return;
                }

                subscription.Status = status;
                await _context.UpdateEntityAsync(subscription);
          
                _logger.LogInformation($"Estado de la suscripción {subscriptionId} actualizado a {status}");

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error al actualizar el estado de la suscripción {subscriptionId} a {status}");
                throw;
            }
        }
    }
}