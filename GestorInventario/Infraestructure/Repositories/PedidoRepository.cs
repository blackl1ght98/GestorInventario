using Aspose.Pdf;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        public PedidoRepository(GestorInventarioContext context, IMemoryCache memory, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _cache = memory;
            _contextAccessor = contextAccessor;
        }
        public IQueryable<Pedido> ObtenerPedidos()
        {
            var pedidos = from p in _context.Pedidos.Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation)
                          select p;
            return pedidos;
        }
        public IQueryable<Pedido> ObtenerPedidoUsuario(int userId)
        {
            var pedidos = _context.Pedidos.Where(p => p.IdUsuario == userId)
                            .Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto)
                            .Include(u => u.IdUsuarioNavigation);
            return pedidos;
        }
        public async Task<(bool, string)> CrearPedido(PedidosViewModel model)
        {
            var pedido = new Pedido()
            {
                NumeroPedido = model.NumeroPedido,
                FechaPedido = model.FechaPedido,
                EstadoPedido = model.EstadoPedido,
                IdUsuario = model.IdUsuario,
            };
            _context.AddEntity(pedido);

            var numeroPedidoGenerado = pedido.NumeroPedido;
            var historialPedido = new HistorialPedido()
            {
                IdUsuario = pedido.IdUsuario,
                Fecha = DateTime.Now,
                Accion = _contextAccessor.HttpContext.Request.Method.ToString(),
                Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AddEntity(historialPedido);
            //Obtiene todos los productos que existan en base de datos
            for (var i = 0; i < model.IdsProducto.Count; i++)
            {
                // Si el producto en la posición i fue seleccionado...
                if (model.ProductosSeleccionados[i])
                {
                    // Se crea un nuevo detalle de pedido para el producto seleccionado.
                    var detallePedido = new DetallePedido()
                    {
                        PedidoId = pedido.Id,
                        ProductoId = model.IdsProducto[i],
                        Cantidad = model.Cantidades[i],
                    };
                    // Se agrega el detalle del pedido al contexto de la base de datos.
                    _context.AddEntity(detallePedido);
                }
            }
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
                    _context.AddEntity(detalleHistorialPedido);
                }
            }
            return (true, null);
        }

        public async Task<List<Producto>> ObtenerProductos()
        {
            var producto = await _context.Productos.ToListAsync();
            return producto;
        }
        public async Task<List<Usuario>> ObtenerUsuarios()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            return usuarios;
        }
        public async Task<Pedido> ObtenerPedidoEliminacion(int id)
        {
            var pedido = await _context.Pedidos
                  .Include(p => p.DetallePedidos)
                      .ThenInclude(dp => dp.Producto)
                  .Include(p => p.IdUsuarioNavigation)
                  .FirstOrDefaultAsync(m => m.Id == id);
            return pedido;
        }
        public async Task<(bool, string)> EliminarPedido(int Id)
        {
            var pedido = await _context.Pedidos.Include(p => p.DetallePedidos).FirstOrDefaultAsync(m => m.Id == Id);
            if (pedido == null)
            {
                return (false, "No hay pedido a eliminar");
            }
            if (pedido.EstadoPedido != "Entregado" && pedido.DetallePedidos.Any())
            {
                return (false, "El pedido tiene que tener el estado Entregado para ser eliminado y no tener historial asociado");
               
            }
            else
            {
                _context.DeleteRangeEntity(pedido.DetallePedidos);
                _context.DeleteEntity(pedido);
            }

            return (true, null);
        }
        public async Task<HistorialPedido> EliminarHistorialPorId(int id)
        {
            var historialPedido = await _context.HistorialPedidos.Include(x => x.DetalleHistorialPedidos).FirstOrDefaultAsync(x => x.Id == id);
            return historialPedido;
        }
        public async Task<(bool, string)> EliminarHistorialPorIdDefinitivo(int Id)
        {
            var historialPedido = await _context.HistorialPedidos.Include(x => x.DetalleHistorialPedidos).FirstOrDefaultAsync(x => x.Id == Id);
            if (historialPedido != null)
            {
                _context.DeleteRangeEntity(historialPedido.DetalleHistorialPedidos);
                _context.DeleteEntity(historialPedido);
            }
            else
            {
                return (false, "No se puede eliminar, el historial no existe");
            }
            return (true, null);
        }
        public async Task<Pedido> ObtenerPedidoId(int id)
        {
            var pedido = await _context.Pedidos
              .FirstOrDefaultAsync(x => x.Id == id);
            return pedido;
        }
        public async Task<(bool, string)> EditarPedido(EditPedidoViewModel model)
        {
            var existeUsuario = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId;
            if (int.TryParse(existeUsuario, out usuarioId))
            {
                var pedidoOriginal = await _context.Pedidos
                   .Include(p => p.DetallePedidos)
                   .FirstOrDefaultAsync(x => x.Id == model.id);
                if (pedidoOriginal == null)
                {
                    return (false, "Pedido no encontrado, no es posible editar un pedido que no existe");
                }
                pedidoOriginal.FechaPedido = model.fechaPedido;
                pedidoOriginal.EstadoPedido = model.estadoPedido;
                _context.UpdateEntity(pedidoOriginal);
                // Crear un nuevo registro en el historial de pedidos
                var historialPedido = new HistorialPedido
                {
                    IdUsuario = usuarioId,
                    Fecha = DateTime.Now,
                    Accion = _contextAccessor.HttpContext.Request.Method.ToString(),
                    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AddEntity(historialPedido);
                foreach (var detalleOriginal in pedidoOriginal.DetallePedidos)
                {
                    var nuevoDetalle = new DetalleHistorialPedido
                    {
                        HistorialPedidoId = historialPedido.Id,
                        ProductoId = detalleOriginal.ProductoId,
                        Cantidad = detalleOriginal.Cantidad
                    };
                    _context.AddEntity(nuevoDetalle);
                }
                return (true, null);
            }
            return (false, "Hubo un error al editar el pedido");
        }
        public IQueryable<HistorialPedido> ObtenerPedidosHistorial()
        {
            var pedidos = from p in _context.HistorialPedidos.Include(dp => dp.DetalleHistorialPedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation)
                          select p;
            return pedidos;
        }
        public IQueryable<HistorialPedido> ObtenerPedidosHistorialUsuario(int usuarioId)
        {
            var pedidos = _context.HistorialPedidos.Where(p => p.IdUsuario == usuarioId)
                        .Include(dp => dp.DetalleHistorialPedidos).ThenInclude(p => p.Producto)
                        .Include(u => u.IdUsuarioNavigation);
            return pedidos;
        }
        public async Task<HistorialPedido> DetallesHistorial(int id)
        {
            var pedido = await _context.HistorialPedidos
              .Include(p => p.DetalleHistorialPedidos)
                  .ThenInclude(dp => dp.Producto)
              .Include(p => p.IdUsuarioNavigation)
              .FirstOrDefaultAsync(p => p.Id == id);
            return pedido;
        }
        public async Task<(bool, string, byte[])> DescargarPDF()
        {
            var historialPedido = await _context.HistorialPedidos
            .Include(hp => hp.DetalleHistorialPedidos)
            .ThenInclude(dp => dp.Producto)
            .ToListAsync();
            if (historialPedido == null || historialPedido.Count == 0)
            {
                return (false, "Datos de pedidos no encontrados", null);
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
            headerRow.Cells.Add("Accion").Alignment = HorizontalAlignment.Center;
            headerRow.Cells.Add("Ip").Alignment = HorizontalAlignment.Center;
            headerRow.Cells.Add("Id Usuario").Alignment = HorizontalAlignment.Center;
            headerRow.Cells.Add("Fecha").Alignment = HorizontalAlignment.Center;

            // Agregar contenido de mediciones a la tabla
            foreach (var historial in historialPedido)
            {

                Aspose.Pdf.Row dataRow = table.Rows.Add();
                Aspose.Pdf.Text.TextFragment textFragment1 = new Aspose.Pdf.Text.TextFragment("");
                page.Paragraphs.Add(textFragment1);
                dataRow.Cells.Add($"{historial.Id}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.Accion}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.Ip}").Alignment = HorizontalAlignment.Center;
                dataRow.Cells.Add($"{historial.IdUsuario}").Alignment = HorizontalAlignment.Center; 
                dataRow.Cells.Add($"{historial.Fecha}").Alignment = HorizontalAlignment.Center;

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
            return (true, null, bytes);
        }
        public async Task<(bool,string)> EliminarHitorial()
        {
            var historialPedidos = await _context.HistorialPedidos.Include(x => x.DetalleHistorialPedidos).ToListAsync();
            if (historialPedidos == null || historialPedidos.Count == 0)
            {
                return (false, "No hay datos en el historial para eliminar");
            }
            // Eliminar todos los registros
            foreach (var historialProducto in historialPedidos)
            {
                _context.DeleteRangeEntity(historialProducto.DetalleHistorialPedidos);
                _context.DeleteEntity(historialProducto);

            }
            var detallePedidos = await _context.DetallePedidos.ToListAsync();
            foreach (var detallePedido in detallePedidos)
            {
                _context.DeleteEntity(detallePedido);
            }
            return (true,null);
        }
    }
}

