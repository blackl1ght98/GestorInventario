using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs.Carrito;
using GestorInventario.Application.DTOs.Checkout;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace GestorInventario.Infraestructure.Repositories
{
    public class CarritoRepository : ICarritoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IPaypalOrderService _paypalOrder;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CarritoRepository> _logger;       
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        public CarritoRepository(GestorInventarioContext context, IPaypalOrderService service,  ICurrentUserAccessor current,
            IConfiguration configuration, ILogger<CarritoRepository> logger,  IUserRepository user)
        {
            _context = context;
            _paypalOrder = service;
            _configuration = configuration;
            _logger = logger;           
            _userRepository = user;
            _currentUserAccessor = current;
        }

        // Obtener el carrito del usuario (Pedidos con EsCarrito = 1)
        public async Task<OperationResult<Pedido>> ObtenerCarritoUsuario(int userId)
        {
            var carrito = await _context.Pedidos
                .FirstOrDefaultAsync(x => x.IdUsuario == userId && x.EsCarrito);
            if (carrito == null) {
                return OperationResult<Pedido>.Fail("No se puede obtener el carrito del usuario");
            }
            return OperationResult<Pedido>.Ok("Carrito obtenido con exito", carrito);
            
        }
     

        // Obtener ítems del carrito (DetallePedido para un Pedido con EsCarrito = 1)
        public async Task<OperationResult<List<DetallePedido>>> ObtenerItemsDelCarritoUsuario(int pedidoId)
        {
            var detallePedido = await _context.DetallePedidos
                .Where(i => i.PedidoId == pedidoId)
                .ToListAsync();
            return OperationResult<List<DetallePedido>>.Ok("", detallePedido);
        }

        // Obtener un ítem específico del carrito por ID
        public async Task<OperationResult<DetallePedido>> ItemsDelCarrito(int id)
        {
            var item = await _context.DetallePedidos
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item is null)
                return OperationResult<DetallePedido>.Fail("No hay productos en el carrito");

            return OperationResult<DetallePedido>.Ok("Productos obtenidos",item);
        }


        // Obtener ítems con datos relacionados (producto y proveedor)
        public OperationResult<IQueryable<DetallePedido>> ObtenerItemsConDetalles(int pedidoId)
        {
            var detalle = _context.DetallePedidos
                .Include(i => i.Producto)
                .Include(i => i.Producto.IdProveedorNavigation)
                .Where(i => i.PedidoId == pedidoId);
            return OperationResult<IQueryable<DetallePedido>>.Ok("", detalle);
        }

        // Obtener monedas disponibles
        public async Task<OperationResult<List<Monedum>>> ObtenerMoneda()
        {
            var monedas = await _context.Moneda.ToListAsync();
            return OperationResult<List<Monedum>>.Ok("",monedas);
        }

        private string GenerarNumeroPedido()
        {
            var length = 10;
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public async Task EliminarCarritoAsync(int carritoId)
        {
            // Opción 1: eliminación física (más simple si no necesitas auditoría)
            var carrito = await _context.Pedidos.FindAsync(carritoId);
            if (carrito != null)
            {
                _context.Pedidos.Remove(carrito);
                await _context.SaveChangesAsync();
            }

           
        }
        // Método para crear un carrito si no existe
        public async Task<OperationResult<Pedido>> CrearCarritoUsuario(int userId)
        {
           
            try
            {
                var resultado = await ObtenerCarritoUsuario(userId);
                var carrito = resultado.Data;
                if (carrito == null)
                {
                    carrito = new Pedido
                    {
                        IdUsuario = userId,
                        NumeroPedido = GenerarNumeroPedido(),
                        FechaPedido = DateTime.Now,
                        EstadoPedido = "Carrito",
                        EsCarrito = true
                    };
                    await _context.AddEntityAsync(carrito);
                   
                }

                return OperationResult<Pedido>.Ok("",carrito);
            }
            catch (Exception ex) {
                _logger.LogError(ex,"Ocurrio un error inesperado");
                return null;
            }
           
        }


        public async Task<OperationResult<string>> Pagar(string moneda, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Recolectar información del usuario
                var result = await ValidarUsuarioYObtenerInfo();
                var infoUsuario = result.Data;
                //Validar y obtener items del carrito
                var resultado = await ValidarCarritoYObtenerItems(userId);
                if (!resultado.Success)
                {
                    return OperationResult<string>.Fail(resultado.Message);
                }
                var carrito = resultado.Data.Carrito;
                var itemsDelCarrito = resultado.Data.Items;
                // Convertir el carrito en un pedido
                await ConvertirCarritoAPedido(carrito);

                // Calcular el total para PayPal
                moneda = string.IsNullOrEmpty(moneda) ? "EUR" : moneda;
                var checkout = await PrepararCheckoutParaPagoPayPal(itemsDelCarrito, moneda, infoUsuario);
                // Iniciar pago con PayPal
                var  approvalUrl = await ProcesarPagoPayPal(checkout);
                if (!approvalUrl.Success)
                {
                    await transaction.RollbackAsync();
                    return OperationResult<string>.Fail(approvalUrl.Message);
                }
                await RegistrarHistorialPedido(carrito, itemsDelCarrito);
                await EliminarCarritosVaciosUsuario(userId);

                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Redirigiendo a PayPal para completar el pago", approvalUrl.Data);
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el pago");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Ocurrió un error inesperado. Por favor, contacte con el administrador o intentelo de nuevo más tarde.");
                
            }
        }
        private async Task<OperationResult<InfoUsuarioDto>> ValidarUsuarioYObtenerInfo()
        {
            var usuarioId = _currentUserAccessor.GetCurrentUserId();
            var usuarioActual = await _userRepository.ObtenerUsuarioPorId(usuarioId);
            if(usuarioActual == null)
            {
                return OperationResult<InfoUsuarioDto>.Fail("El usuario no existe");
            }
            var infoUsuario = new InfoUsuarioDto
            {
                NombreCompletoUsuario = usuarioActual.Data.NombreCompleto ?? "Nombre no facilitado",
                Telefono = usuarioActual.Data.Telefono ?? "Telefono no facilitado",
                CodigoPostal = usuarioActual.Data.CodigoPostal ?? "Codigo Postal no facilitado",
                Ciudad = usuarioActual.Data.Ciudad ?? "Ciudad no facilitado",
                Line1 = usuarioActual.Data.Direccion.Split(",")[0].Trim(),
                Line2 = usuarioActual.Data.Direccion.Split(",").Length > 1 ? usuarioActual.Data.Direccion.Split(",")[1].Trim() : ""
            };
           
            return OperationResult<InfoUsuarioDto>.Ok("Validacion exitosa",infoUsuario);
        }
        private async Task<OperationResult<CarritoConItemsDto>> ValidarCarritoYObtenerItems(int userId)
        {
            var result = await ObtenerCarritoUsuario(userId);
            var carrito = result.Data;
            if (carrito == null)
            {
                return OperationResult<CarritoConItemsDto>.Fail("No se encontró un carrito para el usuario.");
            }

            var itemsDelCarrito = await ObtenerItemsDelCarritoUsuario(carrito.Id);
            if (!itemsDelCarrito.Data.Any())
            {
                return OperationResult<CarritoConItemsDto>.Fail("El carrito está vacío.");
            }

            var resultado = new CarritoConItemsDto
            {
                Carrito = carrito,
                Items = itemsDelCarrito.Data
            };

            return OperationResult<CarritoConItemsDto>.Ok("Validacion exitosa",resultado);
        }

        private async Task ConvertirCarritoAPedido(Pedido carrito)
        {
           
            try {
                carrito.EsCarrito = false;
                carrito.NumeroPedido = GenerarNumeroPedido();
                carrito.FechaPedido = DateTime.Now;
                carrito.EstadoPedido = "En Proceso";
                await _context.UpdateEntityAsync(carrito);
             
            } catch (Exception ex) {
                
                _logger.LogError(ex,"Ocurrio un error inesperado");
            
            }
           
        }
       
        private async Task<CheckoutDto> PrepararCheckoutParaPagoPayPal(List<DetallePedido> itemsDelCarrito, string moneda, InfoUsuarioDto infoUsuario)
        {
            var items = new List<ItemModelDto>();
            decimal totalAmount = 0;
           
                foreach (var item in itemsDelCarrito)
                {
               
                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto != null)
                    {
                   
                        var paypalItem = new ItemModelDto
                        {
                            Name = producto.NombreProducto,
                            Currency = moneda,
                            Price = producto.Precio,
                          
                            Quantity = item.Cantidad.Value.ToString(),
                            Sku = producto.Descripcion
                        };
                        items.Add(paypalItem);
                        totalAmount += Convert.ToDecimal(producto.Precio) * Convert.ToDecimal(item.Cantidad ?? 0);
                    }

                
            }
          
            string returnUrl = ObtenerReturnUrl();
            string cancelUrl = "https://localhost:7056/Payment/Cancel";

            return new CheckoutDto
            {
                TotalAmount = totalAmount,
                Currency = moneda,
                Items = items,
                NombreCompleto = infoUsuario.NombreCompletoUsuario,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                Telefono = infoUsuario.Telefono,
                Ciudad = infoUsuario.Ciudad,
                CodigoPostal = infoUsuario.CodigoPostal,
                Line1 = infoUsuario.Line1,
                Line2 = infoUsuario.Line2
            };
        }
        private string ObtenerReturnUrl()
        {
            var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
            var configKey = isDocker ? "Paypal:returnUrlConDocker" : "Paypal:returnUrlSinDocker";
            var envVarKey = isDocker ? "Paypal_returnUrlConDocker" : "Paypal_returnUrlSinDocker";

            var returnUrl = _configuration[configKey] ?? Environment.GetEnvironmentVariable(envVarKey);
            return returnUrl ?? throw new InvalidOperationException($"La URL de retorno no está configurada. Verifique la clave '{configKey}' o la variable de entorno '{envVarKey}'.");
        }
        private async Task<OperationResult<string>> ProcesarPagoPayPal(CheckoutDto checkout)
        {
            var createdPaymentJson = await _paypalOrder.CreateOrderWithPaypalAsync(checkout);
            var createdPayment = JsonConvert.DeserializeObject<PayPalOrderResponse>(createdPaymentJson);
            var approvalUrl = createdPayment?.Links?.FirstOrDefault(x => x.Rel == "payer-action")?.Href;
            if (!string.IsNullOrEmpty(approvalUrl))
            {
                return OperationResult<string>.Ok("Redirigiendo a PayPal para completar el pago", approvalUrl);
                
            }
            return OperationResult<string>.Fail("Error al procesar el pago con paypal");
        }


        private async Task RegistrarHistorialPedido(Pedido pedido, List<DetallePedido> itemsDelCarrito)
        {
            string ipString= _currentUserAccessor.GetClientIpAddress();           
            try
            {
                // Crear el registro del historial del pedido
                var historialPedido = new HistorialPedido
                {
                    IdUsuario = pedido.IdUsuario,
                    Fecha = DateTime.UtcNow, 
                    Accion = "Nuevo pedido",
                    Ip = ipString
                };
                await _context.AddEntityAsync(historialPedido);

                // Agregar los detalles del historial
                foreach (var item in itemsDelCarrito)
                {
                    var detalleHistorialPedido = new DetalleHistorialPedido
                    {
                        HistorialPedidoId = historialPedido.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad ?? 0,
                        NumeroPedido = pedido.NumeroPedido,
                        FechaPedido = DateTime.UtcNow, 
                        EstadoPedido = pedido.EstadoPedido
                    };
                    await _context.AddEntityAsync(detalleHistorialPedido);
                }

               
                _logger.LogInformation($"Historial registrado para pedido {pedido.NumeroPedido}, ID {historialPedido.Id}");
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex, $"Error al registrar historial para pedido {pedido.NumeroPedido}");
                throw;
            }
        }
        private async Task EliminarCarritosVaciosUsuario(int userId)
        {
          
            try {
                var carritosActivos = await _context.Pedidos
                   .Include(p => p.DetallePedidos)
                   .Where(p => p.IdUsuario == userId && p.EsCarrito)
                   .ToListAsync();
                foreach (var carritoActivo in carritosActivos)
                {
                    if (!carritoActivo.DetallePedidos.Any())
                    {
                        await _context.DeleteEntityAsync(carritoActivo);
                        _logger.LogInformation($"Carrito vacío eliminado para el usuario {userId}, ID: {carritoActivo.Id}");
                    }
                }
                await _context.SaveChangesAsync();
               

            } catch (Exception ex) {
                _logger.LogError(ex,"Ocurrio un error inesperado");
               
           }
           
        }


        public async Task<OperationResult<string>> Incremento(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var resultado = await ItemsDelCarrito(id);
                var detalle = resultado.Data;
                if (detalle == null)
                {
                    return OperationResult<string>.Fail(resultado.Message);
                }

                detalle.Cantidad++;
                await _context.UpdateEntityAsync(detalle);

                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
                if (producto == null || producto.Cantidad <= 0)
                {
                    await transaction.RollbackAsync();
                    return OperationResult<string>.Fail("Inventario insuficiente para el producto elegido");
                }

                producto.Cantidad--;
                await _context.UpdateEntityAsync(producto);
                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Incremento realizado con exito");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el incremento");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Ocurrio un error inesperado al incrementar el producto, intentelo de nuevo mas tarde");
               
            }
        }

        public async Task<OperationResult<string>> Decremento(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var resultado = await ItemsDelCarrito(id);
                var detalle = resultado.Data;
                if (detalle == null)
                {
                    return OperationResult<string>.Fail(resultado.Message);
                }

                detalle.Cantidad--;
                if (detalle.Cantidad <= 0)
                {
                    await _context.DeleteEntityAsync(detalle);
                }
                else
                {
                    await _context.UpdateEntityAsync(detalle);
                }

                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
                if (producto == null)
                {
                    await transaction.RollbackAsync();
                    return OperationResult<string>.Fail("El producto no existe");
                }

                producto.Cantidad++;
                await _context.UpdateEntityAsync(producto);

                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Incremento realizado con exito");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el decremento");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Ocurrio un error inesperado al decrementar el producto, intentelo de nuevo mas tarde");
            }
        }

        public async Task<OperationResult<string>> EliminarProductoCarrito(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var detalle = await _context.DetallePedidos.FirstOrDefaultAsync(x => x.Id == id);
                if (detalle == null)
                {
                    return OperationResult<string>.Fail("No se puede eliminar porque no hay productos");
                }

                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
                if (producto != null)
                {
                    producto.Cantidad += detalle.Cantidad ?? 0;
                    await _context.UpdateEntityAsync(producto);
                }

                await _context.DeleteEntityAsync(detalle);
                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Eliminacion exitosa del producto");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar la eliminación del producto del carrito");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Ocurrió un error inesperado. Por favor, contacte con el administrador o intentelo de nuevo más tarde.");
                
            }
        }
    }
}