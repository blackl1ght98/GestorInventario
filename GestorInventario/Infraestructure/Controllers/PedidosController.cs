using Aspose.Pdf;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces.Infraestructure;
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
        private readonly IPedidoRepository _pedidoRepository;
        public PedidosController(GestorInventarioContext context, GenerarPaginas generarPaginas, ILogger<PedidosController> logger, IPedidoRepository pedido)
        {
            _context = context;
            _generarPaginas = generarPaginas;
            _logger = logger;
            _pedidoRepository = pedido;
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
                    var pedidos = _pedidoRepository.ObtenerPedidos();
                    if (User.IsInRole("administrador"))
                    {
                       
                        pedidos = _pedidoRepository.ObtenerPedidos();
                    }
                    else
                    {
                        
                        pedidos = _pedidoRepository.ObtenerPedidoUsuario(usuarioId);
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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                if (ModelState.IsValid)
                {
                    var pedido = new Pedido()
                    {
                        NumeroPedido = GenerarNumeroPedido(),
                       // FechaPedido = model.FechaPedido,
                        EstadoPedido = model.EstadoPedido,
                        IdUsuario = model.IdUsuario,
                    };

                    _context.Add(pedido);

                    await _context.SaveChangesAsync();
                    var numeroPedidoGenerado = pedido.NumeroPedido; // Almacena el número de pedido generado
                    var historialPedido = new HistorialPedido()
                    {
                        NumeroPedido = numeroPedidoGenerado,
                        FechaPedido = pedido.FechaPedido,
                        EstadoPedido = pedido.EstadoPedido,
                        IdUsuario = pedido.IdUsuario,
                    };
                    _context.Add(historialPedido);
                    await _context.SaveChangesAsync();
                    /*El bucle for funciona de la siguiene manera:
                      * primero inicializa una variable i en 0
                      * depues compara si la variable i es menor que la lista de ids de productos que 
                      * hay en base de datos si es asi el bucle se recorre tantas veces hasta que 0
                      * se iguale a la cantidad de productos
                      * que hay en base de datos por ejemplo en base de datos tenemos 8 productos pues 
                      * i < model.IdsProducto.Count; se ira iterando hasta que i llegue a 8.
                      * Dentro de este bule tenemos una condicion pero ha esta condicion se le pasa la 
                      * posicion del producto que el usuario ha seleccionado a la condicion 
                      * model.ProductosSeleccionados[i] llegara solo los productos que el usuario halla 
                      * seleccionado por ejemplo de esos 8 productos el usuario ha seleccionado 4
                      * pues ha la codicion llegan 4 y de esos 4 se creara un detalle pedido para cada 
                      * producto en otras palabras sus caracteristicas precio, cantidad... como hemos dicho
                      *  no lo llega los 4 productos esos 4 productos vienen con la posicion de cada uno en
                      *  la lista la posicion comienza en 0 asi que si son 4 productos la posicio se ve 
                      * algo asi 0,1,2,3. A productos seleccionados llega una lista de booleanos junto a 
                      * la posicion se puede ver algo asi [true,false,true,false,true,false,true,false] aqui llegarian
                      *  todos los productos pero solo tiene en cuenta los que son
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
                    // Lógica para los detalles de productos (si es necesario)
                    for (var i = 0; i < model.IdsProducto.Count; i++)
                    {
                        if (model.ProductosSeleccionados[i])
                        {
                            var detalleHistorialPedido = new DetalleHistorialPedido()
                            {
                                HistorialPedidoId = historialPedido.Id,
                                ProductoId = model.IdsProducto[i],
                                Cantidad = model.Cantidades[i],
                            };
                            _context.Add(detalleHistorialPedido);
                        }
                    }
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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(x => x.Id == id);

                EditPedidoViewModel pedidosViewModel = new EditPedidoViewModel
                {
                    fechaPedido = pedido.FechaPedido,
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
                    var pedidoOriginal = await _context.Pedidos
                  .Include(p => p.DetallePedidos) 
                  .FirstOrDefaultAsync(x => x.Id == model.id); 
                    pedidoOriginal.FechaPedido = model.fechaPedido;
                    pedidoOriginal.EstadoPedido = model.estadoPedido;
                    _context.Pedidos.Update(pedidoOriginal);
                    await _context.SaveChangesAsync();
                    // Crear un nuevo registro en el historial de pedidos
                    var historialPedido = new HistorialPedido
                    {
                        NumeroPedido = pedidoOriginal.NumeroPedido,
                       // FechaPedido = model.fechaPedido,
                        EstadoPedido = model.estadoPedido,
                        IdUsuario = pedidoOriginal.IdUsuario
                    };
                    _context.Add(historialPedido);
                    await _context.SaveChangesAsync();
                    // Clonar los detalles del pedido del pedido original al nuevo pedido
                    foreach (var detalleOriginal in pedidoOriginal.DetallePedidos)
                    {
                        var nuevoDetalle = new DetalleHistorialPedido
                        {
                            HistorialPedidoId = historialPedido.Id,
                            ProductoId = detalleOriginal.ProductoId,
                            Cantidad = detalleOriginal.Cantidad
                        };
                        _context.Add(nuevoDetalle);
                    }

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
                        var pedidoOriginal = await _context.Pedidos
                                      .Include(p => p.DetallePedidos) // Incluimos los detalles del pedido
                                      .FirstOrDefaultAsync(x => x.Id == model.id); pedidoOriginal.FechaPedido = model.fechaPedido;
                        pedidoOriginal.EstadoPedido = model.estadoPedido;
                        _context.Pedidos.Update(pedidoOriginal);
                        await _context.SaveChangesAsync();
                        // Crear un nuevo registro en el historial de pedidos
                        var historialPedido = new HistorialPedido
                        {
                            NumeroPedido = pedidoOriginal.NumeroPedido,
                            FechaPedido = model.fechaPedido,
                            EstadoPedido = model.estadoPedido,
                            IdUsuario = pedidoOriginal.IdUsuario
                        };
                        _context.Add(historialPedido);
                        await _context.SaveChangesAsync();
                        // Clonar los detalles del pedido del pedido original al nuevo pedido
                        foreach (var detalleOriginal in pedidoOriginal.DetallePedidos)
                        {
                            var nuevoDetalle = new DetalleHistorialPedido
                            {
                                HistorialPedidoId = historialPedido.Id,
                                ProductoId = detalleOriginal.ProductoId,
                                Cantidad = detalleOriginal.Cantidad
                            };
                            _context.Add(nuevoDetalle);
                        }

                        await _context.SaveChangesAsync();

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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
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
                IQueryable<HistorialPedido> pedidos;
                if (User.IsInRole("administrador"))
                {
                    pedidos = _context.HistorialPedidos.Include(dp => dp.DetalleHistorialPedidos)
                        .ThenInclude(p => p.Producto)
                        .Include(u => u.IdUsuarioNavigation);
                }
                else
                {
                    pedidos = _context.HistorialPedidos.Where(p => p.IdUsuario == usuarioId)
                        .Include(dp => dp.DetalleHistorialPedidos).ThenInclude(p => p.Producto)
                        .Include(u => u.IdUsuarioNavigation);
                }
                // Aquí es donde se realiza la búsqueda por el número de pedido
                
                if (!String.IsNullOrEmpty(buscar))
                {
                    pedidos = pedidos.Where(p => p.NumeroPedido.Contains(buscar) || p.EstadoPedido.Contains(buscar));
                }
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
                var pedido = await _context.HistorialPedidos
               .Include(p => p.DetalleHistorialPedidos)
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
        [HttpGet("descargarhistorialpedidoPDF")]
        public async Task<IActionResult> DescargarHistorialPDF()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }
            var historialPedido = await _context.HistorialPedidos
     .Include(hp => hp.DetalleHistorialPedidos)
         .ThenInclude(dp => dp.Producto)
     .ToListAsync();


            if (historialPedido == null || historialPedido.Count == 0)
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
            headerRow.Cells.Add("Numero Pedido").Alignment = HorizontalAlignment.Center;
            headerRow.Cells.Add("Fecha").Alignment = HorizontalAlignment.Center;
            headerRow.Cells.Add("Estado Pedido").Alignment = HorizontalAlignment.Center;
            headerRow.Cells.Add("Id Usuario").Alignment = HorizontalAlignment.Center;

            // Agregar contenido de mediciones a la tabla
            foreach (var historial in historialPedido)
            {

                Aspose.Pdf.Row dataRow = table.Rows.Add();
                Aspose.Pdf.Text.TextFragment textFragment1 = new Aspose.Pdf.Text.TextFragment("");
                page.Paragraphs.Add(textFragment1);
                dataRow.Cells.Add($"{historial.Id}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.NumeroPedido}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.FechaPedido}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.EstadoPedido}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.IdUsuario}").Alignment = HorizontalAlignment.Center;

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
                detalleHeaderRow.Cells.Add("Id Historial Ped.").Alignment = HorizontalAlignment.Center;
                detalleHeaderRow.Cells.Add("Id Producto").Alignment = HorizontalAlignment.Center;
                detalleHeaderRow.Cells.Add("Nombre Producto").Alignment = HorizontalAlignment.Center;

                detalleHeaderRow.Cells.Add("Cantidad").Alignment = HorizontalAlignment.Center;

                // Iterar sobre los DetalleHistorialProductos de cada HistorialProducto
                foreach (var detalle in historial.DetalleHistorialPedidos)
                {
                    Aspose.Pdf.Row detalleRow = detalleTable.Rows.Add();

                    detalleRow.Cells.Add($"{detalle.HistorialPedidoId}").Alignment = HorizontalAlignment.Center;
                    detalleRow.Cells.Add($"{detalle.ProductoId}");
                    detalleRow.Cells.Add($"{detalle.Producto?.NombreProducto}");
                    detalleRow.Cells.Add($"{detalle.Cantidad}").Alignment = HorizontalAlignment.Center;
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
                // Obtener todos los registros del historial
                var historialPedidos = await _context.HistorialPedidos.Include(x => x.DetalleHistorialPedidos).ToListAsync();

                if (historialPedidos == null || historialPedidos.Count == 0)
                {
                    return BadRequest("No hay datos en el historial para eliminar");
                }

                // Eliminar todos los registros
                foreach (var historialProducto in historialPedidos)
                {
                    _context.DetalleHistorialPedidos.RemoveRange(historialProducto.DetalleHistorialPedidos);
                    _context.HistorialPedidos.Remove(historialProducto);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Todos los datos del historial se han eliminado con éxito.";
                return RedirectToAction(nameof(HistorialPedidos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar los datos del historial");
                return BadRequest("Error al eliminar los datos del historial, inténtelo de nuevo más tarde. Si el problema persiste, contacte con el administrador");
            }
        }
    }
}
