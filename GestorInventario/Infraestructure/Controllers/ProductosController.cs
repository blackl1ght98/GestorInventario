using GestorInventario.Application.DTOs.Email;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class ProductosController : Controller
    {
        
       
       
        private readonly ILogger<ProductosController> _logger;   
        private readonly IEmailService _emailService;
        private readonly IProductoRepository _productoRepository;            
        private readonly IPdfService _pdfService;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaginationHelper _paginationHelper;
        private readonly ICurrentUserAccessor _current;
        public ProductosController(IPolicyExecutor executor,IPaginationHelper pagination,ICurrentUserAccessor current,
        ILogger<ProductosController> logger, IEmailService emailService, IProductoRepository producto,IPdfService pdf)
        {
            _logger = logger;         
            _emailService = emailService;
            _productoRepository = producto;         
            _policyExecutor = executor;
            _pdfService = pdf;
            _paginationHelper = pagination;
            _current = current;
        }
        //Metodo para obtener los productos
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index(string buscar, string ordenarPorPrecio, int? idProveedor, [FromQuery] Paginacion paginacion)
        {
            try
            {
                

                // Obtiene la consulta como IQueryable
                var productos =  _policyExecutor.ExecutePolicy(() => _productoRepository.ObtenerTodosLosProductos());

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
                // 🔹 Usamos el helper directamente
                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationHelper.PaginarAsync(productos, paginacion)
                );

                // Obtener proveedores
                var proveedores = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerProveedores());

                // Verificar stock (manteniendo la lógica existente)
                await VerificarStock();

                // Crear el ViewModel
                var viewModel = new ProductsViewModel
                {
                    Productos = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
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
                    var productos = _policyExecutor.ExecutePolicy(() => _productoRepository.ObtenerTodosLosProductos());
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
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create()
        {
            try
            {
               
                ViewData["Productos"] = new SelectList(await _productoRepository.ObtenerProveedores(), "Id", "NombreProveedor");
                var proveedores = await _productoRepository.ObtenerProveedores();
                var model = new ProductosViewModel
                {
                    NombreProducto="",
                    Descripcion="",
                   Proveedores = proveedores.ToSelectList(
                   u => u.Id,
                   u => u.NombreProveedor,
                   placeholder: "Seleccione un proveedor..."
               )
                };

                return View(model);
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
        [Authorize(Roles = "Administrador")]
      
        public async Task<IActionResult> Create(ProductosViewModel model)
        {
            try
            {
               
                                
                if (!ModelState.IsValid)
                {
                    var proveedores = await _productoRepository.ObtenerProveedores();
                    model.Proveedores = proveedores.ToSelectList(
                       u => u.Id,
                       u => u.NombreProveedor,
                       placeholder: "Seleccione un usuario..."
                   );
                }
                var producto = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.CrearProducto(model));
                if (producto.Success)
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = producto.Message;
                    return View(model);
                }


            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al crear el producto");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo que obtiene la información necesaria para eliminar el producto
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
               
                
                var (producto,mensaje) = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerProductoPorId(id));    
                if (producto == null)
                {
                    _logger.LogError(mensaje);
                    return RedirectToAction(nameof(Index));
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
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
               
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {                  
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));
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
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {


                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex,"Error al eliminar el producto");
                return RedirectToAction("Error", "Home");
            }                    
        }
       
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
               

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

       
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ProductosViewModel model)
        {
            try
            {
                
                if (ModelState.IsValid)
                {
                    int usuarioId = _current.GetCurrentUserId();
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.EditarProducto(model, usuarioId));
                    if (success.Success)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = success.Message;
                    }
                    return RedirectToAction("Index");
                }
                return View(nameof(Edit));
            
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
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarAlCarrito(int idProducto, int cantidad)
        {
            try
            {
               
              
                int usuarioId=_current.GetCurrentUserId();
               
                    var success= await _policyExecutor.ExecutePolicyAsync(()=> _productoRepository.AgregarProductoAlCarrito(idProducto, cantidad, usuarioId)) ;
                    if (success.Success) 
                    {
                        return RedirectToAction("Index");

                    }
                    else
                    {
                        TempData["ErrorMessage"] = success.Message;
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



        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> HistorialProducto(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                
                var historialProductos =  _policyExecutor.ExecutePolicy(() => _productoRepository.ObtenerTodoHistorial());
                ViewData["Buscar"] = buscar;
                if (!String.IsNullOrEmpty(buscar))
                {
                    historialProductos = historialProductos.Where(p => p.Accion.Contains(buscar) || p.Ip.Contains(buscar));
                }
                // 🔹 Usamos el helper directamente
                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationHelper.PaginarAsync(historialProductos, paginacion)
                );
                var viewModel = new HistorialProductoViewModel
                {
                    Historial = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
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
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DescargarHistorialPDF()
        {
            try
            {
                
                var  pdfData = await _policyExecutor.ExecutePolicyAsync(() => _pdfService.DescargarProductoPDF());
                if (!pdfData.Success)
                {
                    TempData["ErrorMessage"] = pdfData.Message;
                   
                }
                return File(pdfData.Data, "application/pdf", "historial.pdf");
            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al descargar el pdf");
                return RedirectToAction("Error", "Home");
            }

        }
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DetallesHistorialProducto(int id)
        {
            try
            {
              

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
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteHistorial(int id)
        {
            try
            {
                
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
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedHistorial(int Id)
        {
            try
            {
               
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.EliminarHistorialPorId(Id));
                    if (success.Success)
                    {

                        return RedirectToAction(nameof(HistorialProducto));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = success.Message;
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
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllHistorial()
        {
            try
            {
               

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
