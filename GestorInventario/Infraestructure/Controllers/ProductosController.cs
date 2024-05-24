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
        private readonly GestorInventarioContext _context;
        private readonly IGestorArchivos _gestorArchivos;
        private readonly PaginacionMetodo _paginarMetodo;
        private readonly ILogger<ProductosController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;
        private readonly IProductoRepository _productoRepository;
        private readonly PolicyHandler _PolicyHandler;

        public ProductosController(GestorInventarioContext context, IGestorArchivos gestorArchivos, PaginacionMetodo paginacionMetodo, 
        ILogger<ProductosController> logger, IHttpContextAccessor contextAccessor, IEmailService emailService, IProductoRepository producto, PolicyHandler retry
       )
        {
            _context = context;
            _gestorArchivos = gestorArchivos;
            _paginarMetodo = paginacionMetodo;
            _logger = logger;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
            _productoRepository = producto;
           _PolicyHandler= retry;
        }

        public async Task<IActionResult> Index(string buscar, string ordenarPorprecio, int? idProveedor, [FromQuery] Paginacion paginacion)
        {
            try
            {
                var policyAsync = _PolicyHandler.GetRetryPolicyAsync();
                var policy= _PolicyHandler.GetRetryPolicy();
                // Obtenemos la consulta como IQueryable
                //var productos = ExecutePolicy(() => _productoRepository.ObtenerTodoProducto());
                var productos = ExecutePolicy(() => _productoRepository.ObtenerTodoProducto());
                // Aplicamos los filtros a la consulta
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
                ViewData["Proveedores"] = new SelectList(_context.Proveedores, "Id", "NombreProveedor");
                await VerificarStock();
                await HttpContext.InsertarParametrosPaginacionRespuesta(productos, paginacion.CantidadAMostrar);
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                ViewData["Paginas"] = _paginarMetodo.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);

                return View(productoPaginado);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Tiempo de respuesta del servidor agotado";
                _logger.LogError(ex, "Error al mostrar la vista");
                return BadRequest("Error al mostrar la vista. Inténtelo de nuevo más tarde. Si el problema persiste, contacte con el administrador.");
            }
        }

        public async Task VerificarStock()
        {
            try
            {
                var emailUsuario = User.FindFirstValue(ClaimTypes.Email);

                if (emailUsuario != null)
                {
                    var policy = _PolicyHandler.GetRetryPolicy();

                    var productos = ExecutePolicy(() => _productoRepository.ObtenerTodoProducto());
                    //var productos = await _productoRepository.ObtenerTodos();
                    // var productos = await _context.Productos.ToListAsync();

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
                TempData["ErrorMessage"] = "Tiempo de respuesta del servidor agotado";
                _logger.LogError("Error al verificar el stock", ex);
            }
           
        }
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewData["Productos"] = new SelectList(await _productoRepository.ObtenerProveedores(), "Id", "NombreProveedor");

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Tiempo de respuesta del servidor agotado";
                _logger.LogError(ex, "Error al mostrar la vista de creacion del producto");
                return BadRequest("Error al mostrar la vista intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
           
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductosViewModel model)
        {
            try
            {
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
                return BadRequest("Error al crear el producto intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Tiempo de respuesta del servidor agotado";
                _logger.LogError(ex, "Error al crear el producto");
                return BadRequest("Error al crear el producto intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
        }
       
       

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var policy = _PolicyHandler.GetRetryPolicyAsync();
                var producto = await ExecutePolicyAsync(() => _productoRepository.EliminarProductoObtencion(id));
                //var producto= await _productoRepository.EliminarProductoObtencion(id);
                //Consulta a base de datos
                //var producto = await _context.Productos.Include(p => p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == id);
                //Si no hay cervezas muestra el error 404
                if (producto == null)
                {
                    TempData["ErrorMessage"] = "Producto no encontrado";
                    return NotFound("Producto no encontrado");
                }
                //Llegados ha este punto hay cervezas por lo tanto se muestran las cervezas
                return View(producto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Tiempo de respuesta del servidor agotado";
                _logger.LogError(ex, "Error al eliminar el producto");
                return BadRequest("Error al mostrar la vista de eliminacion intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
            
        }


        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
                
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var policy= _PolicyHandler.GetRetryPolicyAsync();
                    var producto = await ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));
                    //var producto = await _productoRepository.EliminarProducto(Id);
                    if (producto == null)
                    {
                        TempData["ErrorMessage"] = "No hay productos para eliminar";
                        return BadRequest("No hay productos para eliminar");
                    }
                    if (producto.DetallePedidos.Any()||producto.DetalleHistorialProductos.Any())
                    {
                        TempData["ErrorMessage"] = "El producto no se puede eliminar porque tiene pedidos o historial asociados.";
                        return RedirectToAction(nameof(Delete), new { id = Id });
                    }
                    _context.DeleteEntity(producto);
                    TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                return BadRequest("Error al eliminar el producto intentelo de nuevo mas tarde si el problema persiste intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Tiempo de respuesta del servidor agotado";
                _logger.LogError(ex, "Error al eliminar el producto");
                return BadRequest("Error al eliminar el producto intentelo de nuevo mas tarde si el problema persiste intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
            
        }

        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var policy = _PolicyHandler.GetRetryPolicyAsync();
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
                _logger.LogError(ex, "Error al editar el producto");
                return BadRequest("Error al mostrar la vista de edicion del producto intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
           
        }
        [HttpPost]
        public async Task<ActionResult> Edit(ProductosViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        int usuarioId;
                        if (int.TryParse(existeUsuario, out usuarioId))
                        {
                            var policy = _PolicyHandler.GetRetryPolicyAsync();
                            var producto = await ExecutePolicyAsync(() => _productoRepository.ObtenerPorId(model.Id));
                            var productoOriginal = await ExecutePolicyAsync(() => _productoRepository.ProductoOriginal(producto));
                            var historialProductoPostActualizacion = await ExecutePolicyAsync(() => _productoRepository.CrearHitorialAccion(usuarioId));
                            var detalleHistorialProductoPostActualizacion = await ExecutePolicyAsync(() => _productoRepository.EditDetalleHistorialproducto(historialProductoPostActualizacion, productoOriginal));
                            var actualizarProducto = await ExecutePolicyAsync(() => _productoRepository.ActualizarProducto(model));
                            var historialProducto = await ExecutePolicyAsync(() => _productoRepository.CrearHitorialAccionEdit(usuarioId));
                            var detalleHistorialProducto = await ExecutePolicyAsync(() => _productoRepository.DetalleHistorialProductoEdit(historialProducto, actualizarProducto));
                            TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                        }    
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!_productoRepository.ProductoExist(model.Id))
                        {
                            TempData["ErrorMessage"] = "Producto no encontrado";
                            return NotFound("Producto no encontrado");
                        }
                        else
                        {
                            bool isUpdated = await _productoRepository.TryUpdateAndSaveAsync(model);
                            if (!isUpdated)
                            {
                                TempData["ErrorMessage"] = "Error al actualizar";
                            }
                            else
                            {
                                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                                int usuarioId;
                                if (int.TryParse(existeUsuario, out usuarioId))
                                {
                                    var policy = _PolicyHandler.GetRetryPolicyAsync();
                                    var producto = await ExecutePolicyAsync(() => _productoRepository.ObtenerPorId(model.Id));
                                    var productoOriginal = await ExecutePolicyAsync(() => _productoRepository.ProductoOriginal(producto));
                                    var historialProductoPostActualizacion = await ExecutePolicyAsync(() => _productoRepository.CrearHitorialAccion(usuarioId));
                                    var detalleHistorialProductoPostActualizacion = await ExecutePolicyAsync(() => _productoRepository.EditDetalleHistorialproducto(historialProductoPostActualizacion, productoOriginal));
                                    var actualizarProducto = await ExecutePolicyAsync(() => _productoRepository.ActualizarProducto(model));
                                    var historialProducto = await ExecutePolicyAsync(() => _productoRepository.CrearHitorialAccionEdit(usuarioId));
                                    var detalleHistorialProducto = await ExecutePolicyAsync(() => _productoRepository.DetalleHistorialProductoEdit(historialProducto, actualizarProducto));
                                    TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                                }
                            }
                        }
                    }
                    return RedirectToAction("Index");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar el producto");
                return BadRequest("Error al editar el producto intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
           
        }
        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito(int idProducto, int cantidad)
        {
            try
            {
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var policy = _PolicyHandler.GetRetryPolicyAsync();
                    var carrito = await ExecutePolicyAsync(() => _productoRepository.ObtenerCarritoUsuario(usuarioId));
                    //var carrito = await _productoRepository.ObtenerCarritoUsuario(usuarioId);
                    var producto = await ExecutePolicyAsync(() => _productoRepository.ObtenerPorId(idProducto));
                     //var producto = await _productoRepository.ObtenerPorId(idProducto);
                    if (producto != null)
                    {
                        if (producto.Cantidad < cantidad)
                        {
                            TempData["ErrorMessage"] = "No hay suficientes productos en stock.";
                            return RedirectToAction("Index");
                        }
                        var itemCarrito = await ExecutePolicyAsync(() => _productoRepository.AgregarOActualizarProductoCarrito(carrito.Id, idProducto, cantidad));
                        //var itemCarrito = await _productoRepository.AgregarOActualizarProductoCarrito(carrito.Id, idProducto, cantidad);
                        producto = await ExecutePolicyAsync(() => _productoRepository.DisminuirCantidadProducto(idProducto, cantidad));
                        //producto = await _productoRepository.DisminuirCantidadProducto(idProducto, cantidad);
                    }
                    return RedirectToAction("Index");
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar el producto al carrito");
                return BadRequest("Error al agregar el producto al carrito intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
        }

       
        public async Task<IActionResult> HistorialProducto(string buscar)
        {
            var policy = _PolicyHandler.GetRetryPolicyAsync();
            var historialProductos = await ExecutePolicyAsync(() => _productoRepository.ObtenerTodoHistorial());
           //var historialProductos = await _productoRepository.ObtenerTodoHistorial();
           
            //var historialProductos = from p in _context.HistorialProductos.Include(x => x.DetalleHistorialProductos)
            //                select p;
            // Aquí es donde se realiza la búsqueda por el número de pedido
            if (!String.IsNullOrEmpty(buscar))
            {
                historialProductos =  historialProductos.Where(p => p.Accion.Contains(buscar) || p.Ip.Contains(buscar));
            }
            return View(await historialProductos.ToListAsync());
        }
        [HttpGet("descargarhistorialPDF")]
        public async Task<IActionResult> DescargarHistorialPDF()
        {
            var policy= _PolicyHandler.GetRetryPolicyAsync();
            var historialProductos = await ExecutePolicyAsync(() => _productoRepository.DescargarPDF());
           //var historialProductos = await _productoRepository.DescargarPDF();
            if (historialProductos == null || historialProductos.Count == 0)
            {
                TempData["ErrorMessage"] = "Datos de productos no encontrados";
                return BadRequest("Datos de productos no encontrados");
            }
            // Crear un documento PDF con orientación horizontal
            Document document = new Document();
            //Margenes y tamaño del documento
            document.PageInfo.Width = Aspose.Pdf.PageSize.PageLetter.Width;
            document.PageInfo.Height = Aspose.Pdf.PageSize.PageLetter.Height;
            document.PageInfo.Margin = new MarginInfo(1, 10, 10, 10); // Ajustar márgenes
            // Agregar una nueva página al documento con orientación horizontal
            Page page = document.Pages.Add();
            //Control de margenes
            page.PageInfo.Margin.Left = 35;
            page.PageInfo.Margin.Right = 0;
            //Controla la horientacion actualmente es horizontal
            page.SetPageSize(Aspose.Pdf.PageSize.PageLetter.Width, Aspose.Pdf.PageSize.PageLetter.Height);
            // Crear una tabla para mostrar las mediciones
            Aspose.Pdf.Table table = new Aspose.Pdf.Table();
            table.VerticalAlignment = VerticalAlignment.Center;
            table.Alignment = HorizontalAlignment.Left;
            table.DefaultCellBorder = new Aspose.Pdf.BorderInfo(Aspose.Pdf.BorderSide.All, 0.1F);
            table.Border = new Aspose.Pdf.BorderInfo(Aspose.Pdf.BorderSide.All, 1F);
            table.ColumnWidths = "55 50 45 45 45 35 45 45 45 45 35 50"; // Ancho de cada columna

            page.Paragraphs.Add(table);

            // Agregar fila de encabezado a la tabla
            Aspose.Pdf.Row headerRow = table.Rows.Add();
            headerRow.Cells.Add("Id").Alignment = HorizontalAlignment.Center;
            headerRow.Cells.Add("UsuarioId").Alignment = HorizontalAlignment.Center;
            headerRow.Cells.Add("Fecha").Alignment = HorizontalAlignment.Center;
            headerRow.Cells.Add("Accion").Alignment = HorizontalAlignment.Center;
            headerRow.Cells.Add("Ip").Alignment = HorizontalAlignment.Center;
            
            // Agregar contenido de mediciones a la tabla
            foreach (var historial in historialProductos)
            {

                Aspose.Pdf.Row dataRow = table.Rows.Add();
                Aspose.Pdf.Text.TextFragment textFragment1 = new Aspose.Pdf.Text.TextFragment("");
                page.Paragraphs.Add(textFragment1);
                dataRow.Cells.Add($"{historial.Id}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.UsuarioId}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.Fecha}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.Accion}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.Ip}").Alignment = HorizontalAlignment.Center;

                // Crear una segunda tabla para los detalles del producto
                Aspose.Pdf.Table detalleTable = new Aspose.Pdf.Table();
                detalleTable.DefaultCellBorder = new Aspose.Pdf.BorderInfo(Aspose.Pdf.BorderSide.All, 0.1F);
                detalleTable.Border = new Aspose.Pdf.BorderInfo(Aspose.Pdf.BorderSide.All, 1F);
                detalleTable.ColumnWidths = "100 100 100"; // Ancho de cada columna

                // Agregar la segunda tabla a la página
                page.Paragraphs.Add(detalleTable);
                Aspose.Pdf.Text.TextFragment textFragment = new Aspose.Pdf.Text.TextFragment("");
                page.Paragraphs.Add(textFragment);
                // Agregar fila de encabezado a la segunda tabla
                Aspose.Pdf.Row detalleHeaderRow = detalleTable.Rows.Add();
                detalleHeaderRow.Cells.Add("NombreProducto").Alignment = HorizontalAlignment.Center;
                detalleHeaderRow.Cells.Add("IdHistorial").Alignment = HorizontalAlignment.Center;
                detalleHeaderRow.Cells.Add("ProductoId").Alignment = HorizontalAlignment.Center;
                detalleHeaderRow.Cells.Add("Cantidad").Alignment = HorizontalAlignment.Center;
                detalleHeaderRow.Cells.Add("Precio").Alignment = HorizontalAlignment.Center;

                // Iterar sobre los DetalleHistorialProductos de cada HistorialProducto
                foreach (var detalle in historial.DetalleHistorialProductos)
                {
                    Aspose.Pdf.Row detalleRow = detalleTable.Rows.Add();

                    detalleRow.Cells.Add($"{detalle.NombreProducto}").Alignment = HorizontalAlignment.Center;
                    detalleRow.Cells.Add($"{detalle.HistorialProductoId}").Alignment = HorizontalAlignment.Center;
                    detalleRow.Cells.Add($"{detalle.ProductoId}");
                    detalleRow.Cells.Add($"{detalle.Cantidad}").Alignment = HorizontalAlignment.Center;
                    detalleRow.Cells.Add($"{detalle.Precio}").Alignment = HorizontalAlignment.Center;
                }
            }
            // Crear un flujo de memoria para guardar el documento PDF
            MemoryStream memoryStream = new MemoryStream();
            // Guardar el documento en el flujo de memoria
            document.Save(memoryStream);
            // Convertir el documento a un arreglo de bytes
            byte[] bytes = memoryStream.ToArray();
            // Liberar los recursos de la memoria
            memoryStream.Close();
            memoryStream.Dispose();
            // Devolver el archivo PDF para descargar
            return File(bytes, "application/pdf", "historial.pdf");
        }
        public async Task<IActionResult> DetallesHistorialProducto(int id)
        {
            try
            {
                var policy = _PolicyHandler.GetRetryPolicyAsync();
                var historialProducto = await ExecutePolicyAsync(() => _productoRepository.HistorialProductoPorId(id));
                //var historialProducto= await _productoRepository.HistorialProductoPorId(id);          
                if (historialProducto == null)
                {
                    TempData["ErrorMessage"] = "Detalles del historial no encontrado";
                    return NotFound("Detalles del historial no encontrado");
                }

                return View(historialProducto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los detalles del historial de productos");
                return BadRequest("Error al obtener los detalles del historial de productos. Inténtelo de nuevo más tarde o contacte con el administrador.");
            }
        }
        public async Task<IActionResult> DeleteHistorial(int id)
        {
            try
            {
                var policy= _PolicyHandler.GetRetryPolicyAsync();
                var historialProducto = await ExecutePolicyAsync(() => _productoRepository.EliminarHistorialPorId(id));
                //var historialProducto= await _productoRepository.EliminarHistorialPorId(id);
                if (historialProducto == null)
                {

                    TempData["ErrorMessage"] = "Historial no encontrado";
                    return NotFound("Historial no encontrado");
                }
                //Llegados ha este punto hay cervezas por lo tanto se muestran las cervezas
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
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var policy= _PolicyHandler.GetRetryPolicyAsync();
                    var historialProducto = await ExecutePolicyAsync(() => _productoRepository.EliminarHistorialPorIdDefinitivo(Id));
                    //var historialProducto = await _productoRepository.EliminarHistorialPorIdDefinitivo(Id);
                    if (historialProducto != null)
                    {
                        _context.DeleteRangeEntity(historialProducto.DetalleHistorialProductos);
                        _context.DeleteEntity(historialProducto);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "No se puede eliminar, el historial no existe";
                        return BadRequest("No se puede eliminar, el historial no existe");
                    }
                   
                    TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
                    return RedirectToAction(nameof(HistorialProducto));
                }
                return BadRequest("Error al eliminar el producto intentelo de nuevo mas tarde si el problema persiste intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el producto");
                return BadRequest("Error al eliminar el producto intentelo de nuevo mas tarde si el problema persiste intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
        }
        [HttpPost, ActionName("DeleteAllHistorial")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllHistorial()
        {
            try
            {
                var policy= _PolicyHandler.GetRetryPolicyAsync();
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
                _logger.LogError(ex, "Error al eliminar los datos del historial");
                return BadRequest("Error al eliminar los datos del historial, inténtelo de nuevo más tarde. Si el problema persiste, contacte con el administrador");
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
