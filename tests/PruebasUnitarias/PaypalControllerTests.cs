using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.Playwright.Assertions;
namespace PruebasUnitarias
{
    public class PaypalControllerTests: IAsyncLifetime
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IPage _page;
        // ================================================================
        // NOTA IMPORTANTE SOBRE TESTS E2E DE PAYPAL
        // ================================================================
        // Muchos flujos de PayPal son inestables para automatización debido a:
        // - Llamadas externas a la API de PayPal (tiempos variables)
        // - Renderizado dinámico y animaciones en el frontend
        // - Formularios dentro de bucles (múltiples botones iguales)
        // - Redirecciones y estados asíncronos
        //
        // Decisión tomada: 
        // → Solo se automatizan tests simples y estables.
        // → Los flujos complejos (Desactivar plan, Crear producto+plan, etc.) 
        //   se verifican manualmente.
        //
        // Última verificación manual: 01 de Abril de 2026
        // ================================================================
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
        public async Task Mostrar_Productos_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de proveedores");
                await _page.GotoAsync("https://localhost:7056/Paypal/MostrarProductos");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                // Mejor opción
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Productos de PayPal" })).ToBeVisibleAsync();
                await _page.ScreenshotAsync(new() { Path = "paginaProductos.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-productos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Planes_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de planes");
                await _page.GotoAsync("https://localhost:7056/Paypal/MostrarPlanes");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                // Mejor opción
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Planes de Suscripción de PayPal" })).ToBeVisibleAsync();
                await _page.ScreenshotAsync(new() { Path = "paginaPlanes.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-planes.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Suscripciones_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de suscripciones");
                await _page.GotoAsync(" https://localhost:7056/Paypal/TodasSuscripciones");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                // Mejor opción
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Suscripciones" })).ToBeVisibleAsync();
                await _page.ScreenshotAsync(new() { Path = "paginaSuscripciones.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-suscripciones.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Suscripciones_Usuario_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de suscripciones");
                await _page.GotoAsync(" https://localhost:7056/Paypal/ObtenerSuscripcionUsuario");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                // Mejor opción
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Suscripciones" })).ToBeVisibleAsync();
                await _page.ScreenshotAsync(new() { Path = "paginaSuscripciones.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-suscripciones.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Detalles_Suscripcion_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de suscripciones");
                await _page.GotoAsync("  https://localhost:7056/Paypal/DetallesSuscripcion/I-5UJUFDFBU98U");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                // Mejor opción
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Detalles de la Suscripción" })).ToBeVisibleAsync();
                await _page.ScreenshotAsync(new() { Path = "paginaSuscripciones.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-suscripciones.png", FullPage = true });
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
