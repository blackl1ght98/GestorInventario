using System.Security.Claims;
using System.Security.Cryptography;
using GestorInventario.Application.Services.Authentication.Strategies.RefreshToken;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using Microsoft.Extensions.Configuration;
using Moq;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Strategies.RefreshToken;

public class RefreshAsymetricFixedTokenTest
{
    private readonly Mock<IJwtTokenSettings> _claimsBuilderMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly RefreshAsymetricFixedToken _sut;

    public RefreshAsymetricFixedTokenTest()
    {
        // Evita que una variable de entorno real del equipo/CI interfiera con el test
        Environment.SetEnvironmentVariable("PRIVATE_KEY", null);

        _claimsBuilderMock = new Mock<IJwtTokenSettings>();
        _configurationMock = new Mock<IConfiguration>();

        _sut = new RefreshAsymetricFixedToken(_claimsBuilderMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task GenerarTokenRefresco_SinClaveConfigurada_LanzaInvalidOperationException()
    {
        _configurationMock.Setup(c => c["JWT:PrivateKey"]).Returns((string)null);

        var usuario = new Usuario { Id = 1 };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GenerarTokenRefresco(usuario));
    }

    [Fact]
    public async Task GenerarTokenRefresco_ClaveConfigurada_GeneraToken()
    {
        using var rsa = RSA.Create(2048);
        var claveXmlValida = rsa.ToXmlString(true);

        _configurationMock.Setup(c => c["JWT:PrivateKey"]).Returns(claveXmlValida);
        _claimsBuilderMock.Setup(c => c.ObtenerDuracionRefreshTokenHoras()).Returns(24);
        _claimsBuilderMock.Setup(c => c.ObtenerIssuer()).Returns("mi-issuer");
        _claimsBuilderMock.Setup(c => c.ObtenerAudience()).Returns("mi-audience");
        _claimsBuilderMock.Setup(c => c.CrearClaims(It.IsAny<Usuario>())).Returns(new List<Claim>());

        var usuario = new Usuario { Id = 7 };

        var token = await _sut.GenerarTokenRefresco(usuario);

        Assert.False(string.IsNullOrEmpty(token));
    }
}