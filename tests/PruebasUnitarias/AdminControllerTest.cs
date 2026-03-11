using Microsoft.Playwright;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using static Microsoft.Playwright.Assertions;
namespace PruebasUnitarias
{
    public class AdminControllerTest:IAsyncLifetime
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IPage _page;

        public async Task InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();

            // Usar TU Chrome instalado
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false, // TRUE si no quieres ver el navegador
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
            });

            _page = await _browser.NewPageAsync();

            // Configurar timeout más largo (30 segundos) para desarrollo
            _page.SetDefaultTimeout(30000);
        }
        [Fact]
        public async Task Mostrar_Usuarios_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de administracion");
                await _page.GotoAsync("https://localhost:7057/Admin");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Gestión de Usuarios", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 5000 });
                await _page.ScreenshotAsync(new() { Path = "paginaUsuarios.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-usuarios.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Crear_Usuario_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de administracion");
                await _page.GotoAsync("https://localhost:7057/Admin");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Gestión de Usuarios", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 5000 });
                await _page.ClickAsync("a:has-text('Crear Usuario')");
                await _page.FillAsync("#Email", "seihajajoippei-3036@yopmail.com");
                await _page.FillAsync("#Password", "1a2a3a4a5");
                await _page.Locator("#IdRol").SelectOptionAsync("Administrador");
                await _page.FillAsync("#NombreCompleto", "Usuario Prueba");
                await _page.FillAsync("#FechaNacimiento", "1969-11-10");
                await _page.FillAsync("#Telefono", "258456785");
                await _page.FillAsync("#Direccion", "Direccion Prueba");
                await _page.FillAsync("#Ciudad", "Ciudad Prueba");
                await _page.FillAsync("#CodigoPostal", "14550");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Admin**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });
              

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-crear-usuario.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Crear_Usuario_Campos_Vacios_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de administracion");
                await _page.GotoAsync("https://localhost:7057/Admin");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Gestión de Usuarios", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 5000 });
                await _page.ClickAsync("a:has-text('Crear Usuario')");
                await _page.FillAsync("#Email", "");
                await _page.FillAsync("#Password", "");
                await _page.Locator("#IdRol").SelectOptionAsync("");
                await _page.FillAsync("#NombreCompleto", "");
                await _page.FillAsync("#FechaNacimiento", "");
                await _page.FillAsync("#Telefono", "");
                await _page.FillAsync("#Direccion", "");
                await _page.FillAsync("#Ciudad", "");
                await _page.FillAsync("#CodigoPostal", "");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Admin/**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

                // Verifica errores de validación
                await Expect(_page.Locator("span.text-danger:has-text('El email es requerido')"))
                .ToBeVisibleAsync(new() { Timeout = 10000 });
                await Expect(_page.Locator("span.text-danger:has-text('La contraseña es requerida')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });
                
                await Expect(_page.Locator("span.text-danger:has-text('El nombre completo es requerido')"))
                .ToBeVisibleAsync(new() { Timeout = 10000 });
                await Expect(_page.Locator("span.text-danger:has-text('La fecha de nacimiento es requerida')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });
               

                await Expect(_page.Locator("span.text-danger:has-text('El telefono es requerido')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });

                await Expect(_page.Locator("span.text-danger:has-text('La dirección es requerida')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });

                await Expect(_page.Locator("span.text-danger:has-text('La ciudad es requerida')"))
                .ToBeVisibleAsync(new() { Timeout = 10000 });

                await Expect(_page.Locator("span.text-danger:has-text('El codigo postal es requerido')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-crear-usuario.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Confirmar_Registro_TokenValido_RedirectToLogin()
        {
            await _page.GotoAsync(" https://localhost:7057/admin/confirm-registration/49/z59xZfdWU0qoD2C41E7Z1g?redirect=true");
            // Espera redirección
            await _page.WaitForURLAsync("**/Auth/Login");

          
        }
        [Fact]
        public async Task Confirmar_Registro_TokenValido_RedirectToLogin_Mismo_Usuario()
        {
            await _page.GotoAsync(" https://localhost:7057/admin/confirm-registration/49/z59xZfdWU0qoD2C41E7Z1g?redirect=true");
            // Espera redirección
            await _page.WaitForURLAsync("**/Auth/Login");
            await Expect(_page.Locator(".alert.alert-warning"))
              .ToBeVisibleAsync(new() { Timeout = 8000 });

        }
        [Fact]
        public async Task Editar_Usuario_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando editar usuario");
                await _page.GotoAsync("https://localhost:7057/Admin/Edit/11");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                      
                await _page.FillAsync("#Email", "tofradettausseu-5338@yopmail.com");
               
                await _page.Locator("#IdRol").SelectOptionAsync("Administrador");
                await _page.FillAsync("#NombreCompleto", "Usuario Actualizado");
                await _page.FillAsync("#FechaNacimiento", "1969-11-10");
                await _page.FillAsync("#Telefono", "258456785");
                await _page.FillAsync("#Direccion", "Direccion Prueba");
                await _page.FillAsync("#Ciudad", "Ciudad Prueba");
                await _page.FillAsync("#CodigoPostal", "14550");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Admin**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });


            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-editar-usuario.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Editar_Usuario_Campos_Vacios_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando editar usuario");
                await _page.GotoAsync("https://localhost:7057/Admin/Edit/11");
                Console.WriteLine("Confirmando que estamos en esa pagina");

                await _page.FillAsync("#Email", "");

                await _page.Locator("#IdRol").SelectOptionAsync("Administrador");
                await _page.FillAsync("#NombreCompleto", "");
                await _page.FillAsync("#FechaNacimiento", "");
                await _page.FillAsync("#Telefono", "");
                await _page.FillAsync("#Direccion", "");
                await _page.FillAsync("#Ciudad", "");
                await _page.FillAsync("#CodigoPostal", "");
                await _page.ClickAsync("button[type='submit']");

                await Expect(_page.Locator("span.text-danger:has-text('El email es requerido')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });
              

                await Expect(_page.Locator("span.text-danger:has-text('El nombre completo es requerido')"))
                .ToBeVisibleAsync(new() { Timeout = 10000 });
                await Expect(_page.Locator("span.text-danger:has-text('La fecha de nacimiento es requerida')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });


                await Expect(_page.Locator("span.text-danger:has-text('El telefono es requerido')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });

                await Expect(_page.Locator("span.text-danger:has-text('La dirección es requerida')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });

                await Expect(_page.Locator("span.text-danger:has-text('La ciudad es requerida')"))
                .ToBeVisibleAsync(new() { Timeout = 10000 });

                await Expect(_page.Locator("span.text-danger:has-text('El codigo postal es requerido')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-editar-usuario.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Eliminar_Usuario_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando eliminar usuario");
                await _page.GotoAsync("https://localhost:7056/Admin/Delete/49");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Admin**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-eliminar-usuario.png", FullPage = true });
                Assert.Fail(ex.Message);
            }

        }
        [Fact]
        public async Task Eliminar_Usuario_Inexistente_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando eliminar el usuario");
                await _page.GotoAsync("https://localhost:7056/Admin/Delete/49");
                Console.WriteLine("Confirmando que estamos en esa pagina");               
                await _page.WaitForURLAsync("**/Admin**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-eliminar-usuario.png", FullPage = true });
                Assert.Fail(ex.Message);
            }

        }
        [Fact]
        public async Task Mostrar_Roles_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de roles");
                await _page.GotoAsync("https://localhost:7056/Admin/ObtenerRoles");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Gestión de Roles", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 5000 });
                await _page.ScreenshotAsync(new() { Path = "paginaRoles.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-usuarios.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Roles_Usuario_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de roles");
                await _page.GotoAsync("https://localhost:7056/Admin/VerUsuariosPorRol/2");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Usuarios del Rol Administrador", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 5000 });
                await _page.ScreenshotAsync(new() { Path = "paginaRoles.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-usuarios.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task BajaUsuarioPost_ComoAdmin_Exito()
        {
            await PerformSuccessfulLoginAsync();

            var usuarioId = 47;

            var response = await _page.APIRequest.PostAsync("https://localhost:7056/Admin/BajaUsuarioPost", new()
            {
                IgnoreHTTPSErrors = true, 
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                DataObject = new { Id = usuarioId }
            });

            Assert.Equal(200, response.Status);
         
        }
        [Fact]
        public async Task AltaUsuarioPost_ComoAdmin_Exito()
        {
            await PerformSuccessfulLoginAsync();

            var usuarioId = 47;

            var response = await _page.APIRequest.PostAsync("https://localhost:7056/Admin/AltaUsuarioPost", new()
            {
                IgnoreHTTPSErrors = true,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                DataObject = new { Id = usuarioId }
            });

            Assert.Equal(200, response.Status);

        }
        [Fact]
        public async Task CambiarRol_ComoAdmin_Exito()
        {
            await PerformSuccessfulLoginAsync();

            var usuarioId = 47;
            var rolId = 3;

            var response = await _page.APIRequest.PostAsync("https://localhost:7056/Admin/CambiarRol", new()
            {
                IgnoreHTTPSErrors = true,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                DataObject = new { Id = usuarioId,RolId=rolId }
            });

            Assert.Equal(200, response.Status);

        }
       
        private async Task PerformSuccessfulLoginAsync()
        {
            Console.WriteLine("🔄 Realizando login exitoso (método reutilizable)...");

            await _page.GotoAsync("https://localhost:7057/Auth/Login");
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await _page.WaitForSelectorAsync("#email", new PageWaitForSelectorOptions { Timeout = 15000 });
            await _page.FillAsync("#email", "keuppa@yopmail.com");
            await _page.FillAsync("#password", "1A2a3A4a5");

            await _page.ClickAsync("button[type='submit']");

            // Esperamos redirección fuera de login
            await _page.WaitForURLAsync("**", new PageWaitForURLOptions
            {
                Timeout = 15000,

            });

            // Verificación mínima de que el login fue exitoso
            Assert.DoesNotContain("/Auth/Login", _page.Url);

            // Esperamos que aparezca el menú de usuario autenticado
            await _page.WaitForSelectorAsync("#userDropdown", new PageWaitForSelectorOptions
            {
                Timeout = 15000,
                State = WaitForSelectorState.Visible
            });

            Console.WriteLine("✅ Login exitoso (reutilizable)");
        }
        public async Task DisposeAsync()
        {
            await _browser?.CloseAsync();
            _playwright?.Dispose();
        }
    }
}
