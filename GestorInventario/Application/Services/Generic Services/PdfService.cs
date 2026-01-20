
using GestorInventario.Application.Services.Generic_Services;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GestorInventario.Application.Services
{
    public class PdfService:IPdfService
    {
        private readonly GestorInventarioContext _context;

        public PdfService(GestorInventarioContext context)
        {
            _context = context;
        }
        public async Task<OperationResult<byte[]>> GenerarPDF()
        {
            var historialPedido = await _context.HistorialPedidos
                .Include(hp => hp.DetalleHistorialPedidos)
                .ThenInclude(dp => dp.Producto)
                .OrderByDescending(h => h.Fecha)
                .ToListAsync();

            if (historialPedido == null || historialPedido.Count == 0)
                return OperationResult<byte[]>.Fail("No hay historial de pedidos");

            var document = new HistorialPedidosPdf(historialPedido);

            var pdfBytes = document.GeneratePdf();
            return OperationResult<byte[]>.Ok("", pdfBytes);
        }


        public async Task<OperationResult<byte[]>> DescargarProductoPDF()
        {
            var historialProductos = await _context.HistorialProductos
                .Include(hp => hp.DetalleHistorialProductos)
                .ToListAsync();

            if (historialProductos == null || historialProductos.Count == 0)
                return OperationResult<byte[]>.Fail("No hay productos a mostrar");

            // Crear documento QuestPDF
            var document = new HistorialProductosPdf(historialProductos);

            // Generar PDF en memoria
            byte[] pdfBytes = document.GeneratePdf();

            return OperationResult<byte[]>.Ok("", pdfBytes);
        }

    }
}
