using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        public async  Task<IActionResult> Index()
        {
            var productos=await _context.Productos.Include(x=>x.IdProveedorNavigation).ToListAsync();
            return View(productos);
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
                //Crea la cerveza
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
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.Imagen1.CopyToAsync(memoryStream);
                        var contenido = memoryStream.ToArray();
                        var extension = Path.GetExtension(model.Imagen1.FileName);
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
            Producto producto = await _context.Productos.FindAsync(id);
            ViewData["Productos"] = new SelectList(_context.Proveedores, "Id", "NombreProveedor");

            return View(producto);
        }
        [HttpPost]
        public async Task<ActionResult> Edit(Producto producto)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //var proveedor = await _context.Proveedores.FindAsync(proveedores.Id);

                    _context.Entry(producto).State = EntityState.Modified;
                    ViewData["Productos"] = new SelectList(_context.Proveedores, "Id", "NombreProveedor");

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProveedorExist(producto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _context.Entry(producto).Reload();

                        // Intenta guardar de nuevo
                        _context.Entry(producto).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToAction("Index");
            }
            return View(producto);
        }

        private bool ProveedorExist(int Id)
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
                    carrito = new Carrito { 
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
                if (itemCarrito == null)
                {
                    // Si el producto no está en el carrito, crea un nuevo item que se podria decir un articulo ItemsDelCarrito
                    itemCarrito = new ItemsDelCarrito { 
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

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }


    }
}
