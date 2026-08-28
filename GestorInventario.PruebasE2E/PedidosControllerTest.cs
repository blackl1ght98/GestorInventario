using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.Playwright.Assertions;
namespace PruebasUnitarias
{
    public class PedidosControllerTest 
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
        public async Task Mostrar_Pedidos_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de pedidos");
                await _page.GotoAsync("https://localhost:7057/Pedidos");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Pedidos", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 5000 });
                await _page.ScreenshotAsync(new() { Path = "paginaPedidos.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-pedidos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Eliminar_Pedido_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando eliminar pedido");
                await _page.GotoAsync("https://localhost:7056/Pedidos/Delete/490");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Pedidos**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-eliminar-pedido.png", FullPage = true });
                Assert.Fail(ex.Message);
            }

        }
        [Fact]
        public async Task Eliminar_Pedido_No_Entregado_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando eliminar pedido");
                await _page.GotoAsync("https://localhost:7056/Pedidos/Delete/488");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.ClickAsync("button[type='submit']");
                // Selector del alert de TempData
                var alertLocator = _page.Locator(".alert.alert-warning");

                // Obtener el texto completo del alert (sin el botón close)
                var alertText = await alertLocator.TextContentAsync();

                // Limpiar el texto: quitar espacios extra, saltos de línea y el botón close si está
                alertText = alertText.Trim().Replace("\r\n", " ").Replace("×", "").Trim();

                // Verificar que contiene el mensaje esperado (más robusto que igualdad exacta)
                Assert.Contains("El pedido tiene que tener el estado Entregado para ser eliminado y no tener historial asociado", alertText);

              
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-eliminar-pedido.png", FullPage = true });
                Assert.Fail(ex.Message);
            }

        }
        [Fact]
        public async Task Eliminar_Pedido_Inexistente_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando eliminar pedido");
                await _page.GotoAsync("https://localhost:7056/Pedidos/Delete/800");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Pedidos**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });
                // Opcional: verificar que no hay alert de error visible
                await Expect(_page.Locator(".alert.alert-danger, .alert.alert-warning")).Not.ToBeVisibleAsync(new() { Timeout = 5000 });
                await _page.ScreenshotAsync(new() { Path = "success-eliminar-inexistente.png", FullPage = true });


            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-eliminar-pedido.png", FullPage = true });
                Assert.Fail(ex.Message);
            }

        }
        [Fact]
        public async Task Eliminar_Pedido_Historial_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando eliminar el historial del pedido");
                await _page.GotoAsync("https://localhost:7056/Pedidos/DeleteHistorial/433");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/HistorialPedidos**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });
                await Expect(_page.Locator(".alert.alert-danger, .alert.alert-warning")).Not.ToBeVisibleAsync(new() { Timeout = 5000 });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-eliminar-historial-pedido.png", FullPage = true });
                Assert.Fail(ex.Message);
            }

        }
        [Fact]
        public async Task Eliminar_Pedido_Historial_Inexistente_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando eliminar pedido");
                await _page.GotoAsync("https://localhost:7056/Pedidos/DeleteHistorial/800");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/HistorialPedidos**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });
                // Opcional: verificar que no hay alert de error visible
                await Expect(_page.Locator(".alert.alert-danger, .alert.alert-warning")).Not.ToBeVisibleAsync(new() { Timeout = 5000 });
                await _page.ScreenshotAsync(new() { Path = "success-eliminar-inexistente.png", FullPage = true });


            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-eliminar-pedido.png", FullPage = true });
                Assert.Fail(ex.Message);
            }

        }
        [Fact]
        public async Task Editar_Pedido_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando editar pedido");
                await _page.GotoAsync("https://localhost:7057/Pedidos/Edit/487");
                Console.WriteLine("Confirmando que estamos en esa pagina");                         
                await _page.FillAsync("#EstadoPedido", "Entregado");             
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Pedidos**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });


            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-editar-pedido.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Editar_Pedido_Campos_Vacios_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando editar pedido");
                await _page.GotoAsync("https://localhost:7057/Pedidos/Edit/487");
                Console.WriteLine("Confirmando que estamos en esa pagina");
             
                await _page.FillAsync("#EstadoPedido", "");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Edit**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });
                await Expect(_page.Locator("span.text-danger:has-text('El estado del pedido es requerido')"))
               .ToBeVisibleAsync(new() { Timeout = 10000 });
              

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-editar-pedido.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Detalles_Pedido_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de detalles de pedido");
                await _page.GotoAsync("https://localhost:7056/Pedidos/DetallesPedido/487");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Detalles del Pedido" }))
             .ToBeVisibleAsync(new() { Timeout = 10000 });
                await _page.ScreenshotAsync(new() { Path = "detallesPedidos.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-pedidos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Detalles_Pedido_Inexistente_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de detalles de pedido");
                await _page.GotoAsync("https://localhost:7056/Pedidos/DetallesPedido/800");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.WaitForURLAsync("**/Pedidos**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });
               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-pedidos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Historial_Pedido_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de detalles de historial pedidos");
                await _page.GotoAsync("https://localhost:7056/Pedidos/HistorialPedidos");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.WaitForURLAsync("**/HistorialPedidos**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-pedidos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Detalle_Historial_Pedido_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de detalles de historial pedidos");
                await _page.GotoAsync("https://localhost:7056/Pedidos/DetallesHistorialPedido/432");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.WaitForURLAsync("**/DetallesHistorialPedido**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-pedidos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Detalle_Historial_Pedido_Inexistente_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de detalles de historial pedidos");
                await _page.GotoAsync("https://localhost:7056/Pedidos/DetallesHistorialPedido/800");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.WaitForURLAsync("**/HistorialPedido**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-pedidos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Descargar_Historial_PDF_Desde_Enlace_Exito()
        {
            try
            {
                // 1. Login como usuario autorizado
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Navegando a la página que tiene el botón de descarga");

                // 2. Ir a la página donde está el enlace (ajusta si es necesario)
                await _page.GotoAsync("https://localhost:7056/Pedidos/HistorialPedidos");

                // 3. Esperar que el botón esté visible
                var downloadButton = _page.GetByRole(AriaRole.Link, new() { Name = "Descargar PDF" });
                await Expect(downloadButton).ToBeVisibleAsync(new() { Timeout = 10000 });

                // 4. Capturar la descarga (en .NET se usa WaitForDownloadAsync)
                var downloadTask = _page.WaitForDownloadAsync(new PageWaitForDownloadOptions
                {
                    Timeout = 30000 // 30 segundos, ajusta si el PDF tarda
                });

                // 5. Hacer clic en el enlace
                await downloadButton.ClickAsync();

                // 6. Esperar la descarga
                var download = await downloadTask;

                // 7. Verificaciones
                Assert.Equal("historial.pdf", download.SuggestedFilename);

                var filePath = await download.PathAsync();
                Assert.NotNull(filePath);

                // 8. Verificar que el archivo existe y tiene tamaño > 0
                var fileInfo = new FileInfo(filePath);
                Assert.True(fileInfo.Exists);
                Assert.True(fileInfo.Length > 0, "El PDF descargado está vacío");

              

                await _page.ScreenshotAsync(new() { Path = "success-descargar-historial-pdf.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-descargar-historial-pdf.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Eliminar_Todo_Historial_Test()
        {
            try
            {
                // 1. Login como admin (necesario por [Authorize])
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Login exitoso como admin");

                // 2. Ir a la página donde está el formulario (ajusta la ruta real)
                await _page.GotoAsync("https://localhost:7056/Pedidos/HistorialPedidos"); // página que contiene el botón
                Console.WriteLine("En la página de historial de pedidos");

                // 3. Esperar que el botón esté visible y habilitado
                var deleteAllButton = _page.GetByRole(AriaRole.Button, new() { Name = "Eliminar Todo" });
                await Expect(deleteAllButton).ToBeVisibleAsync(new() { Timeout = 10000 });
                await Expect(deleteAllButton).ToBeEnabledAsync();

                // 4. Capturar la respuesta del POST para verificar status/redirección
                var responseTask = _page.WaitForResponseAsync(r => r.Url.Contains("/Pedidos/DeleteAllHistorial"));

                // 5. Simular clic en el botón submit
                await deleteAllButton.ClickAsync();

                // 6. Esperar la respuesta del POST
                var response = await responseTask;

                // 7. Verificaciones básicas
                Assert.Equal(302, response.Status); 

                // 8. Verificar que redirige a HistorialPedidos (o Index)
                await _page.WaitForURLAsync("**/HistorialPedidos**", new() { Timeout = 15000 });

                //9. verificar que no hay alert de error visible
                await Expect(_page.Locator(".alert.alert-danger")).Not.ToBeVisibleAsync(new() { Timeout = 5000 });

                Console.WriteLine("Eliminación de todo el historial exitosa (redirigido correctamente)");
                await _page.ScreenshotAsync(new() { Path = "success-eliminar-todo-historial.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-eliminar-todo-historial.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Detalle_Pago_Ejecutado_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de detalles pago ejecutado");
                await _page.GotoAsync("https://localhost:7056/Pedidos/DetallesPagoEjecutado/3LF96679PR3193619");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.WaitForURLAsync("**/DetallesPagoEjecutado**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-pedidos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Detalle_Pago_Ejecutado_Inexistente_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de detalles pago ejecutado");
                await _page.GotoAsync("https://localhost:7056/Pedidos/DetallesPagoEjecutado/3LF96679PR3193229");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.WaitForURLAsync("**/Pedidos**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-pedidos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Descargar_Factura_Test()
        {
            try
            {
                // 1. Login como admin (necesario por [Authorize])
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Login exitoso como admin");

                // 2. Ir a la página donde está el formulario (ajusta la ruta real)
                await _page.GotoAsync("https://localhost:7056/Pedidos/DetallesPagoEjecutado/3LF96679PR3193619"); // página que contiene el botón
                Console.WriteLine("En la página de historial de pedidos");

                // 3. Esperar que el botón esté visible
                var downloadButton = _page.GetByRole(AriaRole.Link, new() { Name = "Descargar PDF" });
                await Expect(downloadButton).ToBeVisibleAsync(new() { Timeout = 10000 });

                // 4. Capturar la descarga (en .NET se usa WaitForDownloadAsync)
                var downloadTask = _page.WaitForDownloadAsync(new PageWaitForDownloadOptions
                {
                    Timeout = 30000 // 30 segundos, ajusta si el PDF tarda
                });

                // 5. Hacer clic en el enlace
                await downloadButton.ClickAsync();

                // 6. Esperar la descarga
                var download = await downloadTask;

                // 7. Verificaciones
                Assert.Equal("Factura_Pago_3LF96679PR3193619.pdf", download.SuggestedFilename);

                var filePath = await download.PathAsync();
                Assert.NotNull(filePath);

                // 8. Verificar que el archivo existe y tiene tamaño > 0
                var fileInfo = new FileInfo(filePath);
                Assert.True(fileInfo.Exists);
                Assert.True(fileInfo.Length > 0, "El PDF descargado está vacío");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-eliminar-todo-historial.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Enviar_Factura_Email_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de detalles pago ejecutado");
                await _page.GotoAsync("https://localhost:7056/Pedidos/DetallesPagoEjecutado/3LF96679PR3193619");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                // 3. Esperar que el botón esté visible
                var emailButton = _page.GetByRole(AriaRole.Link, new() { Name = "Enviar Factura por Email" });
                await Expect(emailButton).ToBeVisibleAsync(new() { Timeout = 10000 });

                // 4. Capturar la descarga (en .NET se usa WaitForDownloadAsync)
                var downloadTask = _page.WaitForDownloadAsync(new PageWaitForDownloadOptions
                {
                    Timeout = 30000 // 30 segundos, ajusta si el PDF tarda
                });

                // 5. Hacer clic en el enlace
                await emailButton.ClickAsync();
                await _page.WaitForURLAsync("**/Pedidos**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-pedidos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Agregar_Info_Envio_Desde_Modal_Exito()
        {
            try
            {
                // 1. Login como admin (necesario por [Authorize])
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Login exitoso como admin");

                // 2. Ir a la página de lista de pedidos (donde están los botones)
                await _page.GotoAsync("https://localhost:7056/Pedidos?pagina=2"); 
                Console.WriteLine("En la página de lista de pedidos");

                // 3. Encontrar el botón "Agregar info envio" de un pedido específico
                // Selector: botón con clase btn-success y data-pedido-id (o texto)
                var pedidoId = "500"; 
                var addEnvioButton = _page.Locator($".btn-success[data-pedido-id='{pedidoId}']");

                await Expect(addEnvioButton).ToBeVisibleAsync(new() { Timeout = 10000 });
                Console.WriteLine($"Botón encontrado para pedido {pedidoId}");

                // 4. Capturar la respuesta del POST para verificar éxito
                var responseTask = _page.WaitForResponseAsync(r => r.Url.Contains("/Pedidos/AgregarInfoEnvio"));

                // 5. Hacer clic en el botón → abre el modal
                await addEnvioButton.ClickAsync();

                // 6. Esperar que el modal esté visible
                var modal = _page.Locator("#agregarEnvioModal");
                await Expect(modal).ToBeVisibleAsync(new() { Timeout = 10000 });

                // 7. Rellenar el formulario dentro del modal
                // Transportista (select)
                await _page.SelectOptionAsync("#carrierSelect", "DHL"); // o el valor que prefieras: CORREOS_ES, FEDEX, UPS

                // BarCode Type (select)
                await _page.SelectOptionAsync("#barcodeSelect", "EAN_13"); // o UPC_A, EAN_8, CODE_128

                // Si el hidden pedidoId no se rellena automáticamente por JS, forzarlo
                await _page.EvalOnSelectorAsync("#pedidoIdInput", $"el => el.value = '{pedidoId}'");

                // 8. Hacer clic en "Confirmar" dentro del modal
                await _page.ClickAsync("#confirmAgregarEnvioBtn");

                // 9. Esperar respuesta del POST y redirección
                var response = await responseTask;
                Assert.Equal(302, response.Status); 

                await _page.WaitForURLAsync("**/Pedidos**", new() { Timeout = 15000 });

                

                Console.WriteLine("Información de envío agregada correctamente");
                await _page.ScreenshotAsync(new() { Path = "success-agregar-info-envio.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-agregar-info-envio.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Agregar_Info_Envio_Pedido_Inexistente_NoPermiteAccion()
        {
            try
            {
                await PerformSuccessfulLoginAsync();

                // Ir a lista de pedidos
                await _page.GotoAsync("https://localhost:7056/Pedidos");

                // Buscar botón de ID que NO existe
                var pedidoIdInexistente = "999999";
                var buttonInexistente = _page.Locator($".btn-success[data-pedido-id='{pedidoIdInexistente}']");

                // Verificar que NO existe
                await Expect(buttonInexistente).Not.ToBeVisibleAsync(new() { Timeout = 10000 });

                Console.WriteLine("Botón para pedido inexistente no encontrado (esperado)");

                await _page.ScreenshotAsync(new() { Path = "success-info-envio-inexistente.png", FullPage = true });
            }
            catch (Exception ex)
            {
                await _page.ScreenshotAsync(new() { Path = "error-info-envio-inexistente.png", FullPage = true });
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
