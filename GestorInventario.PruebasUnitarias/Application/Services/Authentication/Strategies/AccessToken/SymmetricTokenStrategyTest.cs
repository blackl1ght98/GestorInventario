using System.Security.Claims;
using GestorInventario.Application.Services.Authentication.Strategies.AccessToken;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using Microsoft.Extensions.Configuration;
using Moq;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Strategies.AccessToken;

public class SymmetricTokenStrategyTest
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IJwtTokenSettings> _jwtTokenSettingsMock;
    private readonly SymmetricTokenStrategy _sut;
    public SymmetricTokenStrategyTest()
    {
        Environment.SetEnvironmentVariable("PRIVATE_KEY", null);
        _configurationMock = new Mock<IConfiguration>();
        _jwtTokenSettingsMock = new Mock<IJwtTokenSettings>();
        _sut = new SymmetricTokenStrategy(_configurationMock.Object, _jwtTokenSettingsMock.Object);
    }
    [Fact]
    public async Task GenerateTokenAsync_SinClaveConfigurada_LanzaInvalidOperationException()
    {
        _configurationMock.Setup(c => c["JWT:ClaveJWT"]).Returns((string)null);

        var usuario = new Usuario { Id = 1 };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GenerateTokenAsync(usuario));
    }
    [Fact]
    public async Task GenerateTokenAsync_ClaveConfigurada_GeneraToken()
    {
        var claveSimetricaValida = "una-clave-de-al-menos-32-bytes-de-longitud!!"; 

        _jwtTokenSettingsMock.Setup(c => c.ObtenerClaveJWT()).Returns(claveSimetricaValida);
        _jwtTokenSettingsMock.Setup(c => c.ObtenerDuracionAccessTokenMinutos()).Returns(15);
        _jwtTokenSettingsMock.Setup(c => c.ObtenerIssuer()).Returns("mi-issuer");
        _jwtTokenSettingsMock.Setup(c => c.ObtenerAudience()).Returns("mi-audience");
        _jwtTokenSettingsMock.Setup(c => c.CrearClaims(It.IsAny<Usuario>())).Returns(new List<Claim>());

        var usuario = new Usuario { Id = 7, IdRolNavigation = null };

        var resultado = await _sut.GenerateTokenAsync(usuario);

        Assert.Equal(7, resultado.Id);
        Assert.Equal("Usuario", resultado.Rol);
        Assert.False(string.IsNullOrEmpty(resultado.Token));
    }

    
}