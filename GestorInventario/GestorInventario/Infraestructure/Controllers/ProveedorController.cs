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
using System.Linq;

namespace GestorInventario.Infraestructure.Controllers
{
    public class ProveedorController : Controller
    {
        
        private readonly ILogger<ProveedorController> _logger;
        private readonly GenerarPaginas _generarPaginas;
        private readonly PolicyHandler _PolicyHandler;
        private readonly IProveedorRepository _proveedorRepository;
        public ProveedorController( ILogger<ProveedorController> logger, GenerarPaginas generarPaginas, 
            PolicyHandler policyHandler, IProveedorRepository proveedor)
        {
            
            _logger = logger;
            _generarPaginas = generarPaginas;
            _PolicyHandler = policyHandler;
            _proveedorRepository= proveedor;
        }

        public async Task<IActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                //var proveedores = from p in _context.Proveedores
                //                  select p;
                var proveedores =ExecutePolicy(()=> _proveedorRepository.ObtenerProveedores()) ;
                // var proveedores = await _context.Proveedores.ToListAsync();
                ViewData["Buscar"] = buscar;
                if (!String.IsNullOrEmpty(buscar))
                {
                    proveedores=proveedores.Where(s=>s.NombreProveedor!.Contains(buscar));  
                }
                await HttpContext.InsertarParametrosPaginacionRespuesta(proveedores, paginacion.CantidadAMostrar);
                var proveedor = ExecutePolicy(() => proveedores.Paginar(paginacion).ToList());
                //Obtiene los datos de la cabecera que hace esta peticion
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                //Crea las paginas que el usuario ve.
                ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);
                return View(proveedor);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar los proveedores");
                return RedirectToAction("Error", "Home");
            }

        }
        public IActionResult Create()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }
        [HttpPost]
      
        public async Task<IActionResult> Create(ProveedorViewModel model)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                //Esto toma en cuenta las validaciones puestas en BeerViewModel
                if (ModelState.IsValid)
                {
                   
                    var (success,errorMessage)= await ExecutePolicyAsync(()=> _proveedorRepository.CrearProveedor(model)) ;
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Los datos se han creado con éxito.";
                    }
                   

                    return RedirectToAction(nameof(Index));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al crear el pedido");
                return RedirectToAction("Error", "Home");
            }


        }
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
           
                var proveedor= await ExecutePolicyAsync(()=> _proveedorRepository.ObtenerProveedorId(id));
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
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar la vista de eliminacion del proveedor");
                return RedirectToAction("Error", "Home");
            }

        }


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
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
               var proveedor= await ExecutePolicyAsync(() => _proveedorRepository.ObtenerProveedorId(id));
                return View(proveedor);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar la vista de edicion del proveedor");
                return RedirectToAction("Error", "Home");
            }

        }
        [HttpPost]
        public async Task<ActionResult> Edit(ProveedorViewModel model)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                if (ModelState.IsValid)
                {
                    var (success,errorMessage)=await ExecutePolicyAsync(()=> _proveedorRepository.EditarProveedor(model)) ;
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
                return View(model);
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
