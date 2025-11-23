using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.order;
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
        private readonly IPdfService _pdfservice;
        private readonly PolicyExecutor _policyExecutor;
        private readonly IPaypalService _paypalService;
        private readonly GestorInventarioContext _context;
        public PedidosController( GenerarPaginas generarPaginas, ILogger<PedidosController> logger, 
            IPedidoRepository pedido, IHttpContextAccessor contextAccessor,  IPdfService pdf, PolicyExecutor executor, IPaypalService paypal, GestorInventarioContext context)
        {
      
            _generarPaginas = generarPaginas;
            _logger = logger;
            _pedidoRepository = pedido;
            _contextAccessor = contextAccessor;
            _pdfservice= pdf;   
            _policyExecutor = executor;
            _paypalService = paypal;
            _context = context;
        }
       
     
        public async Task<IActionResult> Index(string buscar, DateTime? fechaInicio, DateTime? fechaFin, [FromQuery] Paginacion paginacion)
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

                    var pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerPedidos());
                    if (User.IsInRole("administrador"))
                    {

                        pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerPedidos());
                    }
                    else
                    {

                        pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerPedidoUsuario(usuarioId));
                    }
                    ViewData["Buscar"] = buscar;
                    ViewData["FechaInicio"] = fechaInicio;
                    ViewData["FechaFin"] = fechaFin;
                    if (!String.IsNullOrEmpty(buscar))
                    {
                        pedidos = pedidos.Where(p => p.NumeroPedido.Contains(buscar) || p.EstadoPedido.Contains(buscar) || p.IdUsuarioNavigation.NombreCompleto.Contains(buscar));
                    }
                    if (fechaInicio.HasValue && fechaFin.HasValue)
                    {
                        pedidos = pedidos.Where(s => s.FechaPedido >= fechaInicio.Value && s.FechaPedido <= fechaFin.Value);
                    }
                    var (orders, totalItems) = await _policyExecutor.ExecutePolicyAsync(() => pedidos.PaginarAsync(paginacion));
                    var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);
                    var paginas = _generarPaginas.GenerarListaPaginas(totalPaginas, paginacion.Pagina, paginacion.Radio);
                    var viewModel = new PedidoViewModel
                    {
                        Pedidos = orders,
                        Paginas = paginas,
                        TotalPaginas = totalPaginas,
                        PaginaActual = paginacion.Pagina,
                        Buscar = buscar
                    };

                 
                    return View(viewModel);
                }
                return Unauthorized("No tienes permiso para ver el contenido o no te has logueado");
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener los pedidos");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo que muestra la informacion necesaria para crear el pedido
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
                ViewData["Productos"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerProductos()) , "Id", "NombreProducto");
             
                ViewBag.Productos = await _pedidoRepository.ObtenerProductos();
                ViewData["Clientes"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerUsuarios()) , "Id", "NombreCompleto");

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar la vista de creacion del pedido");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo que crea el pedido
        [HttpPost]
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
                   
                    var success = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.CrearPedido(model)) ;
                    if (success.Success)
                    {
                        // Se establecen las listas de productos y clientes para la vista.
                        ViewData["Productos"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerProductos()) , "Id", "NombreProducto");
                        ViewBag.Productos = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.ObtenerProductos());
                        ViewData["Clientes"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerUsuarios()) , "Id", "NombreCompleto");
                     
                        TempData["SuccessMessage"] = "Los datos se han creado con éxito.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = success.Message;
                    }
                 
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al crear el pedido");
                return RedirectToAction("Error", "Home");
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
        //Metodo que obtiene la informacion necesaria para eliminar un pedido
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }                           
                var pedido = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoEliminacion(id));
            
                if (pedido == null)
                {
                    TempData["ErrorMessage"] = "Pedido no encontrado";
                }               
                return View(pedido);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar la vista de eliminacion del pedido");
                return RedirectToAction("Error", "Home");
            }

        }

        //Metodo que elimina un pedido
        [HttpPost, ActionName("DeleteConfirmed")]
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
                    
                    var success = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.EliminarPedido(Id)) ;
                    if (success.Success)
                    {
                       
                        
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = success.Message;
                      
                        return RedirectToAction(nameof(Delete), new { id = Id });

                    }

                }
                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar el pedido");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo que obtiene los datos necesarios para eliminar el historial
        public async Task<IActionResult> DeleteHistorial(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var historialProducto = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.EliminarHistorialPorId(id));
                if (historialProducto == null)
                {

                    TempData["ErrorMessage"] = "Historial no encontrado";
                   
                }
                return View(historialProducto);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar el producto");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo que elimina el historial
        [HttpPost, ActionName("DeleteConfirmedHistorial")]
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
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.EliminarHistorialPorIdDefinitivo(Id));
                    if (success.Success)
                    {

                        return RedirectToAction(nameof(HistorialPedidos));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = success.Message;
                        return RedirectToAction(nameof(Delete), new { id = Id });
                    }

                }
                return BadRequest("Error al eliminar el producto intentelo de nuevo mas tarde si el problema persiste intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");

            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar el producto");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo que obtiene los datos para editar un pedido
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }                        
                var pedido = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoId(id)) ;
                EditPedidoViewModel pedidosViewModel = new EditPedidoViewModel
                {
                    FechaPedido = pedido.FechaPedido,
                    EstadoPedido = pedido.EstadoPedido,

                };
                return View(pedidosViewModel);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar la vista de  editar el pedido");
                return RedirectToAction("Error", "Home");
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
                    
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.EditarPedido(model));
                    if (success.Success)
                    {
                        TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = success.Message;
                    }

                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Error de concurrencia");
                  
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.EditarPedido(model));
                    if (success.Success)
                    {
                        TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = success.Message;
                    }
                
                
                }
                catch (Exception ex)
                {
                    TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                    _logger.LogError(ex, "Error al editar el pedido");
                    return RedirectToAction("Error", "Home");
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }   
        //Metodo para obtener los detalles del pedido
        public async Task<IActionResult> DetallesPedido(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
            
               var pedido= await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerDetallesPedido(id)) ;
                if (pedido == null)
                {
                    return NotFound();
                }

                return View(pedido);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener los detalles del pedido");
                return RedirectToAction("Error", "Home");
            }

        }
       
        //Metodo para obtener el historial del pedido
        public async Task<IActionResult> HistorialPedidos(string buscar, [FromQuery] Paginacion paginacion)
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

                    var pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerPedidosHistorial());
                    if (User.IsInRole("administrador"))
                    {

                        pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerPedidosHistorial());
                    }
                    else
                    {

                        pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerPedidosHistorialUsuario(usuarioId));
                    }
                    ViewData["Buscar"] = buscar;
                    var (orders, totalItems) = await _policyExecutor.ExecutePolicyAsync(() => pedidos.PaginarAsync(paginacion));
                    var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);
                    var paginas = _generarPaginas.GenerarListaPaginas(totalPaginas, paginacion.Pagina, paginacion.Radio);
                    var viewModel = new HistorialPedidoViewModel
                    {
                        HistorialPedidos = orders,
                        Paginas = paginas,
                        TotalPaginas = totalPaginas,
                        PaginaActual = paginacion.Pagina,
                        Buscar = buscar
                    };


                    return View(viewModel);
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener el historial de pedidos");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo para obtener los detalles del historial del pedido
        public async Task<IActionResult> DetallesHistorialPedido(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var pedido = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.DetallesHistorial(id)) ;
                if (pedido == null)
                {
                    TempData["ErrorMessage"] = "detalle del historial del pedido no encontrado";
                }

                return View(pedido);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener los detalles del pedido");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo para descargar en pdf el historial
        [HttpGet("descargarhistorialpedidoPDF")]
        public async Task<IActionResult> DescargarHistorialPDF()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var  bytes = await _policyExecutor.ExecutePolicyAsync(() => _pdfservice.GenerarPDF());
                if (!bytes.Success)
                {
                    TempData["ErrorMessage"] = bytes.Message;
                    return RedirectToAction(nameof(HistorialPedidos));
                }
                return File(bytes.Data, "application/pdf", "historial.pdf");
            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al descargar el pdf");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo para eliminar el historial
        [HttpPost, ActionName("DeleteAllHistorial")]
        public async Task<IActionResult> DeleteAllHistorial()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.EliminarHitorial());
                if (!success.Success)
                {
                    TempData["ErrorMessage"] = success.Message;
                    return RedirectToAction(nameof(HistorialPedidos));
                }
                return RedirectToAction(nameof(HistorialPedidos));
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar los datos del historial");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo para obtener los detalles del pago
        public async Task<IActionResult> DetallesPagoEjecutado(string id)
        {
            var detallepago= await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerDetallePagoEjecutadoV2(id));
            if (detallepago.Success)
            {
                return View(detallepago.Data);
            }
            else
            {
                TempData["ErrorMessage"]=detallepago.Message;
                return RedirectToAction("Error", "Home");
            }
        }
       
      
        [HttpPost]
        public async Task<IActionResult> AgregarInfoEnvio(int pedidoId, Carrier carrier, BarcodeType barcode)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            await _paypalService.SeguimientoPedido(pedido.Id,carrier, barcode);
            return RedirectToAction(nameof(Index), new {message="Info Agregada con exito"});
        }
       
    }
}
