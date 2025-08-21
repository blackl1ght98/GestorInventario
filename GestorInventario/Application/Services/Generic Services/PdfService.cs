using Aspose.Pdf;
using Aspose.Pdf.Text;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Services
{
    public class PdfService:IPdfService
    {
        private readonly GestorInventarioContext _context;

        public PdfService(GestorInventarioContext context)
        {
            _context = context;
        }
        public async Task<(bool, string, byte[])> GenerarReporteHistorialPedidosAsync()
        {
            var historialPedido = await _context.HistorialPedidos
                .Include(hp => hp.DetalleHistorialPedidos)
                .ThenInclude(dp => dp.Producto)
                .OrderByDescending(h => h.Fecha)
                .ToListAsync();

            if (historialPedido == null || historialPedido.Count == 0)
            {
                return (false, "Datos de pedidos no encontrados", null);
            }

            // Crear documento PDF
            Document document = new Document();

            // Configurar página
            Page page = document.Pages.Add();
            page.PageInfo.Margin = new MarginInfo(20, 20, 30, 20);
            page.PageInfo.IsLandscape = true;

            // Agregar título principal
            TextFragment titulo = new TextFragment("REPORTE DE HISTORIAL DE PEDIDOS");
            titulo.TextState.FontSize = 16;
            titulo.TextState.FontStyle = FontStyles.Bold;
            titulo.TextState.ForegroundColor = Color.DarkBlue;
            titulo.HorizontalAlignment = HorizontalAlignment.Center;
            titulo.Margin = new MarginInfo(0, 0, 0, 20);
            page.Paragraphs.Add(titulo);

            // Crear tabla principal
            Aspose.Pdf.Table table = new Aspose.Pdf.Table();
            table.VerticalAlignment = VerticalAlignment.Top;
            table.Alignment = HorizontalAlignment.Left;
            table.DefaultCellBorder = new BorderInfo(BorderSide.None, 0.1F);
            table.Border = new BorderInfo(BorderSide.None, 1F);
            table.ColumnWidths = "15% 15% 15% 15% 20% 20%";
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            page.Paragraphs.Add(table);

            // Agregar fila de encabezado a la tabla principal
            Aspose.Pdf.Row headerRow = table.Rows.Add();
            AddHeaderCell(headerRow, "Id");
            AddHeaderCell(headerRow, "Acción");
            AddHeaderCell(headerRow, "IP");
            AddHeaderCell(headerRow, "Id Usuario");
            AddHeaderCell(headerRow, "Fecha");
            AddHeaderCell(headerRow, "Detalles");

            // Agregar contenido a la tabla principal
            foreach (var historial in historialPedido)
            {
                Aspose.Pdf.Row dataRow = table.Rows.Add();

                // Celdas de datos principales
                AddDataCell(dataRow, historial.Id.ToString(), HorizontalAlignment.Center);
                AddDataCell(dataRow, historial.Accion, HorizontalAlignment.Center);
                AddDataCell(dataRow, historial.Ip, HorizontalAlignment.Center);
                AddDataCell(dataRow, historial.IdUsuario.ToString(), HorizontalAlignment.Center);
                AddDataCell(dataRow, historial.Fecha.ToString(), HorizontalAlignment.Center); // Formato corregido

                // Celda para detalles (contendrá la tabla anidada)
                Cell detalleCell = dataRow.Cells.Add();
                detalleCell.Alignment = HorizontalAlignment.Left;
                detalleCell.VerticalAlignment = VerticalAlignment.Top;
                detalleCell.DefaultCellTextState = new TextState()
                {
                    FontSize = 8,
                    ForegroundColor = Color.DarkSlateGray
                };

                if (historial.DetalleHistorialPedidos.Any())
                {
                    // Crear tabla de detalles dentro de la celda
                    Aspose.Pdf.Table detalleTable = new Aspose.Pdf.Table();
                    detalleTable.DefaultCellBorder = new BorderInfo(BorderSide.All, 0.5F, Color.LightGray);
                    detalleTable.Border = new BorderInfo(BorderSide.All, 1F, Color.DarkBlue);
                    detalleTable.ColumnWidths = "20% 25% 15% 20% 20%";
                    detalleTable.DefaultCellPadding = new MarginInfo(3, 3, 3, 3);
                    detalleTable.Margin = new MarginInfo(5, 5, 5, 5);

                    // Encabezados de la tabla de detalles
                    Aspose.Pdf.Row detalleHeaderRow = detalleTable.Rows.Add();
                    AddDetailHeaderCell(detalleHeaderRow, "ID Producto");
                    AddDetailHeaderCell(detalleHeaderRow, "Producto");
                    AddDetailHeaderCell(detalleHeaderRow, "Cantidad");
                    AddDetailHeaderCell(detalleHeaderRow, "Estado");
                    AddDetailHeaderCell(detalleHeaderRow, "N° Pedido");

                    // Datos de detalles
                    foreach (var detalle in historial.DetalleHistorialPedidos)
                    {
                        Aspose.Pdf.Row detalleRow = detalleTable.Rows.Add();
                        AddDetailCell(detalleRow, detalle.ProductoId.ToString(), HorizontalAlignment.Center);
                        AddDetailCell(detalleRow, detalle.Producto?.NombreProducto ?? "N/A", HorizontalAlignment.Left);
                        AddDetailCell(detalleRow, detalle.Cantidad.ToString(), HorizontalAlignment.Center);
                        AddDetailCell(detalleRow, detalle.EstadoPedido ?? "N/A", HorizontalAlignment.Center);
                        AddDetailCell(detalleRow, detalle.NumeroPedido ?? "N/A", HorizontalAlignment.Center);
                    }

                    detalleCell.Paragraphs.Add(detalleTable);
                }
                else
                {
                    // CORRECCIÓN: Usar Paragraphs.Add en lugar de Add directamente
                    TextFragment sinDetalles = new TextFragment("Sin detalles");
                    sinDetalles.TextState.FontSize = 8;
                    sinDetalles.TextState.ForegroundColor = Color.Gray;
                    sinDetalles.TextState.FontStyle = FontStyles.Italic;
                    detalleCell.Paragraphs.Add(sinDetalles);
                }
            }

            // Agregar pie de página
            TextFragment footer = new TextFragment($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm} - Total registros: {historialPedido.Count}");
            footer.TextState.FontSize = 9;
            footer.TextState.FontStyle = FontStyles.Italic;
            footer.TextState.ForegroundColor = Color.Gray;
            footer.HorizontalAlignment = HorizontalAlignment.Right;
            footer.Margin = new MarginInfo(0, 20, 10, 0);
            page.Paragraphs.Add(footer);

            // Guardar en memoria
            using (MemoryStream memoryStream = new MemoryStream())
            {
                document.Save(memoryStream);
                return (true, null, memoryStream.ToArray());
            }
        }

        // Métodos auxiliares para estilos
        private void AddHeaderCell(Row row, string text)
        {
            Cell cell = row.Cells.Add(text);
            cell.BackgroundColor = Color.DarkBlue;
            cell.DefaultCellTextState = new TextState()
            {
                FontSize = 10,
                FontStyle = FontStyles.Bold,
                ForegroundColor = Color.White
            };
            cell.Alignment = HorizontalAlignment.Center;
            cell.VerticalAlignment = VerticalAlignment.Center;
        }

        private void AddDataCell(Row row, string text, HorizontalAlignment alignment)
        {
            Cell cell = row.Cells.Add(text);
            cell.DefaultCellTextState = new TextState()
            {
                FontSize = 9,
                ForegroundColor = Color.Black
            };
            cell.Alignment = alignment;
            cell.VerticalAlignment = VerticalAlignment.Center;
        }

        private void AddDetailHeaderCell(Row row, string text)
        {
            Cell cell = row.Cells.Add(text);
            cell.BackgroundColor = Color.LightSteelBlue;
            cell.DefaultCellTextState = new TextState()
            {
                FontSize = 8,
                FontStyle = FontStyles.Bold,
                ForegroundColor = Color.DarkBlue
            };
            cell.Alignment = HorizontalAlignment.Center;
            cell.VerticalAlignment = VerticalAlignment.Center;
        }

        private void AddDetailCell(Row row, string text, HorizontalAlignment alignment)
        {
            Cell cell = row.Cells.Add(text);
            cell.DefaultCellTextState = new TextState()
            {
                FontSize = 7,
                ForegroundColor = Color.DarkSlateGray
            };
            cell.Alignment = alignment;
            cell.VerticalAlignment = VerticalAlignment.Center;
        }
        public async Task<(bool, string, byte[])> DescargarProductoPDF()
        {
            var historialProductos = await _context.HistorialProductos
                .Include(hp => hp.DetalleHistorialProductos)

                .ToListAsync();
            if (historialProductos == null || historialProductos.Count == 0)
            {
                return (false, "No hay productos a mostrar", null);
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
    }
}
