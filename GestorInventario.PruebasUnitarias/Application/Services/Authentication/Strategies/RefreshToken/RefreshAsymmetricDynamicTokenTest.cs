using System.Security.Claims;
using GestorInventario.Application.Services.Authentication.Strategies.RefreshToken;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using GestorInventario.Interfaces.Application.Services.Common;
using Moq;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Strategies.RefreshToken;

public class RefreshAsymmetricDynamicTokenTest
{
    private readonly Mock<IJwtTokenSettings> _claimsBuilderMock;
    private readonly Mock<IHybridCacheService> _cacheMock;
    private readonly RefreshAsymmetricDynamicToken _sut;

    public RefreshAsymmetricDynamicTokenTest()
    {
        _claimsBuilderMock = new Mock<IJwtTokenSettings>();
        _cacheMock = new Mock<IHybridCacheService>();

        _sut = new RefreshAsymmetricDynamicToken(_claimsBuilderMock.Object, _cacheMock.Object);

        _claimsBuilderMock.Setup(c => c.ObtenerDuracionRefreshTokenHoras()).Returns(24);
        _claimsBuilderMock.Setup(c => c.ObtenerIssuer()).Returns("mi-issuer");
        _claimsBuilderMock.Setup(c => c.ObtenerAudience()).Returns("mi-audience");
        _claimsBuilderMock.Setup(c => c.CrearClaims(It.IsAny<Usuario>())).Returns(new List<Claim>());
    }

    [Fact]
    public async Task GenerarTokenRefresco_CacheaClavePublicaConElFormatoQueEspera_ElMiddleware()
    {
        var usuario = new Usuario { Id = 42 };

        var token = await _sut.GenerarTokenRefresco(usuario);

        Assert.False(string.IsNullOrEmpty(token));

        // Esta clave DEBE coincidir exactamente con la que lee
        // DynamicAsymmetricAuthStrategy.ValidateRefreshTokenAsync: $"{kid}PublicKeyRefresco"
        _cacheMock.Verify(c => c.SetStringAsync(
                "42PublicKeyRefresco",
                It.IsAny<string>(),
                TimeSpan.FromDays(30)),
            Times.Once);
    }
}