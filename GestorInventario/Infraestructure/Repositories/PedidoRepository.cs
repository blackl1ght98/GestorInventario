
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Interfaces.Utils;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.order;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
namespace GestorInventario.Infraestructure.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly GestorInventarioContext _context;              
        private readonly IPaypalOrderService _paypalOrder;
        private readonly ILogger<PedidoRepository> _logger;        
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IConversionUtils _conversion;
        public PedidoRepository(GestorInventarioContext context, IConversionUtils conversion,
        IPaypalOrderService service, ILogger<PedidoRepository> logger,  ICurrentUserAccessor current)
        {
            _context = context;
            _paypalOrder = service;
            _logger = logger;            
            _currentUserAccessor = current;
            _conversion = conversion;
        }
   
        public IQueryable<Pedido> ObtenerPedidos()=>
            from p in _context.Pedidos.Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation)
            select p;
      
        public IQueryable<Pedido> ObtenerPedidoUsuario(int userId)=>_context.Pedidos.Where(p => p.IdUsuario == userId).Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation);
        
          
                       
        public async Task<OperationResult<string>> CrearPedido(PedidosViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var pedido = new Pedido
                {
                    NumeroPedido = model.NumeroPedido,
                    FechaPedido = model.FechaPedido,
                    EstadoPedido = model.EstadoPedido,
                    IdUsuario = model.IdUsuario
                };
                await _context.AddEntityAsync(pedido);

               
                var historialPedido = new HistorialPedido
                {
                    IdUsuario = pedido.IdUsuario,
                    Fecha = DateTime.Now,
                    Accion = _currentUserAccessor.GetRequestMethod(),
                    Ip = _currentUserAccessor.GetClientIpAddress(),
                };
                await _context.AddEntityAsync(historialPedido);

                foreach (var item in model.Productos.Where(p => p.Seleccionado))
                {
                    var detallePedido = new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad
                    };
                    await _context.AddEntityAsync(detallePedido);

                    var detalleHistorial = new DetalleHistorialPedido
                    {
                        HistorialPedidoId = historialPedido.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad,
                        EstadoPedido = model.EstadoPedido,
                        FechaPedido = model.FechaPedido,
                        NumeroPedido = model.NumeroPedido
                    };

                    await _context.AddEntityAsync(detalleHistorial);
                }


                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Pedido creado con exito");
             
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el pedido");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("error al crear el pedido");
            }
        }
        public async Task<Pedido> ObtenerPedidoEliminacion(int id)=>await _context.Pedidos.Include(p => p.DetallePedidos).ThenInclude(dp => dp.Producto).Include(p => p.IdUsuarioNavigation).FirstOrDefaultAsync(m => m.Id == id);            
        public async Task<OperationResult<string>> EliminarPedido(int Id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var pedido = await _context.Pedidos.Include(p => p.DetallePedidos).Include(x=>x.Rembolsos).FirstOrDefaultAsync(m => m.Id == Id);
                if (pedido == null)
                {
                    return OperationResult<string>.Fail("No hay pedido para eliminar");
                }
                if (pedido.EstadoPedido != "Entregado" && pedido.DetallePedidos.Any())
                {
                    return OperationResult<string>.Fail("El pedido tiene que tener el estado Entregado para ser eliminado y no tener historial asociado");
                }
                else
                {
                    _context.DeleteRangeEntity(pedido.DetallePedidos);
                    _context.DeleteRangeEntity(pedido.Rembolsos);
                    _context.DeleteEntity(pedido);
                    await transaction.CommitAsync();
                }

                return OperationResult<string>.Ok("Pedido eliminado con exito");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al eliminar el pedido");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Error al eliminar el pedido");
            }
          
        }
        public async Task<HistorialPedido> EliminarHistorialPorId(int id)=> await _context.HistorialPedidos.Include(x => x.DetalleHistorialPedidos).FirstOrDefaultAsync(x => x.Id == id);    
        public async Task<OperationResult<string>> EliminarHistorialPorIdDefinitivo(int Id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var historialPedido = await EliminarHistorialPorId(Id);
                if (historialPedido != null)
                {
                    _context.DeleteRangeEntity(historialPedido.DetalleHistorialPedidos);
                    _context.DeleteEntity(historialPedido);
                    await transaction.CommitAsync();
                }
                else
                {
                    return OperationResult<string>.Fail("El historial no existe");
                   
                }
                return OperationResult<string>.Ok("Historial eliminado con exito");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al el historial");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Error al eliminar el historial");
            }
           
        }      
        public async Task<Pedido> ObtenerPedidoPorId(int id)=> await _context.Pedidos.FirstOrDefaultAsync(x => x.Id == id);               
        public async Task<OperationResult<string>> EditarPedido(EditPedidoViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                
                    int usuarioId= _currentUserAccessor.GetCurrentUserId();             
                    var pedidoOriginal = await _context.Pedidos.Include(p => p.DetallePedidos).FirstOrDefaultAsync(x => x.Id == model.Id);
                    if (pedidoOriginal == null)
                    {
                        return OperationResult<string>.Fail("Pedido no encontrado");
                    }
                    pedidoOriginal.FechaPedido = model.FechaPedido;
                    pedidoOriginal.EstadoPedido = model.EstadoPedido;
                    await _context.UpdateEntityAsync(pedidoOriginal);
                  
                    var historialPedido = new HistorialPedido
                    {
                        IdUsuario = usuarioId,
                        Fecha = DateTime.Now,
                        Accion = _currentUserAccessor.GetRequestMethod(),
                        Ip = _currentUserAccessor.GetClientIpAddress(),
                    };
                    await _context.AddEntityAsync(historialPedido);
                    foreach (var detalleOriginal in pedidoOriginal.DetallePedidos)
                    {
                        var nuevoDetalle = new DetalleHistorialPedido
                        {
                            HistorialPedidoId = historialPedido.Id,
                            ProductoId = detalleOriginal.ProductoId,
                            Cantidad = detalleOriginal.Cantidad,
                            EstadoPedido = pedidoOriginal.EstadoPedido,
                            FechaPedido = pedidoOriginal.FechaPedido,
                            NumeroPedido = pedidoOriginal.NumeroPedido
                        };
                        await _context.AddEntityAsync(nuevoDetalle);
                    }
                    await transaction.CommitAsync();
                    return OperationResult<string>.Ok("Pedido editado con exito");                             
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al editar el pedido");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Error al editar el pedido");
            }
         
        }
        public async Task<Pedido> ObtenerDetallesPedido(int id)=>await _context.Pedidos.Include(p => p.DetallePedidos).ThenInclude(dp => dp.Producto).Include(p => p.IdUsuarioNavigation).FirstOrDefaultAsync(m => m.Id == id); 
    
        public IQueryable<HistorialPedido> ObtenerHistorialDePedidos(int? usuarioId = null)
        {
            var query = _context.HistorialPedidos
                .Include(h => h.DetalleHistorialPedidos)
                    .ThenInclude(d => d.Producto)
                .Include(h => h.IdUsuarioNavigation);
            if(usuarioId != null)
            {
                query.Where(u=>u.Id==usuarioId);
            }
                          
            return query;
        }
        public async Task<HistorialPedido> DetallesHistorial(int id)=>await _context.HistorialPedidos.Include(p => p.DetalleHistorialPedidos).ThenInclude(dp => dp.Producto).Include(p => p.IdUsuarioNavigation).FirstOrDefaultAsync(p => p.Id == id);          
       
        public async Task<OperationResult<string>> EliminarHitorial()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var historialPedidos = await _context.HistorialPedidos.Include(x => x.DetalleHistorialPedidos).ToListAsync();
                if (historialPedidos == null || historialPedidos.Count == 0)
                {
                    return OperationResult<string>.Fail("No hay datos en el historial para eliminar");
                }
                // Eliminar todos los registros
                foreach (var historialProducto in historialPedidos)
                {
                    _context.DeleteRangeEntity(historialProducto.DetalleHistorialPedidos);
                    _context.DeleteEntity(historialProducto);

                }
                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Historial eliminado");
            }
            catch (Exception ex)
            {


                _logger.LogError(ex, "Error al eliminar todo el historial");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Error al eliminar todo el historial");
            }
           
        }

        public async Task<OperationResult<PayPalPaymentDetail>> ObtenerDetallePagoEjecutadoV2(string id)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                // Buscar el detalle de pago existente en la base de datos
                var existingDetail = await _context.PayPalPaymentDetails
                    .Include(d => d.PayPalPaymentItems)
                    .FirstOrDefaultAsync(x => x.Id == id);

                // Obtener los detalles actualizados desde la API de PayPal
                var detalles = await _paypalOrder.ObtenerDetallesPagoEjecutadoV2(id);
                if (detalles == null)
                {
                    return OperationResult<PayPalPaymentDetail>.Fail("Detalles del pedido no encontrados para generar la factura");
                }

                // Si el detalle no existe, crear uno nuevo; si existe, actualizarlo
                PayPalPaymentDetail detallesPago;
                if (existingDetail == null)
                {
                    detallesPago = new PayPalPaymentDetail
                    {
                        Id = detalles.Id 
                    };
                    _context.PayPalPaymentDetails.Add(detallesPago);
                }
                else
                {
                    detallesPago = existingDetail;
                    // Opcional: Limpiar los ítems existentes si se desea actualizarlos completamente
                    _context.PayPalPaymentItems.RemoveRange(detallesPago.PayPalPaymentItems);
                }

                // Actualizar los campos del objeto PayPalPaymentDetail con los datos de la API
                detallesPago.Intent = detalles.Intent;
                detallesPago.Status = detalles.Status;
                detallesPago.PaymentMethod = "paypal";
                detallesPago.PayerEmail = detalles.Payer?.Email;
                detallesPago.PayerFirstName = detalles.Payer?.Name?.GivenName;
                detallesPago.PayerLastName = detalles.Payer?.Name?.Surname;
                detallesPago.PayerId = detalles.Payer?.PayerId;
               
                detallesPago.ShippingRecipientName = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Name?.FullName;
                detallesPago.ShippingLine1 = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.AddressLine1;
                detallesPago.ShippingCity = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.AdminArea2;
                detallesPago.ShippingState = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.AdminArea1;
                detallesPago.ShippingPostalCode = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.PostalCode;
                detallesPago.ShippingCountryCode = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.CountryCode;

                if (detalles.PurchaseUnits != null)
                {
                    foreach (var purchaseUnit in detalles.PurchaseUnits)
                    {
                        if (purchaseUnit != null)
                        {
                            detallesPago.AmountTotal = _conversion.ConvertToDecimal(purchaseUnit.Amount?.Value);
                            detallesPago.AmountCurrency = purchaseUnit.Amount?.CurrencyCode;
                            detallesPago.AmountItemTotal = _conversion.ConvertToDecimal(purchaseUnit.Amount?.Breakdown?.ItemTotal?.Value);

                            // Calcular subtotal si es necesario
                            if (detallesPago.AmountItemTotal == 0 && purchaseUnit.Items != null)
                            {
                                decimal? subtotal = 0;
                                foreach (var item in purchaseUnit.Items)
                                {
                                   
                                    var unitAmount = _conversion.ConvertToDecimal(item.UnitAmount?.Value.ToString());
                                    var quantity = _conversion.ConvertToInt(item.Quantity?.ToString());
                                    subtotal += unitAmount * quantity;
                                }
                                detallesPago.AmountItemTotal = subtotal;
                            }

                            detallesPago.AmountShipping = _conversion.ConvertToDecimal(purchaseUnit.Amount?.Breakdown?.Shipping?.Value);
                            detallesPago.PayeeMerchantId = purchaseUnit.Payee?.MerchantId;
                            detallesPago.PayeeEmail = purchaseUnit.Payee?.EmailAddress;
                            detallesPago.Description = purchaseUnit.Description;

                            if (purchaseUnit.Payments?.Captures != null)
                            {
                                foreach (var capture in purchaseUnit.Payments.Captures)
                                {
                                    if (capture != null)
                                    {
                                       
                                        detallesPago.SaleId = capture.Id;
                                        detallesPago.CaptureStatus = capture.Status;
                                        detallesPago.CaptureAmount = _conversion.ConvertToDecimal(capture.Amount?.Value);
                                        detallesPago.CaptureCurrency = capture.Amount?.CurrencyCode;
                                        detallesPago.ProtectionEligibility = capture.SellerProtection?.Status;
                                        detallesPago.TransactionFeeAmount = _conversion.ConvertToDecimal(capture.SellerReceivableBreakdown?.PaypalFee?.Value);
                                        detallesPago.TransactionFeeCurrency = capture.SellerReceivableBreakdown?.PaypalFee?.CurrencyCode;
                                        detallesPago.ReceivableAmount = _conversion.ConvertToDecimal(capture.SellerReceivableBreakdown?.NetAmount?.Value);
                                        detallesPago.ReceivableCurrency = capture.SellerReceivableBreakdown?.NetAmount?.CurrencyCode;

                                        var exchangeRateValue = capture.SellerReceivableBreakdown?.ExchangeRate?.Value;
                                        if (decimal.TryParse((string)exchangeRateValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchangeRate))
                                        {
                                            detallesPago.ExchangeRate = exchangeRate;
                                        }
                                        detallesPago.CreateTime = _conversion.ConvertToDateTime(capture.CreateTime);
                                        detallesPago.UpdateTime = _conversion.ConvertToDateTime(capture.UpdateTime);

                                    }

                                }
                                var firstPurchaseUnit = detalles.PurchaseUnits?.FirstOrDefault();
                                if (firstPurchaseUnit != null)
                                {
                                    // Campos de tracking
                                    var firstTracker = firstPurchaseUnit.Shipping?.Trackers?.FirstOrDefault();
                                    if (firstTracker != null)
                                    {
                                        detallesPago.TrackingId = firstTracker.Id;
                                        detallesPago.TrackingStatus = firstTracker.Status;
                                    

                                      
                                    }

                                    // Campos de captura
                                    var firstCapture = firstPurchaseUnit.Payments?.Captures?.FirstOrDefault();
                                    if (firstCapture != null)
                                    {
                                        detallesPago.FinalCapture = firstCapture.FinalCapture;

                                        if (firstCapture.SellerProtection != null)
                                        {
                                            detallesPago.DisputeCategories =
                                                JsonConvert.SerializeObject(firstCapture.SellerProtection.DisputeCategories);
                                        }
                                    }
                                }

                            }

                            var items = purchaseUnit.Items;
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    var paymentItem = new PayPalPaymentItem
                                    {
                                        PayPalId = detallesPago.Id,
                                        ItemName = item.Name,
                                        ItemSku = item.Sku,
                                        ItemPrice = _conversion.ConvertToDecimal(item.UnitAmount?.Value),
                                        ItemCurrency = item.UnitAmount?.CurrencyCode,
                                        ItemTax = _conversion.ConvertToDecimal(item.Tax?.Value),
                                        ItemQuantity = _conversion.ConvertToInt(item.Quantity)
                                    };
                                    _context.PayPalPaymentItems.Add(paymentItem);
                                }
                            }
                        }
                    }
                }               
                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();
                return OperationResult<PayPalPaymentDetail>.Ok("",detallesPago);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los  detalles del pago");
                await transaccion.RollbackAsync();
                return OperationResult<PayPalPaymentDetail>.Fail("Ha ocurrido un error");
            }
        }
     
       
    }
    
}