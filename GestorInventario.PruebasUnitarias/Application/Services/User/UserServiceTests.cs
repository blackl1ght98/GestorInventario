using GestorInventario.Application.Services.User;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Notifications.EmailServices;
using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.DTOS.Email;
using GestorInventario.Shared.DTOS.User;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Moq;


namespace GestorInventario.PruebasUnitarias.Application.Services.User
{
    // ============================================================
    // Tests para UserService
    // Aquí SÍ usamos Mocks porque UserService depende de:
    // - Repositorio (base de datos)
    // - Servicio de hashing
    // - Servicio de email
    // - Repositorio de admin
    // - Logger
    // No queremos levantar toda la aplicación para testear una función.
    // ============================================================
    public class UserServiceTests
    {
        // Mock = "muñeco" que finge ser una dependencia real.
        // En lugar de tocar la base de datos real, usamos estos muñecos.
        private readonly Mock<IUserRepository> _repoMock;
        private readonly Mock<IHashService> _hashMock;
        private readonly Mock<IEmailService> _emailMock;
        private readonly Mock<IAdminRepository> _adminMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;

        // SUT = System Under Test (el objeto REAL que estamos testeando).
        // Le inyectamos los mocks en lugar de las dependencias reales.
        private readonly UserService _sut;

        // Constructor: se ejecuta ANTES de cada test.
        // Creamos los mocks nuevos para cada test para que no se contaminen entre sí.
        public UserServiceTests()
        {
            _repoMock = new Mock<IUserRepository>();
            _hashMock = new Mock<IHashService>();
            _emailMock = new Mock<IEmailService>();
            _adminMock = new Mock<IAdminRepository>();
            _loggerMock = new Mock<ILogger<UserService>>();

            // Inyectamos los mocks en el servicio real.
            // Así UserService cree que está hablando con la base de datos,
            // pero en realidad está hablando con nuestros muñecos.
            _sut = new UserService(
                _repoMock.Object,
                _hashMock.Object,
                _emailMock.Object,
                _adminMock.Object,
                _loggerMock.Object);
        }

        // Método auxiliar: crea un DTO de registro con valores por defecto.
        // Así no repetimos código en cada test.
        private static RegisterUserDto CrearDtoRegistro(string email = "test@test.com") => new()
        {
            Email = email,
            Password = "Password123!",
            NombreCompleto = "Test User",
            FechaNacimiento = DateTime.UtcNow.AddYears(-25),
            Telefono = "123456789",
            Direccion = "Calle Test 123",
            Ciudad = "Madrid",
            CodigoPostal = "28001"
        };

        // ============================================================
        // CrearUsuarioAsync
        // ============================================================

        // -----------------------------------------------------------
        // Test: Si el email ya existe, debe devolver Fail INMEDIATAMENTE
        // sin llegar a hashear la contraseña ni a tocar la base de datos.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearUsuarioAsync_EmailYaExiste_DevuelveFail()
        {
            // Arrange (Preparar el escenario)
            var dto = CrearDtoRegistro();

            // Setup = "Muñeco, cuando te pregunten si existe este email, di que SÍ"
            _repoMock.Setup(r => r.ExisteEmailAsync(dto.Email))
                .Returns(Task.FromResult(true));

            // Act (Ejecutar el método REAL)
            var resultado = await _sut.CrearUsuarioAsync(dto);

            // Assert (Comprobar resultados)
            Assert.False(resultado.Success);  // Debe ser fallido
            Assert.Equal("Ya existe un usuario con este correo electrónico.", resultado.Message);

            // Verify = "Muñeco, ¿te llamaron para hashear?" → "No, nunca"
            _hashMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
            // Verify = "Muñeco, ¿te llamaron para agregar usuario?" → "No, nunca"
            _repoMock.Verify(r => r.AgregarUsuarioAsync(It.IsAny<Usuario>()), Times.Never);
        }

