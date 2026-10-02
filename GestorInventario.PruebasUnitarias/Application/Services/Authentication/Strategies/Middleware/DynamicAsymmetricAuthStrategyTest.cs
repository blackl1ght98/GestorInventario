using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using GestorInventario.Application.Services.Authentication.Strategies.Middleware;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using GestorInventario.Interfaces.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Interfaces.Application.Services.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Newtonsoft.Json;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Strategies.Middleware;

public class DynamicAsymmetricAuthStrategyTest
{
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IHybridCacheService> _cacheMock = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock = new();
    private readonly Mock<IJwtTokenSettings> _claimsBuilderMock = new();
    private readonly Mock<ILogger<DynamicAsymmetricAuthStrategy>> _loggerMock = new();
    private readonly DynamicAsymmetricAuthStrategy _sut;

    private const string Issuer = "mi-issuer";
    private const string Audience = "mi-audience";

    public DynamicAsymmetricAuthStrategyTest()
    {
        _claimsBuilderMock.Setup(c => c.ObtenerIssuer()).Returns(Issuer);
        _claimsBuilderMock.Setup(c => c.ObtenerAudience()).Returns(Audience);
        _claimsBuilderMock.Setup(c => c.ObtenerDuracionAccessTokenMinutos()).Returns(15);
        _claimsBuilderMock.Setup(c => c.ObtenerDuracionRefreshTokenHoras()).Returns(24);

        _sut = new DynamicAsymmetricAuthStrategy(
            _tokenGeneratorMock.Object,
            _userRepositoryMock.Object,
            _cacheMock.Object,
            _refreshTokenGeneratorMock.Object,
            _claimsBuilderMock.Object,
            _loggerMock.Object);
    }

    // Genera un JWT real firmado con RSA, igual que lo haría el sistema real
    private static (string token, RSA rsa) GenerarTokenFirmado(
        string kid, int userId, DateTime expira)
    {
        var rsa = RSA.Create(2048);
        var credenciales = new SigningCredentials(
            new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = kid },
            SecurityAlgorithms.RsaSha256);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };

        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience, claims: claims,
            expires: expira, signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), rsa);
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
        var (token, rsa) = GenerarTokenFirmado("kid1", 42, DateTime.UtcNow.AddMinutes(10));

        _cacheMock.Setup(c => c.GetStringAsync("kid1PublicKey"))
            .ReturnsAsync(JsonConvert.SerializeObject(rsa.ExportParameters(false)));

        var ctx = CrearContexto(auth: token);

        await _sut.ProcessAuthentication(ctx, () => Task.CompletedTask);

        Assert.True(ctx.User.Identity?.IsAuthenticated);
        Assert.Equal("42", ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }
    [Fact]
    public async Task ProcessAuthentication_TokenExpiradoSinRefresh_NoAutenticaNiFalla()
    {
        var (token, _) = GenerarTokenFirmado("kid1", 42, DateTime.UtcNow.AddMinutes(-10));
        var ctx = CrearContexto(auth: token);

        await _sut.ProcessAuthentication(ctx, () => Task.CompletedTask);

        Assert.False(ctx.User.Identity?.IsAuthenticated ?? false);
        _userRepositoryMock.Verify(r => r.ObtenerUsuarioPorId(It.IsAny<int>()), Times.Never);
    }
    [Fact]
    public async Task ProcessAuthentication_RefreshValido_RegeneraCookies()
    {
        var (refreshToken, rsaRefresh) = GenerarTokenFirmado("kid2", 7, DateTime.UtcNow.AddHours(1));

        _cacheMock.Setup(c => c.GetStringAsync("kid2PublicKeyRefresco"))
            .ReturnsAsync(JsonConvert.SerializeObject(rsaRefresh.ExportParameters(false)));

        _userRepositoryMock.Setup(r => r.ObtenerUsuarioPorId(7))
            .ReturnsAsync(new Usuario { Id = 7 });

        _tokenGeneratorMock.Setup(t => t.GenerateTokenAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(new LoginResponseDto { Token = "nuevo-access" });
        _refreshTokenGeneratorMock.Setup(t => t.GenerateTokenAsync(It.IsAny<Usuario>()))
            .ReturnsAsync("nuevo-refresh");

        var ctx = CrearContexto(refresh: refreshToken); // sin "auth"

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
}