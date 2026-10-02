using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GestorInventario.Application.Services.Authentication.Strategies.Middleware;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using GestorInventario.Interfaces.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Strategies.Middleware;

public class SymmetricAuthStrategyTest
{
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock = new();
    private readonly Mock<IJwtTokenSettings> _claimsBuilderMock = new();
    private readonly Mock<ILogger<SymmetricAuthStrategy>> _loggerMock = new();
    private readonly SymmetricAuthStrategy _sut;

    private const string Issuer = "mi-issuer";
    private const string Audience = "mi-audience";
    // Clave simétrica válida (>= 32 bytes), generada de forma determinista para el test
    private readonly string _secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public SymmetricAuthStrategyTest()
    {
        _claimsBuilderMock.Setup(c => c.ObtenerClaveJWT()).Returns(_secret);
        _claimsBuilderMock.Setup(c => c.ObtenerIssuer()).Returns(Issuer);
        _claimsBuilderMock.Setup(c => c.ObtenerAudience()).Returns(Audience);
        _claimsBuilderMock.Setup(c => c.ObtenerDuracionAccessTokenMinutos()).Returns(15);
        _claimsBuilderMock.Setup(c => c.ObtenerDuracionRefreshTokenHoras()).Returns(24);

        _sut = new SymmetricAuthStrategy(_tokenGeneratorMock.Object, _userRepositoryMock.Object, _refreshTokenGeneratorMock.Object, _claimsBuilderMock.Object,_loggerMock.Object);
    }

    private string GenerarTokenFirmado(int userId, DateTime expira)
    {
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };

        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience, claims: claims,
            expires: expira, signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static DefaultHttpContext CrearContexto(string? auth = null, string? refresh = null)
    {
        var ctx = new DefaultHttpContext();
        var cookies = new List<string>();
        if (auth != null) cookies.Add($"auth={auth}");
        if (refresh != null) cookies.Add($"refreshToken={refresh}");
        if (cookies.Any())
            ctx.Request.Headers["Cookie"] = string.Join("; ", cookies);
        return ctx;
    }

    [Fact]
    public async Task ProcessAuthentication_SinCookies_LlamaNextSinAutenticar()
    {
        var ctx = CrearContexto();
        bool nextLlamado = false;

        await _sut.ProcessAuthentication(ctx, () => { nextLlamado = true; return Task.CompletedTask; });

        Assert.True(nextLlamado);
        Assert.False(ctx.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task ProcessAuthentication_TokenValido_AutenticaUsuario()
    {
        var token = GenerarTokenFirmado(42, DateTime.UtcNow.AddMinutes(10));
        var ctx = CrearContexto(auth: token);

        await _sut.ProcessAuthentication(ctx, () => Task.CompletedTask);

        Assert.True(ctx.User.Identity?.IsAuthenticated);
        Assert.Equal("42", ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public async Task ProcessAuthentication_TokenExpiradoSinRefresh_NoAutenticaNiFalla()
    {
        var token = GenerarTokenFirmado(42, DateTime.UtcNow.AddMinutes(-10));
        var ctx = CrearContexto(auth: token);

        await _sut.ProcessAuthentication(ctx, () => Task.CompletedTask);

        Assert.False(ctx.User.Identity?.IsAuthenticated ?? false);
        _userRepositoryMock.Verify(r => r.ObtenerUsuarioPorId(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAuthentication_ClaveJwtNoConfigurada_NoAutenticaNiLanza()
    {
        _claimsBuilderMock.Setup(c => c.ObtenerClaveJWT()).Returns((string)null);

        var token = GenerarTokenFirmado(42, DateTime.UtcNow.AddMinutes(10));
        var ctx = CrearContexto(auth: token);

        // El "throw" interno debe quedar contenido por el try/catch general;
        // el middleware nunca debe propagar la excepción hacia arriba.
        await _sut.ProcessAuthentication(ctx, () => Task.CompletedTask);

        Assert.False(ctx.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task ProcessAuthentication_RefreshValido_RegeneraCookies()
    {
        var refreshToken = GenerarTokenFirmado(7, DateTime.UtcNow.AddHours(1));

        _userRepositoryMock.Setup(r => r.ObtenerUsuarioPorId(7))
            .ReturnsAsync(new Usuario { Id = 7 });

        _tokenGeneratorMock.Setup(t => t.GenerateTokenAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(new LoginResponseDto { Token = "nuevo-access" });
        _refreshTokenGeneratorMock.Setup(t => t.GenerateTokenAsync(It.IsAny<Usuario>()))
            .ReturnsAsync("nuevo-refresh");

        var ctx = CrearContexto(refresh: refreshToken);

        await _sut.ProcessAuthentication(ctx, () => Task.CompletedTask);

        var setCookies = ctx.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("auth=nuevo-access", setCookies);
        Assert.Contains("refreshToken=nuevo-refresh", setCookies);
    }

    [Fact]
    public async Task ProcessAuthentication_RefreshInvalido_BorraCookiesYRedirige()
    {
        var ctx = CrearContexto(refresh: "token-basura-no-es-un-jwt");

        await _sut.ProcessAuthentication(ctx, () => Task.CompletedTask);

        Assert.Equal(302, ctx.Response.StatusCode);
        Assert.Equal("/Auth/Login", ctx.Response.Headers["Location"].ToString());
    }

    [Fact]
    public async Task ProcessAuthentication_UsuarioNoEncontrado_RedirigeALogin()
    {
        var refreshToken = GenerarTokenFirmado(999, DateTime.UtcNow.AddHours(1));

        _userRepositoryMock.Setup(r => r.ObtenerUsuarioPorId(999))
            .ReturnsAsync((Usuario)null);

        var ctx = CrearContexto(refresh: refreshToken);

        await _sut.ProcessAuthentication(ctx, () => Task.CompletedTask);

        Assert.Equal(302, ctx.Response.StatusCode);
        Assert.Equal("/Auth/Login", ctx.Response.Headers["Location"].ToString());
    }
}