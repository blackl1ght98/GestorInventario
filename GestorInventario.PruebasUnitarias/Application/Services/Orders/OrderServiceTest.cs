using GestorInventario.Application.Services.Orders;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Web;
using GestorInventario.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using GestorInventario.Shared.Utilities;
using GestorInventario.Domain.enums.Pedido;

namespace GestorInventario.PruebasUnitarias.Application.Services.Orders
{
    public class OrderServiceTest
    {
        private readonly Mock<IPedidoRepository> _repositoryMock;
        private readonly Mock<ILogger<OrderService>> _loggerMock;
        private readonly Mock<ICurrentUserAccessor> _userAccessorMock;
        private readonly Mock<IPaypalOrderService> _paypalOrderServiceMock;
        private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
        private readonly Mock<IPaypalRepository> _paypalRepositoryMock;
        private readonly OrderService _sut;
        public OrderServiceTest() { 
        _repositoryMock = new Mock<IPedidoRepository>();
            _loggerMock = new Mock<ILogger<OrderService>>();
            _userAccessorMock = new Mock<ICurrentUserAccessor>();
            _paypalOrderServiceMock = new Mock<IPaypalOrderService>();
            _paymentRepositoryMock= new Mock<IPaymentRepository>();
            _paypalRepositoryMock = new Mock<IPaypalRepository>();
            _sut = new OrderService(_loggerMock.Object,_repositoryMock.Object,_userAccessorMock.Object,_paypalOrderServiceMock.Object,_paymentRepositoryMock.Object,_paypalRepositoryMock.Object);
        }
        [Fact]
        public async Task EliminarPedido_CarritoSinCapturas_EliminaYRetornaOk()
        {
            var pedido = new Pedido
            {
                Id = 1,
                EstadoPedido = EstadoPedido.Carrito.ToString(),
                EsCarrito = true,
                DetallePedidos = null,
                PayPalPaymentCaptures = new List<PayPalPaymentCapture>()
            };

            _repositoryMock.Setup(r => r.ObtenerPedidoConDetallesAsync(pedido.Id))
                .ReturnsAsync(pedido);
            _repositoryMock.Setup(r => r.EliminarCarritoAsync(pedido))
                .ReturnsAsync(OperationResult<string>.Ok());

            var resultado = await _sut.EliminarPedido(pedido.Id);

            Assert.True(resultado.IsSuccess);
            _repositoryMock.Verify(r => r.EliminarCarritoAsync(pedido), Times.Once);
        }
        [Fact]
        public async Task EliminarPedido_EstadoNoEsCarrito_RetornaFail()
        {
            var pedido = new Pedido
            {
                Id = 1,
                EstadoPedido = EstadoPedido.Enviado.ToString(),
                EsCarrito = true,
                DetallePedidos = null,
                PayPalPaymentCaptures = new List<PayPalPaymentCapture>()
            };

            _repositoryMock.Setup(r => r.ObtenerPedidoConDetallesAsync(pedido.Id))
                .ReturnsAsync(pedido);

            var resultado = await _sut.EliminarPedido(pedido.Id);

            Assert.False(resultado.IsSuccess);
            _repositoryMock.Verify(r => r.EliminarCarritoAsync(It.IsAny<Pedido>()), Times.Never);
        }

        [Fact]
        public async Task EliminarPedido_CarritoConCapturasPayPal_RetornaFail()
        {
            var pedido = new Pedido
            {
                Id = 1,
                EstadoPedido = EstadoPedido.Carrito.ToString(),
                EsCarrito = true,
                DetallePedidos = null,
                PayPalPaymentCaptures = new List<PayPalPaymentCapture>
                {
                    new PayPalPaymentCapture()
                }
            };

            _repositoryMock.Setup(r => r.ObtenerPedidoConDetallesAsync(pedido.Id))
                .ReturnsAsync(pedido);

            var resultado = await _sut.EliminarPedido(pedido.Id);

            Assert.False(resultado.IsSuccess);
            _repositoryMock.Verify(r => r.EliminarCarritoAsync(It.IsAny<Pedido>()), Times.Never);
        }

        [Fact]
        public async Task EliminarPedido_PedidoNoExiste_RetornaFail()
        {
            _repositoryMock.Setup(r => r.ObtenerPedidoConDetallesAsync(It.IsAny<int>()))
                .ReturnsAsync((Pedido)null);

            var resultado = await _sut.EliminarPedido(999);

            Assert.False(resultado.IsSuccess);
        }
    }
}
