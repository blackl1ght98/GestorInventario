using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class ProductosController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly IGestorArchivos _gestorArchivos;

        public ProductosController(GestorInventarioContext context, IGestorArchivos gestorArchivos)
        {
            _context = context;
            _gestorArchivos = gestorArchivos;
        }

        public async  Task<IActionResult> Index([FromQuery] Paginacion paginacion)
        {
            var productos= _context.Productos.Include(x=>x.IdProveedorNavigation).AsQueryable();
            await HttpContext.InsertarParametrosPaginacionRespuesta(productos, paginacion.CantidadAMostrar);
            var productoPaginado = productos.Paginar(paginacion).ToList();
            var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
            ViewData["Paginas"] = GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);

            return View(productoPaginado);
        }
        private List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual)
        {
            //A la variable paginas le asigna una lista de PaginasModel
            var paginas = new List<PaginasModel>();

          
            var paginaAnterior = (paginaActual > 1) ? paginaActual - 1 : 1;

      
            paginas.Add(new PaginasModel(paginaAnterior, paginaActual != 1, "Anterior"));

     
            for (int i = 1; i <= totalPaginas; i++)
            {
             
                paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
            }


        
            var paginaSiguiente = (paginaActual < totalPaginas) ? paginaActual + 1 : totalPaginas;

       
            paginas.Add(new PaginasModel(paginaSiguiente, paginaActual != totalPaginas, "Siguiente"));


            return paginas;
        }
        public async Task<IActionResult> Create()
        {
            ViewData["Productos"] = new SelectList(_context.Proveedores, "Id", "NombreProveedor");

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductosViewModel model)
        {
            //Esto toma en cuenta las validaciones puestas en BeerViewModel
            if (ModelState.IsValid)
            {
                //Crear Producto
                //Porque tenemos Imagen y Imagen1 facil Imagen almacena la ruta que es string y Imagen1 almacena la imgan en si
                var producto = new Producto()
                {
                    NombreProducto = model.NombreProducto,
                    Descripcion = model.Descripcion,
                    Imagen="",
                    Cantidad = model.Cantidad,
                    Precio = model.Precio,
                    IdProveedor = model.IdProveedor,
                };
                if (producto.Imagen != null)
                {
                    //MemoryStream--> guarda en memoria la imagen
                    using (var memoryStream = new MemoryStream())
                    {
                        //Realiza una copia de la imagen
                        await model.Imagen1.CopyToAsync(memoryStream);
                        //La informacion de la imgen se convierte a un array
                        var contenido = memoryStream.ToArray();
                        //Se obtiene el formato de la imagen .png, .jpg etc
                        var extension = Path.GetExtension(model.Imagen1.FileName);
                        //Guarda la imagen en la carpeta imagenes
                        producto.Imagen = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes"
                     );
                    }
                }

                _context.Add(producto);
                ViewData["Productos"] = new SelectList(_context.Proveedores, "Id", "NombreProveedor");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Los datos se han creado con éxito.";

                return RedirectToAction(nameof(Index));
            }
            return View(model);

        }
        public async Task<IActionResult> Delete(int id)
        {

            //Consulta a base de datos
            var producto = await _context.Productos.Include(p=>p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == id);
            //Si no hay cervezas muestra el error 404
            if (producto == null)
            {
                return NotFound("Producto no encontrado");
            }
            //Llegados ha este punto hay cervezas por lo tanto se muestran las cervezas
            return View(producto);
        }


        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            // Usamos 'Include' para cargar los datos relacionados de 'DetallePedidos' para cada producto.
