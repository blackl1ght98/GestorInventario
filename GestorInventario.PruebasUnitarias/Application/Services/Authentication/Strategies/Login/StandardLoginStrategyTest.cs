using GestorInventario.Application.Services.Authentication.Strategies.Login;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.RetryPolicy;
using GestorInventario.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.Utilities;
using Moq;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Strategies.Login;

public class StandardLoginStrategyTest
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IPolicyExecutor> _policyExecutorMock;
    private readonly StandardLoginStrategy _sut;

    public StandardLoginStrategyTest()
    {
        _authServiceMock = new Mock<IAuthService>();
        _policyExecutorMock = new Mock<IPolicyExecutor>();

        _sut = new StandardLoginStrategy(_authServiceMock.Object, _policyExecutorMock.Object);

        _policyExecutorMock
            .Setup(p => p.ExecutePolicyAsync(It.IsAny<Func<Task<OperationResult<Usuario>>>>()))
            .Returns<Func<Task<OperationResult<Usuario>>>>(func => func());
    }

    [Fact]
    public async Task AuthenticateAsync_LoginFallido_RetornaFail()
    {
        var model = new LoginDto { Email = "test@test.com", Password = "123456789" };

        _authServiceMock
            .Setup(a => a.Login(model.Email, model))
            .ReturnsAsync(OperationResult<Usuario>.Fail("Credenciales incorrectas"));

        var resultado = await _sut.AuthenticateAsync(model);

        Assert.False(resultado.IsSuccess);
        Assert.Equal("Credenciales incorrectas", resultado.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_LoginExitoso_RetornaOkSinRequerirMfa()
    {
        var model = new LoginDto { Email = "test@test.com", Password = "123456789" };
        var usuario = new Usuario { Id = 1, Email = "test@test.com" };

        _authServiceMock
            .Setup(a => a.Login(model.Email, model))
            .ReturnsAsync(OperationResult<Usuario>.Ok("Login correcto", usuario));

        var resultado = await _sut.AuthenticateAsync(model);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(usuario, resultado.Data.User);
        Assert.False(resultado.Data.RequiresMfa);
    }
}