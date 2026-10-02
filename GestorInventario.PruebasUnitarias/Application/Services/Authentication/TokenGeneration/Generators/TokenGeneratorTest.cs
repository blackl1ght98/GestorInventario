using GestorInventario.Application.Services.Authentication.Resolvers;
using GestorInventario.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Resolvers;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Moq;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.TokenGeneration.Generators;

public class TokenGeneratorTest
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenStrategyResolver> _tokenStrategyResolverMock;
    private readonly TokenGenerator _sut;

    public TokenGeneratorTest()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenStrategyResolverMock = new Mock<ITokenStrategyResolver>();

        _sut = new TokenGenerator(_userRepositoryMock.Object, _tokenStrategyResolverMock.Object);
    }

    [Fact]
    public async Task GenerateTokenAsync_UsuarioNoExisteEnBD_LanzaArgumentException()
    {
        _userRepositoryMock.Setup(r => r.ObtenerUsuarioPorId(It.IsAny<int>()))
            .ReturnsAsync((Usuario)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GenerateTokenAsync(new Usuario { Id = 1 }));

        _tokenStrategyResolverMock.Verify(r => r.ResolveToken(), Times.Never);
    }
}