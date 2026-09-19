using GestorInventario.Application.Services.PDFService;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Renderer.PDF;
using Moq;
using Xunit;

namespace GestorInventario.PruebasUnitarias.Application.Services.PDFService
{
    public class PdfServiceTest
    {
        private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
        private readonly Mock<IPayPalInvoiceRenderer> _invoiceRendererMock;
        private readonly PdfService _sut;

        public PdfServiceTest()
        {
            _paymentRepositoryMock = new Mock<IPaymentRepository>();
            _invoiceRendererMock = new Mock<IPayPalInvoiceRenderer>();

            _sut = new PdfService(_paymentRepositoryMock.Object, _invoiceRendererMock.Object);
        }

        [Fact]
        public async Task GenerarFacturaPagoEjecutadoAsync_DetalleNoExiste_RetornaFail()
        {
            _paymentRepositoryMock.Setup(r => r.ObtenerDetallesPagoPorIDAsync("PAGO-INEXISTENTE"))
                .ReturnsAsync((PayPalPaymentDetail)null);

            var resultado = await _sut.GenerarFacturaPagoEjecutadoAsync("PAGO-INEXISTENTE");

            Assert.False(resultado.IsSuccess);
            _invoiceRendererMock.Verify(r => r.Render(It.IsAny<PayPalPaymentDetail>()), Times.Never);
        }

        [Fact]
        public async Task GenerarFacturaPagoEjecutadoAsync_DetalleExiste_GeneraFactura()
        {
            var detalle = new PayPalPaymentDetail
            {
                Id="PAGO-123",
               
            };
            var pdfEsperado = new byte[] { 1, 2, 3 };

            _paymentRepositoryMock.Setup(r => r.ObtenerDetallesPagoPorIDAsync("PAGO-123"))
                .ReturnsAsync(detalle);
            _invoiceRendererMock.Setup(r => r.Render(detalle))
                .Returns(pdfEsperado);

            var resultado = await _sut.GenerarFacturaPagoEjecutadoAsync("PAGO-123");

            Assert.True(resultado.IsSuccess);
            Assert.Equal(pdfEsperado, resultado.Data);
            _invoiceRendererMock.Verify(r => r.Render(detalle), Times.Once);
        }
    }
}