using System;
using System.Linq;
using System.Security.Claims;
using GestorInventario.Application.Services.Authentication.Jwt;
using GestorInventario.Domain.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GestorInventario.PruebasUnitarias.Application.Services.Authentication.Jwt
{
    /// <summary>
    /// Tests para JwtTokenSettings.
    /// Esta clase es 99% infraestructura (lectura de config, carga de claves RSA),
    /// pero tiene una regla de negocio pura: no generar claims si el usuario no tiene rol.
    /// </summary>
    public class JwtTokenSettingsTests
    {
        // Helper: crea el SUT con un IConfiguration vacío (no lo usamos en estos tests).
        private static JwtTokenSettings CrearSut()
        {
            var configMock = new Mock<IConfiguration>();
            return new JwtTokenSettings(configMock.Object);
        }

        // -----------------------------------------------------------
        // Test 1: Si el usuario no tiene rol cargado, no se pueden generar claims.
        // Debe lanzar excepción con mensaje descriptivo.
        // -----------------------------------------------------------
        [Fact]
        public void CrearClaims_UsuarioSinRol_LanzaInvalidOperationException()
        {
            // Arrange
            var sut = CrearSut();
            var usuarioSinRol = new Usuario
            {
                Id = 1,
                Email = "pepe@test.com"
                // IdRolNavigation es null por defecto
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => sut.CrearClaims(usuarioSinRol));

            Assert.Contains("no tiene un rol asignado", ex.Message);
        }

        // -----------------------------------------------------------
        // Test 2: Si el usuario tiene rol, los claims deben contener
        // email, rol y el ID del usuario como NameIdentifier.
        // -----------------------------------------------------------
        [Fact]
        public void CrearClaims_UsuarioConRol_DevuelveClaimsCorrectos()
        {
            // Arrange
            var sut = CrearSut();
            var usuario = new Usuario
            {
                Id = 5,
                Email = "admin@test.com",
                IdRolNavigation = new Role { Nombre = "Administrador" }
            };

            // Act
            var claims = sut.CrearClaims(usuario);

            // Assert
            Assert.Equal(3, claims.Count);

            // Verificamos que existen los 3 claims esperados con los valores correctos
            Assert.Contains(claims, c =>
                c.Type == ClaimTypes.Email && c.Value == "admin@test.com");

            Assert.Contains(claims, c =>
                c.Type == ClaimTypes.Role && c.Value == "Administrador");

            Assert.Contains(claims, c =>
                c.Type == ClaimTypes.NameIdentifier && c.Value == "5");
        }
    }
}