        // -----------------------------------------------------------
        // Test: El PRIMER usuario registrado en el sistema SIEMPRE es Admin.
        // No importa qué rol pidas, el bootstrap gana.
        // Además, al ser el primer usuario NO se envía email de confirmación.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearUsuarioAsync_PrimerUsuario_AsignaRolAdminYNoEnviaEmail()
        {
            // Arrange
            var dto = CrearDtoRegistro("admin@sistema.com");

            // Simulamos que NO existe ese email
            _repoMock.Setup(r => r.ExisteEmailAsync(dto.Email))
                .Returns(Task.FromResult(false));

            // Simulamos que NO hay ningún usuario en el sistema (AnyAsync = false)
            _repoMock.Setup(r => r.AnyAsync())
                .Returns(Task.FromResult(false));

            // Creamos el rol Admin ficticio que devolverá el mock
            var rolAdmin = new Role { Id = 1, Nombre = "Administrador" };
            _repoMock.Setup(r => r.GetRolByNameAsync("Administrador"))
                .Returns(Task.FromResult(rolAdmin));

            // Simulamos que el hasher devuelve un hash ficticio
            _hashMock.Setup(h => h.Hash(dto.Password))
                .Returns(new HashResult { Hash = "hashFakeEnBase64==", Salt = new byte[16] });

            // Simulamos que el repositorio guarda correctamente
            // Task.FromResult = "devuelve este valor ya resuelto dentro de una Task"
            // (necesario porque el método es async).
            _repoMock.Setup(r => r.AgregarUsuarioAsync(It.IsAny<Usuario>()))
                .Returns(Task.FromResult(OperationResult<Usuario>.Ok("Guardado", new Usuario())));

            // Act
            var resultado = await _sut.CrearUsuarioAsync(dto);

            // Assert
            Assert.True(resultado.Success);  // Todo fue bien

            // Verify: ¿Se buscó el rol Administrador? Sí, exactamente 1 vez.
            _repoMock.Verify(r => r.GetRolByNameAsync("Administrador"), Times.Once);

            // Verify: ¿Se intentó enviar email? NO, porque es el primer usuario.
            _emailMock.Verify(e => e.SendEmailAsyncRegister(It.IsAny<EmailDto>(), It.IsAny<int>()), Times.Never);

            // Verify: ¿Se guardó el usuario con ConfirmacionEmail = true y Rol = 1?
            // It.Is<Usuario>(...) = "comprueba que el objeto Usuario cumple esta condición"
            _repoMock.Verify(r => r.AgregarUsuarioAsync(
                It.Is<Usuario>(u => u.ConfirmacionEmail == true && u.IdRol == 1)), Times.Once);
        }

        // -----------------------------------------------------------
        // Test: Si ya hay usuarios y pides un rol específico, se asigna ese rol.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearUsuarioAsync_SegundoUsuario_ConRolSolicitado_AsignaEseRol()
        {
            // Arrange
            var dto = CrearDtoRegistro("vendedor@tienda.com");
            _repoMock.Setup(r => r.ExisteEmailAsync(dto.Email))
                .Returns(Task.FromResult(false));
            _repoMock.Setup(r => r.AnyAsync())
                .Returns(Task.FromResult(true)); // Ya hay usuarios

            var rolVendedor = new Role { Id = 3, Nombre = "Vendedor" };
            _repoMock.Setup(r => r.GetRolByNameAsync("Vendedor"))
                .Returns(Task.FromResult(rolVendedor));

            _hashMock.Setup(h => h.Hash(dto.Password))
                .Returns(new HashResult { Hash = "hashFakeEnBase64==", Salt = new byte[16] });

            _repoMock.Setup(r => r.AgregarUsuarioAsync(It.IsAny<Usuario>()))
                .Returns(Task.FromResult(OperationResult<Usuario>.Ok("Guardado", new Usuario())));

            _emailMock.Setup(e => e.SendEmailAsyncRegister(It.IsAny<EmailDto>(), It.IsAny<int>()))
                .Returns(Task.FromResult(OperationResult<string>.Ok("Enviado")));

            // Act
            var resultado = await _sut.CrearUsuarioAsync(dto, rolSolicitado: "Vendedor");

            // Assert
            Assert.True(resultado.Success);
            _repoMock.Verify(r => r.GetRolByNameAsync("Vendedor"), Times.Once);
            _repoMock.Verify(r => r.AgregarUsuarioAsync(
                It.Is<Usuario>(u => u.IdRol == 3 && u.ConfirmacionEmail == false)), Times.Once);
        }

