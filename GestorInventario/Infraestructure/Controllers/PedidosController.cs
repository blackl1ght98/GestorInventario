using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Controllers
{
    public class PedidosController : Controller
    {
        private readonly GestorInventarioContext _context;

        public PedidosController(GestorInventarioContext context)
        {
            _context = context;
        }

        //public async Task<IActionResult> Index()
        //{
        //    //ThenInclude es usado para consultar datos de una relacion con include si tu tienes un include y 
        //    //de ese include necesitas obtener datos pues usas theninclude
        //    var pedidos =  await _context.Pedidos
        //        .Include(p => p.DetallePedidos)
        //            .ThenInclude(dp => dp.Producto)
        //        .Include(p => p.IdUsuarioNavigation)
        //        .ToListAsync();



        //    return View(pedidos);
        //}
        public async Task<IActionResult> Index([FromQuery] Paginacion paginacion)
        {
            var pedidos = _context.Pedidos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .Include(p => p.IdUsuarioNavigation);


            await HttpContext.InsertarParametrosPaginacionRespuesta(pedidos, paginacion.CantidadAMostrar);
            var pedidosPaginados = await  pedidos.Paginar(paginacion).ToListAsync();
            var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
            ViewData["Paginas"] = GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);

            return View(pedidosPaginados);
        }
        private List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual)
        {

            //A la variable paginas le asigna una lista de PaginasModel
            var paginas = new List<PaginasModel>();


            var paginaAnterior = (paginaActual > 1) ? paginaActual - 1 : 1;


            paginas.Add(new PaginasModel(paginaAnterior, paginaActual != 1, "Anterior"));


            for (int i = 1; i <= totalPaginas; i++)
            {

                paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
            }



            var paginaSiguiente = (paginaActual < totalPaginas) ? paginaActual + 1 : totalPaginas;


            paginas.Add(new PaginasModel(paginaSiguiente, paginaActual != totalPaginas, "Siguiente"));


            return paginas;
        }
     
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var model = new PedidosViewModel
            {
                NumeroPedido = GenerarNumeroPedido()
            };
            ViewData["Productos"] = new SelectList(_context.Productos, "Id", "NombreProducto");
            ViewBag.Productos = _context.Productos.ToList();
            ViewData["Clientes"] = new SelectList(_context.Usuarios, "Id", "NombreCompleto");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PedidosViewModel model)
        {
            // Primero, se verifica si el modelo es válido.
            if (ModelState.IsValid)
            {
                // Se crea un nuevo pedido con los datos proporcionados en el modelo.
                var pedido = new Pedido()
                {
                    NumeroPedido = GenerarNumeroPedido(),
                    FechaPedido = model.FechaPedido,
                    EstadoPedido = model.EstadoPedido,
                    IdUsuario = model.IdUsuario,
                };

                // Se agrega el nuevo pedido al contexto de la base de datos.
                _context.Add(pedido);

                // Se guarda el pedido en la base de datos de forma asíncrona.
                await _context.SaveChangesAsync();

                // Se recorre la lista de productos seleccionados.
                /*¿Si tu seleccionas 3 productos porque se ponen 4 y no afecta a la ejecucion del bucle?
                 El bucle for en tu acción Create recorre todos los productos porque está basado en 
                model.IdsProducto.Count, que es el número total de productos. Sin embargo, dentro del 
                bucle, solo se crea un DetallePedido para los productos que han sido seleccionados, 
                gracias a la condición if (model.ProductosSeleccionados[i]). Por lo tanto, aunque el 
                bucle recorre todos los productos, solo se procesan los productos seleccionados.
                 */
                /*En este bucle que tenemos el bucle se recorre tantas veces como productos existan en la
                 * tabla esto es por i < model.IdsProducto.Count; porque esto cuenta el total de productos 
                 * que hay. 
                 * Una vez que el bucle sabe cuantos productos hay de esos productos mira cual a sido seleccionado
                 * y cual no ha sido seleccionado con esto model.ProductosSeleccionados[i] la [i] es para indicar
                 * la posicion de donde esta el producto seleccionado.
                 Una vez se ahigan tomado todos los productos seleccionados ProductoId = model.IdsProducto[i] 
                se crea un detallePedido para ese pedido
                que este detalle pedido contiene los productos para ese pedido junto a las cantidades de
                cada producto Cantidad = model.Cantidades[i] y la [i] es para saber la posicion de cada producto y
                que cantidad le pertenece a cada prducto.
                 */
                for (var i = 0; i < model.IdsProducto.Count; i++)
                {
                    // Si el producto en la posición i fue seleccionado...
                    if (model.ProductosSeleccionados[i])
                    {
                        // Se crea un nuevo detalle de pedido para el producto seleccionado.
                        var detallePedido = new DetallePedido()
                        {
                            PedidoId = pedido.Id, // Se asocia el detalle del pedido con el pedido recién creado.
                            ProductoId = model.IdsProducto[i], // Se establece el ID del producto.
                            Cantidad = model.Cantidades[i], // Se establece la cantidad del producto.
                        };

                        // Se agrega el detalle del pedido al contexto de la base de datos.
                        _context.Add(detallePedido);
                    }
                }

                // Se guardan los detalles del pedido en la base de datos de forma asíncrona.
                await _context.SaveChangesAsync();

                // Se establecen las listas de productos y clientes para la vista.
                ViewData["Productos"] = new SelectList(_context.Productos, "Id", "NombreProducto");
                ViewBag.Productos = _context.Productos.ToList();
                ViewData["Clientes"] = new SelectList(_context.Usuarios, "Id", "NombreCompleto");

                // Se muestra un mensaje de éxito.
                TempData["SuccessMessage"] = "Los datos se han creado con éxito.";

                // Se redirige al usuario a la vista de índice.
                return RedirectToAction(nameof(Index));
            }

            // Si el modelo no es válido, se devuelve la vista con el modelo original.
            return View(model);
        }




        private string GenerarNumeroPedido()
        {
            var length = 10; 
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
           .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public async Task<IActionResult> Delete(int id)
        {
            //Consulta a base de datos
            var pedido = await _context.Pedidos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .Include(p => p.IdUsuarioNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            //Si no hay pedidos muestra el error 404
            if (pedido == null)
            {
                return NotFound("Pedido no encontrado");
            }

            //Llegados ha este punto hay pedidos por lo tanto se muestran los pedidos
            return View(pedido);
        }


        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            var pedido = await _context.Pedidos.Include(p => p.DetallePedidos).FirstOrDefaultAsync(m => m.Id == Id);
            if (pedido == null)
            {
                return BadRequest();
            }

            // Elimina los detalles del pedido
            _context.DetallePedidos.RemoveRange(pedido.DetallePedidos);

            // Elimina el pedido
            _context.Pedidos.Remove(pedido);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Edit(int id)
        {
            Pedido pedido = await _context.Pedidos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .Include(p => p.IdUsuarioNavigation)
                .FirstOrDefaultAsync(x => x.Id == id);

            PedidosViewModel pedidosViewModel = new PedidosViewModel
            {
                Id = pedido.Id,
                NumeroPedido = pedido.NumeroPedido,
               
                FechaPedido = pedido.FechaPedido,
                EstadoPedido = pedido.EstadoPedido,
                IdUsuario = pedido.IdUsuario
            };

            ViewData["Productos"] = new SelectList(_context.Productos, "Id", "NombreProducto");
            ViewBag.Productos = _context.Productos.ToList();

            ViewData["Clientes"] = new SelectList(_context.Usuarios, "Id", "NombreCompleto");

            return View(pedidosViewModel);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(Pedido pedido)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Actualiza el pedido
                    _context.Entry(pedido).State = EntityState.Modified;

                    // Actualiza los detalles del pedido
                    foreach (var detallePedido in pedido.DetallePedidos)
                    {
                        _context.Entry(detallePedido).State = EntityState.Modified;
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExist(pedido.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _context.Entry(pedido).Reload();

                        // Intenta guardar de nuevo
                        _context.Entry(pedido).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToAction("Index");
            }
            return View(pedido);
        }

        private bool PedidoExist(int Id)
        {

            return _context.Pedidos.Any(e => e.Id == Id);
        }
        //Mostrar en vista a parte los detalles de cada pedido
        public async Task<IActionResult> DetallesPedido(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .Include(p => p.IdUsuarioNavigation)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }


    }
}
