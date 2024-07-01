using Aspose.Pdf;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IGestorArchivos _gestorArchivos;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<ProductoRepository> _logger;
        public ProductoRepository(GestorInventarioContext context, IGestorArchivos gestorArchivos, IHttpContextAccessor contextAccessor, ILogger<ProductoRepository> logger)
        {
            _context = context;
            _gestorArchivos = gestorArchivos;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }
        public IQueryable<Producto> ObtenerTodoProducto()
        {
            var productos = from p in _context.Productos.Include(x => x.IdProveedorNavigation)
                            orderby p.Id
                            select p;
            return productos;
        }
        public async Task<List<Producto>> ObtenerTodos()
        {
            var productos = await _context.Productos.ToListAsync();
            return productos;
        }
        public async Task<Producto> CrearProducto(ProductosViewModel model)
        {
            try
            {
                var producto = new Producto()
                {
                    NombreProducto = model.NombreProducto,
                    Descripcion = model.Descripcion,
                    Imagen = "",
                    Cantidad = model.Cantidad,
                    Precio = model.Precio,
                    FechaCreacion = DateTime.Now,
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
                _context.AddEntity(producto);
                return producto;

            }
            catch (Exception ex)
            {

                _logger.LogError("Error al crear el producto", ex);
                return null;
            }

        }
        public async Task<HistorialProducto> CrearHistorial(int usuarioId, Producto producto)
        {
            try
            {
                var historialProducto = new HistorialProducto()
                {
                    UsuarioId = usuarioId,
                    Fecha = DateTime.Now,
                    Accion = _contextAccessor.HttpContext.Request.Method.ToString(),
                    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                _context.AddEntity(historialProducto);
                return historialProducto;
            }
            catch (Exception ex)
            {

                _logger.LogError("Error al crear el historial para el producto", ex);
                return null;
            }

        }
        public async Task<DetalleHistorialProducto> CrearDetalleHistorial(HistorialProducto historialProducto, Producto producto)
        {
            try
            {
                var detalleHistorialProducto = new DetalleHistorialProducto
                {
                    HistorialProductoId = historialProducto.Id,
                    Cantidad = producto.Cantidad,
                    NombreProducto = producto.NombreProducto,
                    Descripcion = producto.Descripcion,
                    Precio = producto.Precio
                };

                // Guardar el detalle del historial de productos
                _context.AddEntity(detalleHistorialProducto);
                return detalleHistorialProducto;
            }
            catch (Exception ex)
            {

                _logger.LogError("Error al crear el detalle del producto en el historial", ex);
                return null;
            }

        }
        public async Task<List<Proveedore>> ObtenerProveedores()
        {
            var proveedores = await _context.Proveedores.ToListAsync();
            return proveedores;
        }
        public async Task<Producto> EliminarProductoObtencion(int id)
        {
            var producto = await _context.Productos.Include(p => p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == id);
            return producto;
        }

  
        public async Task<(bool, string)> EliminarProducto(int Id)
        {
          
                var producto = await _context.Productos
          .Include(p => p.DetallePedidos)
              .ThenInclude(dp => dp.Pedido)
          .Include(p => p.IdProveedorNavigation).Include(x=>x.ItemsDelCarritos)
          .FirstOrDefaultAsync(m => m.Id == Id);
                if (producto == null )
                {
                    return (false, "No hay productos para eliminar");
                }
            if (producto.ItemsDelCarritos.Count()!=0) 
            {
                return (false, "el producto no se puede eliminar porque esta en el carrito");
            }
              
               
               
               
                _context.DeleteEntity(producto);

                return (true, null);
            
        }

    

        public async Task<Producto> ObtenerPorId(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == id);
            return producto;
        }
        public bool ProductoExist(int Id)
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
        public async Task<IQueryable<HistorialProducto>> ObtenerTodoHistorial()
        {
            var historialProductos = from p in _context.HistorialProductos.Include(x => x.DetalleHistorialProductos)
                                     select p;
            return historialProductos;
        }
        public async Task<(bool, string, byte[])> DescargarPDF()
        {
            var historialProductos = await _context.HistorialProductos
                .Include(hp => hp.DetalleHistorialProductos)
           
                .ToListAsync();
            if(historialProductos==null || historialProductos.Count == 0)
            {
                return (false, "No hay productos a mostrar",null);
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
                detalleTable.ColumnWidths = "100 60 60"; // Ancho de cada columna

                // Agregar la segunda tabla a la página
                page.Paragraphs.Add(detalleTable);
                Aspose.Pdf.Text.TextFragment textFragment = new Aspose.Pdf.Text.TextFragment("");
                page.Paragraphs.Add(textFragment);
                // Agregar fila de encabezado a la segunda tabla
                Aspose.Pdf.Row detalleHeaderRow = detalleTable.Rows.Add();
                detalleHeaderRow.Cells.Add("NombreProducto").Alignment = HorizontalAlignment.Center;
                detalleHeaderRow.Cells.Add("Descripcion").Alignment = HorizontalAlignment.Center;
                detalleHeaderRow.Cells.Add("IdHistorial").Alignment = HorizontalAlignment.Center;
              
                detalleHeaderRow.Cells.Add("Cantidad").Alignment = HorizontalAlignment.Center;
                detalleHeaderRow.Cells.Add("Precio").Alignment = HorizontalAlignment.Center;

                // Iterar sobre los DetalleHistorialProductos de cada HistorialProducto
                foreach (var detalle in historial.DetalleHistorialProductos)
                {
                    Aspose.Pdf.Row detalleRow = detalleTable.Rows.Add();

                    detalleRow.Cells.Add($"{detalle.NombreProducto}").Alignment = HorizontalAlignment.Center;
                    detalleRow.Cells.Add($"{detalle.Descripcion}").Alignment = HorizontalAlignment.Center;
                    detalleRow.Cells.Add($"{detalle.HistorialProductoId}").Alignment = HorizontalAlignment.Center;
                    
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
            return (true, null, bytes);
        }
        public async Task<HistorialProducto> HistorialProductoPorId(int id)
        {
            var historialProducto = await _context.HistorialProductos
                   .Include(hp => hp.DetalleHistorialProductos)
                       

                   .FirstOrDefaultAsync(hp => hp.Id == id);
            return historialProducto;
        }
        public async Task<HistorialProducto> EliminarHistorialPorId(int id)
        {
            var historialProducto = await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).FirstOrDefaultAsync(x => x.Id == id);
            return historialProducto;
        }
        public async Task<(bool, string)> EliminarHistorialPorIdDefinitivo(int Id)
        {
            var historialProducto = await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).FirstOrDefaultAsync(x => x.Id == Id);
            if (historialProducto != null)
            {
                _context.DeleteRangeEntity(historialProducto.DetalleHistorialProductos);
                _context.DeleteEntity(historialProducto);
            }
            else
            {
                return (false, "No se puede eliminar, el historial no existe");
            }
            return (true, null);
        }
        public async Task<List<HistorialProducto>> EliminarTodoHistorial()
        {
            var historialProductos = await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).ToListAsync();
            if (historialProductos != null)
            {
                // Eliminar todos los registros
                foreach (var historialProducto in historialProductos)
                {
                    _context.DeleteRangeEntity(historialProducto.DetalleHistorialProductos);
                    _context.DeleteEntity(historialProducto);
                }
            }
            return historialProductos;

        }
        public async Task<(bool, string)> EditarProducto(ProductosViewModel model, int usuarioId)
        {
            try
            {

                var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (producto == null)
                {
                    return (false, "Producto no encontrado");
                }
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
                var historialProductoPostActualizacion = new HistorialProducto
                {
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    Accion = "Antes-PUT",
                    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AddEntity(historialProductoPostActualizacion);
                var detalleHistorialProductoPostActualizacion = new DetalleHistorialProducto
                {
                    HistorialProductoId = historialProductoPostActualizacion.Id,
                   
                    Cantidad = productoOriginal.Cantidad,
                    NombreProducto = productoOriginal.NombreProducto,
                    Descripcion= productoOriginal.Descripcion,
                    Precio = productoOriginal.Precio,
                };
                _context.AddEntity(detalleHistorialProductoPostActualizacion);
                producto.NombreProducto = model.NombreProducto;
                producto.FechaModificacion = DateTime.Now;
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

                _context.UpdateEntity(producto);
                var historialProducto = new HistorialProducto
                {
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    Accion = "Despues-PUT",
                    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AddEntity(historialProducto);
                var detalleHistorialProducto = new DetalleHistorialProducto
                {
                    HistorialProductoId = historialProducto.Id,
                  
                    Cantidad = producto.Cantidad,
                    NombreProducto = producto.NombreProducto,
                    Descripcion = producto.Descripcion,
                    Precio = producto.Precio,
                };
                _context.AddEntity(detalleHistorialProducto);
                return (true, null);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (producto == null)
                {
                    return (false, "Producto no encontrado");
                }
                _context.ReloadEntity(producto);
                _context.EntityModified(producto);

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
                var historialProductoPostActualizacion = new HistorialProducto
                {
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    Accion = "Antes-PUT",
                    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AddEntity(historialProductoPostActualizacion);
                var detalleHistorialProductoPostActualizacion = new DetalleHistorialProducto
                {
                    HistorialProductoId = historialProductoPostActualizacion.Id,
                    
                    Cantidad = productoOriginal.Cantidad,
                    NombreProducto = productoOriginal.NombreProducto,
                    Precio = productoOriginal.Precio,
                };
                _context.AddEntity(detalleHistorialProductoPostActualizacion);
                producto.NombreProducto = model.NombreProducto;
                producto.FechaModificacion = DateTime.Now;
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

                _context.UpdateEntity(producto);
                var historialProducto = new HistorialProducto
                {
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    Accion = "Despues-PUT",
                    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AddEntity(historialProducto);
                var detalleHistorialProducto = new DetalleHistorialProducto
                {
                    HistorialProductoId = historialProducto.Id,
                 
                    Cantidad = producto.Cantidad,
                    NombreProducto = producto.NombreProducto,
                    Precio = producto.Precio,
                };
                _context.AddEntity(detalleHistorialProducto);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error con la ectualizacion", ex);

                return (false, "Error al actualizar");
            }

        }
        public async Task<(bool, string)> AgregarProductosCarrito(int idProducto, int cantidad, int usuarioId)
        {
            var carrito = await _context.Carritos.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
            if (carrito == null)
            {
                carrito = new Carrito
                {
                    UsuarioId = usuarioId,
                    FechaCreacion = DateTime.Now
                };
                _context.AddEntity(carrito);

            }
            var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == idProducto);
            if (producto != null)
            {
                if (producto.Cantidad < cantidad)
                {
                    return (false, "No hay suficientes productos en stock");
                }

                var itemCarrito = _context.ItemsDelCarritos.FirstOrDefault(i => i.CarritoId == carrito.Id && i.ProductoId == idProducto);
                if (itemCarrito == null)
                {
                    itemCarrito = new ItemsDelCarrito
                    {
                        ProductoId = idProducto,
                        Cantidad = cantidad,
                        CarritoId = carrito.Id,
                    };
                    _context.AddEntity(itemCarrito);
                }
                else
                {
                    itemCarrito.Cantidad = cantidad;
                    _context.UpdateEntity(itemCarrito);
                }
                producto.Cantidad -= cantidad;
                _context.UpdateEntity(producto);
            }
            return (true, null);
        }
    }
}