        // -----------------------------------------------------------
        // Test: Si ya hay usuarios y NO pides rol, se asigna el rol por defecto.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearUsuarioAsync_SegundoUsuario_SinRol_AsignaRolDefault()
        {
            // Arrange
            var dto = CrearDtoRegistro();
            _repoMock.Setup(r => r.ExisteEmailAsync(dto.Email))
                .Returns(Task.FromResult(false));
            _repoMock.Setup(r => r.AnyAsync())
                .Returns(Task.FromResult(true));

            var rolDefault = new Role { Id = 2, Nombre = "Usuario" };
            _repoMock.Setup(r => r.GetRolByNameAsync("Usuario"))
                .Returns(Task.FromResult(rolDefault));

            _hashMock.Setup(h => h.Hash(dto.Password))
                .Returns(new HashResult { Hash = "hashFakeEnBase64==", Salt = new byte[16] });

            _repoMock.Setup(r => r.AgregarUsuarioAsync(It.IsAny<Usuario>()))
                .Returns(Task.FromResult(OperationResult<Usuario>.Ok("Guardado", new Usuario())));

            _emailMock.Setup(e => e.SendEmailAsyncRegister(It.IsAny<EmailDto>(), It.IsAny<int>()))
                .Returns(Task.FromResult(OperationResult<string>.Ok("Enviado")));

            // Act
            var resultado = await _sut.CrearUsuarioAsync(dto);

            // Assert
            Assert.True(resultado.Success);
            _repoMock.Verify(r => r.GetRolByNameAsync("Usuario"), Times.Once);
        }

        // -----------------------------------------------------------
        // Test: Si el rol no existe en la base de datos (seed no ejecutado),
        // el sistema debe lanzar una excepción clara.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearUsuarioAsync_RolNoExisteEnBaseDatos_LanzaInvalidOperationException()
        {
            // Arrange
            var dto = CrearDtoRegistro();
            _repoMock.Setup(r => r.ExisteEmailAsync(dto.Email))
                .Returns(Task.FromResult(false));
            _repoMock.Setup(r => r.AnyAsync())
                .Returns(Task.FromResult(true));

            // null! = "le decimos al compilador que es null, pero no nos avise"
            // (es un truco para los mocks, en producción nunca harías esto).
            _repoMock.Setup(r => r.GetRolByNameAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<Role>(null!));

            // Act & Assert
            // Assert.ThrowsAsync = "xUnit, comprueba que este código lanza esta excepción"
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CrearUsuarioAsync(dto));

            // Comprobamos que el mensaje de error contiene estas palabras clave.
            Assert.Contains("no existe", ex.Message);
            Assert.Contains("seed", ex.Message);
        }

        // -----------------------------------------------------------
        // Test: Si el repositorio falla al guardar (por ejemplo, error de BD),
        // el servicio debe devolver Fail con el mensaje de error original.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearUsuarioAsync_GuardadoEnRepoFallido_DevuelveFail()
        {
            // Arrange
            var dto = CrearDtoRegistro();
            _repoMock.Setup(r => r.ExisteEmailAsync(dto.Email))
                .Returns(Task.FromResult(false));
            _repoMock.Setup(r => r.AnyAsync())
                .Returns(Task.FromResult(true));

            var rol = new Role { Id = 2, Nombre = "Usuario" };
            _repoMock.Setup(r => r.GetRolByNameAsync("Usuario"))
                .Returns(Task.FromResult(rol));

            _hashMock.Setup(h => h.Hash(dto.Password))
                .Returns(new HashResult { Hash = "hashFakeEnBase64==", Salt = new byte[16] });

            // Simulamos que el repositorio devuelve un FAIL
            _repoMock.Setup(r => r.AgregarUsuarioAsync(It.IsAny<Usuario>()))
                .Returns(Task.FromResult(OperationResult<Usuario>.Fail("Error de base de datos")));

            // Act
            var resultado = await _sut.CrearUsuarioAsync(dto);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("Error de base de datos", resultado.Message);

            // Como el guardado falló, NUNCA se debería intentar enviar email.
            _emailMock.Verify(e => e.SendEmailAsyncRegister(It.IsAny<EmailDto>(), It.IsAny<int>()), Times.Never);
        }

