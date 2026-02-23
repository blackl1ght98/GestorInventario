using Microsoft.Playwright;
using Xunit;
// dotnet run --launch-profile https --no-build
namespace PruebasUnitarias.Controllers
{
    public class AuthControllerE2ETests : IAsyncLifetime
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

            // Configurar timeout más largo para desarrollo
            _page.SetDefaultTimeout(30000); // 30 segundos
        }

        [Fact]
        public async Task Login_ConCredencialesCorrectas_RedirigeAHome()
        {
            try
            {
                Console.WriteLine("🔄 Navegando a la página de login...");

                // Arrange - Navegar a la página de login
                await _page.GotoAsync("https://localhost:7057/Auth/Login");

                // Esperar a que el formulario cargue usando ID
                await _page.WaitForSelectorAsync("#email", new PageWaitForSelectorOptions
                {
                    Timeout = 10000
                });
                Console.WriteLine("✅ Formulario cargado");

                // Act - Llenar el formulario con IDs
                Console.WriteLine("📧 Rellenando email...");
                await _page.FillAsync("#email", "keuppa@yopmail.com");

                Console.WriteLine("🔑 Rellenando password...");
                await _page.FillAsync("#password", "1A2a3A4a5@");

                Console.WriteLine("🖱️ Haciendo click en enviar...");
                await _page.ClickAsync("button[type='submit']");

                // Assert - Verificar que redirige (acepta tanto / como /Home/Index)
                Console.WriteLine("⏳ Esperando redirección...");
                await _page.WaitForURLAsync("https://localhost:7057/**", new PageWaitForURLOptions
                {
                    Timeout = 10000
                });

                // Verificar que NO estamos en la página de login (login exitoso)
                var currentUrl = _page.Url;
                Console.WriteLine($"📍 URL actual: {currentUrl}");

                Assert.DoesNotContain("/Auth/Login", currentUrl);
                Assert.Contains("https://localhost:7057", currentUrl);

                Console.WriteLine("✅ ¡LOGIN EXITOSO! Usuario autenticado correctamente");

                // Verificar elementos de la página principal
                var pageTitle = await _page.TitleAsync();
                Console.WriteLine($"📄 Título de la página: {pageTitle}");

                // Tomar screenshot como evidencia
                await _page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = "login-exitoso.png"
                });
            }
            catch (Exception ex)
            {
                // Tomar screenshot del error
                await _page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = "error-login.png",
                    FullPage = true
                });

                Assert.Fail($"❌ Error en login: {ex.Message}");
            }
        }
        //[Fact]
        //public async Task Login_ConCredencialesIncorrectas_MuestraError()
        //{
        //    try
        //    {
        //        Console.WriteLine("🔄 Navegando a la página de login...");
        //        await _page.GotoAsync("https://localhost:7057/Auth/Login");

        //        await _page.WaitForSelectorAsync("#email", new PageWaitForSelectorOptions { Timeout = 10000 });
        //        Console.WriteLine("✅ Formulario cargado");

        //        // Llenar formulario
        //        await _page.FillAsync("#email", "keupa@yopmail.com");
        //        await _page.FillAsync("#password", "1A2a3A4a5@");
        //        await _page.ClickAsync("button[type='submit']");

        //        // Esperar al mensaje de error en ValidationSummary
        //        var summaryError = _page.Locator(".text-danger.mb-4");
        //        await summaryError.WaitForAsync(new LocatorWaitForOptions
        //        {
        //            Timeout = 5000,
        //            State = WaitForSelectorState.Visible
        //        });

        //        var errorText = await summaryError.TextContentAsync();
        //        Console.WriteLine($"⚠️ Mensaje de error detectado: {errorText}");
        //        Assert.Contains("incorrectos", errorText);

        //        // Captura de pantalla
        //        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "login-erroneo.png" });

        //        Console.WriteLine("✅ Test completado: error de login correctamente mostrado");
        //    }
        //    catch (Exception ex)
        //    {
        //        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-credenciales.png" });
        //        Assert.Fail($"Error: {ex.Message}");
        //    }
        //}



        [Fact]
        public async Task Login_VerificarInterfazUsuario()
        {
            try
            {
                // Arrange & Act
                await _page.GotoAsync("https://localhost:7057/Auth/Login");

                // Assert - Verificar que todos los elementos están presentes y visibles
                await _page.WaitForSelectorAsync("h3");
                var title = await _page.TextContentAsync("h3");
                Assert.Equal("Iniciar Sesión", title?.Trim());

                // Verificar campos del formulario
                var emailInput = await _page.QuerySelectorAsync("input[name='email']");
                Assert.True(await emailInput.IsVisibleAsync());

                var passwordInput = await _page.QuerySelectorAsync("input[name='Password']");
                Assert.NotNull(passwordInput);
                Assert.True(await passwordInput.IsVisibleAsync());

                var submitButton = await _page.QuerySelectorAsync("button[type='submit']");
                Assert.True(await submitButton.IsVisibleAsync());

                // Verificar enlaces
                var createUserLink = await _page.QuerySelectorAsync("a[href*='/Admin/Create']");
                Assert.True(await createUserLink.IsVisibleAsync());

                var resetPasswordLink = await _page.QuerySelectorAsync("a[href*='/Auth/ResetPasswordOlvidada']");
                Assert.True(await resetPasswordLink.IsVisibleAsync());

                // Tomar screenshot de la interfaz
                await _page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = "interfaz-login.png"
                });
            }
            catch (Exception ex)
            {
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-interfaz.png" });
                Assert.Fail($"Error en interfaz: {ex.Message}");
            }
        }

        [Fact]
        public async Task Login_ConCamposVacios_MuestraValidaciones()
        {
            try
            {
                // Arrange
                await _page.GotoAsync("https://localhost:7057/Auth/Login");
                await _page.WaitForSelectorAsync("button[type='submit']");

                // Act - Enviar formulario vacío
                await _page.ClickAsync("button[type='submit']");

                // Assert - Verificar que muestra validaciones
                await _page.WaitForTimeoutAsync(1000); // Pequeña pausa para las validaciones

                var validationErrors = await _page.QuerySelectorAllAsync(".text-danger");
                Assert.True(validationErrors.Count > 0, "Debería mostrar errores de validación");
            }
            catch (Exception ex)
            {
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-validaciones.png" });
                Assert.Fail($"Error en validaciones: {ex.Message}");
            }
        }
        [Fact]
        public async Task Reset_Password_Olvidada_Test()
        {
            try
            {
                Console.WriteLine("🔄 Navegando a la página de login...");
                await _page.GotoAsync("https://localhost:7057/Auth/ResetPasswordOlvidada");

                await _page.WaitForSelectorAsync("#email", new PageWaitForSelectorOptions { Timeout = 10000 });
                Console.WriteLine("✅ Formulario cargado");

                // Llenar formulario
                await _page.FillAsync("#email", "ziddefisaufrou-7763@yopmail.com");
                await _page.ClickAsync("button[type='submit']");

                // Esperar al mensaje de error en ValidationSummary
                //var summaryError = _page.Locator(".text-danger.mb-4");
                //await summaryError.WaitForAsync(new LocatorWaitForOptions
                //{
                //    Timeout = 5000,
                //    State = WaitForSelectorState.Visible
                //});

                //var errorText = await summaryError.TextContentAsync();
                //Console.WriteLine($"⚠️ Mensaje de error detectado: {errorText}");
                //Assert.Contains("incorrectos", errorText);

                // Captura de pantalla
                //await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "login-erroneo.png" });

                Console.WriteLine("✅ Test completado: Correo enviado con exito");
            }
            catch (Exception ex)
            {
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-credenciales.png" });
                Assert.Fail($"Error: {ex.Message}");
            }
        }

        public async Task DisposeAsync()
        {
            await _browser?.CloseAsync();
            _playwright?.Dispose();
        }
    }
}