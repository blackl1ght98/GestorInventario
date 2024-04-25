using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Controllers
{
    public class ProveedorController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly ILogger<ProveedorController> _logger;

        public ProveedorController(GestorInventarioContext context, ILogger<ProveedorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var proveedores = await _context.Proveedores.ToListAsync();

                return View(proveedores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar los proveedores");
                return BadRequest("Error al mostrar los proveedores, intentelo de nuevo mas tarde o contacte con el administrador");
            }
            
        }
        public IActionResult Create()
        {

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProveedorViewModel model)
        {
            try
            {
                //Esto toma en cuenta las validaciones puestas en BeerViewModel
                if (ModelState.IsValid)
                {
                    //Crea la cerveza
                    var proveedor = new Proveedore()
                    {
                        NombreProveedor = model.NombreProveedor,
                        Contacto = model.Contacto,
                        Direccion = model.Direccion,
                    };
                    _context.Add(proveedor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Los datos se han creado con éxito.";

                    return RedirectToAction(nameof(Index));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el pedido");
                return BadRequest("Error al crear el proveedor intentelo de nuevo mas tarde o contacte con el administrador");
            }
           

        }
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                //Consulta a base de datos
                var proveedor = await _context.Proveedores.FirstOrDefaultAsync(m => m.Id == id);
                //Si no hay cervezas muestra el error 404
                if (proveedor == null)
                {
                    return NotFound("proveedor no encontradas");
                }
                //Llegados ha este punto hay cervezas por lo tanto se muestran las cervezas
                return View(proveedor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar la vista de eliminacion del proveedor");
                return BadRequest("Error al mostrar la vista de eliminacion del proveedor intentelo de nuevo mas tarde o contacte con el administrador");
            }
           
        }


        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
                var proveedor = await _context.Proveedores.Include(p => p.Productos).FirstOrDefaultAsync(m => m.Id == Id);
                if (proveedor == null)
                {
                    return BadRequest();
                }
                if (proveedor.Productos.Any())
                {
                    TempData["ErrorMessage"] = "El proveedor no se puede eliminar porque tiene productos asociados.";
                    //En caso de que el proveedor tenga productos asociados se devuelve al usuario a la vista Delete y se
                    //muestra el mensaje informandole.
                    //A esta reedireccion se le pasa la vista Delete y al metodo que contiene esa vista la id del proveedor
                    return RedirectToAction(nameof(Delete), new { id = Id });
                }
                _context.Proveedores.Remove(proveedor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el proveedor");
                return BadRequest("Error al eliminar el proveedor intentelo de nuevo mas tarde o contacte con el administrador del sitio");
            }
            
        }
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                Proveedore proveedor = await _context.Proveedores.FindAsync(id);

                return View(proveedor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar la vista de edicion del proveedor");
                return BadRequest("Error al mostrar la vista de edicion del proveedor intentelo de nuevo mas tarde o contacte con el administrador del sitio");
            }
            
        }
        [HttpPost]
        public async Task<ActionResult> Edit(Proveedore proveedores)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        //var proveedor = await _context.Proveedores.FindAsync(proveedores.Id);

                        _context.Entry(proveedores).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";

                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ProveedorExist(proveedores.Id))
                        {
                            return NotFound("Proveedor no encontrado");
                        }
                        else
                        {
                            _context.Entry(proveedores).Reload();

                            // Intenta guardar de nuevo
                            _context.Entry(proveedores).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }
                    }
                    return RedirectToAction("Index");
                }
                return View(proveedores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar el proveedor");
                return BadRequest("Error al editar el proveedor intentelo de nuvo mas tarde o contacte con el administrador del sitio");
            }
           
        }

        private bool ProveedorExist(int Id)
        {
            
            return _context.Proveedores.Any(e => e.Id == Id);
        }


    }
}
