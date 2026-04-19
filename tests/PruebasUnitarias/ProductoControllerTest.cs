using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.Playwright.Assertions;
namespace PruebasUnitarias
{
    public class ProductoControllerTest 
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
        public async Task Mostrar_Productos_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de productos");
                await _page.GotoAsync("https://localhost:7057/Productos");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Productos", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 5000 });
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
        public async Task Crear_Producto_Con_Imagen_Exito()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Login exitoso, navegando a creación de producto");

                await _page.GotoAsync("https://localhost:7057/Productos/Create");

                // Rellenar campos de texto
                await _page.FillAsync("#NombreProducto", "Producto Prueba 2026");
                await _page.FillAsync("#Descripcion", "Descripción de prueba para test E2E");
                await _page.FillAsync("#Cantidad", "25");
                await _page.FillAsync("#Precio", "149.99");

                // Seleccionar proveedor 
                await _page.SelectOptionAsync("#IdProveedor", new[] { "Prueba" });

                
               
                var rutaImagen = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", "imagenes", "test-producto.jpg");

                // Si no tienes uno, crea un archivo dummy o usa uno pequeño
                if (!File.Exists(rutaImagen))
                {
                    // Crea un dummy si no existe (opcional, solo para test)
                    Console.WriteLine("Creando imagen dummy para test...");
                    File.WriteAllBytes(rutaImagen, new byte[] { 0xFF, 0xD8, 0xFF }); 
                }

                // Subir el archivo al input file
                await _page.Locator("#ArchivoImagen").SetInputFilesAsync(rutaImagen);

                Console.WriteLine("Imagen seleccionada para subir");

                // Submit
                await _page.ClickAsync("input[type='submit'][value='Crear']"); 

                // Esperar redirección a lista de productos
                await _page.WaitForURLAsync("**/Productos**", new() { Timeout = 20000 });

                // Verificar éxito: no hay alert de error + mensaje o redirección correcta
                await Expect(_page.Locator(".alert.alert-danger, .text-danger"))
                    .Not.ToBeVisibleAsync(new() { Timeout = 10000 });          

