using GestorInventario.Application.DTOs.Email;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class ProductosController : Controller
    {
        
       
        private readonly PaginacionMetodo _paginarMetodo;
        private readonly ILogger<ProductosController> _logger;   
        private readonly IEmailService _emailService;
        private readonly IProductoRepository _productoRepository;            
        private readonly IPdfService _pdfService;
        private readonly PolicyExecutor _policyExecutor;
        public ProductosController(  PaginacionMetodo paginacionMetodo,  PolicyExecutor executor,
        ILogger<ProductosController> logger, IEmailService emailService, IProductoRepository producto,   IPdfService pdf
       )
        {
                   
            _paginarMetodo = paginacionMetodo;
            _logger = logger;         
            _emailService = emailService;
            _productoRepository = producto;         
            _policyExecutor = executor;
            _pdfService = pdf;
        }
        //Metodo para obtener los productos
        [HttpGet]
        public async Task<IActionResult> Index(string buscar, string ordenarPorPrecio, int? idProveedor, [FromQuery] Paginacion paginacion)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Obtiene la consulta como IQueryable
                var productos =  _policyExecutor.ExecutePolicy(() => _productoRepository.ObtenerTodoProducto());

                // Aplicamos los filtros a la consulta
                if (!string.IsNullOrEmpty(buscar))
                {
                    productos = productos.Where(s => s.NombreProducto.Contains(buscar));
                }

                if (!string.IsNullOrEmpty(ordenarPorPrecio))
                {
                    productos = ordenarPorPrecio switch
                    {
                        "asc" => productos.OrderBy(p => p.Precio),
                        "desc" => productos.OrderByDescending(p => p.Precio),
                        _ => productos
                    };
                }

                if (idProveedor.HasValue)
                {
                    productos = productos.Where(p => p.IdProveedor == idProveedor.Value);
                }

                // Aplicar paginación
                var (productosPaginados, totalItems) = await _policyExecutor.ExecutePolicyAsync(() => productos.PaginarAsync(paginacion));
                var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);
                var paginas = _paginarMetodo.GenerarListaPaginas(totalPaginas, paginacion.Pagina, paginacion.Radio);

                // Obtener proveedores
                var proveedores = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerProveedores());

                // Verificar stock (manteniendo la lógica existente)
                await VerificarStock();

                // Crear el ViewModel
                var viewModel = new ProductsViewModel
                {
                    Productos = productosPaginados,
                    Paginas = paginas,
                    TotalPaginas = totalPaginas,
                    PaginaActual = paginacion.Pagina,
                    Buscar = buscar,
                    OrdenarPorPrecio = ordenarPorPrecio,
                    IdProveedor = idProveedor,
                    Proveedores = new SelectList(proveedores, "Id", "NombreProveedor")
                };
                ViewData["Proveedores"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerProveedores()), "Id", "NombreProveedor");

                return View(viewModel);
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex, "Error al mostrar la vista");
                return RedirectToAction("Error", "Home");
            }
        }

        //Metodo para verificar el stock de un producto
        public async Task VerificarStock()
        {
            try
            {
                var emailUsuario = User.FindFirstValue(ClaimTypes.Email);

                if (emailUsuario != null)
                {
                    var productos = _policyExecutor.ExecutePolicy(() => _productoRepository.ObtenerTodoProducto());
                    foreach (var producto in productos)
                    {
                        if (producto.Cantidad < 10) 
                        {
                            await _emailService.SendEmailAsyncLowStock(new EmailDto
                            {
                                ToEmail = emailUsuario
                            }, producto);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex,"Error al verificar el stock");
            }
           
        }
        //Metodo que obtiene la informacion necesaria para crear un pedido
        public async Task<IActionResult> Create()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                ViewData["Productos"] = new SelectList(await _productoRepository.ObtenerProveedores(), "Id", "NombreProveedor");

                return View();
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar la vista de creacion del producto");
                return RedirectToAction("Error", "Home");
            }

        }
       //Metodo que crea el producto
        [HttpPost]      
        public async Task<IActionResult> Create(ProductosViewModel model)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                                
                    if (ModelState.IsValid)
                    {
                        var producto = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.CrearProducto(model));
                        ViewData["Productos"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(()=> _productoRepository.ObtenerProveedores()) , "Id", "NombreProveedor");                                          
                       return RedirectToAction(nameof(Index));
                    }
                    return View(model);
                
                
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al crear el producto");
                return RedirectToAction("Error", "Home");
            }
        }   
        //Metodo que obtiene la información necesaria para eliminar el producto
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                
                var (producto,mensaje) = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerProductoPorId(id));    
                if (producto == null)
                {
                    TempData["ErrorMessage"] = mensaje;
                }             
                return View(producto);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar el producto");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo que elimina el producto
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
                    var (success, errorMessage) = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));
                    if (success)
                    {
                       
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                        return RedirectToAction(nameof(Delete), new { id = Id });
                    }
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {


                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex,"Error al eliminar el producto");
                return RedirectToAction("Error", "Home");
            }                    
        }
        //Metodo que obtiene la información necesaria para editar el producto
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var (producto,mensaje) = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerProductoPorId(id));
                if (producto == null)
                {
                    TempData["ErrorMessage"] = mensaje;
                }
                
                ViewData["Productos"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerProveedores()), "Id", "NombreProveedor");
                ProductosViewModel viewModel = new ProductosViewModel()
                {
                    Id = producto.Id,
                    NombreProducto = producto.NombreProducto,
                    Descripcion = producto.Descripcion,
                    Cantidad = producto.Cantidad,
                    Imagen = producto.Imagen,
                    Precio = producto.Precio,
                    IdProveedor = producto.IdProveedor
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al editar el producto");
                return RedirectToAction("Error", "Home");
            }

        }

        //Metodo encargado de editar el producto
        [HttpPost]
        public async Task<ActionResult> Edit(ProductosViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                  
                        if (!User.Identity.IsAuthenticated)
                        {
                            return RedirectToAction("Login", "Auth");
                        }
                        var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        int usuarioId;
                        if (int.TryParse(existeUsuario, out usuarioId))
                        {

                            var (success,errorMesage)= await _policyExecutor.ExecutePolicyAsync(()=> _productoRepository.EditarProducto(model, usuarioId))  ;
                            if (success)
                            {
                                return RedirectToAction(nameof(Index));
                            }
                            else
                            {
                                TempData["ErrorMessage"] = errorMesage;
                            }

                           
                        }                                       
                    return RedirectToAction("Index");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al editar el producto");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo que agrega el producto al carrito
        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito(int idProducto, int cantidad)
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
                    var (success,errorMessage)= await _policyExecutor.ExecutePolicyAsync(()=> _productoRepository.AgregarProductoAlCarrito(idProducto, cantidad, usuarioId)) ;
                    if (success) 
                    {
                        return RedirectToAction("Index");

                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                    }
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al agregar el producto al carrito");
                return RedirectToAction("Error", "Home");
            }
        }


   
        //Metodo para obtener el historial del producto
        public async Task<IActionResult> HistorialProducto(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var historialProductos =  _policyExecutor.ExecutePolicy(() => _productoRepository.ObtenerTodoHistorial());
                ViewData["Buscar"] = buscar;
                if (!String.IsNullOrEmpty(buscar))
                {
                    historialProductos = historialProductos.Where(p => p.Accion.Contains(buscar) || p.Ip.Contains(buscar));
                }
                var (historialPaginado, totalItems) = await _policyExecutor.ExecutePolicyAsync(() => historialProductos.PaginarAsync(paginacion));
                var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);
                var paginas = _paginarMetodo.GenerarListaPaginas(totalPaginas, paginacion.Pagina, paginacion.Radio);
                var viewModel = new HistorialProductoViewModel
                {
                    Historial = historialPaginado,
                    Paginas = paginas,
                    TotalPaginas = totalPaginas,
                    PaginaActual = paginacion.Pagina,
                  
                };
             
                return View(viewModel);
            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener el historial de producto");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo para descargar el historial en pdf
        [HttpGet("descargarhistorialPDF")]
        public async Task<IActionResult> DescargarHistorialPDF()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var (success, errorMessage, pdfData) = await _policyExecutor.ExecutePolicyAsync(() => _pdfService.DescargarProductoPDF());
                if (!success)
                {
                    TempData["ErrorMessage"] = errorMessage;
                    return BadRequest(errorMessage);
                }
                return File(pdfData, "application/pdf", "historial.pdf");
            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al descargar el pdf");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo que muestra los detalles del historial del producto
        public async Task<IActionResult> DetallesHistorialProducto(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var historialProducto = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerHistorialProductoPorId(id));
                if (historialProducto == null)
                {
                    TempData["ErrorMessage"] = "Detalles del historial no encontrado";
                }

                return View(historialProducto);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener los detalles del historial de productos");
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
                var historialProducto = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerHistorialProductoPorId(id));
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
                    var (success, errorMessage) = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.EliminarHistorialPorId(Id));
                    if (success)
                    {

                        return RedirectToAction(nameof(HistorialProducto));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                        return RedirectToAction(nameof(Delete), new { id = Id });
                    }

                }
                return RedirectToAction("Index");

            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar el producto");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo que elimina todo el historial
        [HttpPost, ActionName("DeleteAllHistorial")]
        public async Task<IActionResult> DeleteAllHistorial()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var historialProductos = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.EliminarTodoHistorial());
                //var historialProductos = await _productoRepository.EliminarTodoHistorial();
                if (historialProductos == null || historialProductos.Count == 0)
                {
                    TempData["ErrorMessage"] = "No hay datos en el historial para eliminar";
                    return BadRequest("No hay datos en el historial para eliminar");
                }

                return RedirectToAction(nameof(HistorialProducto));
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar los datos del historial");
                return RedirectToAction("Error", "Home");
            }
        }


    }
}
