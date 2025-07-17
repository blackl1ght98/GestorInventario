
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.order;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System.Globalization;
using System.Security.Claims;
namespace GestorInventario.Infraestructure.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IPaypalService _paypalService;
        private readonly ILogger<PedidoRepository> _logger;
        public PedidoRepository(GestorInventarioContext context, IMemoryCache memory, IHttpContextAccessor contextAccessor,
            IPaypalService service, ILogger<PedidoRepository> logger)
        {
            _context = context;
            _cache = memory;
            _contextAccessor = contextAccessor;
            _paypalService = service;
            _logger = logger;
        }
   
        public IQueryable<Pedido> ObtenerPedidos()=>
            from p in _context.Pedidos.Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation)
            select p;
      
        public IQueryable<Pedido> ObtenerPedidoUsuario(int userId)=>_context.Pedidos.Where(p => p.IdUsuario == userId).Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation);
        
        public async Task<List<Producto>> ObtenerProductos()=>await _context.Productos.ToListAsync();      
        public async Task<List<Usuario>> ObtenerUsuarios()=>await _context.Usuarios.ToListAsync();                  
        public async Task<(bool, string)> CrearPedido(PedidosViewModel model)
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
                    Accion = _contextAccessor.HttpContext.Request.Method.ToString(),
                    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                await _context.AddEntityAsync(historialPedido);

                for (var i = 0; i < model.IdsProducto.Count; i++)
                {
                    if (model.ProductosSeleccionados[i])
                    {
                        var detallePedido = new DetallePedido
                        {
                            PedidoId = pedido.Id,
                            ProductoId = model.IdsProducto[i],
                            Cantidad = model.Cantidades[i]
                        };
                        await _context.AddEntityAsync(detallePedido);

                        var detalleHistorialPedido = new DetalleHistorialPedido
                        {
                            HistorialPedidoId = historialPedido.Id,
                            ProductoId = model.IdsProducto[i],
                            Cantidad = model.Cantidades[i],
                            EstadoPedido = model.EstadoPedido,
                            FechaPedido = model.FechaPedido,
                            NumeroPedido = model.NumeroPedido
                        };
                        await _context.AddEntityAsync(detalleHistorialPedido);
                    }
                }

                await transaction.CommitAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el pedido");
                await transaction.RollbackAsync();
                return (false, "Error al crear el pedido");
            }
        }
        public async Task<Pedido> ObtenerPedidoEliminacion(int id)=>await _context.Pedidos.Include(p => p.DetallePedidos).ThenInclude(dp => dp.Producto).Include(p => p.IdUsuarioNavigation).FirstOrDefaultAsync(m => m.Id == id);            
        public async Task<(bool, string)> EliminarPedido(int Id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var pedido = await _context.Pedidos.Include(p => p.DetallePedidos).FirstOrDefaultAsync(m => m.Id == Id);
                if (pedido == null)
                {
                    return (false, "No hay pedido a eliminar");
                }
                if (pedido.EstadoPedido != "Entregado" && pedido.DetallePedidos.Any())
                {
                    return (false, "El pedido tiene que tener el estado Entregado para ser eliminado y no tener historial asociado");

                }
                else
                {
                    _context.DeleteRangeEntity(pedido.DetallePedidos);
                    _context.DeleteEntity(pedido);
                    await transaction.CommitAsync();
                }

                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al eliminar el pedido");
                await transaction.RollbackAsync();
                return (false, "Error al eliminar el pedido");
            }
          
        }
        public async Task<HistorialPedido> EliminarHistorialPorId(int id)=> await _context.HistorialPedidos.Include(x => x.DetalleHistorialPedidos).FirstOrDefaultAsync(x => x.Id == id);    
        public async Task<(bool, string)> EliminarHistorialPorIdDefinitivo(int Id)
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
                    return (false, "No se puede eliminar, el historial no existe");
                }
                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al el historial");
                await transaction.RollbackAsync();
                return (false, "Error al eliminar el historial");
            }
           
        }      
        public async Task<Pedido> ObtenerPedidoId(int id)=> await _context.Pedidos.FirstOrDefaultAsync(x => x.Id == id);               
        public async Task<(bool, string)> EditarPedido(EditPedidoViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existeUsuario = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var pedidoOriginal = await _context.Pedidos.Include(p => p.DetallePedidos).FirstOrDefaultAsync(x => x.Id == model.id);
                    if (pedidoOriginal == null)
                    {
                        return (false, "Pedido no encontrado, no es posible editar un pedido que no existe");
                    }
                    pedidoOriginal.FechaPedido = model.fechaPedido;
                    pedidoOriginal.EstadoPedido = model.estadoPedido;
                    await _context.UpdateEntityAsync(pedidoOriginal);
                  
                    var historialPedido = new HistorialPedido
                    {
                        IdUsuario = usuarioId,
                        Fecha = DateTime.Now,
                        Accion = _contextAccessor.HttpContext.Request.Method.ToString(),
                        Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
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
                    return (true, null);
                }
                return (false, "No puede realizar esta  accion no dispone de permisos");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al editar el pedido");
                await transaction.RollbackAsync();
                return (false, "Error al editar el pedido");
            }
         
        }
        public async Task<Pedido> ObtenerDetallesPedido(int id)=>await _context.Pedidos.Include(p => p.DetallePedidos).ThenInclude(dp => dp.Producto).Include(p => p.IdUsuarioNavigation).FirstOrDefaultAsync(m => m.Id == id);
        public IQueryable<HistorialPedido> ObtenerPedidosHistorial()=>from p in _context.HistorialPedidos.Include(dp => dp.DetalleHistorialPedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation)select p;
        public IQueryable<HistorialPedido> ObtenerPedidosHistorialUsuario(int usuarioId)=> _context.HistorialPedidos.Where(p => p.IdUsuario == usuarioId).Include(dp => dp.DetalleHistorialPedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation);
        public async Task<HistorialPedido> DetallesHistorial(int id)=>await _context.HistorialPedidos.Include(p => p.DetalleHistorialPedidos).ThenInclude(dp => dp.Producto).Include(p => p.IdUsuarioNavigation).FirstOrDefaultAsync(p => p.Id == id);          
       
        public async Task<(bool, string)> EliminarHitorial()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var historialPedidos = await _context.HistorialPedidos.Include(x => x.DetalleHistorialPedidos).ToListAsync();
                if (historialPedidos == null || historialPedidos.Count == 0)
                {
                    return (false, "No hay datos en el historial para eliminar");
                }
                // Eliminar todos los registros
                foreach (var historialProducto in historialPedidos)
                {
                    _context.DeleteRangeEntity(historialProducto.DetalleHistorialPedidos);
                    _context.DeleteEntity(historialProducto);

                }
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {


                _logger.LogError(ex, "Error al eliminar todo el historial");
                await transaction.RollbackAsync();
                return (false, "Error al eliminar todo el historial");
            }
           
        }

        public async Task<(PayPalPaymentDetail, bool, string)> ObtenerDetallePagoEjecutadoV2(string id)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                // Buscar el detalle de pago existente en la base de datos
                var existingDetail = await _context.PayPalPaymentDetails
                    .Include(d => d.PayPalPaymentItems)
                    .FirstOrDefaultAsync(x => x.Id == id);

                // Obtener los detalles actualizados desde la API de PayPal
                var detalles = await _paypalService.ObtenerDetallesPagoEjecutadoV2(id);
                if (detalles == null)
                {
                    return (null, false, "No se ha encontrado el pago para generar la factura");
                }

                // Si el detalle no existe, crear uno nuevo; si existe, actualizarlo
                PayPalPaymentDetail detallesSuscripcion;
                if (existingDetail == null)
                {
                    detallesSuscripcion = new PayPalPaymentDetail
                    {
                        Id = detalles.Id // Asignar el Id antes de agregar al contexto
                    };
                    _context.PayPalPaymentDetails.Add(detallesSuscripcion);
                }
                else
                {
                    detallesSuscripcion = existingDetail;
                    // Opcional: Limpiar los ítems existentes si se desea actualizarlos completamente
                    _context.PayPalPaymentItems.RemoveRange(detallesSuscripcion.PayPalPaymentItems);
                }

                // Actualizar los campos del objeto PayPalPaymentDetail con los datos de la API
                detallesSuscripcion.Intent = detalles.Intent;
                detallesSuscripcion.Status = detalles.Status;
                detallesSuscripcion.PaymentMethod = "paypal";
                detallesSuscripcion.PayerEmail = detalles.Payer?.Email;
                detallesSuscripcion.PayerFirstName = detalles.Payer?.Name?.GivenName;
                detallesSuscripcion.PayerLastName = detalles.Payer?.Name?.Surname;
                detallesSuscripcion.PayerId = detalles.Payer?.PayerId;
                detallesSuscripcion.ShippingRecipientName = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Name?.FullName;
                detallesSuscripcion.ShippingLine1 = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.AddressLine1;
                detallesSuscripcion.ShippingCity = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.AdminArea2;
                detallesSuscripcion.ShippingState = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.AdminArea1;
                detallesSuscripcion.ShippingPostalCode = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.PostalCode;
                detallesSuscripcion.ShippingCountryCode = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.CountryCode;

                if (detalles.PurchaseUnits != null)
                {
                    foreach (var purchaseUnit in detalles.PurchaseUnits)
                    {
                        if (purchaseUnit != null)
                        {
                            detallesSuscripcion.TransactionsTotal = ConvertToDecimal(purchaseUnit.Amount?.Value);
                            detallesSuscripcion.TransactionsCurrency = purchaseUnit.Amount?.CurrencyCode;
                            detallesSuscripcion.TransactionsSubtotal = ConvertToDecimal(purchaseUnit.Amount?.Breakdown?.ItemTotal?.Value);

                            // Calcular subtotal si es necesario
                            if (detallesSuscripcion.TransactionsSubtotal == 0 && purchaseUnit.Items != null)
                            {
                                decimal? subtotal = 0;
                                foreach (var item in purchaseUnit.Items)
                                {
                                    var unitAmount = ConvertToDecimal(item.UnitAmount?.Value.ToString());
                                    var quantity = ConvertToInt(item.Quantity?.ToString());
                                    subtotal += unitAmount * quantity;
                                }
                                detallesSuscripcion.TransactionsSubtotal = subtotal;
                            }

                            detallesSuscripcion.TransactionsShipping = ConvertToDecimal(purchaseUnit.Amount?.Breakdown?.Shipping?.Value);
                            detallesSuscripcion.PayeeMerchantId = purchaseUnit.Payee?.MerchantId;
                            detallesSuscripcion.PayeeEmail = purchaseUnit.Payee?.EmailAddress;
                            detallesSuscripcion.Description = purchaseUnit.Description;

                            if (purchaseUnit.Payments?.Captures != null)
                            {
                                foreach (var capture in purchaseUnit.Payments.Captures)
                                {
                                    if (capture != null)
                                    {
                                        detallesSuscripcion.SaleId = capture.Id;
                                        detallesSuscripcion.SaleState = capture.Status;
                                        detallesSuscripcion.SaleTotal = ConvertToDecimal(capture.Amount?.Value);
                                        detallesSuscripcion.SaleCurrency = capture.Amount?.CurrencyCode;
                                        detallesSuscripcion.ProtectionEligibility = capture.SellerProtection?.Status;
                                        detallesSuscripcion.TransactionFeeAmount = ConvertToDecimal(capture.SellerReceivableBreakdown?.PaypalFee?.Value);
                                        detallesSuscripcion.TransactionFeeCurrency = capture.SellerReceivableBreakdown?.PaypalFee?.CurrencyCode;
                                        detallesSuscripcion.ReceivableAmount = ConvertToDecimal(capture.SellerReceivableBreakdown?.NetAmount?.Value);
                                        detallesSuscripcion.ReceivableCurrency = capture.SellerReceivableBreakdown?.NetAmount?.CurrencyCode;

                                        var exchangeRateValue = capture.SellerReceivableBreakdown?.ExchangeRate?.Value;
                                        if (decimal.TryParse((string)exchangeRateValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchangeRate))
                                        {
                                            detallesSuscripcion.ExchangeRate = exchangeRate;
                                        }
                                        detallesSuscripcion.CreateTime = ConvertToDateTime(capture.CreateTime);
                                        detallesSuscripcion.UpdateTime = ConvertToDateTime(capture.UpdateTime);
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
                                        PayPalId = detallesSuscripcion.Id,
                                        ItemName = item.Name,
                                        ItemSku = item.Sku,
                                        ItemPrice = ConvertToDecimal(item.UnitAmount?.Value),
                                        ItemCurrency = item.UnitAmount?.CurrencyCode,
                                        ItemTax = ConvertToDecimal(item.Tax?.Value),
                                        ItemQuantity = ConvertToInt(item.Quantity)
                                    };
                                    _context.PayPalPaymentItems.Add(paymentItem);
                                }
                            }
                        }
                    }
                }

                // Guardar los cambios en la base de datos
                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return (detallesSuscripcion, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los Junta detalles del pago");
                await transaccion.RollbackAsync();
                return (null, false, "Ha ocurrido un error");
            }
        }
        private decimal? ConvertToDecimal(object value)
        {
            if (value == null)
            {
                return null;
            }
            
            if (value is decimal decimalValue)
            {
                return decimalValue;
            }
            // Si el valor es una cadena, intenta convertirlo a decimal
            if (value is string stringValue)
            {
                if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            // Si el valor es de otro tipo, intenta convertirlo explícitamente a string y luego a decimal
            try
            {
                var stringRepresentation = value.ToString();
                if (decimal.TryParse(stringRepresentation, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar la conversión");
            }
            return null; 
        }
        private int? ConvertToInt(object value)
        {
            if (value == null)
            {
                return null;
            }
           
            if (value is int intValue)
            {
                return intValue;
            }
            // Si el valor es una cadena, intenta convertirlo a int
            if (value is string stringValue)
            {
                if (int.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            // Si el valor es de otro tipo, intenta convertirlo explícitamente a string y luego a int
            try
            {
                var stringRepresentation = value.ToString();
                if (int.TryParse(stringRepresentation, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar la conversión a int");
            }
            return null; 
        }
        public DateTime? ConvertToDateTime(object value)
        {
            if (value == null)
            {
                return null;
            }
            // Si el valor ya es un DateTime, simplemente lo devuelve
            if (value is DateTime dateTimeValue)
            {
                return dateTimeValue;
            }
            // Convertir el valor a cadena si no es ya un string
            string stringValue;
            if (value is string strValue)
            {
                stringValue = strValue;
            }
            else
            {
                // Convertir el valor a cadena, asumiendo que es un tipo no-string
                stringValue = value.ToString();
            }
            // Quitar corchetes si están presentes
            stringValue = stringValue.Trim('{', '}').Trim();
            // Intentar convertir usando formatos específicos
            var formats = new[]
            {
                "dd/MM/yyyy HH:mm:ss",   // Formato de 24 horas
                "dd/MM/yyyy H:mm:ss",    // Formato de 24 horas sin ceros a la izquierda
                "dd/MM/yyyy h:mm:ss tt", // Formato de 12 horas con AM/PM
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-dd",
                "MM/dd/yyyy",
                "MM-dd-yyyy"
            };
            // Intentar convertir con los formatos definidos
            if (DateTime.TryParseExact(stringValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }          
            Console.WriteLine($"No se pudo convertir. Formatos intentados: {string.Join(", ", formats)}");

            return null;
        }
    }
}