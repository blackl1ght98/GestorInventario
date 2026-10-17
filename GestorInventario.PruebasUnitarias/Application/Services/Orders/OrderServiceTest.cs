
using GestorInventario.Application.Services.Orders;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestorInventario.Infrastructure.ExternalServices;
using Xunit;

namespace GestorInventario.PruebasUnitarias.Application.Services.Orders
{
    public class OrderServiceTest
    {
        private readonly Mock<IPedidoRepository> _repositoryMock;
        private readonly Mock<ILogger<OrderService>> _loggerMock;
        private readonly Mock<IPaypalOrderService> _paypalOrderServiceMock;
        private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
        private readonly OrderService _sut;

        public OrderServiceTest()
        {
            _repositoryMock = new Mock<IPedidoRepository>();
            _loggerMock = new Mock<ILogger<OrderService>>();
            _paypalOrderServiceMock = new Mock<IPaypalOrderService>();
            _paymentRepositoryMock = new Mock<IPaymentRepository>();

            _sut = new OrderService(_loggerMock.Object, _repositoryMock.Object, _paypalOrderServiceMock.Object, _paymentRepositoryMock.Object);
        }

        [Fact]
        public async Task EliminarPedido_CarritoSinCapturas_EliminaYRetornaOk()
        {
            // Arrange
            var pedido = new Pedido
            {
                Id = 1,
                EstadoPedido = EstadoPedido.Carrito.ToString(),
                PayPalPaymentCaptures = new List<PayPalPaymentCapture>()
            };

            _repositoryMock.Setup(r => r.ObtenerPedidoConDetallesAsync(pedido.Id))
                .ReturnsAsync(pedido);
            _repositoryMock.Setup(r => r.EliminarCarritoAsync(pedido))
                .ReturnsAsync(OperationResult<string>.Ok("Eliminado"));

            // Act
            var resultado = await _sut.EliminarPedido(pedido.Id);

            // Assert
            Assert.True(resultado.IsSuccess);
            _repositoryMock.Verify(r => r.EliminarCarritoAsync(pedido), Times.Once);
        }

        [Fact]
        public async Task SincronizarDetallePagoAsync_DetalleNuevo_CreaYProcesaUnidad()
        {
            // Arrange
            var orderResponse = CrearOrderDetailsResponseBasico();
            _paypalOrderServiceMock.Setup(s => s.ObtenerDetallesPagoEjecutadoAsync("ORDER-123"))
                .ReturnsAsync(OperationResult<OrderDetailsResponse>.Ok(orderResponse)); // Corrección: Devolver OperationResult

            _paymentRepositoryMock.Setup(p => p.ObtenerDetallesPago("ORDER-123"))
                .ReturnsAsync((PayPalPaymentDetail)null);

            _paymentRepositoryMock.Setup(p => p.AgregarDetallePagoAsync(It.IsAny<PayPalPaymentDetail>()))
                .ReturnsAsync(OperationResult<PayPalPaymentDetail>.Ok(new PayPalPaymentDetail()));

            _paymentRepositoryMock.Setup(p => p.AgregarInfoEnvioAsync(It.IsAny<PayPalPaymentShipping>()))
               .ReturnsAsync(OperationResult<PayPalPaymentShipping>.Ok(new PayPalPaymentShipping()));

            // Act
            var resultado = await _sut.SincronizarDetallePagoAsync("ORDER-123", 1);

            // Assert
            Assert.True(resultado.IsSuccess);
            _paymentRepositoryMock.Verify(p => p.AgregarDetallePagoAsync(It.Is<PayPalPaymentDetail>(
                d => d.Id == "ORDER-123")), Times.Once);
        }

        [Fact]
        public async Task ConfirmarPagoPedido_DetalleExistente_AgregaCaptureYActualizaPedido()
        {
            // Arrange
            var pedido = new Pedido { Id = 1, NumeroPedido = "1001", EstadoPedido = EstadoPedido.Carrito.ToString() };
            var paymentDetailExistente = new PayPalPaymentDetail { Id = "ORD-123", AmountTotal = 100 };

            _repositoryMock.Setup(r => r.ObtenerPedidoPendienteUsuarioAsync(1))
                .ReturnsAsync(pedido);

            _paymentRepositoryMock.Setup(r => r.ObtenerDetallesPago("ORD-123"))
                .ReturnsAsync(paymentDetailExistente);

            _paymentRepositoryMock.Setup(r => r.AgregarCaptureAsync(It.IsAny<PayPalPaymentCapture>()))
                .ReturnsAsync(OperationResult<PayPalPaymentCapture>.Ok(new PayPalPaymentCapture()));

            _repositoryMock.Setup(r => r.ActualizarPedidoAsync(It.IsAny<Pedido>()))
                 .ReturnsAsync(OperationResult<Pedido>.Ok(pedido));

            // Act
            var resultado = await _sut.ConfirmarPagoDelPedidoAsync(1, "CAP-123", 100m, "EUR", "ORD-123");

            // Assert
            Assert.True(resultado.IsSuccess);
            _paymentRepositoryMock.Verify(r => r.AgregarDetallePagoAsync(It.IsAny<PayPalPaymentDetail>()), Times.Never);
            _paymentRepositoryMock.Verify(r => r.AgregarCaptureAsync(It.Is<PayPalPaymentCapture>(c =>
                c.PaymentId == "ORD-123" && c.CaptureId == "CAP-123")), Times.Once);
            _repositoryMock.Verify(r => r.ActualizarPedidoAsync(It.Is<Pedido>(p =>
                p.EstadoPedido == EstadoPedido.Pagado.ToString())), Times.Once);
        }

