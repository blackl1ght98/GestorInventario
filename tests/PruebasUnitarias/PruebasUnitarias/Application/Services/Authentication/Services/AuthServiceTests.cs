using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GestorInventario.Application.Services.Authentication.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Notifications.EmailServices;
using GestorInventario.Interfaces.Web;
using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.DTOS.Email;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace PruebasUnitarias.Application.Services.Authentication.Services
{
    /// <summary>
    /// Tests para AuthService.
    /// Este servicio contiene la lógica crítica de autenticación:
    /// login, cambio de contraseña, restablecimiento de contraseña y validación de tokens.
    /// Todas las dependencias (repositorio, hasher, email, usuario actual) se mockean.
    /// </summary>
    public class AuthServiceTests
    {
        private readonly Mock<IHashService> _hashMock;
        private readonly Mock<IUserRepository> _repoMock;
        private readonly Mock<ICurrentUserAccessor> _currentUserMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly Mock<IEmailService> _emailMock;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _hashMock = new Mock<IHashService>();
            _repoMock = new Mock<IUserRepository>();
            _currentUserMock = new Mock<ICurrentUserAccessor>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _emailMock = new Mock<IEmailService>();

            _sut = new AuthService(
                _hashMock.Object,
                _repoMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object,
                _emailMock.Object);
        }

        // ============================================================
        // Helper: crea un usuario base para los tests.
        // ============================================================
        private static Usuario CrearUsuario(
            string email = "pepe@test.com",
            string passwordHash = "hashReal",
            byte[]? salt = null,
            bool confirmado = true,
            bool baja = false) => new()
            {
                Id = 1,
                Email = email,
                Password = passwordHash,
                Salt = salt ?? new byte[] { 1, 2, 3 },
                ConfirmacionEmail = confirmado,
                BajaUsuario = baja
            };

        // ============================================================
        // Login
        // ============================================================

        [Fact]
        public async Task Login_EmailNoExiste_DevuelveFail()
        {
            // Arrange: el repositorio no encuentra el email
            _repoMock.Setup(r => r.ObtenerEmail("noexiste@test.com"))
                .Returns(Task.FromResult<Usuario>(null!));

            // Act
            var resultado = await _sut.Login("noexiste@test.com", new LoginDto { Password = "cualquiera",Email="noexiste@test.com" });

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("email y/o la contraseña son incorrectos", resultado.Message);
        }

        [Fact]
        public async Task Login_EmailNoConfirmado_DevuelveFail()
        {
            // Arrange: usuario existe pero no ha confirmado el email
            var usuario = CrearUsuario(confirmado: false);
            _repoMock.Setup(r => r.ObtenerEmail("pepe@test.com"))
                .Returns(Task.FromResult(usuario));

            // Act
            var resultado = await _sut.Login("pepe@test.com", new LoginDto { Password = "123", Email = "pepe@test.com" });

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("confirma tu correo electrónico", resultado.Message);
        }

        [Fact]
        public async Task Login_UsuarioDadoDeBaja_DevuelveFail()
        {
            // Arrange: usuario existe, confirmado, pero dado de baja
            var usuario = CrearUsuario(baja: true);
            _repoMock.Setup(r => r.ObtenerEmail("pepe@test.com"))
                .Returns(Task.FromResult(usuario));

            // Act
            var resultado = await _sut.Login("pepe@test.com", new LoginDto { Password = "123",Email="pepe@test.com" });

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("dado de baja", resultado.Message);
        }

        [Fact]
        public async Task Login_ContraseñaIncorrecta_DevuelveFail()
        {
            // Arrange: usuario válido, pero el hash no coincide
            var usuario = CrearUsuario();
            _repoMock.Setup(r => r.ObtenerEmail("pepe@test.com"))
                .Returns(Task.FromResult(usuario));

            // El hasher devuelve un hash diferente al almacenado
            _hashMock.Setup(h => h.Hash("mala", usuario.Salt))
                .Returns(new HashResult { Hash = "hashDiferente", Salt = usuario.Salt });

            // Act
            var resultado = await _sut.Login("pepe@test.com", new LoginDto { Password = "mala", Email = "pepe@test.com" });

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("email y/o la contraseña son incorrectos", resultado.Message);
        }

        [Fact]
        public async Task Login_TodoCorrecto_DevuelveOkConUsuario()
        {
            // Arrange
            var usuario = CrearUsuario();
            _repoMock.Setup(r => r.ObtenerEmail("pepe@test.com"))
                .Returns(Task.FromResult(usuario));

            _hashMock.Setup(h => h.Hash("correcta", usuario.Salt))
                .Returns(new HashResult { Hash = "hashReal", Salt = usuario.Salt });

            // Act
            var resultado = await _sut.Login("pepe@test.com", new LoginDto { Password = "correcta", Email = "pepe@test.com" });

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal(usuario, resultado.Data);
        }

        // ============================================================
        // ChangePassword
        // ============================================================

        [Fact]
        public async Task ChangePassword_UsuarioNoEncontrado_DevuelveFail()
        {
            // Arrange
            _currentUserMock.Setup(c => c.GetCurrentUserId()).Returns(99);
            _repoMock.Setup(r => r.ObtenerUsuarioPorId(99))
                .Returns(Task.FromResult<Usuario>(null!));

            // Act
            var resultado = await _sut.ChangePassword("old", "new");

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("Usuario no encontrado", resultado.Message);
        }

        [Fact]
        public async Task ChangePassword_ContraseñaAnteriorIncorrecta_DevuelveFail()
        {
            // Arrange
            var usuario = CrearUsuario(passwordHash: "hashCorrecto");
            _currentUserMock.Setup(c => c.GetCurrentUserId()).Returns(1);
            _repoMock.Setup(r => r.ObtenerUsuarioPorId(1))
                .Returns(Task.FromResult(usuario));

            _hashMock.Setup(h => h.Hash("oldMala", usuario.Salt))
                .Returns(new HashResult { Hash = "hashIncorrecto", Salt = usuario.Salt });

            // Act
            var resultado = await _sut.ChangePassword("oldMala", "nueva123");

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("Contraseña anterior incorrecta", resultado.Message);
        }

        [Fact]
        public async Task ChangePassword_TodoCorrecto_ActualizaHashYSalt()
        {
            // Arrange
            var usuario = CrearUsuario(passwordHash: "hashCorrecto");
            _currentUserMock.Setup(c => c.GetCurrentUserId()).Returns(1);
            _repoMock.Setup(r => r.ObtenerUsuarioPorId(1))
                .Returns(Task.FromResult(usuario));

            _hashMock.Setup(h => h.Hash("oldCorrecta", usuario.Salt))
                .Returns(new HashResult { Hash = "hashCorrecto", Salt = usuario.Salt });

            var nuevoHash = new HashResult { Hash = "nuevoHash", Salt = new byte[] { 9, 8, 7 } };
            _hashMock.Setup(h => h.Hash("nueva123"))
                .Returns(nuevoHash);

            _repoMock.Setup(r => r.ActualizarUsuarioAsync(usuario))
      .Returns(Task.FromResult(OperationResult<string>.Ok("ok")));

            // Act
            var resultado = await _sut.ChangePassword("oldCorrecta", "nueva123");

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal("nuevoHash", usuario.Password);
            Assert.Equal(nuevoHash.Salt, usuario.Salt);
        }

        // ============================================================
        // SetNewPasswordAsync
        // ============================================================

        [Fact]
        public async Task SetNewPasswordAsync_TokenInvalido_DevuelveFail()
        {
            // Arrange: token vacío → ValidarTokenAsync falla inmediatamente
            var dto = new RestoresPasswordDto
            {
                UserId = 1,
                Token = "",
                Password = "nueva",
                TemporaryPassword = "temp"
            };

            // Act
            var resultado = await _sut.SetNewPasswordAsync(dto);

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("token no es valido", resultado.Message);
        }

        [Fact]
        public async Task SetNewPasswordAsync_ContraseñaVacia_DevuelveFail()
        {
            // Arrange: token válido pero contraseña nueva vacía
            var usuario = new Usuario
            {
                Id = 1,
                EmailVerificationToken = "tokenValido",
                ResetTokenSalt = new byte[] { 1, 2, 3 },
                TemporaryPassword = "hashTemp",
                FechaExpiracionContrasenaTemporal = DateTime.UtcNow.AddHours(1)
            };

            _repoMock.Setup(r => r.ObtenerUsuarioPorId(1))
                .Returns(Task.FromResult(usuario));

            var dto = new RestoresPasswordDto
            {
                UserId = 1,
                Token = "tokenValido",
                Password = "", // vacía
                TemporaryPassword = "temp"
            };

            // Act
            var resultado = await _sut.SetNewPasswordAsync(dto);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("La contraseña no puede estar vacía", resultado.Message);
        }

        [Fact]
        public async Task SetNewPasswordAsync_ContraseñaTemporalIncorrecta_DevuelveFail()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                EmailVerificationToken = "tokenValido",
                ResetTokenSalt = new byte[] { 1, 2, 3 },
                TemporaryPassword = "hashTempCorrecto",
                FechaExpiracionContrasenaTemporal = DateTime.UtcNow.AddHours(1)
            };

            _repoMock.Setup(r => r.ObtenerUsuarioPorId(1))
                .Returns(Task.FromResult(usuario));

            _hashMock.Setup(h => h.Hash("tempMala", usuario.ResetTokenSalt))
                .Returns(new HashResult { Hash = "hashDiferente", Salt = usuario.ResetTokenSalt });

            var dto = new RestoresPasswordDto
            {
                UserId = 1,
                Token = "tokenValido",
                Password = "nueva123",
                TemporaryPassword = "tempMala"
            };

            // Act
            var resultado = await _sut.SetNewPasswordAsync(dto);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("La contraseña temporal no es válida", resultado.Message);
        }

        [Fact]
        public async Task SetNewPasswordAsync_TodoCorrecto_LimpiaTokensYActualiza()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                EmailVerificationToken = "tokenValido",
                ResetTokenSalt = new byte[] { 1, 2, 3 },
                TemporaryPassword = "hashTemp",
                FechaExpiracionContrasenaTemporal = DateTime.UtcNow.AddHours(1)
            };

            _repoMock.Setup(r => r.ObtenerUsuarioPorId(1))
                .Returns(Task.FromResult(usuario));

            _hashMock.Setup(h => h.Hash("tempCorrecta", usuario.ResetTokenSalt))
                .Returns(new HashResult { Hash = "hashTemp", Salt = usuario.ResetTokenSalt });

            var nuevoHash = new HashResult { Hash = "nuevoHash", Salt = new byte[] { 4, 5, 6 } };
            _hashMock.Setup(h => h.Hash("nueva123"))
                .Returns(nuevoHash);
            _repoMock.Setup(r => r.ActualizarUsuarioAsync(usuario))
                .Returns(Task.FromResult(OperationResult<string>.Ok("ok")));

            var dto = new RestoresPasswordDto
            {
                UserId = 1,
                Token = "tokenValido",
                Password = "nueva123",
                TemporaryPassword = "tempCorrecta"
            };

            // Act
            var resultado = await _sut.SetNewPasswordAsync(dto);

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal("Contraseña cambiada con exito", resultado.Message);

            // Verificamos que se limpiaron todos los datos sensibles del reset
            Assert.Null(usuario.EmailVerificationToken);
            Assert.Null(usuario.TemporaryPassword);
            Assert.Null(usuario.ResetTokenSalt);
            Assert.Null(usuario.FechaExpiracionContrasenaTemporal);
            Assert.True(usuario.ResetTokenUsed);

            // Verificamos que se actualizó la contraseña
            Assert.Equal("nuevoHash", usuario.Password);
            Assert.Equal(nuevoHash.Salt, usuario.Salt);
        }

        // ============================================================
        // PrepareRestorePassModel
        // ============================================================

        [Fact]
        public async Task PrepareRestorePassModel_TokenValido_DevuelveOkConDto()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                EmailVerificationToken = "abc123",
                ResetTokenSalt = new byte[] { 1, 2, 3 },
                TemporaryPassword = "hashTemp",
                FechaExpiracionContrasenaTemporal = DateTime.UtcNow.AddHours(1)
            };

            _repoMock.Setup(r => r.ObtenerUsuarioPorId(1))
                .Returns(Task.FromResult(usuario));

            _hashMock.Setup(h => h.Hash("temp", usuario.ResetTokenSalt))
                .Returns(new HashResult { Hash = "hashTemp", Salt = usuario.ResetTokenSalt });

            // Act
            var resultado = await _sut.PrepareRestorePassModel(1, "abc123");

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal("Modelo preparado con éxito", resultado.Message);
            Assert.NotNull(resultado.Data);
            Assert.Equal(1, resultado.Data.UserId);
            Assert.Equal("abc123", resultado.Data.Token);
        }

        [Fact]
        public async Task PrepareRestorePassModel_TokenInvalido_DevuelveFail()
        {
            // Arrange: token vacío → falla validación
            // Act
            var resultado = await _sut.PrepareRestorePassModel(1, "");

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("token no es valido", resultado.Message);
        }

        [Fact]
        public async Task PrepareRestorePassModel_Excepcion_DevuelveFailGenerico()
        {
            // Arrange: forzamos una excepción inesperada en el repositorio
            _repoMock.Setup(r => r.ObtenerUsuarioPorId(It.IsAny<int>()))
                .ThrowsAsync(new Exception("BD caída"));

            // Act
            var resultado = await _sut.PrepareRestorePassModel(1, "token");

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("tardó mucho en responder", resultado.Message);
        }

        // ============================================================
        // EnviarCorreoResetAsync
        // ============================================================

        [Fact]
        public async Task EnviarCorreoResetAsync_EmailVacio_DevuelveFail()
        {
            // Act
            var resultado = await _sut.EnviarCorreoResetAsync("");

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("correo proporcionado no es valido", resultado.Message);
        }

        [Fact]
        public async Task EnviarCorreoResetAsync_EmailInvalido_DevuelveFail()
        {
            // Act
            var resultado = await _sut.EnviarCorreoResetAsync("no-es-email");

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("correo proporcionado no es valido", resultado.Message);
        }

        [Fact]
        public async Task EnviarCorreoResetAsync_UsuarioNoExiste_DevuelveFail()
        {
            // Arrange
            _repoMock.Setup(r => r.ObtenerEmail("nadie@test.com"))
                .Returns(Task.FromResult<Usuario>(null!));

            // Act
            var resultado = await _sut.EnviarCorreoResetAsync("nadie@test.com");

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("No existe ningún usuario con ese correo", resultado.Message);
        }

        [Fact]
        public async Task EnviarCorreoResetAsync_TodoCorrecto_DevuelveOk()
        {
            // Arrange
            var usuario = CrearUsuario();
            _repoMock.Setup(r => r.ObtenerEmail("pepe@test.com"))
                .Returns(Task.FromResult(usuario));

            _emailMock.Setup(e => e.SendEmailAsyncResetPassword(
                    It.Is<EmailDto>(m => m.ToEmail == "pepe@test.com"), 1))
                .Returns(Task.FromResult(OperationResult<string>.Ok("Enviado", "idEmail")));

            // Act
            var resultado = await _sut.EnviarCorreoResetAsync("pepe@test.com");

            // Assert
            Assert.True(resultado.Success);
        }

        [Fact]
        public async Task EnviarCorreoResetAsync_EmailServiceFalla_LogErrorYDevuelveOk()
        {
            // Arrange: el email falla pero el método no rompe, solo loguea
            var usuario = CrearUsuario();
            _repoMock.Setup(r => r.ObtenerEmail("pepe@test.com"))
                .Returns(Task.FromResult(usuario));

            _emailMock.Setup(e => e.SendEmailAsyncResetPassword(
                    It.Is<EmailDto>(m => m.ToEmail == "pepe@test.com"), 1))
                .Returns(Task.FromResult(OperationResult<string>.Fail("SMTP caído")));

            // Act
            var resultado = await _sut.EnviarCorreoResetAsync("pepe@test.com");

            // Assert
            Assert.True(resultado.Success); // No falla el método, devuelve Ok con el resultado del email
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error al enviar")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}