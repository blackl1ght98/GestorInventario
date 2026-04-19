using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.Playwright.Assertions;
namespace PruebasUnitarias
{
    public class CarritoControllerTest 
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
        public async Task Mostrar_Productos_Carrito_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de carrito");
                await _page.GotoAsync("https://localhost:7057/Carrito");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Tu Carrito de Compras", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 5000 });
                await _page.ScreenshotAsync(new() { Path = "carrito.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-carrito.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Checkout_Redirecciona_A_PayPal_Exito()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("✅ Login exitoso");

                await _page.GotoAsync("https://localhost:7057/Carrito");
                Console.WriteLine("📦 En la página del carrito");

                var checkoutButton = _page.GetByRole(AriaRole.Button, new() { Name = "Checkout" });
                await Expect(checkoutButton).ToBeVisibleAsync(new() { Timeout = 15000 });
                await Expect(checkoutButton).ToBeEnabledAsync(new() { Timeout = 10000 });

                // ───────────────────────────────────────────────────────────────
                // Espera más fiable para redirección externa a PayPal
                // ───────────────────────────────────────────────────────────────
                var paypalRedirectTask = _page.WaitForURLAsync("**sandbox.paypal.com**", new()
                {
                    Timeout = 30000,                    // más tiempo (PayPal es lento)
                    WaitUntil = WaitUntilState.Load     // menos estricto que NetworkIdle
                });

                Console.WriteLine("🖱️ Haciendo clic en Checkout...");
                await checkoutButton.ClickAsync();

                // Esperamos la redirección
                await paypalRedirectTask;

                string currentUrl = _page.Url;
                Console.WriteLine($"✅ Redirección detectada: {currentUrl}");

                // Asserts
                Assert.Contains("sandbox.paypal.com", currentUrl, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("token=", currentUrl);   // PayPal siempre devuelve token

                await _page.ScreenshotAsync(new() { Path = "success-checkout-paypal.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en Checkout: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-checkout-paypal.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Incrementar_Producto_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("✅ Login exitoso");

                // 1. Ir a la página del carrito
                await _page.GotoAsync("https://localhost:7057/Carrito");
                Console.WriteLine("📦 En la página del carrito");

                // 2. Esperar que el botón de incrementar esté visible
                var botonIncrementar = _page.Locator("form[action*='Incrementar'] button");
                await Expect(botonIncrementar).ToBeVisibleAsync(new() { Timeout = 15000 });

                // 3. Capturar la redirección o actualización después del clic
                var redirectTask = _page.WaitForURLAsync("**/Carrito**", new()
                {
                    Timeout = 15000,
                    WaitUntil = WaitUntilState.Load
                });

                // 4. Hacer clic en el botón +
                Console.WriteLine("➕ Haciendo clic en el botón de incrementar...");
                await botonIncrementar.ClickAsync();

                // 5. Esperar que vuelva al carrito
                await redirectTask;

                Console.WriteLine("✅ Incremento realizado correctamente");
                await _page.ScreenshotAsync(new() { Path = "success-incrementar-producto.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al incrementar: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-incrementar.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Decrementar_Producto_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("✅ Login exitoso");

                // 1. Ir a la página del carrito
                await _page.GotoAsync("https://localhost:7057/Carrito");
                Console.WriteLine("📦 En la página del carrito");

                // 2. Esperar que el botón de decrementar esté visible
                var botonIncrementar = _page.Locator("form[action*='Decrementar'] button");
                await Expect(botonIncrementar).ToBeVisibleAsync(new() { Timeout = 15000 });

                // 3. Capturar la redirección o actualización después del clic
                var redirectTask = _page.WaitForURLAsync("**/Carrito**", new()
                {
                    Timeout = 15000,
                    WaitUntil = WaitUntilState.Load
                });

                // 4. Hacer clic en el botón +
                Console.WriteLine("➕ Haciendo clic en el botón de decrementar...");
                await botonIncrementar.ClickAsync();

                // 5. Esperar que vuelva al carrito
                await redirectTask;

                Console.WriteLine("✅ Decremento realizado correctamente");
                await _page.ScreenshotAsync(new() { Path = "success-decremento-producto.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al incrementar: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-decremento.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task EliminarProductoCarrito_Redirecciona_A_Index_Exito()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("✅ Login exitoso");

                // 1. Ir al carrito (asegúrate de que tenga al menos un producto)
                await _page.GotoAsync("https://localhost:7057/Carrito");
                Console.WriteLine("📦 En la página del carrito");

                // 2. Esperar que exista al menos un botón de eliminar
                var botonEliminar = _page.Locator("form[action*='EliminarProductoCarrito'] button");
                await Expect(botonEliminar).ToBeVisibleAsync(new() { Timeout = 15000 });
                await Expect(botonEliminar).ToBeEnabledAsync(new() { Timeout = 10000 });

                // 3. Capturar la redirección de vuelta al carrito
                var redirectTask = _page.WaitForURLAsync("**/Carrito**", new()
                {
                    Timeout = 15000,
                    WaitUntil = WaitUntilState.Load
                });

                Console.WriteLine("🗑️ Haciendo clic en el botón Eliminar...");
                await botonEliminar.First.ClickAsync();  // .First porque puede haber varios

                // 4. Esperar que vuelva al carrito
                await redirectTask;

                string currentUrl = _page.Url;
                Console.WriteLine($"✅ Redirección a carrito detectada: {currentUrl}");

                // Asserts principales
                Assert.Contains("/Carrito", currentUrl, StringComparison.OrdinalIgnoreCase);

                // Opcional: verificar que no hay mensaje de error visible
                await Expect(_page.Locator(".alert.alert-danger")).Not.ToBeVisibleAsync(new() { Timeout = 5000 });

                await _page.ScreenshotAsync(new() { Path = "success-eliminar-producto.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al eliminar producto: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-eliminar-producto.png", FullPage = true });
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
       
    }
}
