using GestorInventario.Application.Services.Authentication.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.Utilities;
using Moq;
using Xunit;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Services
{
    public class PasswordResetServiceTests
    {
        private readonly Mock<IHashService> _hashServiceMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly PasswordResetService _sut;

        public PasswordResetServiceTests()
        {
            _hashServiceMock = new Mock<IHashService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _sut = new PasswordResetService(_hashServiceMock.Object, _userRepositoryMock.Object);
        }

        // -----------------------------------------------------------
        // Test 1: Camino feliz. Email existe, se genera password, 
        // se hashea, se guarda, y devuelve la contraseña temporal.
        // -----------------------------------------------------------
        [Fact]
        public async Task GenerarPasswordTemporalAsync_EmailExiste_DevuelveOkConPasswordTemporal()
        {
            // Arrange
            var email = "pepe@test.com";

            // PASO 1: El repositorio dice que el email SÍ existe.
            // Sin este Setup, ExisteEmailAsync devuelve null y await null lanza NullReferenceException.
            _userRepositoryMock.Setup(r => r.ExisteEmailAsync(email))
                .Returns(Task.FromResult(true));

            // PASO 2: El hasher devuelve un hash ficticio.
            // El servicio necesita esto para crear el HashResult que guardará en BD.
            _hashServiceMock.Setup(h => h.Hash(It.IsAny<string>()))
                .Returns(new HashResult { Hash = "hashFake", Salt = new byte[] { 1, 2, 3 } });

            // PASO 3: El repositorio guarda correctamente.
            // Sin este Setup, GuardarPasswordTemporalAsync devuelve null y await null explota.
            _userRepositoryMock.Setup(r => r.GuardarPasswordTemporalAsync(
                    email, "hashFake", It.IsAny<byte[]>(), It.IsAny<DateTime>()))
                .Returns(Task.FromResult(OperationResult<Usuario>.Ok("Guardado")));

            // Act
            var resultado = await _sut.GenerarPasswordTemporalAsync(email);

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal("Password temporal generada", resultado.Message);
            Assert.NotNull(resultado.Data);               // La contraseña temporal generada
            Assert.Equal(12, resultado.Data.Length);      // GenerarContrasenaTemporal genera 12 chars
        }

        // -----------------------------------------------------------
        // Test 2: Email no existe. Debe devolver Fail sin tocar hasher ni guardar.
        // -----------------------------------------------------------
        [Fact]
        public async Task GenerarPasswordTemporalAsync_EmailNoExiste_DevuelveFail()
        {
            // Arrange
            var email = "noexiste@test.com";

            // El repositorio dice que el email NO existe.
            _userRepositoryMock.Setup(r => r.ExisteEmailAsync(email))
                .Returns(Task.FromResult(false));

            // Act
            var resultado = await _sut.GenerarPasswordTemporalAsync(email);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("El correo electronico no existe", resultado.Message);

            // Verificamos que NUNCA se llamó al hasher ni al guardado.
            _hashServiceMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
            _userRepositoryMock.Verify(r => r.GuardarPasswordTemporalAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTime>()), Times.Never);
        }

        // -----------------------------------------------------------
        // Test 3: El repositorio falla al guardar. Debe devolver Fail con el mensaje del repo.
        // -----------------------------------------------------------
        [Fact]
        public async Task GenerarPasswordTemporalAsync_GuardadoFallido_DevuelveFail()
        {
            // Arrange
            var email = "pepe@test.com";

            _userRepositoryMock.Setup(r => r.ExisteEmailAsync(email))
                .Returns(Task.FromResult(true));

            _hashServiceMock.Setup(h => h.Hash(It.IsAny<string>()))
                .Returns(new HashResult { Hash = "hashFake", Salt = new byte[] { 1, 2, 3 } });

            // Simulamos que el repositorio falla al guardar (por ejemplo, error de BD).
            _userRepositoryMock.Setup(r => r.GuardarPasswordTemporalAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTime>()))
                .Returns(Task.FromResult(OperationResult<Usuario>.Fail("Error de base de datos")));

            // Act
            var resultado = await _sut.GenerarPasswordTemporalAsync(email);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("Error de base de datos", resultado.Message);
        }
       
    }
}