using GestorInventario.Application.Services.Orders;
using GestorInventario.Domain.enums.Paypal;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Web;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GestorInventario.PruebasUnitarias.Application.Services.Orders
{
    public class OrderServiceTest
    {
        private readonly Mock<IPedidoRepository> _repositoryMock;
        private readonly Mock<ILogger<OrderService>> _loggerMock;
        
        private readonly Mock<IPaypalOrderService> _paypalOrderServiceMock;
        private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
  
        private readonly OrderService _sut;
        public OrderServiceTest() { 
        _repositoryMock = new Mock<IPedidoRepository>();
            _loggerMock = new Mock<ILogger<OrderService>>();
           
            _paypalOrderServiceMock = new Mock<IPaypalOrderService>();
            _paymentRepositoryMock= new Mock<IPaymentRepository>();
          
            _sut = new OrderService(_loggerMock.Object,_repositoryMock.Object,_paypalOrderServiceMock.Object,_paymentRepositoryMock.Object);
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
       
        [Fact]
        public async Task SincronizarDetallePagoAsync_DetallesPayPalNull_RetornaFail()
        {
            _paypalOrderServiceMock.Setup(s => s.ObtenerDetallesPagoEjecutadoAsync("ORDER-123"))
                .ReturnsAsync((OrderDetailsResponse)null);

            var resultado = await _sut.SincronizarDetallePagoAsync("ORDER-123", 1);

            Assert.False(resultado.IsSuccess);
            Assert.Contains("no encontrados", resultado.Message, StringComparison.OrdinalIgnoreCase);
            _paymentRepositoryMock.Verify(p => p.ObtenerDetallesPago(It.IsAny<string>()), Times.Never);
        }
        [Fact]
        public async Task SincronizarDetallePagoAsync_DetalleNuevo_CreaYProcesaUnidad()
        {
            var orderResponse = CrearOrderDetailsResponseBasico();
            _paypalOrderServiceMock.Setup(s => s.ObtenerDetallesPagoEjecutadoAsync("ORDER-123"))
                .ReturnsAsync(orderResponse);
            _paymentRepositoryMock.Setup(p => p.ObtenerDetallesPago("ORDER-123"))
                .ReturnsAsync((PayPalPaymentDetail)null);
            _paymentRepositoryMock.Setup(p => p.AgregarDetallePagoAsync(It.IsAny<PayPalPaymentDetail>()))
                .Returns(Task.FromResult(OperationResult<PayPalPaymentDetail>.Ok()));
            _paymentRepositoryMock.Setup(p => p.AgregarInfoEnvioAsync(It.IsAny<PayPalPaymentShipping>()))
               .Returns(Task.FromResult(OperationResult<PayPalPaymentShipping>.Ok()));

            var resultado = await _sut.SincronizarDetallePagoAsync("ORDER-123", 1);

            Assert.True(resultado.IsSuccess);
            Assert.NotNull(resultado.Data);
            Assert.Equal("ORDER-123", resultado.Data.Id);
            _paymentRepositoryMock.Verify(p => p.AgregarDetallePagoAsync(It.Is<PayPalPaymentDetail>(
                d => d.Id == "ORDER-123")), Times.Once);
            _paymentRepositoryMock.Verify(p => p.AgregarInfoEnvioAsync(It.IsAny<PayPalPaymentShipping>()), Times.Once);
        }
        [Fact]
        public async Task SincronizarDetallePagoAsync_DetalleExistente_EliminaYReutiliza()
        {
            var orderResponse = CrearOrderDetailsResponseBasico();
            var existente = new PayPalPaymentDetail { Id = "ORDER-123", Intent = "OLD" };

            _paypalOrderServiceMock.Setup(s => s.ObtenerDetallesPagoEjecutadoAsync("ORDER-123"))
                .ReturnsAsync(orderResponse);
            _paymentRepositoryMock.Setup(p => p.ObtenerDetallesPago("ORDER-123"))
                .ReturnsAsync(existente);
            _paymentRepositoryMock.Setup(p => p.EliminarDetallesPagoAsync(existente))
                .Returns(Task.FromResult(OperationResult<string>.Ok()));
            _paymentRepositoryMock.Setup(p => p.AgregarInfoEnvioAsync(It.IsAny<PayPalPaymentShipping>()))
                .Returns(Task.FromResult(OperationResult<PayPalPaymentShipping>.Ok()));

            var resultado = await _sut.SincronizarDetallePagoAsync("ORDER-123", 1);

            Assert.True(resultado.IsSuccess);
            Assert.Same(existente, resultado.Data);
            _paymentRepositoryMock.Verify(p => p.EliminarDetallesPagoAsync(existente), Times.Once);
        }

        [Fact]
        public async Task SincronizarDetallePagoAsync_SinUnidadDeCompra_RetornaOkSinProcesar()
        {
            var orderResponse = new OrderDetailsResponse
            {
                Id = "ORDER-123",
                Intent = "CAPTURE",
                Status = "COMPLETED",
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
                PurchaseUnits = new List<PurchaseUnitDetails>(), // vacía
                CreateTime = "2024-01-01T00:00:00Z",
                UpdateTime = "2024-01-01T00:00:00Z"
            };

            _paypalOrderServiceMock.Setup(s => s.ObtenerDetallesPagoEjecutadoAsync("ORDER-123"))
                .ReturnsAsync(orderResponse);
            _paymentRepositoryMock.Setup(p => p.ObtenerDetallesPago("ORDER-123"))
                .ReturnsAsync((PayPalPaymentDetail)null);
            _paymentRepositoryMock.Setup(p => p.AgregarDetallePagoAsync(It.IsAny<PayPalPaymentDetail>()))
              .Returns(Task.FromResult(OperationResult<PayPalPaymentDetail>.Ok()));

            var resultado = await _sut.SincronizarDetallePagoAsync("ORDER-123", 1);

            Assert.True(resultado.IsSuccess);
            // No debe llamar a AgregarInfoEnvio porque no hay unidad de compra
            _paymentRepositoryMock.Verify(p => p.AgregarInfoEnvioAsync(It.IsAny<PayPalPaymentShipping>()), Times.Never);
        }

        [Fact]
        public async Task SincronizarDetallePagoAsync_ConCaptures_ProcesaCaptures()
        {
            var orderResponse = CrearOrderDetailsResponseConCapture();
            _paypalOrderServiceMock.Setup(s => s.ObtenerDetallesPagoEjecutadoAsync("ORDER-123"))
                .ReturnsAsync(orderResponse);
            _paymentRepositoryMock.Setup(p => p.ObtenerDetallesPago("ORDER-123"))
                .ReturnsAsync((PayPalPaymentDetail)null);
            _paymentRepositoryMock.Setup(p => p.AgregarDetallePagoAsync(It.IsAny<PayPalPaymentDetail>()))
                .Returns(Task.FromResult(OperationResult<PayPalPaymentDetail>.Ok()));
            _paymentRepositoryMock.Setup(p => p.AgregarInfoEnvioAsync(It.IsAny<PayPalPaymentShipping>()))
                .Returns(Task.FromResult(OperationResult<PayPalPaymentShipping>.Ok()));
            _paymentRepositoryMock.Setup(p => p.AgregarCaptureAsync(It.IsAny<PayPalPaymentCapture>()))
                 .Returns(Task.FromResult(OperationResult<PayPalPaymentCapture>.Ok()));

            var resultado = await _sut.SincronizarDetallePagoAsync("ORDER-123", 5);

            Assert.True(resultado.IsSuccess);
            _paymentRepositoryMock.Verify(p => p.AgregarCaptureAsync(It.Is<PayPalPaymentCapture>(
                c => c.CaptureId == "CAP-001" && c.PedidoId == 5)), Times.Once);
        }

        [Fact]
        public async Task SincronizarDetallePagoAsync_ConRefunds_ProcesaRefunds()
        {
            var orderResponse = CrearOrderDetailsResponseConRefund();
            _paypalOrderServiceMock.Setup(s => s.ObtenerDetallesPagoEjecutadoAsync("ORDER-123"))
                .ReturnsAsync(orderResponse);
            _paymentRepositoryMock.Setup(p => p.ObtenerDetallesPago("ORDER-123"))
                .ReturnsAsync((PayPalPaymentDetail)null);
            _paymentRepositoryMock.Setup(p => p.AgregarDetallePagoAsync(It.IsAny<PayPalPaymentDetail>()))
                .Returns(Task.FromResult(OperationResult<PayPalPaymentDetail>.Ok()));
            _paymentRepositoryMock.Setup(p => p.AgregarInfoEnvioAsync(It.IsAny<PayPalPaymentShipping>()))
                 .Returns(Task.FromResult(OperationResult<PayPalPaymentShipping>.Ok()));
            _paymentRepositoryMock.Setup(p => p.AgregarRefundAsync(It.IsAny<PayPalPaymentRefund>()))
            .Returns(Task.FromResult(OperationResult<PayPalPaymentRefund>.Ok()));
            var resultado = await _sut.SincronizarDetallePagoAsync("ORDER-123", 1);

            Assert.True(resultado.IsSuccess);
            _paymentRepositoryMock.Verify(p => p.AgregarRefundAsync(It.Is<PayPalPaymentRefund>(
                r => r.RefundId == "REF-001")), Times.Once);
        }

        [Fact]
        public async Task SincronizarDetallePagoAsync_ConItems_ProcesaItems()
        {
            var orderResponse = CrearOrderDetailsResponseConItem();
            _paypalOrderServiceMock.Setup(s => s.ObtenerDetallesPagoEjecutadoAsync("ORDER-123"))
                .ReturnsAsync(orderResponse);
            _paymentRepositoryMock.Setup(p => p.ObtenerDetallesPago("ORDER-123"))
                .ReturnsAsync((PayPalPaymentDetail)null);
            _paymentRepositoryMock.Setup(p => p.AgregarDetallePagoAsync(It.IsAny<PayPalPaymentDetail>()))
                .Returns(Task.FromResult(OperationResult<PayPalPaymentDetail>.Ok()));
            _paymentRepositoryMock.Setup(p => p.AgregarInfoEnvioAsync(It.IsAny<PayPalPaymentShipping>()))
                .Returns(Task.FromResult(OperationResult<PayPalPaymentShipping>.Ok()));
            _paymentRepositoryMock.Setup(p => p.AgregarPagoItemAsync(It.IsAny<PayPalPaymentItem>()))
                .Returns(Task.FromResult(OperationResult<PayPalPaymentItem>.Ok()));

            var resultado = await _sut.SincronizarDetallePagoAsync("ORDER-123", 1);

            Assert.True(resultado.IsSuccess);
            _paymentRepositoryMock.Verify(p => p.AgregarPagoItemAsync(It.Is<PayPalPaymentItem>(
                i => i.ItemName == "Producto A" && i.ItemQuantity == 2)), Times.Once);
        }
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
                        Description = "Pedido de prueba",
                        InvoiceId = "INV-001",
                        Amount = new AmountDetails
                        {
                            CurrencyCode = "EUR",
                            Value = "100.00",
                            Breakdown = new BreakdownDetails
                            {
                                ItemTotal = new MoneyDetails { CurrencyCode = "EUR", Value = "80.00" },
                                Shipping = new MoneyDetails { CurrencyCode = "EUR", Value = "10.00" },
                                TaxTotal = new MoneyDetails { CurrencyCode = "EUR", Value = "10.00" }
                            }
                        },
                        Payee = new PayeeDetails
                        {
                            MerchantId = "MERCH-001",
                            EmailAddress = "tienda@test.com"
                        },
                        Items = new List<ItemDetails>(),
                        Shipping = new ShippingDetails
                        {
                            Name = new ShippingName { FullName = "Juan Pérez" },
                            Address = new ShippingAddress
                            {
                                AddressLine1 = "Calle Falsa 123",
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

        private static OrderDetailsResponse CrearOrderDetailsResponseConCapture()
        {
            var baseResponse = CrearOrderDetailsResponseBasico();
            var unit = baseResponse.PurchaseUnits.First();

            var capture = new CaptureDetails
            {
                Id = "CAP-001",
                Status = "COMPLETED",
                FinalCapture = true,
                InvoiceId = "INV-CAP-001",
                CreateTime = "2024-01-01T00:00:00Z",
                UpdateTime = "2024-01-01T00:00:00Z",
                Amount = new MoneyDetails { CurrencyCode = "EUR", Value = "100.00" },
                SellerProtection = new SellerProtection
                {
                    Status = "ELIGIBLE",
                    DisputeCategories = new List<string> { "ITEM_NOT_RECEIVED" }
                },
                SellerReceivableBreakdown = new SellerReceivableBreakdown
                {
                    GrossAmount = new MoneyDetails { CurrencyCode = "EUR", Value = "100.00" },
                    PaypalFee = new MoneyDetails { CurrencyCode = "EUR", Value = "3.50" },
                    NetAmount = new MoneyDetails { CurrencyCode = "EUR", Value = "96.50" },
                    ExchangeRate = new ExchangeRate { Value = "1.0" }
                }
            };

            var updatedUnit = unit with
            {
                Payments = new PaymentsDetails
                {
                    Captures = new List<CaptureDetails> { capture },
                    Refunds = new List<RefundDetails>()
                }
            };

            return baseResponse with
            {
                PurchaseUnits = new List<PurchaseUnitDetails> { updatedUnit }
            };
        }

        private static OrderDetailsResponse CrearOrderDetailsResponseConRefund()
        {
            var baseResponse = CrearOrderDetailsResponseBasico();
            var unit = baseResponse.PurchaseUnits.First();

            var refund = new RefundDetails
            {
                Id = "REF-001",
                Status = "COMPLETED",
                Amount = new MoneyDetails { CurrencyCode = "EUR", Value = "50.00" },
                NoteToPayer = "Reembolso parcial",
                InvoiceId = "INV-REF-001",
                CreateTime = "2024-01-01T00:00:00Z",
                UpdateTime = "2024-01-01T00:00:00Z",
                SellerPayableBreakdown = new SellerPayableBreakdown
                {
                    GrossAmount = new MoneyDetails { CurrencyCode = "EUR", Value = "50.00" },
                    PaypalFee = new MoneyDetails { CurrencyCode = "EUR", Value = "1.50" },
                    NetAmount = new MoneyDetails { CurrencyCode = "EUR", Value = "48.50" },
                    TotalRefundedAmount = new MoneyDetails { CurrencyCode = "EUR", Value = "50.00" },
                    PlatformFees = new List<PlatformFee>(),
                    ExchangeRate = null
                }
            };

            var updatedUnit = unit with
            {
                Payments = new PaymentsDetails
                {
                    Captures = new List<CaptureDetails>(),
                    Refunds = new List<RefundDetails> { refund }
                }
            };

            return baseResponse with
            {
                PurchaseUnits = new List<PurchaseUnitDetails> { updatedUnit }
            };
        }

        private static OrderDetailsResponse CrearOrderDetailsResponseConItem()
        {
            var baseResponse = CrearOrderDetailsResponseBasico();
            var unit = baseResponse.PurchaseUnits.First();

            var item = new ItemDetails
            {
                Name = "Producto A",
                Sku = "SKU-001",
                Quantity = "2",
                UnitAmount = new MoneyDetails { CurrencyCode = "EUR", Value = "25.00" },
                Tax = new MoneyDetails { CurrencyCode = "EUR", Value = "5.00" }
            };

            var updatedUnit = unit with
            {
                Items = new List<ItemDetails> { item }
            };

            return baseResponse with
            {
                PurchaseUnits = new List<PurchaseUnitDetails> { updatedUnit }
            };
        }
        [Fact]
        public async Task ConfirmarPagoPedido_DetalleExistente_AgregaCaptureYActualizaPedido()
        {
            // Arrange
            var pedido = new Pedido
            {
                Id = 1,
                NumeroPedido = "1001",
                EstadoPedido = EstadoPedido.Carrito.ToString(),
                EsCarrito = true,
                DetallePedidos = new List<DetallePedido>(),
                PayPalPaymentCaptures = new List<PayPalPaymentCapture>()
            };

            var paymentDetailExistente = new PayPalPaymentDetail
            {
                Id = "ORD-123",
                Intent = "CAPTURE",
                AmountTotal = 100,
                AmountCurrency = "EUR"
            };

            _repositoryMock.Setup(r => r.ObtenerPedidoPendienteUsuarioAsync(1))
                .ReturnsAsync(pedido);

            // El servicio busca por orderId ("ORD-123"), no por el objeto entero
            _paymentRepositoryMock.Setup(r => r.ObtenerDetallesPago("ORD-123"))
                .ReturnsAsync(paymentDetailExistente);

            // Usa It.IsAny porque el servicio crea su propia instancia internamente
            _paymentRepositoryMock.Setup(r => r.AgregarCaptureAsync(It.IsAny<PayPalPaymentCapture>()))
                .Returns(Task.FromResult(OperationResult<PayPalPaymentCapture>.Ok()));

            _repositoryMock.Setup(r => r.ActualizarPedidoAsync(It.IsAny<Pedido>()))
                 .Returns(Task.FromResult(OperationResult<Pedido>.Ok()));

            // Act
            var resultado = await _sut.ConfirmarPagoDelPedidoAsync(
                usuarioActual: 1,
                captureId: "CAP-123",
                total: 100m,
                currency: "EUR",
                orderId: "ORD-123");

            // Assert
            Assert.True(resultado.IsSuccess);
            Assert.NotNull(resultado.Data);
            Assert.Equal(100m, resultado.Data.Total);
            Assert.Equal("EUR", resultado.Data.Currency);
            Assert.Equal(EstadoPedido.Pagado.ToString(), resultado.Data.EstadoPedido);

            // Verify DESPUÉS de la ejecución
            _paymentRepositoryMock.Verify(r => r.ObtenerDetallesPago("ORD-123"), Times.Once);

            // Como el detalle YA existía, NO debe llamar a AgregarDetallePagoAsync
            _paymentRepositoryMock.Verify(r => r.AgregarDetallePagoAsync(It.IsAny<PayPalPaymentDetail>()), Times.Never);

            // Sí debe agregar el capture
            _paymentRepositoryMock.Verify(r => r.AgregarCaptureAsync(It.Is<PayPalPaymentCapture>(c =>
                c.PaymentId == "ORD-123" &&
                c.CaptureId == "CAP-123" &&
                c.PedidoId == 1 &&
                c.Amount == 100m &&
                c.Currency == "EUR")), Times.Once);

            // Debe actualizar el pedido
            _repositoryMock.Verify(r => r.ActualizarPedidoAsync(It.Is<Pedido>(p =>
                p.Id == 1 &&
                p.Total == 100m &&
                p.Currency == "EUR" &&
                p.EstadoPedido == EstadoPedido.Pagado.ToString())), Times.Once);
        }
        [Fact]
        public async Task ConfirmarPagoPedido_DetalleNoExistente_CreaDetalleYAgregaCapture()
        {
            // Arrange
            var pedido = new Pedido
            {
                Id = 2,
                NumeroPedido = "1002",
                EstadoPedido = EstadoPedido.Carrito.ToString(),
                EsCarrito = true,
                DetallePedidos = new List<DetallePedido>(),
                PayPalPaymentCaptures = new List<PayPalPaymentCapture>()
            };

            _repositoryMock.Setup(r => r.ObtenerPedidoPendienteUsuarioAsync(1))
                .ReturnsAsync(pedido);

            // El detalle NO existe en BD
            _paymentRepositoryMock.Setup(r => r.ObtenerDetallesPago("ORD-NUEVO"))
                .ReturnsAsync((PayPalPaymentDetail)null);

            _paymentRepositoryMock.Setup(r => r.AgregarDetallePagoAsync(It.IsAny<PayPalPaymentDetail>()))
                 .Returns(Task.FromResult(OperationResult<PayPalPaymentDetail>.Ok()));

            _paymentRepositoryMock.Setup(r => r.AgregarCaptureAsync(It.IsAny<PayPalPaymentCapture>()))
                 .Returns(Task.FromResult(OperationResult<PayPalPaymentCapture>.Ok()));

            _repositoryMock.Setup(r => r.ActualizarPedidoAsync(It.IsAny<Pedido>()))
                 .Returns(Task.FromResult(OperationResult<Pedido>.Ok()));

            // Act
            var resultado = await _sut.ConfirmarPagoDelPedidoAsync(
                usuarioActual: 1,
                captureId: "CAP-456",
                total: 250m,
                currency: "USD",
                orderId: "ORD-NUEVO");

            // Assert
            Assert.True(resultado.IsSuccess);

            // Ahora SÍ debe crear el detalle
            _paymentRepositoryMock.Verify(r => r.AgregarDetallePagoAsync(It.Is<PayPalPaymentDetail>(d =>
                d.Id == "ORD-NUEVO" &&
                d.Intent == "CAPTURE" &&
                d.AmountTotal == 250m &&
                d.AmountCurrency == "USD" &&
                d.Description.Contains("1002"))), Times.Once);

            _paymentRepositoryMock.Verify(r => r.AgregarCaptureAsync(It.Is<PayPalPaymentCapture>(c =>
                c.PaymentId == "ORD-NUEVO" &&
                c.CaptureId == "CAP-456")), Times.Once);
        }
        /*REGLA DE ORO PARA MOCK
         
         Setup  →  antes del Act  (prepara el mock para responder)
         Verify →  después del Act (comprueba que se llamó)
        */
     
    }
 }
