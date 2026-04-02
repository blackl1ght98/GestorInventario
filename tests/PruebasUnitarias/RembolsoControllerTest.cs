using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.Playwright.Assertions;
namespace PruebasUnitarias
{
    public class RembolsoControllerTest: IAsyncLifetime
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
        public async Task Mostrar_Rembolsos_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de rembolsos");
                await _page.GotoAsync("  https://localhost:7056/Rembolso");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                // Mejor opción
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Gestión de Rembolsos" })).ToBeVisibleAsync();
                await _page.ScreenshotAsync(new() { Path = "paginaRembolsos.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-suscripciones.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        //TEST DE REFERENCIA: MANEJO DE MODAL JS
        [Fact]
        public async Task Eliminar_Rembolso_Exito()
        {
            await PerformSuccessfulLoginAsync();

            // 1. Ir a la lista de reembolsos
            await _page.GotoAsync("https://localhost:7056/Rembolso");

            await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Gestión de Rembolsos" }))
                  .ToBeVisibleAsync(new() { Timeout = 10000 });

            // 2. Seleccionar el primer botón de eliminar (o el que quieras probar)
            var botonEliminar = _page.Locator(".user-action-button[data-user-action='Eliminar']").First;
            var rembolsoId = await botonEliminar.GetAttributeAsync("data-rembolso-id");

            await botonEliminar.ClickAsync();

            // 3. Esperar a que se abra el modal de confirmación
            await Expect(_page.Locator("#confirmModal")).ToBeVisibleAsync(new() { Timeout = 8000 });

            // 4. Confirmar la eliminación
            await _page.Locator("#confirmActionBtn").ClickAsync();

            // 5. Esperar a que el modal se cierre
            await Expect(_page.Locator("#confirmModal")).ToBeHiddenAsync(new() { Timeout = 10000 });

            // 6. Esperar la redirección de vuelta a la lista
            await _page.WaitForURLAsync("**/Rembolso**", new() { Timeout = 15000 });

            // 7. Verificar que el rembolso ya no aparece
            await Expect(_page.Locator($"[data-rembolso-id='{rembolsoId}']")).ToHaveCountAsync(0);

            await _page.ScreenshotAsync(new() { Path = "eliminar-rembolso-exito.png", FullPage = true });

            Console.WriteLine($"✅ Test OK - Reembolso {rembolsoId} eliminado correctamente");
        }
        private async Task PerformSuccessfulLoginAsync()
        {
            Console.WriteLine("🔄 Realizando login exitoso (método reutilizable)...");

            await _page.GotoAsync("https://localhost:7057/Auth/Login");
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await _page.WaitForSelectorAsync("#email", new PageWaitForSelectorOptions { Timeout = 15000 });
            await _page.FillAsync("#email", "keuppa@yopmail.com");
            await _page.FillAsync("#password", "1A2a3A4a5@");

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
