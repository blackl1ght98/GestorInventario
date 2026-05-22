using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Infraestructure.Repositories;

namespace GestorInventario.Application.Services.External_Sevices.Refunds
{
    public class PaypalFullRefundService : PaypalRefundBaseService, IPaypalFullRefundService
    {
        private readonly IPaypalOrderService _orderService;

        public PaypalFullRefundService(
            ILogger<PaypalFullRefundService> logger,
            IPayPalHttpClient paypal,
            IPedidoRepository pedidoRepository,
            IPaypalOrderService orderService) : base(logger, paypal, pedidoRepository)
        {
            _orderService = orderService;
        }

        public async Task<OperationResult<(int pedidoId, string refundId, decimal totalAmount, string orderId)>> 
            RefundSaleAsync(int pedidoId, string currency)
        {
            var pedido = await _pedidoRepository.GetPedidoWithDetailsAsync(pedidoId);

           
            var totalReembolso = pedido.Data.total;

            _logger.LogInformation(
                "Reembolso total pedido {PedidoId} -> Subtotal:{Subtotal} IVA:{Iva} Total:{Total}",
                pedidoId, pedido.Data.subtotal, pedido.Data.iva, totalReembolso);

            if (totalReembolso < 0)
            {
                return OperationResult<(int, string, decimal, string)>.Fail("El total del pedido no puede ser negativo.");
            }

            var request = BuildRefundRequest(totalReembolso, currency);
            var response = await ExecuteRefundAsync(pedido.Data.captureId, request);

            return OperationResult<(int, string, decimal, string)>.Ok("",
                (pedidoId, response.Id, totalReembolso, pedido.Data.orderId));
        }
    }
}
