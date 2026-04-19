using AutoMapper;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
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
        private readonly ILogger<AdminController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPolicyExecutor _policyExecutor;  
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPaginationHelper _paginationHelper;
        private readonly IUserManagementService _userManagementService;
        public AdminController(ILogger<AdminController> logger, IAdminRepository adminRepository, IUnitOfWork unit, IUserManagementService userManagement,
        IMapper map, IPolicyExecutor executor, IUserRepository user, IPaginationHelper paginationHelper, ICurrentUserAccessor current)
        {           
            
            _logger = logger;         
            _mapper= map;
            _policyExecutor = executor;   
            _unitOfWork = unit;
            _paginationHelper = paginationHelper;
            _currentUserAccessor = current;
            _userManagementService = userManagement;
        }
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                var currentUserId = _currentUserAccessor.GetCurrentUserId();

                var queryable = _policyExecutor.ExecutePolicy(() =>
                    _unitOfWork.AdminRepository.ObtenerUsuarios())
                    .Where(u => u.Id != currentUserId); 

                if (!string.IsNullOrEmpty(buscar))
                {
                    queryable = queryable.Where(s => s.NombreCompleto.Contains(buscar));
                }

                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationHelper.PaginarAsync(queryable, paginacion));

                var viewModel = new UsuariosViewModel
                {
                    Usuarios = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginationResult.PaginaActual,
                    Buscar = buscar
                };

                CargarRolesEnViewData();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los datos del usuario");
                return RedirectToAction("Error", "Home");
            }
        }

        public IActionResult Create()
        {
            try
            {
                 CargarRolesEnViewData();
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                     CargarRolesEnViewData();
                    return View(model);
                }

                var result = await _policyExecutor.ExecutePolicyAsync(() => _userManagementService.CrearUsuarioAsync(model));

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                     CargarRolesEnViewData();
                    return View(model);
                }

                TempData["SuccessMessage"] = result.Message;

                if (User.IsInRole("Administrador") && (User.Identity?.IsAuthenticated ?? false))
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
                var usuarioDB = await  _unitOfWork.UserRepository.ObtenerUsuarioPorId(confirmar.UserId);

                if(usuarioDB is null|| usuarioDB.Data is null)
                {
                   
                    _logger.LogWarning("Intento de confirmar un usuario inexistente con ID {UserId}", confirmar.UserId);
                    return RedirectToAction("Login", "Auth");
                }
               
                if (usuarioDB.Data.ConfirmacionEmail != false)
                {
                    TempData["ErrorMessageConfirm"] = "Usuario ya validado con anterioridad";
                    TempData.Keep("ErrorMessageConfirm");
                    _logger.LogInformation($"El usuario con email {usuarioDB.Data.Email} ha intentado confirmar su correo estando confirmado");
                      return RedirectToAction("Login", "Auth");
                }
                if (usuarioDB.Data.EmailVerificationToken != confirmar.Token)
                {
                    _logger.LogCritical("Intento de manipulacion del token por el usuario: " + usuarioDB.Data.Id);

                }
                await _unitOfWork.UserRepository.ConfirmEmail(new ConfirmRegistrationDto
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
        [Authorize]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var userIdClaim = _currentUserAccessor.GetCurrentUserId();
                int usuarioAEditarId = id == 0 ? userIdClaim : id;

                // Obtenemos el usuario como EntityUser (dominio)
                var resultado = await _policyExecutor.ExecutePolicyAsync(
                    () => _unitOfWork.UserRepository.ObtenerUsuarioParaEdicionAsync(usuarioAEditarId));

                if (!resultado.Success || resultado.Data == null)
                {
                    TempData["ErrorMessage"] = resultado.Message ?? "Usuario no encontrado";
                    _logger.LogWarning("Intento de editar usuario inexistente con ID {UserId}", id);
                    return RedirectToAction(nameof(Index));
                }

                var usuarioDominio = resultado.Data;

                // Mapeamos de EntityUser (dominio) a ViewModel
                var viewModel = _mapper.Map<UsuarioEditViewModel>(usuarioDominio);

                // Marcamos si es edición propia
                viewModel.EsEdicionPropia = (usuarioAEditarId == userIdClaim);

                // Solo cargamos los roles si NO es edición propia (el admin editando a otro)
                if (!viewModel.EsEdicionPropia)
                {
                     CargarRolesEnViewData();
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder. Inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al visualizar la vista de edición de usuario {UserId}", id);
                return RedirectToAction("Error", "Home");
            }
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(UsuarioEditViewModel userVM)
        {
            try
            {             
                // Cargar roles para la vista en caso de error
                if (!userVM.EsEdicionPropia)
                {
                     CargarRolesEnViewData();
                }           
                if (!ModelState.IsValid)
                {
                    return View(userVM);
                }
                var result= await _userManagementService.EditarUsuarioAsync(userVM);
               
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    if (User.IsAdministrador())
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
                var (user, mensaje) = await _policyExecutor.ExecutePolicyAsync(() => _unitOfWork.UserRepository.ObtenerUsuarioConPedido(id));
                if (user == null)
                {
                    _logger.LogCritical("Se intento manipular la url por el usuario: " + _currentUserAccessor.GetCurrentUserId());
                    return RedirectToAction(nameof(Index));
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
                var result = await _policyExecutor.ExecutePolicyAsync(() =>_userManagementService.EliminarUsuarioAsync(Id));
                if (result.Success)
                    return RedirectToAction(nameof(Index));

                TempData["ErrorMessage"] = result.Message;
                _logger.LogError(result.Message);
                return RedirectToAction(nameof(Delete), new { id = Id });
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder. Inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al eliminar un usuario");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo que da de baja el usuario, este metodo se llaman desde el script alta-baja-usuario.js
        [HttpPost]
        [Authorize(Roles ="Administrador")]
      
        public async Task<IActionResult> BajaUsuarioPost([FromBody] UsuarioRequestDto request)
        {
            var currentUserId = _currentUserAccessor.GetCurrentUserId();
            if (request.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "No puedes darte de baja a ti mismo";
                return RedirectToAction(nameof(Index));
            }
            var result = await _policyExecutor.ExecutePolicyAsync(() => _unitOfWork.AdminRepository.BajaUsuario(request.Id));
         
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
     
        public async Task<IActionResult> AltaUsuarioPost([FromBody] UsuarioRequestDto request)
        {
            var currentUserId = _currentUserAccessor.GetCurrentUserId();
            if (request.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "No puedes darte de baja a ti mismo";
                return RedirectToAction(nameof(Index));
            }
            var result = await _policyExecutor.ExecutePolicyAsync(() => _unitOfWork.AdminRepository.AltaUsuario(request.Id));
          
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
                var queryable = _policyExecutor.ExecutePolicy(() => _unitOfWork.AdminRepository.ObtenerRoles());

                var paginationResult = await _policyExecutor.ExecutePolicyAsync(()=>_paginationHelper.PaginarAsync(queryable.Data,paginacion));

                var viewModel = new RolesViewModel
                {
                    Roles = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginationResult.PaginaActual,
                   
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
                

                var usuariosQueryable =  _policyExecutor.ExecutePolicy(() => _unitOfWork.AdminRepository.ObtenerUsuariosPorRol(id));
                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() => _paginationHelper.PaginarAsync(usuariosQueryable, paginacion));

                var todosLosRoles = _policyExecutor.ExecutePolicy(() => _unitOfWork.AdminRepository.ObtenerRoles());
               
                var viewModel = new UsuariosPorRolViewModel
                {
                    Usuarios = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginacion.Pagina,
                    RolId = id,
                    TodosLosRoles = todosLosRoles.Data.ToList(),
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
                var usuario = await _policyExecutor.ExecutePolicyAsync(() =>
                    _unitOfWork.UserRepository.ObtenerUsuarioPorId(request.Id));

                if (usuario?.Data == null)
                {
                    return Json(new { success = false, errorMessage = usuario?.Message ?? "Usuario no encontrado" });
                }

                if (usuario.Data.IdRolNavigation == null)
                {
                    return Json(new { success = false, errorMessage = "El rol actual del usuario no está definido." });
                }

                // Obtener todos los roles disponibles
                var rolesDomainResult = _policyExecutor.ExecutePolicy(() => _unitOfWork.AdminRepository.ObtenerRoles()); ;

                if (rolesDomainResult == null || !rolesDomainResult.Data.Any())
                {
                    return Json(new { success = false, errorMessage = "No se encontraron roles disponibles." });
                }

                // Mapear correctamente la lista
                var roles = _mapper.Map<List<Role>>(rolesDomainResult.Data);

                // Buscar el nuevo rol
                var nuevoRol = roles.FirstOrDefault(r => r.Id == request.RolId);
                if (nuevoRol == null)
                {
                    return Json(new { success = false, errorMessage = "El rol seleccionado no existe." });
                }

                // Evitar cambio innecesario
                if (usuario.Data.IdRol == nuevoRol.Id)
                {
                    return Json(new { success = false, errorMessage = "El usuario ya tiene el rol seleccionado." });
                }

                // Actualizar el rol
                await _policyExecutor.ExecutePolicy(() =>
                    _unitOfWork.AdminRepository.ActualizarRolUsuario(request.Id, nuevoRol.Id));

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

        private void CargarRolesEnViewData()
        {
            var roles = _policyExecutor.ExecutePolicy(() => _unitOfWork.AdminRepository.ObtenerRoles()); 
            ViewData["Roles"] = new SelectList(roles.Data, "Id", "Nombre");
        }
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ObtenerInfoReasignacion(int id)
        {
            var usuario = await _policyExecutor.ExecutePolicyAsync(() =>
                _unitOfWork.AdminRepository.ObtenerUsuarioConProveedoresYPedidosAsync(id));

            if (usuario.Data is null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            if (!usuario.Data.Proveedores.Any())
                return Json(new { success = false, message = "Este usuario no tiene proveedores asignados" });

            var usuarios = _unitOfWork.AdminRepository.ObtenerUsuarios()
                .Where(u => u.Id != id)
                .Select(u => new { u.Id, u.NombreCompleto })
                .ToList();

            return Json(new { success = true, usuarios });
        }
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ReasignarProveedores([FromBody] ReasignarProveedoresDto request)
        {
            var result = await _policyExecutor.ExecutePolicyAsync(() =>
                _unitOfWork.AdminRepository.ReasignarProveedoresAsync(
                    request.UsuarioOrigenId, request.UsuarioDestinoId));

            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