                Console.WriteLine("✅ Producto creado con imagen exitosamente");
                await _page.ScreenshotAsync(new() { Path = "success-crear-producto-con-imagen.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-crear-producto.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        //TODO-> HACER MAS ADELANTE LOS CAMINOS INFELICES DE CREAR UN PRODUCTO
        [Fact]
        public async Task Eliminar_Producto_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando eliminar producto");
                await _page.GotoAsync("https://localhost:7056/Productos/Delete/1115");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Productos**", new PageWaitForURLOptions
                {
                    Timeout = 15000,

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-eliminar-producto.png", FullPage = true });
                Assert.Fail(ex.Message);
            }

        }
        [Fact]
        public async Task Eliminar_Productos_Inexistente_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando eliminar producto");
                await _page.GotoAsync("https://localhost:7056/Productos/Delete/2000");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/Productos**", new PageWaitForURLOptions
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
        public async Task Editar_Producto_Con_Imagen_Exito()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Login exitoso, navegando a edición de producto");

                await _page.GotoAsync("https://localhost:7057/Productos/Edit/1114");

                // Rellenar campos...
                await _page.FillAsync("#NombreProducto", "Producto Prueba 2026 Editado");
                await _page.FillAsync("#Descripcion", "Descripción de prueba para test E2E Editado");
                await _page.FillAsync("#Cantidad", "26");
                await _page.FillAsync("#Precio", "150.99");
                await _page.SelectOptionAsync("#IdProveedor", new[] { "Prueba" });

                // Crear dummy JPEG válido en temp
                var rutaImagen = Path.Combine(Path.GetTempPath(), "test-producto.jpg");

                if (!File.Exists(rutaImagen))
                {
                    Console.WriteLine("Creando JPEG dummy válido para test...");
                    var dummyJpegBytes = Convert.FromBase64String(
                        "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0a" +
                        "HBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIy" +
                        "MjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAf/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8AAQ=="
                    );
                    await File.WriteAllBytesAsync(rutaImagen, dummyJpegBytes);
                }

                await _page.Locator("#ArchivoImagen").SetInputFilesAsync(rutaImagen);
                Console.WriteLine("Imagen dummy seleccionada para subir");

                await _page.ClickAsync("button[type='submit']");

                await _page.WaitForURLAsync("**/Productos**", new() { Timeout = 20000 });

                await Expect(_page.Locator(".alert.alert-danger, .text-danger"))
                    .Not.ToBeVisibleAsync(new() { Timeout = 10000 });

                Console.WriteLine("✅ Producto editado con imagen exitosamente");
                await _page.ScreenshotAsync(new() { Path = "success-editar-producto-con-imagen.png", FullPage = true });

                // Limpieza opcional
                File.Delete(rutaImagen);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-editar-producto.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Agregar_Al_Carrito_Exito_Desde_Lista_Productos()
        {
            try
            {
                //  Login como usuario normal (el que puede agregar al carrito)
                await PerformSuccessfulLoginAsync(); // asegúrate que sea usuario con carrito
                Console.WriteLine("Login exitoso como usuario");

                // Ir a la página de catálogo de productos
                await _page.GotoAsync("https://localhost:7057/Productos");
                Console.WriteLine("En la página de catálogo de productos");

                //  Buscar el formulario de un producto específico
                // Usa un ID o nombre real que exista y tenga stock > 0
                var productoId = "78"; 
                var formLocator = _page.Locator($"form[action*='AgregarAlCarrito'] input[name='idProducto'][value='{productoId}']")
                    .Locator(".."); 

                await Expect(formLocator).ToBeVisibleAsync(new() { Timeout = 10000 });
                Console.WriteLine($"Formulario encontrado para producto {productoId}");

                //  Rellenar cantidad (si no está en 1 por defecto)
                var cantidadInput = formLocator.Locator("input[name='cantidad']");
                await cantidadInput.ClearAsync();
                await cantidadInput.FillAsync("3"); 

            
               
                await _page.WaitForURLAsync("**/Productos**", new() { Timeout = 20000 });

                //  Verificar que NO hay mensaje de error visible
                await Expect(_page.Locator(".alert.alert-danger, .text-danger"))
                    .Not.ToBeVisibleAsync(new() { Timeout = 10000 });

            

                Console.WriteLine("Producto agregado al carrito correctamente");
                await _page.ScreenshotAsync(new() { Path = "success-agregar-al-carrito.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-agregar-al-carrito.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Mostrar_Historial_Productos_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de historial de productos");
                await _page.GotoAsync("https://localhost:7057/Productos/HistorialProducto");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Historial de Productos", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 5000 });
                await _page.ScreenshotAsync(new() { Path = "paginaHistorialProductos.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-historial-Productos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Descargar_Historial_PDF_Como_Admin_Exito()
        {
            try
            {
                // 1. Login como ADMINISTRADOR (necesario por [Authorize(Roles = "Administrador")])
                await PerformSuccessfulLoginAsync(); // asegúrate que este método use credenciales de admin
                Console.WriteLine("Login exitoso como Administrador");

                // 2. Ir a la página de historial de productos (donde está el enlace)
                await _page.GotoAsync("https://localhost:7056/Productos/HistorialProducto");
                Console.WriteLine("En la página de historial de productos");

                // 3. Encontrar el enlace de descarga
                var downloadLink = _page.GetByRole(AriaRole.Link, new() { Name = "Descargar PDF" });
                await Expect(downloadLink).ToBeVisibleAsync(new() { Timeout = 10000 });

                // 4. Capturar la descarga (Playwright intercepta archivos)
                var downloadTask = _page.WaitForDownloadAsync(new PageWaitForDownloadOptions
                {
                    Timeout = 30000 // 30 segundos, por si el PDF es grande
                });

                // 5. Hacer clic en el enlace
                await downloadLink.ClickAsync();

                // 6. Esperar la descarga
                var download = await downloadTask;

                // 7. Verificaciones básicas
                Assert.Equal("historial.pdf", download.SuggestedFilename);

                var filePath = await download.PathAsync();
                Assert.NotNull(filePath);

                var fileInfo = new FileInfo(filePath);
                Assert.True(fileInfo.Exists);
                Assert.True(fileInfo.Length > 0, "El PDF descargado está vacío");

                

                Console.WriteLine("Historial PDF descargado correctamente");
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
        public async Task Mostrar_Detalles_Historial_Productos_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Dirigiendo a la pagina de detalle historial");
                await _page.GotoAsync("https://localhost:7057/Productos/DetallesHistorialProducto/1505");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Detalles del Producto" }))
                .ToBeVisibleAsync(new() { Timeout = 10000 });
                await _page.ScreenshotAsync(new() { Path = "paginaDetallesHistorialProductos.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "error-ver-detalles-historial-Productos.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
        }
        [Fact]
        public async Task Eliminar_Historial_Producto_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Intentando eliminar historialProductos");
                await _page.GotoAsync("https://localhost:7056/Productos/DeleteHistorial/1505");
                Console.WriteLine("Confirmando que estamos en esa pagina");
                await _page.ClickAsync("button[type='submit']");
                await _page.WaitForURLAsync("**/HistorialProducto**", new PageWaitForURLOptions
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
        public async Task Delete_All_Historial_Productos_Test()
        {
            try
            {
                await PerformSuccessfulLoginAsync();
                Console.WriteLine("Login exitoso como admin");

                Console.WriteLine("Dirigiendo a la página de historial de productos");
                await _page.GotoAsync("https://localhost:7057/Productos/HistorialProducto");

                Console.WriteLine("Confirmando que estamos en esa página");

                // Interceptar y aceptar automáticamente cualquier diálogo confirm()
                _page.Dialog += async (sender, dialog) =>
                {
                    if (dialog.Type == DialogType.Confirm)
                    {
                        Console.WriteLine("Diálogo confirm() detectado - aceptando automáticamente");
                        await dialog.AcceptAsync(); // responde "OK"
                    }
                };

                // Esperar botón visible
                var deleteButton = _page.GetByRole(AriaRole.Button, new() { Name = "Eliminar Todo" });
                await Expect(deleteButton).ToBeVisibleAsync(new() { Timeout = 15000 });

                // Capturar respuesta del POST
                var responseTask = _page.WaitForResponseAsync(r => r.Url.Contains("/Productos/DeleteAllHistorial"));

                // Clic → abre confirm() → handler lo acepta → submit POST
                await deleteButton.ClickAsync();

                // Esperar respuesta del POST
                var response = await responseTask;

                // Verificar redirección
                await _page.WaitForURLAsync("**/HistorialProducto**", new() { Timeout = 20000 });

                // Verificar no hay alert de error
                await Expect(_page.Locator(".alert.alert-danger")).Not.ToBeVisibleAsync(new() { Timeout = 5000 });

                Console.WriteLine("Eliminación de todo el historial ejecutada correctamente (confirm aceptado y redirigido)");
                await _page.ScreenshotAsync(new() { Path = "success-delete-all-historial.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción: {ex.Message}");
                await _page.ScreenshotAsync(new() { Path = "error-delete-all-historial.png", FullPage = true });
                Assert.Fail(ex.Message);
            }
            finally
            {
                // Limpiar el handler (buena práctica)
                _page.Dialog -= null;
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
