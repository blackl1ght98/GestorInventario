using Aspose.Pdf;
using Aspose.Pdf.Operators;
using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
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
       
        private readonly GenerarPaginas _generarPaginas;
        private readonly ILogger<PedidosController> _logger;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly PolicyHandler _PolicyHandler;
        public PedidosController( GenerarPaginas generarPaginas, ILogger<PedidosController> logger, 
            IPedidoRepository pedido, IHttpContextAccessor contextAccessor, PolicyHandler policy)
        {
            _PolicyHandler = policy;
            _generarPaginas = generarPaginas;
            _logger = logger;
            _pedidoRepository = pedido;
            _contextAccessor = contextAccessor;
        }

        public async Task<IActionResult> Index(string buscar, DateTime? fechaInicio, DateTime? fechaFin, [FromQuery] Paginacion paginacion)
        {
            try
            {
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    //IQueryable<Pedido> pedidos;
                    var pedidos = ExecutePolicy(()=> _pedidoRepository.ObtenerPedidos()) ;
                    if (User.IsInRole("administrador"))
                    {
                       
                        pedidos = ExecutePolicy(() => _pedidoRepository.ObtenerPedidos());
                    }
                    else
                    {
                        
                        pedidos = ExecutePolicy(() => _pedidoRepository.ObtenerPedidoUsuario(usuarioId));
                    }
                    ViewData["Buscar"] = buscar;
                    ViewData["FechaInicio"] = fechaInicio;
                    ViewData["FechaFin"] = fechaFin;

                    // Aquí es donde se realiza la búsqueda por el número de pedido
                    if (!String.IsNullOrEmpty(buscar))
                    {
                        pedidos = pedidos.Where(p => p.NumeroPedido.Contains(buscar) || p.EstadoPedido.Contains(buscar) || p.IdUsuarioNavigation.NombreCompleto.Contains(buscar));
                    }
                    if (fechaInicio.HasValue && fechaFin.HasValue)
                    {
                        pedidos = pedidos.Where(s => s.FechaPedido >= fechaInicio.Value && s.FechaPedido <= fechaFin.Value);
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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var model = new PedidosViewModel
                {
                    NumeroPedido = GenerarNumeroPedido(),
                    FechaPedido = DateTime.Now
                };
                //Obtenemos los datos para generar los desplegables
                ViewData["Productos"] = new SelectList(await ExecutePolicyAsync(()=> _pedidoRepository.ObtenerProductos()) , "Id", "NombreProducto");
               // ViewBag.Productos = _context.Productos.ToList();
                ViewBag.Productos = await _pedidoRepository.ObtenerProductos();
                ViewData["Clientes"] = new SelectList(await ExecutePolicyAsync(()=> _pedidoRepository.ObtenerUsuarios()) , "Id", "NombreCompleto");

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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                if (ModelState.IsValid)
                {
                   
                    var (success, errorMessage) = await ExecutePolicyAsync(()=> _pedidoRepository.CrearPedido(model)) ;
                    if (success)
                    {
                        // Se establecen las listas de productos y clientes para la vista.
                        ViewData["Productos"] = new SelectList(await ExecutePolicyAsync(()=> _pedidoRepository.ObtenerProductos()) , "Id", "NombreProducto");
                        ViewBag.Productos = await ExecutePolicyAsync(() => _pedidoRepository.ObtenerProductos());
                        ViewData["Clientes"] = new SelectList(await ExecutePolicyAsync(()=> _pedidoRepository.ObtenerUsuarios()) , "Id", "NombreCompleto");
                        // Se muestra un mensaje de éxito.
                        TempData["SuccessMessage"] = "Los datos se han creado con éxito.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error Message"] = errorMessage;
                    }
                 
                }
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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                //Consulta a base de datos
             
                var pedido = await ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoEliminacion(id));
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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    
                    var (success, errorMessage) = await ExecutePolicyAsync(()=> _pedidoRepository.EliminarPedido(Id)) ;
                    if (success)
                    {
                       
                        TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                      
                        return RedirectToAction(nameof(Delete), new { id = Id });

                    }

                }
                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el pedido");
                return BadRequest("Error al eliminar el pedido, intentelo de nuevo mas tarde o contacte con el administrador");
            }

        }
        public async Task<IActionResult> DeleteHistorial(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var historialProducto = await ExecutePolicyAsync(()=> _pedidoRepository.EliminarHistorialPorId(id));
                if (historialProducto == null)
                {

                    TempData["ErrorMessage"] = "Historial no encontrado";
                    return NotFound("Historial no encontrado");
                }
                return View(historialProducto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el producto");
                return BadRequest("Error al mostrar la vista de eliminacion intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }

        }
        [HttpPost, ActionName("DeleteConfirmedHistorial")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedHistorial(int Id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var (success, errorMessage) = await ExecutePolicyAsync(()=> _pedidoRepository.EliminarHistorialPorIdDefinitivo(Id));
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Los datos se han eliminado con exito";
                        return RedirectToAction(nameof(HistorialPedidos));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                        return RedirectToAction(nameof(Delete), new { id = Id });
                    }

                }
                return BadRequest("Error al eliminar el producto intentelo de nuevo mas tarde si el problema persiste intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el producto");
                return BadRequest("Error al eliminar el producto intentelo de nuevo mas tarde si el problema persiste intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
        }
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                //var pedido = await _context.Pedidos
                //.FirstOrDefaultAsync(x => x.Id == id);
                var pedido = await ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoId(id)) ;
                EditPedidoViewModel pedidosViewModel = new EditPedidoViewModel
                {
                    fechaPedido = DateTime.Now,
                    estadoPedido = pedido.EstadoPedido,

                };
                return View();
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
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    
                    var (success, errorMessage) = await ExecutePolicyAsync(()=> _pedidoRepository.EditarPedido(model)) ;
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                    }

                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Error de concurrencia");
                  
                    var (success, errorMessage) = await ExecutePolicyAsync(()=> _pedidoRepository.EditarPedido(model)) ;
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                    }
                
                
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al editar el pedido");
                    return BadRequest("Error al editar el pedido, intentelo de nuevo mas tarde o contacte con el administrador");
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }

      
        //Mostrar en vista a parte los detalles de cada pedido
        public async Task<IActionResult> DetallesPedido(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
               // var pedido = await _context.Pedidos
               //.Include(p => p.DetallePedidos)
               //    .ThenInclude(dp => dp.Producto)
               //.Include(p => p.IdUsuarioNavigation)
               //.FirstOrDefaultAsync(p => p.Id == id);
               var pedido= await ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoEliminacion(id)) ;
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
        public async Task<IActionResult> HistorialPedidos(string buscar,[FromQuery] Paginacion paginacion)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }
            var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId;
            if (int.TryParse(existeUsuario, out usuarioId))
            {
                
                var pedidos = ExecutePolicy(()=> _pedidoRepository.ObtenerPedidosHistorial()) ;
                if (User.IsInRole("administrador"))
                {
                    
                    pedidos = ExecutePolicy(() => _pedidoRepository.ObtenerPedidosHistorial());
                }
                else
                {
                    //pedidos = _context.HistorialPedidos.Where(p => p.IdUsuario == usuarioId)
                    //    .Include(dp => dp.DetalleHistorialPedidos).ThenInclude(p => p.Producto)
                    //    .Include(u => u.IdUsuarioNavigation);
                    pedidos= ExecutePolicy(() => _pedidoRepository.ObtenerPedidosHistorialUsuario(usuarioId));
                }
                // Aquí es donde se realiza la búsqueda por el número de pedido
                
                
                ViewData["Buscar"] = buscar;
                await HttpContext.InsertarParametrosPaginacionRespuesta(pedidos, paginacion.CantidadAMostrar);
                var pedidosPaginados = await pedidos.Paginar(paginacion).ToListAsync();
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);
                return View(pedidosPaginados);
            }
            return Unauthorized("Es necesario loguearse para ver el historial de pedidos");


        }
        public async Task<IActionResult> DetallesHistorialPedido(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                // var pedido = await _context.HistorialPedidos
                //.Include(p => p.DetalleHistorialPedidos)
                //    .ThenInclude(dp => dp.Producto)
                //.Include(p => p.IdUsuarioNavigation)
                //.FirstOrDefaultAsync(p => p.Id == id);
                var pedido = await ExecutePolicyAsync(()=> _pedidoRepository.DetallesHistorial(id)) ;
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
        [HttpGet("descargarhistorialpedidoPDF")]
        public async Task<IActionResult> DescargarHistorialPDF()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }
           
            var (success, errorMessage, bytes) = await ExecutePolicyAsync(()=> _pedidoRepository.DescargarPDF()) ;
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction(nameof(HistorialPedidos));
            }
            return File(bytes, "application/pdf", "historial.pdf");


        }
        [HttpPost, ActionName("DeleteAllHistorial")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllHistorial()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
               
                var (success, errorMessage) = await ExecutePolicyAsync(() => _pedidoRepository.EliminarHitorial());
                if (!success)
                {
                    TempData["ErrorMessage"] = errorMessage;
                    return RedirectToAction(nameof(HistorialPedidos));
                }
               

                TempData["SuccessMessage"] = "Todos los datos del historial se han eliminado con éxito.";
                return RedirectToAction(nameof(HistorialPedidos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar los datos del historial");
                return BadRequest("Error al eliminar los datos del historial, inténtelo de nuevo más tarde. Si el problema persiste, contacte con el administrador");
            }
        }
        private async Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicyAsync<T>();
            return await policy.ExecuteAsync(operation);
        }
        private T ExecutePolicy<T>(Func<T> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicy<T>();
            return policy.Execute(operation);
        }
    }
}