        [Fact]
        public async Task ConfirmarPagoPedido_DetalleNoExistente_CreaDetalleYAgregaCapture()
        {
            // Arrange
            var pedido = new Pedido { Id = 2, NumeroPedido = "1002", EstadoPedido = EstadoPedido.Carrito.ToString() };

            _repositoryMock.Setup(r => r.ObtenerPedidoPendienteUsuarioAsync(1))
                .ReturnsAsync(pedido);

            _paymentRepositoryMock.Setup(r => r.ObtenerDetallesPago("ORD-NUEVO"))
                .ReturnsAsync((PayPalPaymentDetail)null);

            _paymentRepositoryMock.Setup(r => r.AgregarDetallePagoAsync(It.IsAny<PayPalPaymentDetail>()))
                 .ReturnsAsync(OperationResult<PayPalPaymentDetail>.Ok(new PayPalPaymentDetail()));

            _paymentRepositoryMock.Setup(r => r.AgregarCaptureAsync(It.IsAny<PayPalPaymentCapture>()))
                 .ReturnsAsync(OperationResult<PayPalPaymentCapture>.Ok(new PayPalPaymentCapture()));

            _repositoryMock.Setup(r => r.ActualizarPedidoAsync(It.IsAny<Pedido>()))
                 .ReturnsAsync(OperationResult<Pedido>.Ok(pedido));

            // Act
            var resultado = await _sut.ConfirmarPagoDelPedidoAsync(1, "CAP-456", 250m, "USD", "ORD-NUEVO");

            // Assert
            Assert.True(resultado.IsSuccess);
            _paymentRepositoryMock.Verify(r => r.AgregarDetallePagoAsync(It.Is<PayPalPaymentDetail>(d =>
                d.Id == "ORD-NUEVO" && d.AmountTotal == 250m)), Times.Once);
            _paymentRepositoryMock.Verify(r => r.AgregarCaptureAsync(It.Is<PayPalPaymentCapture>(c =>
                c.PaymentId == "ORD-NUEVO" && c.CaptureId == "CAP-456")), Times.Once);
        }

        // --- HELPERS PARA MOCKS ---
        private static OrderDetailsResponse CrearOrderDetailsResponseBasico()
        {
            return new OrderDetailsResponse
            {
                Id = "ORDER-123",
                Intent = "CAPTURE",
                Status = "COMPLETED",
                CreateTime = "2024-01-01T00:00:00Z",
                UpdateTime = "2024-01-01T00:00:00Z",
                Payer = new Payer
                {
                    Name = new NameDetails { GivenName = "Juan", Surname = "Pérez" },
                    Email = "juan@test.com",
                    PayerId = "PAYER-001",
                    Address = new AddressDetails { CountryCode = "ES" }
                },
                PaymentSource = new PaymentSourceDetails
                {
                    Paypal = new PayPalDetails
                    {
                        Email = "juan@test.com",
                        AccountId = "ACC-001",
                        AccountStatus = "VERIFIED",
                        Name = new NameDetails { GivenName = "Juan", Surname = "Pérez" },
                        Address = new AddressDetails { CountryCode = "ES" }
                    }
                },
                PurchaseUnits = new List<PurchaseUnitDetails>
                {
                    new PurchaseUnitDetails
                    {
                        ReferenceId = "REF-001",
                        Description = "Prueba",
                        InvoiceId = "INV-001",
                        Payee = new PayeeDetails { MerchantId = "M-01", EmailAddress = "t@t.com" },
                        Items = new List<ItemDetails>(),
                        Amount = new AmountDetails
                        {
                            CurrencyCode = "EUR",
                            Value = "100.00",
                            Breakdown = new BreakdownDetails { ItemTotal = new MoneyDetails { Value = "100.00",CurrencyCode = "EUR"},Shipping = new MoneyDetails(){Value = "100.00",CurrencyCode = "EUR"}, TaxTotal = new MoneyDetails { Value = "100.00",CurrencyCode = "EUR"}},
                        },
                        Shipping = new ShippingDetails
                        {
                            Name = new ShippingName { FullName = "Juan Pérez" },
                            Address = new ShippingAddress
                            {
                                AddressLine1 = "Calle 1",
                                AdminArea1 = "Madrid",
                                AdminArea2 = "Madrid",
                                PostalCode = "28001",
                                CountryCode = "ES"
                            },
                            Trackers = new List<Tracker>()
                        },
                        Payments = new PaymentsDetails
                        {
                            Captures = new List<CaptureDetails>(),
                            Refunds = new List<RefundDetails>()
                        }
                    }
                }
            };
        }
    }
}