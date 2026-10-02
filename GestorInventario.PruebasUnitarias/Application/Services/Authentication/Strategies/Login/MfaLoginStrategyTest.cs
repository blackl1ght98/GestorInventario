using GestorInventario.Application.Services.Authentication.Strategies.Login;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.RetryPolicy;
using GestorInventario.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Interfaces.Application.Services.Common;
using GestorInventario.Interfaces.Notifications.EmailServices;
using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.Utilities;
using Moq;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Strategies.Login;

public class MfaLoginStrategyTest
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IPolicyExecutor> _policyExecutorMock;
    private readonly Mock<IHybridCacheService> _cacheMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly MfaLoginStrategy _sut;

    public MfaLoginStrategyTest()
    {
        _authServiceMock = new Mock<IAuthService>();
        _policyExecutorMock = new Mock<IPolicyExecutor>();
        _cacheMock = new Mock<IHybridCacheService>();
        _emailServiceMock = new Mock<IEmailService>();

        _sut = new MfaLoginStrategy(
            _authServiceMock.Object,
            _policyExecutorMock.Object,
            _cacheMock.Object,
            _emailServiceMock.Object);

        // El PolicyExecutor normalmente solo reintenta y delega — para el test,
        // que ejecute directamente la función que le pasan.
        _policyExecutorMock
            .Setup(p => p.ExecutePolicyAsync(It.IsAny<Func<Task<OperationResult<Usuario>>>>()))
            .Returns<Func<Task<OperationResult<Usuario>>>>(func => func());
    }

    [Fact]
    public async Task AuthenticateAsync_LoginFallido_NoEnviaCodigoNiCachea()
    {
        var model = new LoginDto { Email = "test@test.com",Password = "123456789"};

        _authServiceMock
            .Setup(a => a.Login(model.Email, model))
            .ReturnsAsync(OperationResult<Usuario>.Fail("Credenciales incorrectas"));

        var resultado = await _sut.AuthenticateAsync(model);

        Assert.False(resultado.IsSuccess);
        Assert.Equal("Credenciales incorrectas", resultado.Message);

        _cacheMock.Verify(c => c.SetStringAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        _emailServiceMock.Verify(e => e.SendMfaCodeEmail(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_LoginExitoso_GeneraCodigoCacheaYEnviaEmail()
    {
        var model = new LoginDto { Email = "test@test.com", Password = "123456789"};
        var usuario = new Usuario { Id = 42, Email = "test@test.com" };

        _authServiceMock
            .Setup(a => a.Login(model.Email, model))
            .ReturnsAsync(OperationResult<Usuario>.Ok("Login correcto", usuario));

        var resultado = await _sut.AuthenticateAsync(model);

        Assert.True(resultado.IsSuccess);
        Assert.True(resultado.Data.RequiresMfa); // ajusta el nombre real de la propiedad

        _cacheMock.Verify(c => c.SetStringAsync(
                "MFA_42",
                It.Is<string>(codigo => codigo.Length == 6 && codigo.All(char.IsDigit)),
                TimeSpan.FromMinutes(5)),
            Times.Once);

        _emailServiceMock.Verify(e => e.SendMfaCodeEmail(
            "test@test.com",
            It.Is<string>(codigo => codigo.Length == 6)),
            Times.Once);
    }
}