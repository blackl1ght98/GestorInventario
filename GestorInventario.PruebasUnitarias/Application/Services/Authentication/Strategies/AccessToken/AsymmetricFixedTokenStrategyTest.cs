using GestorInventario.Application.Services.Authentication.Strategies.AccessToken;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Strategies.AccessToken;

public class AsymmetricFixedTokenStrategyTest
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IJwtTokenSettings> _claimsBuilderMock;
    private readonly AsymmetricFixedTokenStrategy _sut;

    public AsymmetricFixedTokenStrategyTest()
    {
        // Evita que una variable de entorno real del equipo/CI interfiera con el test
        Environment.SetEnvironmentVariable("PRIVATE_KEY", null);

        _configurationMock = new Mock<IConfiguration>();
        _claimsBuilderMock = new Mock<IJwtTokenSettings>();

        _sut = new AsymmetricFixedTokenStrategy(_configurationMock.Object, _claimsBuilderMock.Object);
    }

    [Fact]
    public async Task GenerateTokenAsync_SinClaveConfigurada_LanzaInvalidOperationException()
    {
        _configurationMock.Setup(c => c["JWT:PrivateKey"]).Returns((string)null);

        var usuario = new Usuario { Id = 1 };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GenerateTokenAsync(usuario));
    }

    [Fact]
    public async Task GenerateTokenAsync_ClaveConfigurada_GeneraToken()
    {
        using var rsa = RSA.Create(2048);
        var claveXmlValida = rsa.ToXmlString(true);

        _configurationMock.Setup(c => c["JWT:PrivateKey"]).Returns(claveXmlValida);
        _claimsBuilderMock.Setup(c => c.ObtenerDuracionAccessTokenMinutos()).Returns(15);
        _claimsBuilderMock.Setup(c => c.ObtenerIssuer()).Returns("mi-issuer");
        _claimsBuilderMock.Setup(c => c.ObtenerAudience()).Returns("mi-audience");
        _claimsBuilderMock.Setup(c => c.CrearClaims(It.IsAny<Usuario>())).Returns(new List<Claim>());

        var usuario = new Usuario { Id = 7, IdRolNavigation = null };

        var resultado = await _sut.GenerateTokenAsync(usuario);

        Assert.Equal(7, resultado.Id);
        Assert.Equal("Usuario", resultado.Rol);
        Assert.False(string.IsNullOrEmpty(resultado.Token));
    }
}