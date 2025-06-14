
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Security.Claims;
namespace GestorInventario.Infraestructure.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PedidoRepository> _logger;
        public PedidoRepository(GestorInventarioContext context, IMemoryCache memory, IHttpContextAccessor contextAccessor, IUnitOfWork unitOfWork, ILogger<PedidoRepository> logger)
        {
            _context = context;
            _cache = memory;
            _contextAccessor = contextAccessor;
            _unitOfWork = unitOfWork;
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
                var existingDetail = await _context.PayPalPaymentDetails
                    .Include(d => d.PayPalPaymentItems)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (existingDetail != null)
                {
                    return (existingDetail, true, null);
                }

                var detallespago = await _unitOfWork.PaypalService.ObtenerDetallesPagoEjecutadoV2(id);
                if (detallespago == null)
                {
                    return (null, false, "No se ha encontrado el pago para generar la factura");
                }

                var detallesSuscripcion = new PayPalPaymentDetail
                {
                    Id = detallespago["id"]?.ToString() ?? string.Empty,
                    Intent = detallespago["intent"]?.ToString() ?? string.Empty,
                    Status = detallespago["status"]?.ToString() ?? string.Empty,
                    PaymentMethod = "paypal",
                    PayerEmail = detallespago["payer"]?["email_address"]?.ToString() ?? string.Empty,
                    PayerFirstName = detallespago["payer"]?["name"]?["given_name"]?.ToString() ?? string.Empty,
                    PayerLastName = detallespago["payer"]?["name"]?["surname"]?.ToString() ?? string.Empty,
                    PayerId = detallespago["payer"]?["payer_id"]?.ToString() ?? string.Empty,
                    ShippingRecipientName = detallespago["purchase_units"]?[0]?["shipping"]?["name"]?["full_name"]?.ToString() ?? string.Empty,
                    ShippingLine1 = detallespago["purchase_units"]?[0]?["shipping"]?["address"]?["address_line_1"]?.ToString() ?? string.Empty,
                    ShippingCity = detallespago["purchase_units"]?[0]?["shipping"]?["address"]?["admin_area_2"]?.ToString() ?? string.Empty,
                    ShippingState = detallespago["purchase_units"]?[0]?["shipping"]?["address"]?["admin_area_1"]?.ToString() ?? string.Empty,
                    ShippingPostalCode = detallespago["purchase_units"]?[0]?["shipping"]?["address"]?["postal_code"]?.ToString() ?? string.Empty,
                    ShippingCountryCode = detallespago["purchase_units"]?[0]?["shipping"]?["address"]?["country_code"]?.ToString() ?? string.Empty
                };

                _context.PayPalPaymentDetails.Add(detallesSuscripcion);
                await _context.SaveChangesAsync();

                if (detallespago["purchase_units"] != null)
                {
                    foreach (var purchaseUnit in detallespago["purchase_units"])
                    {
                        if (purchaseUnit != null)
                        {
                            detallesSuscripcion.TransactionsTotal = ConvertToDecimal(purchaseUnit?["amount"]?["value"]?.ToString());
                            detallesSuscripcion.TransactionsCurrency = purchaseUnit?["amount"]?["currency_code"]?.ToString() ?? string.Empty;
                            detallesSuscripcion.TransactionsSubtotal = ConvertToDecimal(purchaseUnit?["amount"]?["breakdown"]?["item_total"]?["value"]?.ToString());
                            if (detallesSuscripcion.TransactionsSubtotal == 0 && purchaseUnit["items"] != null)
                            {
                                decimal subtotal = 0;
                                foreach (var item in purchaseUnit["items"])
                                {
                                    var unitAmount = ConvertToDecimal(item["unit_amount"]?["value"]?.ToString());
                                    var quantity = ConvertToInt(item["quantity"]?.ToString());
                                    subtotal += unitAmount * quantity;
                                }
                                detallesSuscripcion.TransactionsSubtotal = subtotal;
                            }
                            detallesSuscripcion.TransactionsShipping = ConvertToDecimal(purchaseUnit?["amount"]?["breakdown"]?["shipping"]?["value"]?.ToString());
                            detallesSuscripcion.PayeeMerchantId = purchaseUnit?["payee"]?["merchant_id"]?.ToString() ?? string.Empty;
                            detallesSuscripcion.PayeeEmail = purchaseUnit?["payee"]?["email_address"]?.ToString() ?? string.Empty;
                            detallesSuscripcion.Description = purchaseUnit?["description"]?.ToString() ?? string.Empty;

                            if (purchaseUnit["payments"]?["captures"] != null)
                            {
                                foreach (var capture in purchaseUnit["payments"]["captures"])
                                {
                                    if (capture != null)
                                    {
                                        detallesSuscripcion.SaleId = capture["id"]?.ToString() ?? string.Empty;
                                        detallesSuscripcion.SaleState = capture["status"]?.ToString() ?? string.Empty;
                                        detallesSuscripcion.SaleTotal = ConvertToDecimal(capture["amount"]?["value"]?.ToString());
                                        detallesSuscripcion.SaleCurrency = capture["amount"]?["currency_code"]?.ToString() ?? string.Empty;
                                        detallesSuscripcion.ProtectionEligibility = capture["seller_protection"]?["status"]?.ToString() ?? string.Empty;
                                        detallesSuscripcion.TransactionFeeAmount = ConvertToDecimal(capture["seller_receivable_breakdown"]?["paypal_fee"]?["value"]?.ToString());
                                        detallesSuscripcion.TransactionFeeCurrency = capture["seller_receivable_breakdown"]?["paypal_fee"]?["currency_code"]?.ToString() ?? string.Empty;
                                        detallesSuscripcion.ReceivableAmount = ConvertToDecimal(capture["seller_receivable_breakdown"]?["net_amount"]?["value"]?.ToString());
                                        detallesSuscripcion.ReceivableCurrency = capture["seller_receivable_breakdown"]?["net_amount"]?["currency_code"]?.ToString() ?? string.Empty;
                                        var exchangeRateValue = capture["seller_receivable_breakdown"]?["exchange_rate"]?["value"]?.ToString() ?? string.Empty;
                                        if (decimal.TryParse((string)exchangeRateValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchangeRate))
                                        {
                                            detallesSuscripcion.ExchangeRate = exchangeRate;
                                        }
                                        detallesSuscripcion.CreateTime = ConvertToDateTime(capture["create_time"]);
                                        detallesSuscripcion.UpdateTime = ConvertToDateTime(capture["update_time"]);
                                    }
                                }
                            }

                            var items = purchaseUnit["items"];
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    var paymentItem = new PayPalPaymentItem
                                    {
                                        PayPalId = detallesSuscripcion.Id,
                                        ItemName = item["name"]?.ToString(),
                                        ItemSku = item["sku"]?.ToString(),
                                        ItemPrice = ConvertToDecimal(item["unit_amount"]?["value"]?.ToString()),
                                        ItemCurrency = item["unit_amount"]?["currency_code"]?.ToString(),
                                        ItemTax = ConvertToDecimal(item["tax"]?["value"]?.ToString()),
                                        ItemQuantity = ConvertToInt(item["quantity"]?.ToString())
                                    };
                                    _context.PayPalPaymentItems.Add(paymentItem);
                                }
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                await transaccion.CommitAsync();
                return (detallesSuscripcion, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los detalles del pago");
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