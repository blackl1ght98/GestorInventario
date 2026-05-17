using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.Productos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers.ProductosController
{
    public class ProductosController : Controller
    {
             
        private readonly ILogger<ProductosController> _logger;       
        private readonly IProductoRepository _productoRepository;                    
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaginationHelper _paginationHelper;
        private readonly ICurrentUserAccessor _current;
        private readonly IProductManagementService _productoService;
        private readonly ICarritoService _carritoService;
        public ProductosController(
            IPolicyExecutor policyExecutor,
            IPaginationHelper pagination,
            ICurrentUserAccessor currentUserAccessor, 
            ICarritoService carritoService,
            ILogger<ProductosController> logger, 
            IProductoRepository productoRepository,    
            IProductManagementService productoService)
        {
            _logger = logger;                  
            _productoRepository = productoRepository;         
            _policyExecutor = policyExecutor;      
            _paginationHelper = pagination;
            _current = currentUserAccessor;
            _productoService = productoService;
            _carritoService= carritoService;
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
        [ValidateAntiForgeryToken]  
        public async Task<IActionResult> Create(ProductosViewModel model)
        {
            // Recargamos siempre la lista de proveedores para que no se pierda el dropdown
            var proveedores = await _productoRepository.ObtenerProveedores();
            model.Proveedores = proveedores.ToSelectList(
                u => u.Id,
                u => u.NombreProveedor,
                placeholder: "Seleccione un proveedor..."
            );

            // VALIDACIÓN DEL MODELO
            if (!ModelState.IsValid)
            {
                return View(model);  
            }

            try
            {
                var resultado = await _policyExecutor.ExecutePolicyAsync(() =>
                    _productoService.CrearProducto(model));

                if (resultado.Success)
                {
                    TempData["SuccessMessage"] = "Producto creado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = resultado.Message ?? "Error al crear el producto.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el producto: {NombreProducto}", model.NombreProducto);
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde.";
                return View(model);  
            }
        }
        //Metodo que obtiene la información necesaria para eliminar el producto
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
               
                
                var producto = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerProductoPorIdAsync(id));    
                if (producto == null)
                {
                    _logger.LogError("No se ha encontrado el producto");
                    return RedirectToAction(nameof(Index));
                }
                var viewmodel = new DeleteProductoViewmodel 
                { 
                    Id = id,
                    NombreProducto= producto.NombreProducto,
                    Descripcion= producto.Descripcion,
                    Cantidad= producto.Cantidad,
                    Precio= producto.Precio,
                    NombreProvedor= producto.IdProveedorNavigation.NombreProveedor
                
                
                };
                return View(viewmodel);
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
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _productoService.EliminarProducto(Id));
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
               

                var producto = await _policyExecutor.ExecutePolicyAsync(() => _productoRepository.ObtenerProductoPorIdAsync(id));
                if (producto == null)
                {
                    TempData["ErrorMessage"] = "No se ha encontrado el producto";
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
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _productoService.EditarProducto(model, usuarioId));
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
       
       

    }
}
