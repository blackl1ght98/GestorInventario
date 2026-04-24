
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;


namespace GestorInventario.Application.Services.Generic_Services
{
    public class CarritoService : ICarritoService
    {
        
        private readonly ICarritoRepository _carritoRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ILogger<CarritoService> _logger;
        private readonly IPedidoManagementService _pedidoService;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IProductoRepository _productoRepository;
        public CarritoService(ICarritoRepository carritoRepository, IPedidoManagementService pedido, IProductoRepository producto,
        ICurrentUserAccessor currentUserAccessor,ILogger<CarritoService> logger, IPedidoRepository pedidoRepository)
        {
          
            _carritoRepository = carritoRepository;
            _currentUserAccessor = currentUserAccessor;
            _logger = logger;
            _pedidoService = pedido;
            _pedidoRepository = pedidoRepository;
            _productoRepository = producto;
        }

        public async Task EliminarCarritoActivoAsync()  
        {
            var usuarioId = _currentUserAccessor.GetCurrentUserId();

            if (usuarioId <= 0)  
            {
                _logger.LogWarning("Intento de eliminar carrito sin usuario autenticado");
                return;
            }

            try
            {
                // 1. Obtener el carrito activo del usuario (debería ser solo uno)
                var carritoActivo = await _carritoRepository.ObtenerCarritoUsuario(usuarioId);

                if (carritoActivo.Data == null)
                {
                    _logger.LogDebug("No se encontró carrito activo para el usuario {UsuarioId}", usuarioId);
                    return;
                }

                // 2. Verificar si tiene items
                var items = await _carritoRepository.ObtenerItemsDelCarritoUsuario(carritoActivo.Data.Id);

                if (items?.Data?.Any() == true)
                {
                    _logger.LogInformation("Carrito activo del usuario {UsuarioId} tiene items → no se elimina", usuarioId);
                    return;
                }

                // 3. Eliminar (delegado al repositorio)
                await EliminarCarritoAsync(carritoActivo.Data.Id);

                _logger.LogInformation("Carrito activo vacío eliminado para el usuario {UsuarioId}", usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar eliminar el carrito activo del usuario {UsuarioId}", usuarioId);
                return;
            }
        }
        private async Task EliminarCarritoAsync(int carritoId)
        {
            await _pedidoService.EliminarPedido(carritoId);
        }
        // Método para crear un carrito si no existe
        public async Task<OperationResult<Pedido>> CrearCarritoUsuario(int userId)
        {

            try
            {
                var resultado = await _carritoRepository.ObtenerCarritoUsuario(userId);
                var carrito = resultado.Data;
                if (carrito == null)
                {
                    carrito = new Pedido
                    {
                        IdUsuario = userId,
                        NumeroPedido = GenerarNumPedido.GenerarNumeroPedido(),
                        FechaPedido = DateTime.Now,
                        EstadoPedido = EstadoPedido.Carrito.ToString(),
                        EsCarrito = true
                    };
                    await _pedidoRepository.AgregarPedidoAsync(carrito);

                }

                return OperationResult<Pedido>.Ok("", carrito);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrio un error inesperado");
                return OperationResult<Pedido>.Fail("Error al crear el carrito");
            }

        }
        public async Task<OperationResult<string>> Incremento(int id)
        {
            
                var resultado = await _carritoRepository.ItemsDelCarrito(id);
                var detalle = resultado.Data;
                if (detalle == null)
                {
                    return OperationResult<string>.Fail(resultado.Message);
                }

                detalle.Cantidad++;
                await _pedidoRepository.ActualizarDetallePedidoAsync(detalle);

                var producto = await _productoRepository.ObtenerProductoPorIdAsync((int)detalle.ProductoId);
                if (producto == null || producto.Cantidad <= 0)
                {

                    return OperationResult<string>.Fail("Inventario insuficiente para el producto elegido");
                }

                producto.Cantidad--;
                await _productoRepository.ActualizarProductoAsync(producto);

                return OperationResult<string>.Ok("Incremento realizado con exito");
         

        }
        public async Task<OperationResult<string>> AgregarProductoAlCarrito(int idProducto, int cantidad, int usuarioId)
        {

            // Validar cantidad
            if (cantidad <= 0)
            {
                return OperationResult<string>.Fail("La cantidad debe ser mayor a cero.");
            }

            // Validar existencia del producto y stock
            var producto = await _productoRepository.ObtenerProductoPorId(idProducto);
            if (producto == null)
            {
                return OperationResult<string>.Fail("El producto no existe.");
            }
            if (producto.Cantidad < cantidad)
            {
                return OperationResult<string>.Fail("No hay suficientes productos en stock.");
            }
            // Obtener o crear el carrito
            var carrito = await CrearCarritoUsuario(usuarioId);
            if (carrito != null)
            {
                var detalleExistente = await _productoRepository.ObtenerDetallesCarrito(carrito.Data.Id, idProducto);
                if (detalleExistente != null)
                {
                    // Sumar la cantidad al ítem existente
                    detalleExistente.Cantidad += cantidad;
                    await _pedidoRepository.ActualizarDetallePedidoAsync(detalleExistente);
                }
                else
                {
                    // Crear un nuevo ítem en el carrito
                    var detalle = new DetallePedido
                    {
                        PedidoId = carrito.Data.Id,
                        ProductoId = idProducto,
                        Cantidad = cantidad
                    };
                    await _pedidoRepository.AgregarDetallePedidoAsync(detalle);
                }

            }

            // Actualizar el inventario del producto
            producto.Cantidad -= cantidad;
            await _productoRepository.ActualizarProductoAsync(producto);
            return OperationResult<string>.Ok("Producto agregado con exito");


        }
        public async Task<OperationResult<string>> Decremento(int id)
        {
           
                var resultado = await _carritoRepository.ItemsDelCarrito(id);
                var detalle = resultado.Data;
                if (detalle == null)
                {
                    return OperationResult<string>.Fail(resultado.Message);
                }

                detalle.Cantidad--;
                if (detalle.Cantidad <= 0)
                {
                    await _pedidoRepository.EliminarDetallePedidoAsync(detalle);
                }
                else
                {
                    await _pedidoRepository.ActualizarDetallePedidoAsync(detalle);
                }

                var producto = await _productoRepository.ObtenerProductoPorIdAsync((int)detalle.ProductoId);
                if (producto == null)
                {

                    return OperationResult<string>.Fail("El producto no existe");
                }

                producto.Cantidad++;
            await _productoRepository.ActualizarProductoAsync(producto);


            return OperationResult<string>.Ok("Incremento realizado con exito");
           
        }
        public async Task<OperationResult<string>> EliminarProductoCarrito(int id)
        {

            var detalle = await _pedidoRepository.ObtenerDetallePorIdAsync(id);
                if (detalle == null)
                {
                    return OperationResult<string>.Fail("No se puede eliminar porque no hay productos");
                }

                var producto = await _productoRepository.ObtenerProductoPorIdAsync((int)detalle.ProductoId);
                if (producto != null)
                {
                    producto.Cantidad += detalle.Cantidad ?? 0;
                    await _productoRepository.ActualizarProductoAsync(producto);
                }

                await _pedidoRepository.EliminarDetallePedidoAsync(detalle);

                return OperationResult<string>.Ok("Eliminacion exitosa del producto");
           
        }
    }
}
