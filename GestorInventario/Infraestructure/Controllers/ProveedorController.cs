using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.Models.ViewModels;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Linq;

namespace GestorInventario.Infraestructure.Controllers
{
    public class ProveedorController : Controller
    {
        
        private readonly ILogger<ProveedorController> _logger;
        private readonly GenerarPaginas _generarPaginas;
        private readonly PolicyHandler _PolicyHandler;
        private readonly IProveedorRepository _proveedorRepository;
        private readonly GestorInventarioContext _context;
        public ProveedorController( ILogger<ProveedorController> logger, GenerarPaginas generarPaginas, 
            PolicyHandler policyHandler, IProveedorRepository proveedor, GestorInventarioContext context)
        {
            
            _logger = logger;
            _generarPaginas = generarPaginas;
            _PolicyHandler = policyHandler;
            _proveedorRepository= proveedor;
            _context = context;
        }
        //Metodo que muestra todos los proveedores
        public async Task<IActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var proveedores = _proveedorRepository.ObtenerProveedores(); 

                ViewData["Buscar"] = buscar;
                if (!string.IsNullOrEmpty(buscar))
                {
                    proveedores = proveedores.Where(s => s.NombreProveedor.Contains(buscar));
                }

                await HttpContext.TotalPaginas(proveedores, paginacion.CantidadAMostrar);
                var proveedorList = proveedores.Paginar(paginacion).ToList(); 

                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();

                ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);
                return View(proveedorList);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al mostrar los proveedores");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo que muestra la vista para crear el proveedor
        public async Task<IActionResult> Create()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Cargar los usuarios desde la base de datos
            var usuarios = await _context.Usuarios 
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.NombreCompleto 
                })
                .ToListAsync();

            var model = new ProveedorViewModel
            {
                Usuarios = usuarios
            };

            return View(model);
        }
        //Metodo que crea el proveedor
        [HttpPost]
        public async Task<IActionResult> Create(ProveedorViewModel model)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

               

                    var (success, errorMessage) = await ExecutePolicyAsync(() => _proveedorRepository.CrearProveedor(model));
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Los datos se han creado con éxito.";
                    }

                
                    return RedirectToAction(nameof(Index));
                
              
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al crear el pedido");
                return RedirectToAction("Error", "Home");
            }


        }
        //Metodo encargado de obtener los datos necesarios para eliminar el proveedor
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var proveedor = await ExecutePolicyAsync(() => _proveedorRepository.ObtenerProveedorId(id));
                if (proveedor == null)
                {
                    return NotFound("El proveedor no fue encontrado");
                }

                return View(proveedor);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al mostrar la vista de eliminación del proveedor");
                return RedirectToAction("Error", "Home");
            }
        }

        //Metodo que elimina el proveedor
        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
               
                var (success,errorMessagee)=await ExecutePolicyAsync(()=> _proveedorRepository.EliminarProveedor(Id)) ;
                if (success)
                {
                    TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"]=errorMessagee;
                    return RedirectToAction(nameof(Delete), new { id = Id });
                }
              
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar el proveedor");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo que obtiene la informacion necesaria para editar el proveedor
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                
                var usuarios = await _context.Usuarios
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = u.NombreCompleto
                    })
                    .ToListAsync();
                var proveedores = await _proveedorRepository.ObtenerProveedorId(id);
                var model = new ProveedorViewModel
                {
                    NombreProveedor= proveedores.NombreProveedor,
                    Contacto = proveedores.Contacto,
                    Direccion = proveedores.Direccion,
                    IdUsuario = proveedores.IdUsuario,
                    Usuarios = usuarios
                };
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar la vista de edicion del proveedor");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo encargado de editar el proveedor
        [HttpPost]
        public async Task<ActionResult> Edit(ProveedorViewModel model, int Id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                
                    var (success,errorMessage)=await ExecutePolicyAsync(()=> _proveedorRepository.EditarProveedor(model,Id)) ;
                    if (success)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                    }
                      
                    return RedirectToAction("Index");
                
               
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al editar el proveedor");
                return RedirectToAction("Error", "Home");
            }

        }       
        private async Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicyAsync<T>();
            return await policy.ExecuteAsync(operation);
        }
        private T ExecutePolicy<T>(Func<T> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicy<T>();
            return policy.Execute(operation);
        }



    }
}
