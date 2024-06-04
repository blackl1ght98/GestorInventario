using Aspose.Pdf;
using Aspose.Pdf.Operators;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.Models.ViewModels;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using System.Linq;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class ProductosController : Controller
    {
        
        private readonly IGestorArchivos _gestorArchivos;
        private readonly PaginacionMetodo _paginarMetodo;
        private readonly ILogger<ProductosController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;
        private readonly IProductoRepository _productoRepository;
        private readonly PolicyHandler _PolicyHandler;
        private readonly GenerarPaginas _generarPaginas;
        public ProductosController( IGestorArchivos gestorArchivos, PaginacionMetodo paginacionMetodo, GenerarPaginas paginas,
        ILogger<ProductosController> logger, IHttpContextAccessor contextAccessor, IEmailService emailService, IProductoRepository producto, PolicyHandler retry
       )
        {
           
            _gestorArchivos = gestorArchivos;
            _paginarMetodo = paginacionMetodo;
            _logger = logger;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
            _productoRepository = producto;
           _PolicyHandler= retry;
            _generarPaginas= paginas;
        }

        public async Task<IActionResult> Index(string buscar, string ordenarPorprecio, int? idProveedor, [FromQuery] Paginacion paginacion)
        {
            try
            {
                
                // Obtenemos la consulta como IQueryable
                var productos = ExecutePolicy(() => _productoRepository.ObtenerTodoProducto());
                // Aplicamos los filtros a la consulta
                ViewData["Buscar"] = buscar;
                ViewData["OrdenarPorprecio"] = ordenarPorprecio;
                ViewData["idProveedor"] = idProveedor;
                if (!string.IsNullOrEmpty(buscar))
                {
                    productos = productos.Where(s => s.NombreProducto.Contains(buscar));
                }
                if (!string.IsNullOrEmpty(ordenarPorprecio))
                {
                   //Si hay dudas mirar archivo de explicacionSwitch.txt
                   //Mirar porque no persiste al cambio de pagina el filtro
                    productos = ordenarPorprecio switch
                    {
                        "asc" => productos.OrderBy(p => p.Precio),//resultado o patron a evaluar.
                        "desc" => productos.OrderByDescending(p => p.Precio),
                        _ => productos //si no se cumplen los patrones anteriores devuelve los productos sin filtrar 
                    };
                }
                if (idProveedor.HasValue)
                {
                    productos = productos.Where(p => p.IdProveedor == idProveedor.Value);
                }
                // Materializamos la consulta aplicando la política de reintento
                var productoPaginado = await ExecutePolicyAsync(() => productos.Paginar(paginacion).ToListAsync());
                // Configuración adicional
                ViewData["Proveedores"] = new SelectList(await ExecutePolicyAsync(()=>_productoRepository.ObtenerProveedores()), "Id", "NombreProveedor");
                await VerificarStock();
                await HttpContext.InsertarParametrosPaginacionRespuesta(productos, paginacion.CantidadAMostrar);
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                ViewData["Paginas"] = _paginarMetodo.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);

                return View(productoPaginado);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar la vista");
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task VerificarStock()
        {
            try
            {
                var emailUsuario = User.FindFirstValue(ClaimTypes.Email);

                if (emailUsuario != null)
                {


                    var productos = ExecutePolicy(() => _productoRepository.ObtenerTodoProducto());


                    foreach (var producto in productos)
                    {
                        if (producto.Cantidad < 10) // Define tu propio umbral
                        {
                            await _emailService.SendEmailAsyncLowStock(new DTOEmail
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
                _logger.LogError("Error al verificar el stock", ex);
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
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductosViewModel model)
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
                    // Esto toma en cuenta las validaciones puestas en BeerViewModel
                    if (ModelState.IsValid)
                    {
                        var producto = await ExecutePolicyAsync(() => _productoRepository.CrearProducto(model));
                        ViewData["Productos"] = new SelectList(await ExecutePolicyAsync(()=> _productoRepository.ObtenerProveedores()) , "Id", "NombreProveedor");

                        var historialProducto = await ExecutePolicyAsync(() => _productoRepository.CrearHistorial(usuarioId, producto));
                        var detalleHistorialProducto = await ExecutePolicyAsync(() => _productoRepository.CrearDetalleHistorial(historialProducto, producto));

                        TempData["SuccessMessage"] = "Los datos se han creado con éxito.";
                        return RedirectToAction(nameof(Index));
                    }
                    return View(model);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al crear el producto");
                return RedirectToAction("Error", "Home");
            }
        }
       
       

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var policy = _PolicyHandler.GetRetryPolicyAsync();
                var producto = await ExecutePolicyAsync(() => _productoRepository.EliminarProductoObtencion(id));
                //Si no hay cervezas muestra el error 404
                if (producto == null)
                {
                    TempData["ErrorMessage"] = "Producto no encontrado";
                }
                //Llegados ha este punto hay cervezas por lo tanto se muestran las cervezas
                return View(producto);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar el producto");
                return RedirectToAction("Error", "Home");
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
                    //Lo que vemos aqui es una carcateristica de c# llamada desestructuración de tuplas.
                    //Una tupla es una forma de agrupar múltiples valores en un solo objeto.
                    var (success, errorMessage) = await ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));
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
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {


                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError("Error al eliminar el producto", ex);
                return RedirectToAction("Error", "Home");
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

                var producto = await ExecutePolicyAsync(() => _productoRepository.ObtenerPorId(id));
                
                ViewData["Productos"] = new SelectList(await ExecutePolicyAsync(() => _productoRepository.ObtenerProveedores()), "Id", "NombreProveedor");
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

                            var (success,errorMesage)= await ExecutePolicyAsync(()=> _productoRepository.EditarProducto(model, usuarioId))  ;
                            if (success)
                            {
                                return RedirectToAction(nameof(Index));
                            }
                            else
                            {
                                TempData["ErrorMessage"] = errorMesage;
                            }

                            TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
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
                    var (success,errorMessage)= await ExecutePolicyAsync(()=> _productoRepository.AgregarProductosCarrito(idProducto, cantidad, usuarioId)) ;
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

       
        public async Task<IActionResult> HistorialProducto(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                var historialProductos = await ExecutePolicyAsync(() => _productoRepository.ObtenerTodoHistorial());
                ViewData["Buscar"] = buscar;
                if (!String.IsNullOrEmpty(buscar))
                {
                    historialProductos = historialProductos.Where(p => p.Accion.Contains(buscar) || p.Ip.Contains(buscar));
                }
                await HttpContext.InsertarParametrosPaginacionRespuesta(historialProductos, paginacion.CantidadAMostrar);
                var usuarios = ExecutePolicy(() => historialProductos.Paginar(paginacion).ToList());
                //Obtiene los datos de la cabecera que hace esta peticion
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                //Crea las paginas que el usuario ve.
                ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);
                return View(await historialProductos.ToListAsync());
            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener el historial de producto");
                return RedirectToAction("Error", "Home");
            }
           
        }
        [HttpGet("descargarhistorialPDF")]
        public async Task<IActionResult> DescargarHistorialPDF()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var (success, errorMessage, pdfData) = await ExecutePolicyAsync(() => _productoRepository.DescargarPDF());
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
        public async Task<IActionResult> DetallesHistorialProducto(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var historialProducto = await ExecutePolicyAsync(() => _productoRepository.HistorialProductoPorId(id));
                //var historialProducto= await _productoRepository.HistorialProductoPorId(id);          
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
        public async Task<IActionResult> DeleteHistorial(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var historialProducto = await ExecutePolicyAsync(() => _productoRepository.EliminarHistorialPorId(id));
                //var historialProducto= await _productoRepository.EliminarHistorialPorId(id);
                if (historialProducto == null)
                {

                    TempData["ErrorMessage"] = "Historial no encontrado";
                }
                //Llegados ha este punto hay cervezas por lo tanto se muestran las cervezas
                return View(historialProducto);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar el producto");
                return RedirectToAction("Error", "Home");
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
                    var (success, errorMessage) = await ExecutePolicyAsync(() => _productoRepository.EliminarHistorialPorIdDefinitivo(Id));
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Los datos se han eliminado con exito";
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

                var historialProductos = await ExecutePolicyAsync(() => _productoRepository.EliminarTodoHistorial());
               //var historialProductos = await _productoRepository.EliminarTodoHistorial();
                if (historialProductos == null || historialProductos.Count == 0)
                {
                    TempData["ErrorMessage"] = "No hay datos en el historial para eliminar";
                    return BadRequest("No hay datos en el historial para eliminar");
                }
                TempData["SuccessMessage"] = "Todos los datos del historial se han eliminado con éxito.";
                return RedirectToAction(nameof(HistorialProducto));
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar los datos del historial");
                return RedirectToAction("Error", "Home");
            }
        }
        //Task<T>-->Representa una operacion asincrona que puede devolver un valor, la <T> quiere decir que admite cualquier tipo de dato.
        //puede decirse asi quiero que este metodo sea de tipo Task y que me admita cualquier tipo de dato.
        //Func<Task<T>>--> Encapsula un metodo que no tiene parametros y devuelve un valor del tipo especificado como parametro,
        //en este caso como esta la <T> hemos dicho que esto puede representar cualquier tipo de dato que se devuelva de una operación asíncrona.
        /*Ejemplo del flujo:
         public async Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicyAsync<T>();
            return await policy.ExecuteAsync(operation);
        }aqui tenemos la operacion original y por ejemplo nuestro tipo de dato va a devolver algo de tipo Producto pues las <T> se cambiarian asi:
         public async Task<Producto> ExecutePolicyAsync<Producto>(Func<Task<Producto>> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicyAsync<Producto>();
            return await policy.ExecuteAsync(operation);
        }
         */
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