// 'ThenInclude' se utiliza para cargar aún más datos relacionados. En este caso, los datos de 'Pedido' para cada 'DetallePedidos'.
// Finalmente, usamos otro 'Include' para cargar los datos del proveedor relacionados con cada producto.

            var producto = await _context.Productos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Pedido)
                .Include(p => p.IdProveedorNavigation)
                .FirstOrDefaultAsync(m => m.Id == Id);

            if (producto == null)
            {
                return BadRequest();
            }

            if (producto.DetallePedidos.Any())
            {
                TempData["ErrorMessage"] = "El producto no se puede eliminar porque tiene pedidos asociados.";
                //En caso de que el proveedor tenga productos asociados se devuelve al usuario a la vista Delete y se
                //muestra el mensaje informandole.
                //A esta reedireccion se le pasa la vista Delete y al metodo que contiene esa vista la id del proveedor
                return RedirectToAction(nameof(Delete), new { id = Id });
            }

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Edit(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(x=>x.Id==id);
            ViewData["Productos"] = new SelectList(_context.Proveedores, "Id", "NombreProveedor");
            ProductosViewModel viewModel = new ProductosViewModel()
            {
                Id = producto.Id,
                NombreProducto = producto.NombreProducto,
                Descripcion = producto.Descripcion,
                Cantidad = producto.Cantidad,
                Imagen = producto.Imagen,
                Precio = producto.Precio,
                IdProveedor = producto.IdProveedor
            };
            return View(viewModel);
        }
        [HttpPost]
        public async Task<ActionResult> Edit(ProductosViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);

                    // Actualizar las propiedades del producto
                    producto.NombreProducto = model.NombreProducto;
                    producto.Descripcion = model.Descripcion;
                    producto.Cantidad = model.Cantidad;
                    producto.Precio = model.Precio;
                    producto.Imagen= model.Imagen;
                    producto.IdProveedor = model.IdProveedor;

                    if (model.Imagen1 != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await model.Imagen1.CopyToAsync(memoryStream);
                            var contenido = memoryStream.ToArray();
                            var extension = Path.GetExtension(model.Imagen1.FileName);

                            // Borrar la imagen antigua
                            await _gestorArchivos.BorrarArchivo(producto.Imagen, "imagenes");

                            // Guardar la nueva imagen
                            producto.Imagen = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes");
                        }
                    }

                    _context.Productos.Update(producto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExist(model.Id))
                    {
                        return NotFound("Producto no encontrado");
                    }
                    else
                    {
                        _context.Entry(model).Reload();

                        //Intenta guardar de nuevo
                        _context.Entry(model).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }


        private bool ProductoExist(int Id)
        {

            return _context.Productos.Any(e => e.Id == Id);
        }
        public async Task<IActionResult> AgregarAlCarrito(int idProducto, int cantidad)
        {
            //Detecta el usuario logueado
            var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId;
            if (int.TryParse(existeUsuario, out usuarioId))
            {
                var usuarioActual = usuarioId;
                //Asigna un carrito al usuario logueado
                var carrito = _context.Carritos.FirstOrDefault(c => c.UsuarioId == usuarioActual);
                if (carrito == null)
                {
                    carrito = new Carrito
                    {
                        UsuarioId = usuarioActual,
                        FechaCreacion = DateTime.Now
                    };
                    _context.Carritos.Add(carrito);
                    // Guarda el nuevo Carrito en la base de datos
                    await _context.SaveChangesAsync();
                }
                //Una vez asignado el carrito al usuario ese carrito tiene una id y a ese carrito se le asignan productos los cuales
                //tienen una id la id del producto se obtiene mediante la realacion de clave foranea en esta tabla con la tabla Productos
                var itemCarrito = _context.ItemsDelCarritos.FirstOrDefault(i => i.CarritoId == carrito.Id && i.ProductoId == idProducto);
                Producto producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == idProducto);

                if (producto != null)
                {
                    // Verifica si la cantidad del producto es suficiente
                    if (producto.Cantidad < cantidad)
                    {
                        TempData["ErrorMessage"] = "No hay suficientes productos en stock.";
                        return RedirectToAction("Index");
                    }

                    // Si el producto no está en el carrito, crea un nuevo item que se podria decir un articulo ItemsDelCarrito
                    if (itemCarrito == null)
                    {
                        itemCarrito = new ItemsDelCarrito
                        {
                            ProductoId = idProducto,
                            Cantidad = cantidad,
                            CarritoId = carrito.Id
                        };
                        _context.ItemsDelCarritos.Add(itemCarrito);
                    }
                    else
                    {
                        // Si el producto ya está en el carrito, incrementa la cantidad del producto
                        itemCarrito.Cantidad += cantidad;
                        _context.ItemsDelCarritos.Update(itemCarrito);
                    }

                    //Una vez agregado al carrito se quita la cantidad de productos
                    producto.Cantidad -= cantidad;
                    _context.Productos.Update(producto);
                    // Guarda los cambios en la base de datos
                    await _context.SaveChangesAsync();
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> Incrementar(int id)
        {
            // Busca el producto en la base de datos
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);

            if (producto != null)
            {
                // Incrementa la cantidad del producto
                producto.Cantidad++;
                _context.Productos.Update(producto);
                // Guarda los cambios en la base de datos
                await _context.SaveChangesAsync();
            }

            // Redirige al usuario a la página de índice
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> Decrementar(int id)
        {
            // Busca el producto en la base de datos
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);

            if (producto != null)
            {
                // Incrementa la cantidad del producto
                producto.Cantidad--;
                _context.Productos.Update(producto);
                // Guarda los cambios en la base de datos
                await _context.SaveChangesAsync();
            }

            // Redirige al usuario a la página de índice
            return RedirectToAction("Index");
        }
    }
}
