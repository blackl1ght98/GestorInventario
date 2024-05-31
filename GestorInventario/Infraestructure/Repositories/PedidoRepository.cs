using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PedidoRepository:IPedidoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        public PedidoRepository(GestorInventarioContext context, IMemoryCache memory, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _cache = memory;
            _contextAccessor = contextAccessor;
        }
        public IQueryable<Pedido> ObtenerPedidos()
        {
            var pedidos= from p in _context.Pedidos.Include(dp=>dp.DetallePedidos).ThenInclude(p=>p.Producto).Include(u=>u.IdUsuarioNavigation)
                         select p;
            return pedidos;
        }
        public IQueryable<Pedido> ObtenerPedidoUsuario(int userId)
        {
            var pedidos= _context.Pedidos.Where(p => p.IdUsuario == userId)
                            .Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto)
                            .Include(u => u.IdUsuarioNavigation);
            return pedidos;
        }
        public async Task<(bool, string)> CrearPedido(PedidosViewModel model)
        {
            var pedido = new Pedido()
            {
                NumeroPedido = model.NumeroPedido,
                FechaPedido = model.FechaPedido,
                EstadoPedido = model.EstadoPedido,
                IdUsuario = model.IdUsuario,
            };
            _context.AddEntity(pedido);
         
            var numeroPedidoGenerado = pedido.NumeroPedido;
            var historialPedido = new HistorialPedido()
            {
                IdUsuario = pedido.IdUsuario,
                Fecha = DateTime.Now,
                Accion = _contextAccessor.HttpContext.Request.Method.ToString(),
                Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
            };
           _context.AddEntity(historialPedido);
            //Obtiene todos los productos que existan en base de datos
            for (var i = 0; i < model.IdsProducto.Count; i++)
            {
                // Si el producto en la posición i fue seleccionado...
                if (model.ProductosSeleccionados[i])
                {
                    // Se crea un nuevo detalle de pedido para el producto seleccionado.
                    var detallePedido = new DetallePedido()
                    {
                        PedidoId = pedido.Id,
                        ProductoId = model.IdsProducto[i],
                        Cantidad = model.Cantidades[i],
                    };
                    // Se agrega el detalle del pedido al contexto de la base de datos.
                    _context.AddEntity(detallePedido);
                }
            }
            // Lógica para los detalles de productos (si es necesario)
            for (var i = 0; i < model.IdsProducto.Count; i++)
            {
                if (model.ProductosSeleccionados[i])
                {
                    var detalleHistorialPedido = new DetalleHistorialPedido()
                    {
                        HistorialPedidoId = historialPedido.Id,
                        ProductoId = model.IdsProducto[i],
                        Cantidad = model.Cantidades[i],
                    };
                    _context.AddEntity(detalleHistorialPedido);
                }
            }
            return (true, null);
        }
       
        public async Task<List<Producto>> ObtenerProductos()
        {
            var producto = await _context.Productos.ToListAsync();
            return producto;
        }
        public async Task<List<Usuario>> ObtenerUsuarios()
        {
            var usuarios= await _context.Usuarios.ToListAsync();
            return usuarios;
        }
        public async Task<Pedido> ObtenerPedidoEliminacion(int id)
        {
            var pedido = await _context.Pedidos
                  .Include(p => p.DetallePedidos)
                      .ThenInclude(dp => dp.Producto)
                  .Include(p => p.IdUsuarioNavigation)
                  .FirstOrDefaultAsync(m => m.Id == id);
            return pedido;
        }
        public async Task<(bool, string)> EliminarPedido(int Id)
        {
            var pedido = await _context.Pedidos.Include(p => p.DetallePedidos).FirstOrDefaultAsync(m => m.Id == Id);
            if (pedido == null)
            {
                return (false, "No hay pedido a eliminar");
            }
            if (pedido.EstadoPedido == "Entregado")
            {
                var historialPedido = new HistorialPedido()
                {
                    IdUsuario = pedido.IdUsuario,
                    Fecha = DateTime.Now,
                    Accion = "DELETE",
                    Ip = _contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                };
                _context.AddEntity(historialPedido); 
                foreach (var detalle in pedido.DetallePedidos)
                {
                    var detalleHistorial = new DetalleHistorialPedido()
                    {
                        HistorialPedidoId = historialPedido.Id,
                        ProductoId = detalle.ProductoId,
                        Cantidad = detalle.Cantidad,
                    };
                    _context.AddEntity(detalleHistorial);  
                }
                _context.DeleteRangeEntity(pedido.DetallePedidos);
                _context.DeleteEntity(pedido);
            }
            else
            {
                return (false, "El pedido tiene que tener el estado Entregado para ser eliminado");
            }

            return (true, null);
        }
        public async Task<HistorialPedido> EliminarHistorialPorId(int id)
        {
            var historialPedido = await _context.HistorialPedidos.Include(x => x.DetalleHistorialPedidos).FirstOrDefaultAsync(x => x.Id == id);
            return historialPedido;
        }
        public async Task<(bool, string)> EliminarHistorialPorIdDefinitivo(int Id)
        {
            var historialPedido = await _context.HistorialPedidos.Include(x => x.DetalleHistorialPedidos).FirstOrDefaultAsync(x => x.Id == Id);
            if (historialPedido != null)
            {
                _context.DeleteRangeEntity(historialPedido.DetalleHistorialPedidos);
                _context.DeleteEntity(historialPedido);
            }
            else
            {
                return (false, "No se puede eliminar, el historial no existe");
            }
            return (true, null);
        }
    }
}
