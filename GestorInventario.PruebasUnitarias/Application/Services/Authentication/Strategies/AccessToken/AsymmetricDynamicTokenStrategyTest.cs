using GestorInventario.Application.Services.Authentication.Strategies.AccessToken;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using GestorInventario.Interfaces.Application.Services.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using GestorInventario.Domain.enums.Usuario;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Strategies.AccessToken;

public class AsymmetricDynamicTokenStrategyTest
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IJwtTokenSettings> _claimsBuilderMock;
    private readonly Mock<IHybridCacheService> _cacheMock;
    private readonly Mock<ILogger<AsymmetricDynamicTokenStrategy>> _loggerMock;

    private readonly AsymmetricDynamicTokenStrategy _sut;

    public AsymmetricDynamicTokenStrategyTest()
    {
        _configurationMock = new Mock<IConfiguration>();
        _claimsBuilderMock = new Mock<IJwtTokenSettings>();
        _cacheMock = new Mock<IHybridCacheService>();
        _loggerMock = new Mock<ILogger<AsymmetricDynamicTokenStrategy>>();

        _sut = new AsymmetricDynamicTokenStrategy(
            _configurationMock.Object,
            _claimsBuilderMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateTokenAsync_UsuarioSinRol_LanzaInvalidOperationException()
    {
        var usuario = new Usuario { Id = 1, IdRolNavigation = null };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GenerateTokenAsync(usuario));

        _cacheMock.Verify(c => c.SetStringAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateTokenAsync_UsuarioConRol_GeneraTokenYCacheaClavePublica()
    {
        var usuario = new Usuario
        {
            Id = 42,
            IdRolNavigation = new Role() { Nombre = "Admin" }
        };

        _claimsBuilderMock.Setup(c => c.ObtenerDuracionAccessTokenMinutos()).Returns(15);
        _claimsBuilderMock.Setup(c => c.ObtenerIssuer()).Returns("mi-issuer");
        _claimsBuilderMock.Setup(c => c.ObtenerAudience()).Returns("mi-audience");
        _claimsBuilderMock.Setup(c => c.CrearClaims(usuario)).Returns(new List<Claim>());

        var resultado = await _sut.GenerateTokenAsync(usuario);

        Assert.Equal(42, resultado.Id);
        Assert.Equal("Admin", resultado.Rol);
        Assert.False(string.IsNullOrEmpty(resultado.Token));

        _cacheMock.Verify(c => c.SetStringAsync(
            "42PublicKey",
            It.IsAny<string>()),
            Times.Once);
    }
}