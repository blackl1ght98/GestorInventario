using GestorInventario.Application.Services.Authentication.Services;
using Xunit;

namespace PruebasUnitarias.Application.Services.Authentication.Services
{
    // ============================================================
    // Tests para HashService
    // Este servicio NO depende de base de datos ni de nada externo.
    // Solo usa matemáticas (PBKDF2) por eso no necesitamos Mocks.
    // ============================================================
    public class HashServiceTests
    {
        // SUT = System Under Test (Sistema Bajo Prueba)
        // Es el objeto REAL que estamos testeando. No es un mock.
        private readonly HashService _sut = new();

        // ============================================================
        // Test 1: Si hasheas la misma contraseña dos veces, los salts
        // deben ser DIFERENTES. Si fueran iguales, sería inseguro.
        // ============================================================
        [Fact]  // [Fact] le dice a xUnit: "esto es un test, ejecútalo"
        public void Hash_MismaPassword_DosVeces_ProduceSaltsDiferentes()
        {
            // Arrange (Preparar)
            // No necesitamos preparar nada aquí porque HashService no depende de nadie.

            // Act (Actuar)
            // Llamamos al método real dos veces con la MISMA contraseña.
            var resultado1 = _sut.Hash("MiPassword123!");
            var resultado2 = _sut.Hash("MiPassword123!");

            // Assert (Comprobar)
            // Assert.NotEqual = "xUnit, falla el test si estos dos valores SON iguales"
            Assert.NotEqual(resultado1.Salt, resultado2.Salt);
            Assert.NotEqual(resultado1.Hash, resultado2.Hash);
        }

        // ============================================================
        // Test 2: Si hasheas la misma contraseña con el MISMO salt,
        // el hash debe ser IGUAL. Esto permite verificar contraseñas.
        // ============================================================
        [Fact]
        public void Hash_MismaPassword_MismoSalt_ProduceMismoHash()
        {
            // Arrange
            // Creamos un salt fijo (en la vida real nunca harías esto, pero
            // para el test nos sirve para comprobar que el algoritmo es determinista).
            var salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

            // Act
            // Sobrecarga del método que acepta salt manual (la usas en VerifyPassword).
            var resultado1 = _sut.Hash("PasswordFijo", salt);
            var resultado2 = _sut.Hash("PasswordFijo", salt);

            // Assert
            // Assert.Equal = "xUnit, falla el test si estos dos valores NO son iguales"
            Assert.Equal(resultado1.Hash, resultado2.Hash);
            Assert.Equal(resultado1.Salt, resultado2.Salt);
        }

        // ============================================================
        // Test 3: Si cambias un solo carácter de la contraseña,
        // el hash debe ser COMPLETAMENTE diferente (avalancha).
        // ============================================================
        [Fact]
        public void Hash_DiferentePassword_MismoSalt_ProduceHashDiferente()
        {
            // Arrange
            var salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

            // Act
            var resultado1 = _sut.Hash("PasswordA", salt);
            var resultado2 = _sut.Hash("PasswordB", salt);

            // Assert
            Assert.NotEqual(resultado1.Hash, resultado2.Hash);
        }

        // ============================================================
        // Test 4: Verificamos que el hash generado cumple el formato Base64.
        // PBKDF2 con 32 bytes en Base64 siempre produce 44 caracteres.
        // ============================================================
        [Fact]
        public void Hash_Resultado_HashEsBase64Valido()
        {
            // Act
            var resultado = _sut.Hash("CualquierPassword");

            // Assert
            Assert.NotNull(resultado.Hash);          // No debe ser null
            Assert.NotEmpty(resultado.Hash);         // No debe estar vacío
            Assert.Equal(44, resultado.Hash.Length); // Base64 de 32 bytes = 44 chars
            Assert.DoesNotContain(" ", resultado.Hash); // Base64 nunca tiene espacios
        }

        // ============================================================
        // Test 5: Verificamos que el salt siempre tiene 16 bytes.
        // Si un día cambias el tamaño del salt en el código, este test fallará.
        // ============================================================
        [Fact]
        public void Hash_Resultado_SaltTiene16Bytes()
        {
            // Act
            var resultado = _sut.Hash("CualquierPassword");

            // Assert
            Assert.NotNull(resultado.Salt);
            Assert.Equal(16, resultado.Salt.Length);
        }
    }
}