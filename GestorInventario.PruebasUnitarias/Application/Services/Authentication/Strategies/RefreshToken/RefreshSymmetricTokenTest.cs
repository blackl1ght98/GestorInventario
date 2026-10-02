using System.Security.Claims;
using System.Security.Cryptography;
using GestorInventario.Application.Services.Authentication.Strategies.RefreshToken;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Strategies.RefreshToken;

public class RefreshSymmetricTokenTest
{
    private readonly Mock<IJwtTokenSettings> _claimsBuilderMock;
    private readonly Mock<ILogger<RefreshSymmetricToken>> _loggerMock;
    private readonly RefreshSymmetricToken _sut;

    public RefreshSymmetricTokenTest()
    {
        _claimsBuilderMock = new Mock<IJwtTokenSettings>();
        _loggerMock = new Mock<ILogger<RefreshSymmetricToken>>();

        _sut = new RefreshSymmetricToken(_claimsBuilderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GenerarTokenRefresco_SinClaveConfigurada_LanzaInvalidOperationException()
    {
        _claimsBuilderMock.Setup(c => c.ObtenerClaveJWT()).Returns((string)null);

        var usuario = new Usuario { Id = 1 };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GenerarTokenRefresco(usuario));

        Assert.Equal("La clave JWT no está configurada.", ex.Message);
    }

    [Fact]
    public async Task GenerarTokenRefresco_ClaveDemasiadoCorta_LanzaInvalidOperationException()
    {
        _claimsBuilderMock.Setup(c => c.ObtenerClaveJWT()).Returns("clave-corta");

        var usuario = new Usuario { Id = 1 };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GenerarTokenRefresco(usuario));

        Assert.Equal("La clave JWT debe tener al menos 32 bytes.", ex.Message);
    }

    [Fact]
    public async Task GenerarTokenRefresco_ClaveValida_GeneraToken()
    {
        var claveValida = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        _claimsBuilderMock.Setup(c => c.ObtenerClaveJWT()).Returns(claveValida);
        _claimsBuilderMock.Setup(c => c.ObtenerDuracionRefreshTokenHoras()).Returns(24);
        _claimsBuilderMock.Setup(c => c.ObtenerIssuer()).Returns("mi-issuer");
        _claimsBuilderMock.Setup(c => c.ObtenerAudience()).Returns("mi-audience");
        _claimsBuilderMock.Setup(c => c.CrearClaims(It.IsAny<Usuario>())).Returns(new List<Claim>());

        var usuario = new Usuario { Id = 7 };

        var token = await _sut.GenerarTokenRefresco(usuario);

        Assert.False(string.IsNullOrEmpty(token));
    }
}