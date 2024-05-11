using Aspose.Pdf;
using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
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
        public ProductosController(GestorInventarioContext context, IGestorArchivos gestorArchivos, PaginacionMetodo paginacionMetodo, ILogger<ProductosController> logger, IHttpContextAccessor contextAccessor, IEmailService emailService)
        {
            _context = context;
            _gestorArchivos = gestorArchivos;
            _paginarMetodo = paginacionMetodo;
            _logger = logger;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
        }

        public async  Task<IActionResult> Index(string buscar, string ordenarPorprecio,[FromQuery] Paginacion paginacion)
        {
            try
            {
                //var productos = _context.Productos.Include(x => x.IdProveedorNavigation).AsQueryable();
                var productos = from p in _context.Productos.Include(x => x.IdProveedorNavigation)
                                select p;
                if (!String.IsNullOrEmpty(buscar))
                {
                    productos = productos.Where(s => s.NombreProducto.Contains(buscar));
                }
                if (!String.IsNullOrEmpty(ordenarPorprecio))
                {
                    if (ordenarPorprecio == "asc")
                    {
                        productos = productos.OrderBy(p => p.Precio);
                    }
                    else if (ordenarPorprecio == "desc")
                    {
                        productos = productos.OrderByDescending(p => p.Precio);
                    }
                }
                await VerificarStock();
                await HttpContext.InsertarParametrosPaginacionRespuesta(productos, paginacion.CantidadAMostrar);
                var productoPaginado = productos.Paginar(paginacion).ToList();
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                // ViewData["Paginas"] = GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);
                ViewData["Paginas"] = _paginarMetodo.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);

                return View(productoPaginado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar la vista");
                return BadRequest("Error al mostrar la vista intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
            
        }
        public async Task VerificarStock()
        {
            var emailUsuario = User.FindFirstValue(ClaimTypes.Name);

            if (emailUsuario != null)
            {
                var productos = await _context.Productos.ToListAsync();

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




        public async Task<IActionResult> Create()
        {
            try
            {
                ViewData["Productos"] = new SelectList(_context.Proveedores, "Id", "NombreProveedor");

                return View();
            }
            catch (Exception ex)
            {
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
                    //Esto toma en cuenta las validaciones puestas en BeerViewModel
                    if (ModelState.IsValid)
                    {
                        //Crear Producto
                        //Porque tenemos Imagen y Imagen1 facil Imagen almacena la ruta que es string y Imagen1 almacena la imgan en si
                        var producto = new Producto()
                        {
                            NombreProducto = model.NombreProducto,
                            Descripcion = model.Descripcion,
                            Imagen = "",
                            Cantidad = model.Cantidad,
                            Precio = model.Precio,
                            FechaCreacion=DateTime.Now,
                            IdProveedor = model.IdProveedor,
                        };
                        if (producto.Imagen != null)
                        {
                            //MemoryStream--> guarda en memoria la imagen
                            using (var memoryStream = new MemoryStream())
                            {
                                //Realiza una copia de la imagen
                                await model.Imagen1.CopyToAsync(memoryStream);
                                //La informacion de la imgen se convierte a un array
                                var contenido = memoryStream.ToArray();
                                //Se obtiene el formato de la imagen .png, .jpg etc
                                var extension = Path.GetExtension(model.Imagen1.FileName);
                                //Guarda la imagen en la carpeta imagenes
                                producto.Imagen = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes"
                             );
                            }
                        }

                        _context.Add(producto);
                        ViewData["Productos"] = new SelectList(_context.Proveedores, "Id", "NombreProveedor");
                        await _context.SaveChangesAsync();
                        //Después de guardar el producto en la base de datos
                        var historialProducto = new HistorialProducto()
                        {
                            UsuarioId = usuarioId,
                            Fecha = DateTime.Now,
                            Accion = _contextAccessor.HttpContext.Request.Method.ToString(),
                            Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                        };
                        //var direccionIP = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();

                        _context.Add(historialProducto);
                        await _context.SaveChangesAsync();
                        var detalleHistorialProducto = new DetalleHistorialProducto
                        {
                            HistorialProductoId = historialProducto.Id, 
                            ProductoId = producto.Id, 
                            Cantidad = producto.Cantidad,
                            NombreProducto=producto.NombreProducto,
                            Precio=producto.Precio
                        };

                        // Guardar el detalle del historial de productos
                        _context.Add(detalleHistorialProducto);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Los datos se han creado con éxito.";

                        return RedirectToAction(nameof(Index));
                    }
                    return View(model);
                }
                return BadRequest("Error al crear el producto intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el producto");
                return BadRequest("Error al crear el producto intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
            

        }
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                //Consulta a base de datos
                var producto = await _context.Productos.Include(p => p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == id);
                //Si no hay cervezas muestra el error 404
                if (producto == null)
                {
                    return NotFound("Producto no encontrado");
                }
                //Llegados ha este punto hay cervezas por lo tanto se muestran las cervezas
                return View(producto);
            }
            catch (Exception ex)
            {
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
                // Usamos 'Include' para cargar los datos relacionados de 'DetallePedidos' para cada producto.
                // 'ThenInclude' se utiliza para cargar aún más datos relacionados. En este caso, los datos de 'Pedido' para cada 'DetallePedidos'.
                // Finalmente, usamos otro 'Include' para cargar los datos del proveedor relacionados con cada producto.
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var producto = await _context.Productos
                    .Include(p => p.DetallePedidos)
                        .ThenInclude(dp => dp.Pedido)
                    .Include(p => p.IdProveedorNavigation).Include(x=>x.DetalleHistorialProductos)
                    .FirstOrDefaultAsync(m => m.Id == Id);

                    if (producto == null)
                    {
                        return BadRequest("No hay productos para eliminar");
                    }

                    if (producto.DetallePedidos.Any()||producto.DetalleHistorialProductos.Any())
                    {
                        TempData["ErrorMessage"] = "El producto no se puede eliminar porque tiene pedidos o historial asociados.";
                        //En caso de que el proveedor tenga productos asociados se devuelve al usuario a la vista Delete y se
                        //muestra el mensaje informandole.
                        //A esta reedireccion se le pasa la vista Delete y al metodo que contiene esa vista la id del producto
                        return RedirectToAction(nameof(Delete), new { id = Id });
                    }
                    //Después de guardar el producto en la base de datos
                    //var historialProducto = new HistorialProducto
                    //{
                    //    Fecha = DateTime.Now,
                    //    UsuarioId = usuarioId,
                    //    Accion = "DELETE",
                    //    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                    //};
                    //_context.Add(historialProducto);
                    //await _context.SaveChangesAsync();
                    //var detalleHistorialProducto = new DetalleHistorialProducto
                    //{
                    //    HistorialProductoId = historialProducto.Id,
                    //    ProductoId = producto.Id,
                    //    Cantidad = producto.Cantidad
                    //};
                    //_context.Add(detalleHistorialProducto);
                    //await _context.SaveChangesAsync();

                    _context.Productos.Remove(producto);


                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
                    return RedirectToAction(nameof(Index));
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
                var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == id);
                ViewData["Productos"] = new SelectList(_context.Proveedores, "Id", "NombreProveedor");
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
                            var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);
                            // Guardar el estado original del producto
                            var productoOriginal = new ProductosViewModel
                            {
                                Id = producto.Id,
                                NombreProducto = producto.NombreProducto,
                                Descripcion = producto.Descripcion,
                                Cantidad = producto.Cantidad,
                                Precio = producto.Precio,
                                Imagen = producto.Imagen,
                                IdProveedor = producto.IdProveedor
                            };
                            var historialProducto1 = new HistorialProducto
                            {
                                Fecha = DateTime.Now,
                                UsuarioId = usuarioId,
                                Accion = "Antes-PUT",
                                Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                            };

                            _context.Add(historialProducto1);
                            await _context.SaveChangesAsync();
                            // Guardar el estado original del producto en DetalleHistorialProducto
                            var detalleHistorialProducto1 = new DetalleHistorialProducto
                            {
                                HistorialProductoId = historialProducto1.Id,
                                ProductoId = productoOriginal.Id,
                                Cantidad = productoOriginal.Cantidad,
                                NombreProducto = productoOriginal.NombreProducto,
                                Precio = productoOriginal.Precio,
                            };
                            _context.Add(detalleHistorialProducto1);
                            await _context.SaveChangesAsync();
                            // Obtener de nuevo el producto del contexto para poder actualizarlo
                            producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);
                            // Después de guardar el producto en la base de datos
                            producto.NombreProducto = model.NombreProducto;
                            producto.FechaModificacion=DateTime.Now;
                            producto.Descripcion = model.Descripcion;
                            producto.Cantidad = model.Cantidad;
                            producto.Precio = model.Precio;  
                            producto.IdProveedor = model.IdProveedor;
                            if (model.Imagen1 != null)
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    await model.Imagen1.CopyToAsync(memoryStream);
                                    var contenido = memoryStream.ToArray();
                                    var extension = Path.GetExtension(model.Imagen1.FileName);
                                    // Borrar la imagen antigua
                                    await _gestorArchivos.BorrarArchivo(producto.Imagen, "imagenes");
                                    // Guardar la nueva imagen
                                    producto.Imagen = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes");
                                }
                            }
                            
                            _context.Productos.Update(producto);
                            await _context.SaveChangesAsync();
                            //Después de guardar el producto en la base de datos
                            var historialProducto = new HistorialProducto
                            {
                                Fecha = DateTime.Now,
                                UsuarioId = usuarioId,
                                Accion = "Despues-PUT",
                                Ip= _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                            };
                            _context.Add(historialProducto);
                            await _context.SaveChangesAsync();
                            var detalleHistorialProducto = new DetalleHistorialProducto
                            {
                                HistorialProductoId = historialProducto.Id,
                                ProductoId = producto.Id,
                                Cantidad = producto.Cantidad,
                                NombreProducto=producto.NombreProducto,
                                Precio=producto.Precio,
                            };
                            _context.Add(detalleHistorialProducto);
                            await _context.SaveChangesAsync();
                            TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                        }
                        
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        _logger.LogError(ex,"Error de concurrencia ");
                        if (!ProductoExist(model.Id))
                        {
                            return NotFound("Producto no encontrado");
                        }
                        else
                        {
                            _context.Entry(model).Reload();

                            //Intenta guardar de nuevo
                            _context.Entry(model).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
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


        private bool ProductoExist(int Id)
        {
            try
            {
                return _context.Productos.Any(e => e.Id == Id);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el producto");
                return false;
            }
        }
        public async Task<IActionResult> AgregarAlCarrito(int idProducto, int cantidad)
        {
            try
            {
                //Detecta el usuario logueado
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var usuarioActual = usuarioId;
                    //Asigna un carrito al usuario logueado
                    var carrito = _context.Carritos.FirstOrDefault(c => c.UsuarioId == usuarioActual);
                    if (carrito == null)
                    {
                        carrito = new Carrito
                        {
                            UsuarioId = usuarioActual,
                            FechaCreacion = DateTime.Now
                        };
                        _context.Carritos.Add(carrito);
                        // Guarda el nuevo Carrito en la base de datos
                        await _context.SaveChangesAsync();
                    }
                    //Una vez asignado el carrito al usuario ese carrito tiene una id y a ese carrito se le asignan productos los cuales
                    //tienen una id la id del producto se obtiene mediante la realacion de clave foranea en esta tabla con la tabla Productos
                    var itemCarrito = _context.ItemsDelCarritos.FirstOrDefault(i => i.CarritoId == carrito.Id && i.ProductoId == idProducto);
                    Producto producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == idProducto);

                    if (producto != null)
                    {
                        // Verifica si la cantidad del producto es suficiente
                        if (producto.Cantidad < cantidad)
                        {
                            TempData["ErrorMessage"] = "No hay suficientes productos en stock.";
                            return RedirectToAction("Index");
                        }

                        // Si el producto no está en el carrito, crea un nuevo item que se podria decir un articulo ItemsDelCarrito
                        if (itemCarrito == null)
                        {
                            itemCarrito = new ItemsDelCarrito
                            {
                                ProductoId = idProducto,
                                Cantidad = cantidad,
                                CarritoId = carrito.Id
                            };
                            _context.ItemsDelCarritos.Add(itemCarrito);
                        }
                        else
                        {
                            // Si el producto ya está en el carrito, incrementa la cantidad del producto
                            itemCarrito.Cantidad += cantidad;
                            _context.ItemsDelCarritos.Update(itemCarrito);
                        }

                        //Una vez agregado al carrito se quita la cantidad de productos
                        producto.Cantidad -= cantidad;
                        _context.Productos.Update(producto);
                        // Guarda los cambios en la base de datos
                        await _context.SaveChangesAsync();
                    }
                    await _context.SaveChangesAsync();
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

        [HttpPost]
        public async Task<ActionResult> Incrementar(int id)
        {
            // Busca el producto en la base de datos
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);

            if (producto != null)
            {
                // Incrementa la cantidad del producto
                producto.Cantidad++;
                _context.Productos.Update(producto);
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
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);

            if (producto != null)
            {
                // Incrementa la cantidad del producto
                producto.Cantidad--;
                _context.Productos.Update(producto);
                // Guarda los cambios en la base de datos
                await _context.SaveChangesAsync();
            }

            // Redirige al usuario a la página de índice
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> HistorialProducto(string buscar)
        {
            //var historialProductos = await _context.HistorialProductos
            //    .Include(hp => hp.DetalleHistorialProductos)
            //    .ToListAsync();
            var historialProductos = from p in _context.HistorialProductos.Include(x => x.DetalleHistorialProductos)
                            select p;
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
         
            var historialProductos = await _context.HistorialProductos
     .Include(hp => hp.DetalleHistorialProductos)
         .ThenInclude(dp => dp.Producto)
     .ToListAsync();

    
            if (historialProductos == null || historialProductos.Count == 0)
            {
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

                    detalleRow.Cells.Add($"{detalle.Producto?.NombreProducto}").Alignment = HorizontalAlignment.Center;
                    detalleRow.Cells.Add($"{detalle.HistorialProductoId}").Alignment = HorizontalAlignment.Center;
                    detalleRow.Cells.Add($"{detalle.ProductoId}");
                    detalleRow.Cells.Add($"{detalle.Cantidad}").Alignment = HorizontalAlignment.Center;
                    detalleRow.Cells.Add($"{detalle.Producto?.Precio}").Alignment = HorizontalAlignment.Center;
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
                var historialProducto = await _context.HistorialProductos
                    .Include(hp => hp.DetalleHistorialProductos)
                        .ThenInclude(dp => dp.Producto)
                  
                    .FirstOrDefaultAsync(hp => hp.Id == id);

                if (historialProducto == null)
                {
                    return NotFound();
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
                //Consulta a base de datos
                var historialProducto = await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).FirstOrDefaultAsync(x => x.Id == id);
                //Si no hay cervezas muestra el error 404
                if (historialProducto == null)
                {
                    return NotFound("Producto no encontrado");
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
                // Usamos 'Include' para cargar los datos relacionados de 'DetallePedidos' para cada producto.
                // 'ThenInclude' se utiliza para cargar aún más datos relacionados. En este caso, los datos de 'Pedido' para cada 'DetallePedidos'.
                // Finalmente, usamos otro 'Include' para cargar los datos del proveedor relacionados con cada producto.
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var historialProducto = await _context.HistorialProductos.Include(x=>x.DetalleHistorialProductos).FirstOrDefaultAsync(x => x.Id == Id);
                    if (historialProducto == null)
                    {
                        return BadRequest("no se puede eliminar porque es null");
                    }
                    _context.DetalleHistorialProductos.RemoveRange(historialProducto.DetalleHistorialProductos);
                    _context.HistorialProductos.Remove(historialProducto);
                    await _context.SaveChangesAsync();
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
        [HttpDelete("eliminarhistorial")]
        public async Task<IActionResult> EliminarHistorial()
        {
            // Obtener todos los registros del historial
            var historialProductos = await _context.HistorialProductos.ToListAsync();

            // Eliminar todos los registros
            _context.HistorialProductos.RemoveRange(historialProductos);

            // Guardar los cambios en la base de datos
            await _context.SaveChangesAsync();

            // Devolver una respuesta al cliente
            return Ok("Historial eliminado con éxito");
        }
        [HttpPost, ActionName("DeleteAllHistorial")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllHistorial()
        {
            try
            {
                // Obtener todos los registros del historial
                var historialProductos = await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).ToListAsync();

                if (historialProductos == null || historialProductos.Count == 0)
                {
                    return BadRequest("No hay datos en el historial para eliminar");
                }

                // Eliminar todos los registros
                foreach (var historialProducto in historialProductos)
                {
                    _context.DetalleHistorialProductos.RemoveRange(historialProducto.DetalleHistorialProductos);
                    _context.HistorialProductos.Remove(historialProducto);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Todos los datos del historial se han eliminado con éxito.";
                return RedirectToAction(nameof(HistorialProducto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar los datos del historial");
                return BadRequest("Error al eliminar los datos del historial, inténtelo de nuevo más tarde. Si el problema persiste, contacte con el administrador");
            }
        }


    }
}
