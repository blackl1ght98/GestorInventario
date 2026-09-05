using GestorInventario.Application.Services.Orders;
using GestorInventario.Application.Services.Refunds;
using GestorInventario.Domain.enums.Paypal;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Refunds;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Web;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace GestorInventario.PruebasUnitarias.Application.Services.Refunds
{
    public class RefundServiceTest
    {
        private readonly Mock<IPedidoRepository> _repositoryMock;
        private readonly Mock<IPaypalRepository> _paypalRepositoryMock;
        private readonly Mock<ICurrentUserAccessor> _userAccessorMock;
        private readonly Mock<ILogger<RefundService>> _loggerMock;
        private readonly Mock<IPaypalOrderService> _paypalOrderServiceMock;
        private readonly Mock<IPaypalRefundService> _paypalRefundService;
        private readonly RefundService _sut;
        public RefundServiceTest() {
            _repositoryMock = new Mock<IPedidoRepository>();
            _paypalRepositoryMock = new Mock<IPaypalRepository>();
            _userAccessorMock = new Mock<ICurrentUserAccessor>();
            _paypalOrderServiceMock = new Mock<IPaypalOrderService>();
            _paypalRefundService = new Mock<IPaypalRefundService>();
        _sut= new RefundService(_repositoryMock.Object,_userAccessorMock.Object,_paypalRepositoryMock.Object,_paypalOrderServiceMock.Object,_paypalRefundService.Object,_loggerMock.Object);
        
        }


        [Fact]
        public async Task ProcesarRembolso_PedidoNoExiste_RetornaFail()
        {
            _repositoryMock.Setup(r => r.ObtenerPedidoConDetallesAsync(999))
                .ReturnsAsync((Pedido)null);

            var resultado = await _sut.ProcesarRembolsoAsync(999, "Reembolsado", "REF-001");

            Assert.False(resultado.IsSuccess);
            Assert.Contains("999", resultado.Message);
            _repositoryMock.Verify(r => r.ActualizarPedidoAsync(It.IsAny<Pedido>()), Times.Never);
            _paypalRepositoryMock.Verify(r => r.ObtenRembolsoAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProcesarRembolso_SinDetallePedidos_NoLanzaExcepcion()
        {
            var pedido = new Pedido
            {
                Id = 1,
                NumeroPedido = "1001",
                EstadoPedido = EstadoPedido.Pagado.ToString(),
                DetallePedidos = null, // <-- el caso edge que detectaste
                Total = 50m,
                Currency = "EUR"
            };

            _repositoryMock.Setup(r => r.ObtenerPedidoConDetallesAsync(1))
                .ReturnsAsync(pedido);
            _repositoryMock.Setup(r => r.ActualizarPedidoAsync(It.IsAny<Pedido>()))
                 .Returns(Task.FromResult(OperationResult<Pedido>.Ok()));
            _paypalRepositoryMock.Setup(r => r.ObtenRembolsoAsync("1001"))
                .ReturnsAsync((Rembolso)null);
            _paypalRepositoryMock.Setup(r => r.AgregarRembolsoAsync(It.IsAny<Rembolso>()))
                 .Returns(Task.FromResult(OperationResult<Rembolso>.Ok()));
            _userAccessorMock.Setup(u => u.GetCurrentUserId()).Returns(7);

            var resultado = await _sut.ProcesarRembolsoAsync(1, "Reembolsado", "REF-001");

            Assert.True(resultado.IsSuccess);
            _repositoryMock.Verify(r => r.ActualizarPedidoAsync(pedido), Times.Once);
        }

        [Fact]
        public async Task ProcesarRembolso_RembolsoNuevo_CreaRembolsoYMarcaDetalles()
        {
            var pedido = new Pedido
            {
                Id = 2,
                NumeroPedido = "1002",
                EstadoPedido = EstadoPedido.Pagado.ToString(),
                Total = 100m,
                Currency = "EUR",
                DetallePedidos = new List<DetallePedido>
        {
            new DetallePedido { Id = 10, Rembolsado = false },
            new DetallePedido { Id = 11, Rembolsado = false }
        },
                IdUsuarioNavigation = new Usuario
                {
                    NombreCompleto = "Juan Pérez",
                    Email = "juan@test.com"
                }
            };

            _repositoryMock.Setup(r => r.ObtenerPedidoConDetallesAsync(2))
                .ReturnsAsync(pedido);
            _repositoryMock.Setup(r => r.ActualizarPedidoAsync(It.IsAny<Pedido>()))
                .Returns(Task.FromResult(OperationResult<Pedido>.Ok()));
            _paypalRepositoryMock.Setup(r => r.ObtenRembolsoAsync("1002"))
                .ReturnsAsync((Rembolso)null);
            _paypalRepositoryMock.Setup(r => r.AgregarRembolsoAsync(It.IsAny<Rembolso>()))
                .Returns(Task.FromResult(OperationResult<Rembolso>.Ok()));
            _userAccessorMock.Setup(u => u.GetCurrentUserId()).Returns(5);

            var resultado = await _sut.ProcesarRembolsoAsync(2, "Reembolsado", "REF-NEW");

            Assert.True(resultado.IsSuccess);
            Assert.Equal("Rembolso procesado con éxito", resultado.Message);

            // Verifica que los detalles fueron marcados
            Assert.All(pedido.DetallePedidos, d => Assert.True(d.Rembolsado));

            // Verifica que se creó el rembolso con los datos correctos
            _paypalRepositoryMock.Verify(r => r.AgregarRembolsoAsync(It.Is<Rembolso>(rem =>
                rem.NumeroPedido == "1002" &&
                rem.NombreCliente == "Juan Pérez" &&
                rem.EmailCliente == "juan@test.com" &&
                rem.RefundIdPayPal == "REF-NEW" &&
                rem.MontoRembolsado == 100m &&
                rem.Currency == "EUR" &&
                rem.UsuarioId == 5 &&
                rem.TipoRembolso == TipoRembolso.Total.ToString() &&
                rem.ReembolsoCompletado == true)), Times.Once);
        }

        [Fact]
        public async Task ProcesarRembolso_RembolsoExistente_ActualizaRembolso()
        {
            var pedido = new Pedido
            {
                Id = 3,
                NumeroPedido = "1003",
                EstadoPedido = EstadoPedido.Pagado.ToString(),
                Total = 75m,
                Currency = "USD",
                DetallePedidos = new List<DetallePedido>
        {
            new DetallePedido { Id = 20, Rembolsado = false }
        }
            };

            var rembolsoExistente = new Rembolso
            {
                Id = 99,
                NumeroPedido = "1003",
                EstadoRembolso = EstadoRembolso.EnRevision.ToString(),
                ReembolsoCompletado = false,
                TipoRembolso = TipoRembolso.Parcial.ToString(),
                FechaRembolso = DateTime.UtcNow.AddDays(-1)
            };

            _repositoryMock.Setup(r => r.ObtenerPedidoConDetallesAsync(3))
                .ReturnsAsync(pedido);
            _repositoryMock.Setup(r => r.ActualizarPedidoAsync(It.IsAny<Pedido>()))
                .Returns(Task.FromResult(OperationResult<Pedido>.Ok()));
            _paypalRepositoryMock.Setup(r => r.ObtenRembolsoAsync("1003"))
                .ReturnsAsync(rembolsoExistente);
            _paypalRepositoryMock.Setup(r => r.ActualizarRembolsoAsync(It.IsAny<Rembolso>()))
                .Returns(Task.FromResult(OperationResult<Rembolso>.Ok()));
            _userAccessorMock.Setup(u => u.GetCurrentUserId()).Returns(3);

            var resultado = await _sut.ProcesarRembolsoAsync(3, "Reembolsado", "REF-OLD");

            Assert.True(resultado.IsSuccess);
            Assert.Equal("Rembolso actualizado con éxito", resultado.Message);

            // Verifica que se actualizó el rembolso existente
            _paypalRepositoryMock.Verify(r => r.ActualizarRembolsoAsync(It.Is<Rembolso>(rem =>
              rem.Id == 99 &&
              rem.EstadoRembolso == EstadoRembolso.Aprobado.ToString() &&
              rem.ReembolsoCompletado == true &&
              rem.TipoRembolso == TipoRembolso.Total.ToString())), Times.Once);

            // Cuando el rembolso ya existe, NO debe llamar a AgregarRembolsoAsync
            _paypalRepositoryMock.Verify(r => r.AgregarRembolsoAsync(It.IsAny<Rembolso>()), Times.Never);
        }
    }
}
