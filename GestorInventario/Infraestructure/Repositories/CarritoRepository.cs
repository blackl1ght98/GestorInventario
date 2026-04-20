
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;

namespace GestorInventario.Infraestructure.Repositories
{
    public class CarritoRepository : ICarritoRepository
    {
        private readonly GestorInventarioContext _context;     
        private readonly ILogger<CarritoRepository> _logger;         
        private readonly IPedidoManagementService _pedidoService;
        public CarritoRepository(GestorInventarioContext context,  
            ILogger<CarritoRepository> logger,  IPedidoManagementService pedido)
        {
            _context = context;        
            _logger = logger;                
            _pedidoService = pedido;
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
        public async Task<List<Pedido>> ObtenerCarritosActivosAsync(int userId)=> await _context.Pedidos
               .Include(p => p.DetallePedidos)
               .Where(p => p.IdUsuario == userId && p.EsCarrito)
               .ToListAsync();

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
                        NumeroPedido = _pedidoService.GenerarNumeroPedido(),
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


    


        public async Task<OperationResult<string>> Incremento(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
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

                    return OperationResult<string>.Fail("Inventario insuficiente para el producto elegido");
                }

                producto.Cantidad--;
                await _context.UpdateEntityAsync(producto);

                return OperationResult<string>.Ok("Incremento realizado con exito");
            });
           
        }

        public async Task<OperationResult<string>> Decremento(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
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

                    return OperationResult<string>.Fail("El producto no existe");
                }

                producto.Cantidad++;
                await _context.UpdateEntityAsync(producto);


                return OperationResult<string>.Ok("Incremento realizado con exito");
            });
        }

        public async Task<OperationResult<string>> EliminarProductoCarrito(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
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

                return OperationResult<string>.Ok("Eliminacion exitosa del producto");
            });
        }
    }
}