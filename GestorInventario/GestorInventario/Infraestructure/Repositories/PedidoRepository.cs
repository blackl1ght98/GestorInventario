
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
      //Estos metodos de aqui estan en PedidoController
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
        public async Task<(PayPalPaymentDetail, bool, string)> ObtenerDetallePagoEjecutado(string id)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {              
                var existingDetail = await _context.PayPalPaymentDetails
                    .Include(d => d.PayPalPaymentItems)
                    .FirstOrDefaultAsync(x => x.Id == id);

                // Si ya existe, devolver el detalle existente
                if (existingDetail != null)
                {
                    return (existingDetail,true,null);
                }

                // Obtener detalles del pago desde PayPal
                var detallespago = await _unitOfWork.PaypalService.ObtenerDetallesPagoEjecutado(id);
                if (detallespago == null)
                {
                    return (null,false,"No se ha encontrado el pago para generar la factura");
                }
                var detallesSuscripcion = new PayPalPaymentDetail
                {
                    Id = detallespago.id ?? string.Empty,
                    Intent = detallespago.intent ?? string.Empty,
                    State = detallespago.state ?? string.Empty,
                    Cart = detallespago.cart ?? string.Empty,
                    PaymentMethod = detallespago.payer.payment_method ?? string.Empty,
                    PayerStatus = detallespago.payer.status ?? string.Empty,
                    PayerEmail = detallespago.payer.payer_info.email ?? string.Empty,
                    PayerFirstName = detallespago.payer.payer_info.first_name ?? string.Empty,
                    PayerLastName = detallespago.payer.payer_info.last_name ?? string.Empty,
                    PayerId = detallespago.payer.payer_info.payer_id ?? string.Empty,
                    ShippingRecipientName = detallespago.payer.payer_info.shipping_address.recipient_name ?? string.Empty,
                    ShippingLine1 = detallespago.payer.payer_info.shipping_address.line1 ?? string.Empty,
                    ShippingCity = detallespago.payer.payer_info.shipping_address.city ?? string.Empty,
                    ShippingState = detallespago.payer.payer_info.shipping_address.state ?? string.Empty,
                    ShippingPostalCode = detallespago.payer.payer_info.shipping_address.postal_code ?? string.Empty,
                    ShippingCountryCode = detallespago.payer.payer_info.shipping_address.country_code ?? string.Empty
                };

                // Guardar detalles del pago en la base de datos primero
                _context.PayPalPaymentDetails.Add(detallesSuscripcion);
                await _context.SaveChangesAsync();

                // Ahora, guardar los items de pago relacionados
                if (detallespago.transactions != null && detallespago.transactions.Count > 0)
                {
                    foreach (var transaction in detallespago.transactions)
                    {
                        if (transaction != null)
                        {
                            detallesSuscripcion.TransactionsTotal = ConvertToDecimal(transaction?.amount?.total);
                            detallesSuscripcion.TransactionsCurrency = transaction?.amount?.currency ?? string.Empty;
                            detallesSuscripcion.TransactionsSubtotal = ConvertToDecimal(transaction?.amount?.details?.subtotal);
                            detallesSuscripcion.TransactionsShipping = ConvertToDecimal(transaction?.amount?.details?.shipping);
                            detallesSuscripcion.TransactionsInsurance = ConvertToDecimal(transaction?.amount?.details?.insurance);
                            detallesSuscripcion.TransactionsHandlingFee = ConvertToDecimal(transaction?.amount?.details?.handling_fee);
                            detallesSuscripcion.TransactionsShippingDiscount = ConvertToDecimal(transaction?.amount?.details?.shipping_discount);
                            detallesSuscripcion.TransactionsDiscount = ConvertToDecimal(transaction?.amount?.details?.discount);
                            detallesSuscripcion.PayeeMerchantId = transaction?.payee?.merchant_id ?? string.Empty;
                            detallesSuscripcion.PayeeEmail = transaction?.payee?.email ?? string.Empty;
                            detallesSuscripcion.Description = transaction?.description ?? string.Empty;

                            if (transaction.related_resources != null && transaction.related_resources.Count > 0)
                            {
                                foreach (var resource in transaction.related_resources)
                                {
                                    if (resource?.sale != null)
                                    {
                                        detallesSuscripcion.SaleId = resource.sale.id ?? string.Empty;
                                        detallesSuscripcion.SaleState = resource.sale.state ?? string.Empty;
                                        detallesSuscripcion.SaleTotal = ConvertToDecimal(resource.sale.amount?.total);
                                        detallesSuscripcion.SaleCurrency = resource.sale.amount?.currency ?? string.Empty;
                                        detallesSuscripcion.SaleSubtotal = ConvertToDecimal(resource.sale.amount?.details?.subtotal);
                                        detallesSuscripcion.SaleShipping = ConvertToDecimal(resource.sale.amount?.details?.shipping);
                                        detallesSuscripcion.SaleInsurance = ConvertToDecimal(resource.sale.amount?.details?.insurance);
                                        detallesSuscripcion.SaleHandlingFee = ConvertToDecimal(resource.sale.amount?.details?.handling_fee);
                                        detallesSuscripcion.SaleShippingDiscount = ConvertToDecimal(resource.sale.amount?.details?.shipping_discount);
                                        detallesSuscripcion.SaleDiscount = ConvertToDecimal(resource.sale.amount?.details?.discount);
                                        detallesSuscripcion.PaymentMode = resource.sale.payment_mode ?? string.Empty;
                                        detallesSuscripcion.ProtectionEligibility = resource.sale.protection_eligibility ?? string.Empty;
                                        detallesSuscripcion.ProtectionEligibilityType = resource.sale.protection_eligibility_type ?? string.Empty;
                                        detallesSuscripcion.TransactionFeeAmount = ConvertToDecimal(resource.sale.transaction_fee?.value);
                                        detallesSuscripcion.TransactionFeeCurrency = resource.sale.transaction_fee?.currency ?? string.Empty;
                                        detallesSuscripcion.ReceivableAmount = ConvertToDecimal(resource.sale.receivable_amount?.value);
                                        detallesSuscripcion.ReceivableCurrency = resource.sale.receivable_amount?.currency ?? string.Empty;
                                        detallesSuscripcion.ExchangeRate = ConvertToDecimal(resource.sale.exchange_rate);
                                        detallesSuscripcion.ParentPayment = resource.sale.parent_payment ?? string.Empty;
                                        detallesSuscripcion.CreateTime = ConvertToDateTime(resource.sale.create_time);
                                        detallesSuscripcion.UpdateTime = ConvertToDateTime(resource.sale.update_time);
                                    }
                                }
                            }

                            var items = transaction?.item_list?.items;
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    var paymentItem = new PayPalPaymentItem
                                    {
                                        PayPalId = detallesSuscripcion.Id,
                                        ItemName = item.name,
                                        ItemSku = item.sku,
                                        ItemPrice = item.price,
                                        ItemCurrency = item.currency,
                                        ItemTax = item.tax,
                                        ItemQuantity = item.quantity,
                                        ItemImageUrl = item.image_url
                                    };

                                    _context.PayPalPaymentItems.Add(paymentItem);
                                }
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                await transaccion.CommitAsync();
                return (detallesSuscripcion,true,null);
            }
            catch (Exception ex)
            {           
                _logger.LogError(ex, "Error al obtener los detalles del pago");
                await transaccion.RollbackAsync();
                return (null,false,"Ha ocurrido un error");
            }
        }
        public decimal? ConvertToDecimal(object value)
        {
            if (value == null)
            {
                return null;
            }
            // Si el valor es un decimal, simplemente lo devuelve
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
            catch(Exception ex) 
            {
                _logger.LogError(ex, "Error al realizar la conversion");
            }
            return null; // Si no puede convertir el valor, devuelve null
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