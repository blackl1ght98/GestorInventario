using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.MetodosExtension;
using GestorInventario.Shared.DTOS.User;
using GestorInventario.Shared.Utilities;
using GestorInventario.ViewModels.Proveedor;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;



namespace GestorInventario.Controllers.ProveedorController
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class ProveedorController : Controller
    {
        
        private readonly ILogger<ProveedorController> _logger;       
        private readonly IProveedorRepository _proveedorRepository;     
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaginationHelper _paginationHelper;
        private readonly IUserRepository _userRepository;
       
        public ProveedorController( 
            ILogger<ProveedorController> logger, 
            IUserRepository userRepository, 
            IProveedorRepository proveedorRepository,  
            IPolicyExecutor policyExecutor, 
            IPaginationHelper pagination)
        {           
            _logger = logger;                
            _proveedorRepository= proveedorRepository;           
            _policyExecutor = policyExecutor;
            _paginationHelper = pagination;
            _userRepository = userRepository;  
        }
      
     
        public async Task<IActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                

                var proveedores = _proveedorRepository.ObtenerProveedores();

                ViewData["Buscar"] = buscar;
                if (!string.IsNullOrEmpty(buscar))
                {
                    proveedores = proveedores.Where(s => s.NombreProveedor.Contains(buscar));
                }

                // 🔹 Usamos el helper directamente
                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationHelper.PaginarAsync(proveedores, paginacion)
                );
                var viewModel = new ProviderViewModel
                {
                    Proveedores = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginacion.Pagina,
                    Buscar = buscar,                 
                   
                };              
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al mostrar los proveedores");
                return RedirectToAction("Error", "Home");
            }
        }
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create()
        {
            

            // Cargar los usuarios desde la base de datos
            var usuarios = await _policyExecutor.ExecutePolicyAsync(() => _userRepository.ObtenerUsuariosAsync());

            var model = new ProveedorViewModel
            {
                
                Usuarios = usuarios.ToSelectList(
                    u => u.Id,                     
                    u => u.NombreCompleto,         
                    placeholder: "Seleccione un usuario..."  
                )
            };
            return View(model);
        }
        
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProveedorViewModel model)
        {
            try
            {
              
               
                if (!ModelState.IsValid)
                {
                    var usuarios = await _policyExecutor.ExecutePolicyAsync(() => _userRepository.ObtenerUsuariosAsync());
                    model.Usuarios = usuarios.ToSelectList(
                        u => u.Id,
                        u => u.NombreCompleto,
                        placeholder: "Seleccione un usuario..."
                    );
                    return View(model);  
                }
                var dto = new CrearProveedorDto
                {
                    NombreProveedor = model.NombreProveedor,
                    Contacto = model.Contacto,
                    Direccion = model.Direccion,
                    IdUsuario = model.IdUsuario,
                };
                var proveedor = await _policyExecutor.ExecutePolicyAsync(() => _proveedorRepository.CrearProveedor(dto));
                if (!proveedor.IsSuccess)
                {
                    TempData["ErrorMessage"] = proveedor.Message;
                    return RedirectToAction(nameof(Create));
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
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                

                var proveedor = await _policyExecutor.ExecutePolicyAsync(() => _proveedorRepository.ObtenerProveedorId(id));
                if (proveedor == null)
                {
                    _logger.LogInformation("Proveedor no encontrado");
                    return RedirectToAction(nameof(Index));
                }
                var viewmodel = new DeleteProvedorViewmodel 
                {
                    Id = id,
                    NombreProveedor=proveedor.NombreProveedor,
                    Contacto=proveedor.Contacto,
                    Direccion=proveedor.Direccion,
                
                };
                return View(viewmodel);
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
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
             
               
                var success = await _policyExecutor.ExecutePolicyAsync(() => _proveedorRepository.EliminarProveedor(Id));
                if (success.Success)
                {
                    TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"]=success.Message;
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
                var listaUsuarios = await _policyExecutor.ExecutePolicyAsync(() => _userRepository.ObtenerUsuariosAsync());

                var usuarios = new SelectList(listaUsuarios, "Id", "NombreCompleto");

                var proveedor = await _proveedorRepository.ObtenerProveedorId(id);
                if(proveedor == null)
                {
                    _logger.LogInformation("Proveedor no encontrado");
                    return RedirectToAction(nameof(Index));
                }
                var model = new ProveedorViewModel
                {
                    NombreProveedor= proveedor.NombreProveedor,
                    Contacto = proveedor.Contacto,
                    Direccion = proveedor.Direccion,
                    IdUsuario = proveedor.IdUsuario,
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ProveedorViewModel model, int Id)
        {
            try
            {
                var listaUsuarios = await _policyExecutor.ExecutePolicyAsync(() => _userRepository.ObtenerUsuariosAsync());

                // Validación del modelo (opcional, pero recomendado)
                if (!ModelState.IsValid)
                {
                    
                    model.Usuarios = new SelectList(
                       listaUsuarios,
                        "Id",
                        "NombreCompleto",
                        model.IdUsuario
                    );

                    return View(model);  
                }
                var dto = new EditarProveedorDto
                {
                    Id = Id,
                    NombreProveedor = model.NombreProveedor,
                    Direccion = model.Direccion,
                    Contacto = model.Contacto,
                    IdUsuario = model.IdUsuario

                };
                var success = await _policyExecutor.ExecutePolicyAsync(() => _proveedorRepository.EditarProveedor(dto, Id));

                if (success.Success)
                {
                    TempData["SuccessMessage"] = "Proveedor editado correctamente";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = success.Message ?? "Error al editar el proveedor";

                    // Recargar SelectList para mostrarlo en caso de error
                    model.Usuarios = new SelectList(
                       listaUsuarios,
                        "Id",
                        "NombreCompleto",
                        model.IdUsuario
                    );

                    return View(model); 
                }
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor tardó mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al editar el proveedor");
                return RedirectToAction("Error", "Home");
            }
        }



    }
}