        // -----------------------------------------------------------
        // Test: En un registro normal (no primer usuario), se debe enviar
        // el email de confirmación al email del usuario registrado.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearUsuarioAsync_RegistroNormal_EnviaEmailDeConfirmacion()
        {
            // Arrange
            var dto = CrearDtoRegistro("nuevo@usuario.com");
            _repoMock.Setup(r => r.ExisteEmailAsync(dto.Email))
                .Returns(Task.FromResult(false));
            _repoMock.Setup(r => r.AnyAsync())
                .Returns(Task.FromResult(true));

            var rol = new Role { Id = 2, Nombre = "Usuario" };
            _repoMock.Setup(r => r.GetRolByNameAsync("Usuario"))
                .Returns(Task.FromResult(rol));

            _hashMock.Setup(h => h.Hash(dto.Password))
                .Returns(new HashResult { Hash = "hashFakeEnBase64==", Salt = new byte[16] });

            _repoMock.Setup(r => r.AgregarUsuarioAsync(It.IsAny<Usuario>()))
                .Returns(Task.FromResult(OperationResult<Usuario>.Ok("Guardado", new Usuario())));

            // It.Is<EmailDto>(m => m.ToEmail == dto.Email)
            // = "Verifica que el email se envió EXACTAMENTE a esta dirección"
            _emailMock.Setup(e => e.SendEmailAsyncRegister(
                    It.Is<EmailDto>(m => m.ToEmail == dto.Email), It.IsAny<int>()))
                .Returns(Task.FromResult(OperationResult<string>.Ok("Email enviado")));

            // Act
            var resultado = await _sut.CrearUsuarioAsync(dto);

            // Assert
            Assert.True(resultado.Success);
            _emailMock.Verify(e => e.SendEmailAsyncRegister(
                It.Is<EmailDto>(m => m.ToEmail == dto.Email), It.IsAny<int>()), Times.Once);
        }

