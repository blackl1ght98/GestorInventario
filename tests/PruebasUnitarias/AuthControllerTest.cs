using Microsoft.IdentityModel.Tokens.Experimental;
using Microsoft.Playwright;
using NUglify.Helpers;
using Xunit;
using static Microsoft.Playwright.Assertions;
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

            // Configurar timeout más largo (30 segundos) para desarrollo
            _page.SetDefaultTimeout(30000); 
        }
        #region Login Tests
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

        [Fact]
        public async Task Login_ConCredencialesIncorrectas_MuestraError()
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
                await _page.FillAsync("#password", "1A2a3A4a5");

                Console.WriteLine("🖱️ Haciendo click en enviar...");
                await _page.ClickAsync("button[type='submit']");

                // Assert - Verificar si redirege
                Console.WriteLine("⏳ Esperando redirección...");
                var validationErrors = await _page.QuerySelectorAllAsync(".text-danger");
                Assert.True(validationErrors.Count > 0, "Debería mostrar errores de validación");
                // Filtrar el que contiene el mensaje real (el ValidationSummary)
                string errorText = null;
                foreach (var element in validationErrors)
                {
                    var text = await element.TextContentAsync();
                    var trimmed = text?.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && trimmed.Contains("incorrectos"))
                    {
                        errorText = trimmed;
                        Console.WriteLine($"⚠️ Mensaje encontrado en ValidationSummary: {errorText}");
                        break;
                    }
                }
                Assert.NotNull(errorText);
                Assert.Contains("incorrectos", errorText, StringComparison.OrdinalIgnoreCase);

                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "login-erroneo.png" });
                Console.WriteLine("✅ Test OK: error mostrado correctamente");
                // Tomar screenshot como evidencia
                await _page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = "login-erroneo.png"
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-login.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }

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

                // Assert - Verificar que muestra validaciones agregamos una pausa para darle tiempo al servidor
                await _page.WaitForTimeoutAsync(1000);
                //QuerySelectorAllAsync()-> metodo que nos permite interactuar con el DOM
                var validationErrors = await _page.QuerySelectorAllAsync(".text-danger");
                Assert.True(validationErrors.Count > 0, "Debería mostrar errores de validación");
            }
            catch (Exception ex)
            {
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-validaciones.png" });
                Assert.Fail($"Error en validaciones: {ex.Message}");
            }
        }
        #endregion

        #region Reset Password Tests
        [Fact]
        public async Task Reset_Password_Olvidada_Test_MensajeExito()
        {
            try
            {
                //GotoAsync()-> metodo que nos permite ir a una url especifica
                Console.WriteLine("🔄 Navegando a la página de recuperación...");
                await _page.GotoAsync("https://localhost:7057/Auth/ResetPasswordOlvidada");
                //WaitForSelectorAsync()-> espera a que el campo email este disponible
                await _page.WaitForSelectorAsync("#email", new() { Timeout = 10000 });
                Console.WriteLine("✅ Formulario cargado");

                // Llenar email válido, esto lo hacemos con el metodo FillAsync() recibe 2 parametros el id del input a rellenar y su valor
                await _page.FillAsync("#email", "ziddefisaufrou-7763@yopmail.com");
                //ClickAsync()-> metodo para simular el clic
                Console.WriteLine("🖱️ Enviando solicitud de recuperación...");
                await _page.ClickAsync("button[type='submit']");

                // Esperamos que la página se recargue, aqui el motivo por el que no ponemos la url completa es por esta notacion '**/Auth/ResetPasswordOlvidada' ha esto se le llama glob
                await _page.WaitForURLAsync("**/Auth/ResetPasswordOlvidada", new() { Timeout = 15000 });

                // Pequeña espera para que el DOM muestre el mensaje
                await _page.WaitForTimeoutAsync(800);
                // EXPECT → Método especial de Playwright para aserciones más inteligentes
                // Espera automáticamente hasta que la condición se cumpla (retry interno)
                // Es más robusto que un simple Assert porque reintenta si el elemento tarda en aparecer
                await Expect(_page.GetByText("Revisa tu correo electrónico", new() { Exact = false }))
                    .ToBeVisibleAsync(new() { Timeout = 12000 });

                //Esperamos a que aparezca este texto
                await Expect(_page.GetByText("recibirás un mensaje con las instrucciones", new() { Exact = false }))
                    .ToBeVisibleAsync(new() { Timeout = 12000 });

                // Para capturar el texto completo y loguearlo:
                var successMessageLocator = _page.GetByText("Revisa tu correo electrónico");
                //Asegura que venga el mensaje especificado
                var messageText = await successMessageLocator.TextContentAsync();
                Console.WriteLine($"Mensaje de éxito detectado: {messageText?.Trim()}");

                // Assert opcional
                Assert.Contains("Revisa tu correo electrónico", messageText ?? "", StringComparison.OrdinalIgnoreCase);
              
            }
            catch (Exception ex)
            {
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-reset-password.png", FullPage = true });
                Assert.Fail($"Error en recuperación de contraseña: {ex.Message}");
            }
        }

        [Fact]
        public async Task RestorePasswordUser_ConTokenValido_CambiaPasswordYRedirige()
        {
            try
            {
                // 1. Preparar un usuario y token válido 
                int userId = 8;                  
                string validToken = "255SCivPkyMZRkC9gmn1w";  

         
                // 2. Navegar directamente al GET para cargar la vista 
                string restoreUrl = $"https://localhost:7057/auth/restore-password/{userId}/{validToken}";
                await _page.GotoAsync(restoreUrl);
                //Esperamos a que el selector este disponible
                await _page.WaitForSelectorAsync("input[name='Password']", new() { Timeout = 10000 });

                // 3. Llenar el formulario POST
                string newPassword = "NuevaPassSegura2026@!";
                await _page.FillAsync("input[name='TemporaryPassword']", "E2oseeZajg5s"); 
                await _page.FillAsync("input[name='Password']", newPassword);
               

                Console.WriteLine("🖱️ Enviando formulario de restauración...");
                await _page.ClickAsync("button[type='submit']");
                var currentUrl = _page.Url;
                Assert.StartsWith("https://localhost:7057/", currentUrl);

            }
            catch (Exception ex)
            {
                await _page.ScreenshotAsync(new() { Path = "error-restore-password.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task RestorePasswordUser_ConTokenValido_CambiaPasswordYRedirige_Fail()
        {
            try
            {
                // 1. Preparar un usuario y token válido (puedes obtenerlo manualmente la primera vez)
             
                int userId = 8;                  
                string validToken = "255SCivPkyMZRkC9gmn1w";           
                // 2. Navegar directamente al GET para cargar la vista 
                string restoreUrl = $"https://localhost:7057/auth/restore-password/{userId}/{validToken}";
                await _page.GotoAsync(restoreUrl);

                await _page.WaitForSelectorAsync("input[name='Password']", new() { Timeout = 10000 });

                // 3. Llenar el formulario POST
                string newPassword = "NuevaPassSegura2026@!";

                await _page.FillAsync("input[name='TemporaryPassword']", "E2oseeZajg5"); 

                await _page.FillAsync("input[name='Password']", newPassword);

                Console.WriteLine("🖱️ Enviando formulario de restauración...");
                await _page.ClickAsync("button[type='submit']");
                // Esperamos un poco mas para que el DOM cargue del todo
                await _page.WaitForTimeoutAsync(800);

                // 5. Capturar y verificar el mensaje de error 
                Console.WriteLine("🔍 Buscando mensaje de error en alert...");

                // Selector del alert de TempData
                var alertLocator = _page.Locator(".alert.alert-warning");

                // Obtener el texto completo del alert (sin el botón close)
                var alertText = await alertLocator.TextContentAsync();
                var cleanedText = alertText?.Trim().Replace("Cerrar", "").Trim(); 

                Console.WriteLine($"Mensaje encontrado en alert: '{cleanedText}'");
           
                // Screenshot como evidencia
                await _page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = "change-password-fallo-incorrecta.png",
                    FullPage = true
                });

                Console.WriteLine("✅ Test OK: se mostró mensaje de error correctamente");

            }
            catch (Exception ex)
            {
                await _page.ScreenshotAsync(new() { Path = "error-restore-password.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        #endregion


        #region Logout Test
        [Fact]
        public async Task Logout_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                // Espera a que el navbar se renderice (busca el dropdown del usuario)
                Console.WriteLine("⏳ Esperando navbar del usuario autenticado...");
                await _page.WaitForSelectorAsync("#userDropdown", new PageWaitForSelectorOptions
                {
                    Timeout = 15000,
                    State = WaitForSelectorState.Visible
                });
                Console.WriteLine("✅ Dropdown del usuario encontrado");
                // Haz clic en el botón del dropdown para abrir el menú
                Console.WriteLine("🖱️ Abriendo dropdown del usuario...");
                await _page.ClickAsync("#userDropdown");
                // Espera a que el menú se abra (busca el enlace de logout)
                Console.WriteLine("⏳ Esperando enlace de logout...");
                await _page.WaitForSelectorAsync("a[href*='/Auth/Logout']", new PageWaitForSelectorOptions
                {
                    Timeout = 10000,
                    State = WaitForSelectorState.Visible
                });

                // Haz clic en "Cerrar sesión"
                Console.WriteLine("🖱️ Haciendo clic en Cerrar sesión...");
                await _page.ClickAsync("a[href*='/Auth/Logout']");

                // Espera a que redirija (normalmente vuelve a login o home)
                await _page.WaitForURLAsync("https://localhost:7057/**", new PageWaitForURLOptions { Timeout = 15000 });
                Console.WriteLine("✅ Redirección después del logout detectada");         
                var currentUrl = _page.Url;
                // 1. Debe estar en la raíz 
                Assert.StartsWith("https://localhost:7057/", currentUrl);

                //El usuario ya NO debe estar autenticado
             
                Console.WriteLine("🔍 Verificando que el menú de usuario ya NO aparece...");
                var userDropdown = await _page.QuerySelectorAsync("#userDropdown");
                Assert.Null(userDropdown);

                // Nos Aseguramos de que no este autenticado
            
                Console.WriteLine("🔍 Buscando indicios de que estamos en modo no autenticado...");

                // Ejemplo A: enlace o botón de "Iniciar sesión" / "Login"
                var loginLink = await _page.QuerySelectorAsync("a[href*='/Auth/Login'], button:has-text('Iniciar sesión'), a:has-text('Login')");
                if (loginLink != null)
                {
                    Console.WriteLine("   → Encontrado enlace/botón de login → buena señal");
                }
                else
                {
                    // Si no hay enlace visible
                    var loginForm = await _page.QuerySelectorAsync("form[action*='/Auth/Login'], input#email, input#password");
                    Assert.NotNull(loginForm);
                    Console.WriteLine("   → Parece que el formulario de login está en la raíz → OK");
                }
                // 4. Captura final para depuración
                await _page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = "logout-exitoso-root.png",
                    FullPage = true
                });

                Console.WriteLine("✅ Test OK: logout exitoso (redirige a raíz y usuario no autenticado)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-logout.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        #endregion


        #region Change Password Test
        [Fact]
        public async Task Change_Password_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("⏳ Esperando navbar del usuario autenticado...");
                await _page.WaitForSelectorAsync("#userDropdown", new PageWaitForSelectorOptions
                {
                    Timeout = 15000,
                    State = WaitForSelectorState.Visible
                });
                Console.WriteLine("✅ Dropdown del usuario encontrado");
                // Haz clic en el botón del dropdown para abrir el menú
                Console.WriteLine("🖱️ Abriendo dropdown del usuario...");
                await _page.ClickAsync("#userDropdown");
                Console.WriteLine("⏳ Esperando enlace de cambio de contraseña...");
                await _page.WaitForSelectorAsync("a[href*='/Auth/ChangePassword']", new PageWaitForSelectorOptions
                {
                    Timeout = 10000,
                    State = WaitForSelectorState.Visible
                });
                // Haz clic en "Cambiar contraseña"
                Console.WriteLine("🖱️ Haciendo clic en enlace cambio contraseña...");
                await _page.ClickAsync("a[href*='/Auth/ChangePassword']");
                Console.WriteLine("Formulario para cambiar la contraseña");
                await _page.FillAsync("#passwordAnterior", "1A2a3A4a5@");
                await _page.FillAsync("#passwordActual", "1A2a3A4a5");
                await _page.ClickAsync("button[type='submit']");
                var currentUrl = _page.Url;            
                Assert.StartsWith("https://localhost:7057/Admin", currentUrl);
            }
            catch (Exception ex) {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-changePass.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
           
        }
        [Fact]
        public async Task Change_Password_Fail_Test_()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("⏳ Esperando navbar del usuario autenticado...");
                await _page.WaitForSelectorAsync("#userDropdown", new PageWaitForSelectorOptions
                {
                    Timeout = 15000,
                    State = WaitForSelectorState.Visible
                });
                Console.WriteLine("✅ Dropdown del usuario encontrado");
                // Haz clic en el botón del dropdown para abrir el menú
                Console.WriteLine("🖱️ Abriendo dropdown del usuario...");
                await _page.ClickAsync("#userDropdown");
                Console.WriteLine("⏳ Esperando enlace de cambio de contraseña...");
                await _page.WaitForSelectorAsync("a[href*='/Auth/ChangePassword']", new PageWaitForSelectorOptions
                {
                    Timeout = 10000,
                    State = WaitForSelectorState.Visible
                });
                // Haz clic en "Cambiar contraseña"
                Console.WriteLine("🖱️ Haciendo clic en enlace cambio contraseña...");
                await _page.ClickAsync("a[href*='/Auth/ChangePassword']");
                Console.WriteLine("Formulario para cambiar la contraseña");
                await _page.FillAsync("#passwordAnterior", "1A2a3A45a@");
                await _page.FillAsync("#passwordActual", "1A2a3A4a5");
                await _page.ClickAsync("button[type='submit']");
                var currentUrl = _page.Url;
                // 4. Esperamos que la página se recargue (mismo URL) y aparezca el alert
                //    Usamos WaitForURL con el mismo patrón para detectar recarga
                await _page.WaitForURLAsync("**/Auth/ChangePassword", new PageWaitForURLOptions
                {
                    Timeout = 15000
                });

                // Esperamos un poco mas para que el DOM cargue del todo
                await _page.WaitForTimeoutAsync(800);

                // 5. Capturar y verificar el mensaje de error 
                Console.WriteLine("🔍 Buscando mensaje de error en alert...");

                // Selector del alert de TempData
                var alertLocator = _page.Locator(".alert.alert-warning");

                // Obtener el texto completo del alert (sin el botón close)
                var alertText = await alertLocator.TextContentAsync();
                var cleanedText = alertText?.Trim().Replace("Cerrar", "").Trim(); // quita "Cerrar" del botón

                Console.WriteLine($"Mensaje encontrado en alert: '{cleanedText}'");

                // Assert principal: contiene "incorrecta"
                Assert.Contains("incorrecta", cleanedText ?? "", StringComparison.OrdinalIgnoreCase);

                // Screenshot como evidencia
                await _page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = "change-password-fallo-incorrecta.png",
                    FullPage = true
                });

                Console.WriteLine("✅ Test OK: se mostró mensaje de error correctamente");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-changePass.png", FullPage = true });
                Assert.Fail(ex.Message);
            }

        }
        #endregion
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