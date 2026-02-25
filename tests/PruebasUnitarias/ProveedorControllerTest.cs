using Microsoft.Playwright;
using NuGet.ContentModel;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.Playwright.Assertions;
namespace PruebasUnitarias
{
    public class ProveedorControllerTest:IAsyncLifetime
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
        public async Task Mostrar_Proveedores_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de proveedores");
                await _page.GotoAsync("https://localhost:7057/Proveedor");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByText("Gestión de Proveedores", new() { Exact = false })).ToBeVisibleAsync(new() { Timeout = 1000 });
                await _page.ScreenshotAsync(new() { Path = "paginaProveedores.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-provedor.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Crear_Proveedor_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                await _page.GotoAsync("https://localhost:7057/Proveedor/Create");
                Console.WriteLine("Rellenando formulario");
                await _page.FillAsync("#NombreProveedor", "Provedor Test");
                await _page.FillAsync("#Contacto", "provedortest@gmail.com");
                await _page.FillAsync("#Direccion", "direccion proveedor test");
                await _page.Locator("#IdUsuario").SelectOptionAsync("Rosa perez lopez");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Proveedor**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });
                // 1. No debería haber mensaje de error visible
                await Expect(_page.Locator(".alert.alert-danger, .text-danger, [role='alert']:has-text('error')"))
                    .Not.ToBeVisibleAsync(new() { Timeout = 8000 });
                await Expect(_page.GetByText("Gestión de Proveedores", new() { Exact = false })).ToBeVisibleAsync(new() { Timeout = 1000 });

                Console.WriteLine("✅ Proveedor creado con exito");
            }
            catch (Exception ex) {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-crear-provedor.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
            

        }
        [Fact]
        public async Task Eliminar_Proveedor_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                int id = 38;
                string url = $"https://localhost:7057/Proveedor/Delete/{id}";
                await _page.GotoAsync(url);
                //Esto sirve para inputs con comportamiento de boton
                await _page.GetByRole(AriaRole.Button, new() { Name = "Eliminar" }).ClickAsync(new() { Timeout = 10000 });
                await _page.WaitForURLAsync("**/Proveedor**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });
                await Expect(_page.Locator(".alert.alert-danger, .text-danger, [role='alert']:has-text('error')"))
              .Not.ToBeVisibleAsync(new() { Timeout = 8000 });
                await Expect(_page.GetByText("Gestión de Proveedores", new() { Exact = false })).ToBeVisibleAsync(new() { Timeout = 1000 });
                Console.WriteLine("✅ Proveedor eliminado con exito");
            }
            catch (Exception ex) {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-eliminar-provedor.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
           

        }
        [Fact]
        public async Task Editar_Proveedor_Test()
        {
            try
            {

                await PerformSuccessfulLoginAsync();
                int id = 37;
                await _page.GotoAsync($"https://localhost:7057/Proveedor/Edit/{id}");
                Console.WriteLine("Rellenando formulario");
                await _page.FillAsync("#NombreProveedor", "Provedor Test 2");
                await _page.FillAsync("#Contacto", "provedortes2@gmail.com");
                await _page.FillAsync("#Direccion", "direccion proveedor 2");
                await _page.Locator("#IdUsuario").SelectOptionAsync("Rosa perez lopez");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Proveedor**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });
                await Expect(_page.Locator(".alert.alert-danger, .text-danger, [role='alert']:has-text('error')"))
              .Not.ToBeVisibleAsync(new() { Timeout = 8000 });
                await Expect(_page.GetByText("Gestión de Proveedores", new() { Exact = false })).ToBeVisibleAsync(new() { Timeout = 1000 });
                Console.WriteLine("✅ Proveedor editado con exito");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-editar-provedor.png", FullPage = true });
                Assert.Fail(ex.Message);
            }


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