        // -----------------------------------------------------------
        // Test: Si el email de confirmación falla (SMTP caído),
        // el usuario DEBE seguir creado. El email no es transaccional.
        // Además, se debe loguear un Warning.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearUsuarioAsync_EmailFalla_UsuarioCreadoYLogWarning()
        {
            // Arrange
            var dto = CrearDtoRegistro();
            _repoMock.Setup(r => r.ExisteEmailAsync(dto.Email))
                .Returns(Task.FromResult(false));
            _repoMock.Setup(r => r.AnyAsync())
                .Returns(Task.FromResult(true));

            var rol = new Role { Id = 2, Nombre = "Usuario" };
            _repoMock.Setup(r => r.GetRolByNameAsync("Usuario"))
                .Returns(Task.FromResult(rol));

            _hashMock.Setup(h => h.Hash(dto.Password))
                .Returns(new HashResult { Hash = "hashFakeEnBase64==", Salt = new byte[16] });

            _repoMock.Setup(r => r.AgregarUsuarioAsync(It.IsAny<Usuario>()))
                .Returns(Task.FromResult(OperationResult<Usuario>.Ok("Guardado", new Usuario())));

            // Simulamos que el servicio de email devuelve FAIL
            _emailMock.Setup(e => e.SendEmailAsyncRegister(It.IsAny<EmailDto>(), It.IsAny<int>()))
                .Returns(Task.FromResult(OperationResult<string>.Fail("SMTP caído")));

            // Act
            var resultado = await _sut.CrearUsuarioAsync(dto);

            // Assert
            Assert.True(resultado.Success); // El usuario se creó a pesar del email

            // Verificamos que se llamó a Log con nivel Warning y el texto esperado.
            // Esta es la forma estándar de Moq para verificar ILogger.
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,                          // Nivel: Warning
                    It.IsAny<EventId>(),                       // Cualquier EventId
                    It.Is<It.IsAnyType>((v, t) =>              // El mensaje contiene este texto
                        v.ToString().Contains("No se pudo enviar")),
                    It.IsAny<Exception>(),                     // Cualquier excepción (o null)
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()), // Formatter por defecto
                Times.Once);                                 // Exactamente 1 vez
        }

        // ============================================================
        // EliminarUsuarioAsync
        // ============================================================

        // -----------------------------------------------------------
        // Test: Si intentas eliminar un usuario que no existe,
        // debe devolver Fail sin llamar al repositorio de admin.
        // -----------------------------------------------------------
        [Fact]
        public async Task EliminarUsuarioAsync_UsuarioNoExiste_DevuelveFail()
        {
            // Arrange
            // Simulamos que el repositorio no encuentra el usuario
            _repoMock.Setup(r => r.ObtenerUsuarioConProveedoresYPedidosAsync(99))
                .Returns(Task.FromResult<Usuario>(null!));

            // Act
            var resultado = await _sut.EliminarUsuarioAsync(99);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("El usuario no existe", resultado.Message);

            // NUNCA debe llegar a llamar al admin repository
            _adminMock.Verify(a => a.EliminarUsuario(It.IsAny<int>()), Times.Never);
        }

        // -----------------------------------------------------------
        // Test: No se puede eliminar un usuario que tiene pedidos.
        // Debe devolver Fail antes de llamar al admin repository.
        // -----------------------------------------------------------
        [Fact]
        public async Task EliminarUsuarioAsync_UsuarioConPedidos_DevuelveFail()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                // Creamos un pedido ficticio para que la validación falle
                Pedidos = new List<Pedido> { new Pedido() }
            };
            _repoMock.Setup(r => r.ObtenerUsuarioConProveedoresYPedidosAsync(1))
                .Returns(Task.FromResult(usuario));

            // Act
            var resultado = await _sut.EliminarUsuarioAsync(1);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("El usuario no se puede eliminar porque tiene pedidos asociados", resultado.Message);
            _adminMock.Verify(a => a.EliminarUsuario(It.IsAny<int>()), Times.Never);
        }

        // -----------------------------------------------------------
        // Test: No se puede eliminar un usuario que tiene proveedores.
        // -----------------------------------------------------------
        [Fact]
        public async Task EliminarUsuarioAsync_UsuarioConProveedores_DevuelveFail()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                Pedidos = new List<Pedido>(), // Sin pedidos
                Proveedores = new List<Proveedore> { new Proveedore() } // Pero con proveedores
            };
            _repoMock.Setup(r => r.ObtenerUsuarioConProveedoresYPedidosAsync(1))
                .Returns(Task.FromResult(usuario));

            // Act
            var resultado = await _sut.EliminarUsuarioAsync(1);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("El usuario no se puede eliminar porque tiene proveedores asociados", resultado.Message);
            _adminMock.Verify(a => a.EliminarUsuario(It.IsAny<int>()), Times.Never);
        }

        // -----------------------------------------------------------
        // Test: Si el usuario no tiene pedidos ni proveedores,
        // se delega la eliminación al AdminRepository.
        // -----------------------------------------------------------
        [Fact]
        public async Task EliminarUsuarioAsync_UsuarioSinRelaciones_EliminaCorrectamente()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                Pedidos = new List<Pedido>(),
                Proveedores = new List<Proveedore>()
            };
            _repoMock.Setup(r => r.ObtenerUsuarioConProveedoresYPedidosAsync(1))
                .Returns(Task.FromResult(usuario));

            _adminMock.Setup(a => a.EliminarUsuario(1))
                .Returns(Task.FromResult(OperationResult<string>.Ok("Eliminado")));

            // Act
            var resultado = await _sut.EliminarUsuarioAsync(1);

            // Assert
            Assert.True(resultado.Success);
            _adminMock.Verify(a => a.EliminarUsuario(1), Times.Once);
        }
    }
}