using GestorInventario.Shared.DTOS.Rembolso;
using GestorInventario.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GestorInventario.Interfaces.Application.Services.Refunds
{
    public interface IRefundService
    {
        Task<OperationResult<string>> ProcesarRembolsoAsync( int pedidoId, string status, string refundId);
        Task<OperationResult<(int pedidoId, decimal precioProducto, string motivo)>> RealizarRembolsoParcial(RefundPartialDto request);
    }
}
