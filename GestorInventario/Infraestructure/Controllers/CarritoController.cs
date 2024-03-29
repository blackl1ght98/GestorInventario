using GestorInventario.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class CarritoController : Controller
    {
        private readonly GestorInventarioContext _context;

        public CarritoController(GestorInventarioContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId;
            if (int.TryParse(existeUsuario, out usuarioId))
            {
                
                var carrito = await _context.Carritos.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
                if (carrito != null)
                {
                    
                    var itemsDelCarrito = await _context.ItemsDelCarritos
                        .Include(i => i.Producto)
                     
                         .Include(i => i.Producto.IdProveedorNavigation)
                       
                        .Where(i => i.CarritoId == carrito.Id)
                        .ToListAsync();

                    // Pasa los productos a la vista
                    return View(itemsDelCarrito);
                }
            }

            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> Checkout()
        {
            var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId;
            if (int.TryParse(existeUsuario, out usuarioId))
            {
                var carrito = await _context.Carritos.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
                if (carrito != null)
                {
                    var itemsDelCarrito = await _context.ItemsDelCarritos
                        .Where(i => i.CarritoId == carrito.Id)
                        .ToListAsync();

                    var pedido = new Pedido
                    {
                        NumeroPedido = GenerarNumeroPedido(),
                        FechaPedido = DateTime.Now,
                        EstadoPedido = "Pendiente",
                        IdUsuario = usuarioId
                    };

                    _context.Pedidos.Add(pedido);
                    await _context.SaveChangesAsync();
                    //Este bucle se va a recorrer como items existan en el carrito
                    foreach (var item in itemsDelCarrito)
                    {
                        var detallePedido = new DetallePedido
                        {
                            PedidoId = pedido.Id,
                            ProductoId = item.ProductoId,
                            Cantidad = item.Cantidad ?? 0
                        };

                        _context.DetallePedidos.Add(detallePedido);
                    }

                    _context.ItemsDelCarritos.RemoveRange(itemsDelCarrito);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "El pedido se ha creado con éxito.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return RedirectToAction("Index", "Home");
        }


        private string GenerarNumeroPedido()
        {
            var length = 10;
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
           .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        [HttpPost]
        public async Task<ActionResult> Incrementar(int id)
        {
            // Busca el producto en la base de datos
            var carrito = await _context.ItemsDelCarritos.FirstOrDefaultAsync(p => p.Id == id);

            if (carrito != null)
            {
                // Incrementa la cantidad del producto
                carrito.Cantidad++;
                _context.ItemsDelCarritos.Update(carrito);
                // Guarda los cambios en la base de datos
                await _context.SaveChangesAsync();
            }

            // Redirige al usuario a la página de índice
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> Decrementar(int id)
        {
            // Busca el producto en la base de datos
            var carrito = await _context.ItemsDelCarritos.FirstOrDefaultAsync(p => p.Id == id);

            if (carrito != null)
            {
                // Decrementa la cantidad del producto en el carrito
                carrito.Cantidad--;

                // Busca el producto correspondiente en la tabla de productos
                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == carrito.ProductoId);

                if (producto != null)
                {
                    // Incrementa la cantidad del producto en la tabla de productos
                    producto.Cantidad++;
                    _context.Productos.Update(producto);
                }

                _context.ItemsDelCarritos.Update(carrito);
                // Guarda los cambios en la base de datos
                await _context.SaveChangesAsync();
            }

            // Redirige al usuario a la página de índice
            return RedirectToAction("Index");
        }

    }
}
