using GestorInventario.Application.DTOS;
using GestorInventario.ViewModels.Orders;

namespace GestorInventario.Mappers;


    public static class OrderMapper
    {
        // Mapea la orden completa
        public static OrderDetailsViewmodel ToViewModel(this DetalleOrdenDto dto)
        {
            if (dto == null) return null;

            return new OrderDetailsViewmodel
            {
                FechaPedido = dto.FechaPedido,
                NombreCompleto = dto.NombreCompleto,
                NumeroPedido = dto.NumeroPedido,
                EstadoPedido = dto.EstadoPedido,
                Currency = dto.Currency,
                PedidoId = dto.PedidoId,
                PaymentId = dto.PaymentId,
                TotalSinIva = dto.TotalSinIva,
                TotalIva = dto.TotalIva,
                GranTotal = dto.Total,
                // Mapeamos la lista usando el otro método de extensión
                DetallePedido = dto.DetallePedidoDtos?.Select(d => d.ToViewModel()).ToList()
                                ?? new List<DetallePedidoLineaViewModel>()
            };
        }

        // Mapea cada línea del detalle
        public static DetallePedidoLineaViewModel ToViewModel(this DetallePedidoDto dto)
        {
            if (dto == null) return null;

            return new DetallePedidoLineaViewModel
            {
                DetalleId = dto.DetalleId,
                NombreProducto = dto.NombreProducto,
                Descripcion = dto.Descripcion,
                Cantidad = dto.Cantidad,
                PrecioUnitario = dto.PrecioUnitario,
                SubtotalSinIva = dto.SubtotalSinIva,
                Iva = dto.Iva,
                TotalConIva = dto.Total,
                Rembolsado = dto.Rembolsado
            };
        }
    }
