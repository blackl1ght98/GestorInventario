using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    [Authorize]
    public class PedidosController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly GenerarPaginas _generarPaginas;
        private readonly ILogger<PedidosController> _logger;

        public PedidosController(GestorInventarioContext context, GenerarPaginas generarPaginas, ILogger<PedidosController> logger)
        {
            _context = context;
            _generarPaginas = generarPaginas;
            _logger = logger;
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
        //public async Task<IActionResult> Index([FromQuery] Paginacion paginacion)
        //{
        //    var pedidos = _context.Pedidos
        //        .Include(p => p.DetallePedidos)
        //            .ThenInclude(dp => dp.Producto)
        //        .Include(p => p.IdUsuarioNavigation);


        //    await HttpContext.InsertarParametrosPaginacionRespuesta(pedidos, paginacion.CantidadAMostrar);
        //    var pedidosPaginados = await  pedidos.Paginar(paginacion).ToListAsync();
        //    var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
        //    ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);

        //    return View(pedidosPaginados);
        //}
        public async Task<IActionResult> Index([FromQuery] Paginacion paginacion)
        {
            try
            {
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    IQueryable<Pedido> pedidos;
                    if (User.IsInRole("administrador"))
                    {
                        pedidos = _context.Pedidos.Include(dp => dp.DetallePedidos)
                            .ThenInclude(p => p.Producto)
                            .Include(u => u.IdUsuarioNavigation);
                    }
                    else
                    {
                        pedidos = _context.Pedidos.Where(p => p.IdUsuario == usuarioId)
                            .Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto)
                            .Include(u => u.IdUsuarioNavigation);
                    }
                    await HttpContext.InsertarParametrosPaginacionRespuesta(pedidos, paginacion.CantidadAMostrar);
                    var pedidosPaginados = await pedidos.Paginar(paginacion).ToListAsync();
                    var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                    ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);
                    return View(pedidosPaginados);
                }
                return Unauthorized("No tienes permiso para ver el contenido o no te has logueado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los pedidos");
                return BadRequest("Error al obtener los pedidos intentelo de nuevo mas tarde o si el problema persiste contacte con el administrador");
            }
            
            
        }
       
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new PedidosViewModel
                {
                    NumeroPedido = GenerarNumeroPedido()
                };
                //Obtenemos los datos para generar los desplegables
                ViewData["Productos"] = new SelectList(_context.Productos, "Id", "NombreProducto");
                ViewBag.Productos = _context.Productos.ToList();
                ViewData["Clientes"] = new SelectList(_context.Usuarios, "Id", "NombreCompleto");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar la vista de creacion del pedido");
                return BadRequest("Error al mostrar la vista de creacion del pedido intentelo de nuevo mas tarde o contacte con el administrador");
            }
            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PedidosViewModel model)
        {
            try
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

                    /*El bucle for funciona de la siguiene manera:
                     * primero inicializa una variable i en 0
                     * depues compara si la variable i es menor que la lista de ids de productos que hay en base
                     * de datos si es asi el bucle se recorre tantas veces hasta que 0 se iguale a la cantidad de productos
                     * que hay en base de datos por ejemplo en base de datos tenemos 8 productos pues 
                     * i < model.IdsProducto.Count; se ira iterando hasta que i llegue a 8.
                     * Dentro de este bule tenemos una condicion pero ha esta condicion se le pasa la posicion del producto
                     * que el usuario ha seleccionado a la condicion model.ProductosSeleccionados[i] llegara solo los
                     * productos que el usuario halla seleccionado por ejemplo de esos 8 productos el usuario ha seleccionado 4
                     * pues ha la codicion llegan 4 y de esos 4 se creara un detalle pedido para cada producto en otras palabras
                     * sus caracteristicas precio, cantidad... como hemos dicho no lo llega los 4 productos esos 4 productos vienen con
                     * la posicion de cada uno en la lista la posicion comienza en 0 asi que si son 4 productos la posicio se ve 
                     * algo asi 0,1,2,3. A productos seleccionados llega una lista de booleanos junto a la posicion se puede ver algo asi
                     * [true,false,true,false,true,false,true,false] aqui llegarian todos los productos pero solo tiene en cuenta los que son
                     * true 
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
                    /*El operador nameof en C# se utiliza para obtener el nombre de una variable, tipo o 
                     * miembro como una cadena constante en tiempo de compilación. En tu caso, nameof(Index)
                     * devuelve la cadena "Index".

                     La ventaja de usar nameof en lugar de una cadena literal es que ayuda a mantener tu 
                    código seguro contra errores tipográficos y refactorizaciones. Si cambias el nombre del 
                    método Index en tu controlador, el compilador te advertirá que nameof(Index) ya no es 
                    válido. Sin embargo, si hubieras usado una cadena literal como "Index", el compilador 
                    no habría detectado el problema y podrías haber terminado con un error en tiempo de 
                    ejecución.
                     */
                    return RedirectToAction(nameof(Index));
                }

                // Si el modelo no es válido, se devuelve la vista con el modelo original.
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el pedido");
                return BadRequest("Error al crear el pedido intentelo de nuevo mas tarde o contacte con el administrador si el problema persiste");
            }
           
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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar la vista de eliminacion del pedido");
                return BadRequest("Error al mostrar la vista de eliminacion del pedido, intentelo de nuevo mas tarde o contacte con el administrador ");
            }
           
        }


        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el pedido");
                return BadRequest("Error al eliminar el pedido, intentelo de nuevo mas tarde o contacte con el administrador");
            }
           
        }

        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(x => x.Id == id);

                EditPedidoViewModel pedidosViewModel = new EditPedidoViewModel
                {
                    fechaPedido = pedido.FechaPedido,
                    estadoPedido = pedido.EstadoPedido,

                };
                return View(pedidosViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar la vista de  editar el pedido");
                return BadRequest("Error al mostrar la vista de edicion del pedido intentelo de nuevo mas tarde o contacte con el administrador");
            }
            
        }

        [HttpPost]
        public async Task<ActionResult> Edit(EditPedidoViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var pedido = await _context.Pedidos
                    .FirstOrDefaultAsync(x => x.Id == model.id);
                    pedido.FechaPedido=model.fechaPedido;
                    pedido.EstadoPedido=model.estadoPedido;
                    _context.Pedidos.Update(pedido);


                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Error de concurrencia");
                    if (!PedidoExist(model.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                       
                        _context.Entry(model).Reload();
                        var pedido = await _context.Pedidos
                  .FirstOrDefaultAsync(x => x.Id == model.id);
                        // Intenta guardar de nuevo
                        model.fechaPedido = model.fechaPedido;
                        model.estadoPedido = model.estadoPedido;
                        _context.Pedidos.Update(pedido);
                        await _context.SaveChangesAsync();
                        
                    }
                }catch(Exception ex)
                {
                    _logger.LogError(ex, "Error al editar el pedido");
                    return BadRequest("Error al editar el pedido, intentelo de nuevo mas tarde o contacte con el administrador");
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }

        private bool PedidoExist(int Id)
        {
            try
            {
                return _context.Pedidos.Any(e => e.Id == Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el pedido");
                return false;
            }
            
        }
        //Mostrar en vista a parte los detalles de cada pedido
        public async Task<IActionResult> DetallesPedido(int id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los detalles del pedido");
               return BadRequest("Error al obtener los detalles del pedido intentelo de nuevo mas tarde o contacte con el administrador");
            }
           
        }


    }
}
