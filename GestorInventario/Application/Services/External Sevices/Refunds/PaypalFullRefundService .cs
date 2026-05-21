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
            var totalConIva = Math.Round(pedido.Data.totalAmount * 1.21m, 2);

            var request = BuildRefundRequest(totalConIva, currency);
            var response = await ExecuteRefundAsync(pedido.Data.captureId, request);

            return OperationResult<(int, string, decimal, string)>.Ok("",
                (pedidoId, response.Id, totalConIva, pedido.Data.orderId));
        }
    }
}
