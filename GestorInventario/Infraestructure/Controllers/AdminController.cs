using AutoMapper;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.Services;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.user;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace GestorInventario.Infraestructure.Controllers
{
    public class AdminController : Controller
    {
      
       
        private readonly IConfirmEmailService _confirmEmailService;
        private readonly ILogger<AdminController> _logger;
        private readonly IAdminRepository _adminrepository;
        private readonly GenerarPaginas _generarPaginas;      
        private readonly IMapper _mapper;
        private readonly PolicyExecutor _policyExecutor;
        private readonly UtilityClass _utilityClass;
        private readonly PaginationHelper _paginationHelper;
        public AdminController(IConfirmEmailService confirmEmailService, ILogger<AdminController> logger, IAdminRepository adminRepository,  GenerarPaginas generarPaginas, 
        IMapper map, PolicyExecutor executor, UtilityClass utility, PaginationHelper paginationHelper)
        {           
            _confirmEmailService = confirmEmailService;
            _logger = logger;
            _adminrepository = adminRepository;
            _generarPaginas = generarPaginas;            
            _mapper= map;
            _policyExecutor = executor;   
            _utilityClass = utility;
            _paginationHelper = paginationHelper;
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                var queryable = _policyExecutor.ExecutePolicy(() => _adminrepository.ObtenerUsuarios());

                if (!string.IsNullOrEmpty(buscar))
                {
                    queryable = queryable.Where(s => s.NombreCompleto.Contains(buscar));
                }

                // 🔹 Usamos el helper directamente
                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationHelper.PaginarAsync(queryable, paginacion)
                );

                var viewModel = new UsuariosViewModel
                {
                    Usuarios = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),              // viene del helper
                    TotalPaginas = paginationResult.TotalPaginas,    // viene del helper
                    PaginaActual = paginationResult.PaginaActual,    // viene del helper
                    Buscar = buscar
                };

                await CargarRolesEnViewData();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los datos del usuario");
                return RedirectToAction("Error", "Home");
            }
        }


        public async Task<IActionResult> Create()
        {
            try
            {
                await CargarRolesEnViewData();
                return View();
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al visualizar la vista de creación de usuario");
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarRolesEnViewData();
                    return View(model);
                }

                var result = await _policyExecutor.ExecutePolicyAsync(() => _adminrepository.CrearUsuario(model));

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    await CargarRolesEnViewData();
                    return View(model);
                }

                TempData["SuccessMessage"] = result.Message;

                if (User.IsInRole("administrador") && (User.Identity?.IsAuthenticated ?? false))
                {
                    return RedirectToAction(nameof(Index));
                }

                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor tardó mucho en responder, inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al realizar el registro del usuario");
                return RedirectToAction("Error", "Home");
            }
        }

            [AllowAnonymous]
        [HttpGet("admin/confirm-registration/{UserId}/{Token}")]
        public async Task<IActionResult> ConfirmRegistration(ConfirmRegistrationDto confirmar)
        {
            try
            {               
                var (usuarioDB,mensaje) = await  _adminrepository.ObtenerPorId(confirmar.UserId);

                if(usuarioDB is null)
                {
                    TempData["ErrorMessage"] = mensaje; 
                    _logger.LogWarning("Intento de confirmar un usuario inexistente con ID {UserId}", confirmar.UserId);
                    return RedirectToAction("Login", "Auth");
                }
                if (usuarioDB.ConfirmacionEmail != false)
                {
                    TempData["ErrorMessage"] = "Usuario ya validado con anterioridad";
                    _logger.LogInformation($"El usuario con email {usuarioDB.Email} ha intentado confirmar su correo estando confirmado");
                }
                if (usuarioDB.EnlaceCambioPass != confirmar.Token)
                {
                    _logger.LogCritical("Intento de manipulacion del token por el usuario: " + usuarioDB.Id);
                   
                }
                await _confirmEmailService.ConfirmEmail(new ConfirmRegistrationDto
                {
                    UserId = confirmar.UserId
                });
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al confirmar la cuenta del usuario");
                return RedirectToAction("Error", "Home");
            }
            
        }
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var userIdClaim = _utilityClass.ObtenerUsuarioIdActual();
                // Si el id recibido es 0, significa que se quiere editar el usuario actual (obtenido desde los claims),
                // si no, se edita el usuario cuyo id se pasó en el parámetro.
                int usuarioAEditarId = id == 0 ? userIdClaim : id;

                var (user,mensaje) = await _policyExecutor.ExecutePolicyAsync(() => _adminrepository.ObtenerPorId(usuarioAEditarId));
                if (user is null)
                {
                    TempData["ErrorMessage"] = mensaje;
                    _logger.LogWarning("Intento de editar un usuario inexistente con ID {UserId}", id);
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = _mapper.Map<UsuarioEditViewModel>(user);
                viewModel.EsEdicionPropia = usuarioAEditarId == userIdClaim;

                if (!viewModel.EsEdicionPropia)
                {
                    await CargarRolesEnViewData();
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder. Inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al visualizar la vista de edición de usuario");
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public async Task<ActionResult> Edit(UsuarioEditViewModel userVM)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Cargar roles para la vista en caso de error
                if (!userVM.EsEdicionPropia)
                {
                    await CargarRolesEnViewData();
                }           
                if (!ModelState.IsValid)
                {
                    return View(userVM);
                }
                var result= await _adminrepository.EditarUsuario(userVM);
               
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    if (User.IsAdministrador() && User.Identity.IsAuthenticated)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    return RedirectToAction("Index", "Home");
                }

                TempData["ErrorMessage"] = result.Message;
                return View(userVM);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al editar la información del usuario");
                return RedirectToAction("Error", "Home");
            }
        }
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {             
                var (user,mensaje) = await _policyExecutor.ExecutePolicyAsync(() => _adminrepository.ObtenerUsuarioConPedido(id));            
                if (user == null)
                {
                    TempData["ErrorMessage"] = mensaje;
                }        
                return View(user);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al visualizar la vista de eliminacion del usuario");
                return RedirectToAction("Error", "Home");
            }
           
        }    
        
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
               var result = await _policyExecutor.ExecutePolicyAsync(() => _adminrepository.EliminarUsuario(Id));
               
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(Delete), new { id = Id });
                }
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar un usuario");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo que da de baja el usuario, este metodo se llaman desde el script alta-baja-usuario.js
        [HttpPost]
        [Authorize(Roles ="Administrador")]
        public async Task<IActionResult> BajaUsuarioPost([FromBody] UsuarioRequest request)
        {
            var result = await _policyExecutor.ExecutePolicyAsync(() => _adminrepository.BajaUsuario(request.Id));
         
            if (result.Success)
            {
                return Json(new { success = true });
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                return Json(new { success = false, errorMessage = result.Message });
            }
        }
        //Metodo que da de alta el usuario, este metodo se llaman desde el script alta-baja-usuario.js
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AltaUsuarioPost([FromBody] UsuarioRequest request)
        {
            var result = await _policyExecutor.ExecutePolicyAsync(() => _adminrepository.AltaUsuario(request.Id));
          
            if (result.Success)
            {
                return Json(new { success = true });
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                return Json(new { success = false, errorMessage = result.Message });
            }
        }
        [Authorize(Roles ="Administrador")]
        [HttpGet]
        public async Task<IActionResult> ObtenerRoles([FromQuery] Paginacion paginacion)
        {
            try
            {
                var queryable =  _policyExecutor.ExecutePolicy(() => _adminrepository.ObtenerRolesConUsuarios());

                // Aplicar paginación usando PaginarAsync
                var (roles, totalItems) = await _policyExecutor.ExecutePolicyAsync(() => queryable.PaginarAsync(paginacion));
                var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);
                var paginas = _generarPaginas.GenerarListaPaginas(totalPaginas, paginacion.Pagina, paginacion.Radio);

                var viewModel = new RolesViewModel
                {
                    Roles = roles,
                    Paginas = paginas,
                    TotalPaginas = totalPaginas,
                    PaginaActual = paginacion.Pagina,
                   
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
             
                _logger.LogError(ex, "Error al obtener los roles");
                return RedirectToAction("Error", "Home");
            }
        }
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> VerUsuariosPorRol(int id, [FromQuery] Paginacion paginacion)
        {
            try
            {
                var rol = ( _policyExecutor.ExecutePolicy(() => _adminrepository.ObtenerRolesConUsuarios()))
                    .FirstOrDefault(r => r.Id == id);
                if (rol == null)
                {
                    TempData["Error"] = "Rol no encontrado.";
                    return RedirectToAction("ObtenerRoles");
                }

                var usuariosQueryable =  _policyExecutor.ExecutePolicy(() => _adminrepository.ObtenerUsuariosPorRol(id));
                var (usuariosPaginados, totalItems) = await _policyExecutor.ExecutePolicyAsync(() => usuariosQueryable.PaginarAsync(paginacion));
                var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);
                var paginas = _generarPaginas.GenerarListaPaginas(totalPaginas, paginacion.Pagina, paginacion.Radio);

                var todosLosRoles = await _policyExecutor.ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());

                var viewModel = new UsuariosPorRolViewModel
                {
                    Usuarios = usuariosPaginados,
                    Paginas = paginas,
                    TotalPaginas = totalPaginas,
                    PaginaActual = paginacion.Pagina,
                    RolId = id,
                    TodosLosRoles = todosLosRoles
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
            
                _logger.LogError(ex, "Error al obtener los usuarios del rol con Id {Id}", id);
                return RedirectToAction("Error", "Home");
            }
        }
       
        //Metodo para editar el rol se llama desde el script ver-usuario-rol.js
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CambiarRol([FromBody] CambiarRolDto request)
        {
            try
            {
              
                var (usuario,mensaje) = await _policyExecutor.ExecutePolicyAsync(() => _adminrepository.ObtenerPorId(request.Id));
                if (usuario == null)
                {
                    return Json(new { success = false, errorMessage = mensaje });
                }

               
                if (usuario.IdRolNavigation == null)
                {
                    return Json(new { success = false, errorMessage = "El rol actual del usuario no está definido." });
                }

                // Obtener todos los roles disponibles
                var roles = await _policyExecutor.ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
                if (roles == null || !roles.Any())
                {
                    return Json(new { success = false, errorMessage = "No se encontraron roles disponibles." });
                }

                // Buscar el nuevo rol por Id
                var nuevoRol = roles.FirstOrDefault(r => r.Id == request.RolId);
                if (nuevoRol == null)
                {
                    return Json(new { success = false, errorMessage = "El rol seleccionado no existe." });
                }

                // Evitar un cambio innecesario si el rol seleccionado es el mismo que el actual
                if (usuario.IdRol == nuevoRol.Id)
                {
                    return Json(new { success = false, errorMessage = "El usuario ya tiene el rol seleccionado." });
                }

                // Actualizar el rol del usuario
                await _policyExecutor.ExecutePolicy(() => _adminrepository.ActualizarRolUsuario(request.Id, nuevoRol.Id));
                return Json(new
                {
                    success = true,
                    message = $"El rol del usuario ha sido cambiado a {nuevoRol.Nombre}."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar el rol del usuario con Id {Id}", request.Id);
                return Json(new { success = false, errorMessage = "El servidor ha tardado mucho en responder, intenta de nuevo más tarde." });
            }
        }
       
        private async Task CargarRolesEnViewData()
        {
            var roles = await _policyExecutor.ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
            ViewData["Roles"] = new SelectList(roles, "Id", "Nombre");
        }
      
    }
}
