using AutoMapper;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.Services.Notifications;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace GestorInventario.Infraestructure.Controllers.AdminControllers
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
        public AdminController(
            ILogger<AdminController> logger, 
            IUnitOfWork unitOfWork, 
            IUserManagementService userManagement,
            IMapper map, 
            IPolicyExecutor policyExecutor, 
            IPaginationHelper paginationHelper, 
            ICurrentUserAccessor currentUser
         )
        {                    
            _logger = logger;         
            _mapper= map;
            _policyExecutor = policyExecutor;   
            _unitOfWork = unitOfWork;
            _paginationHelper = paginationHelper;
            _currentUserAccessor = currentUser;
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

                if (User.IsAdministrador())
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
                var validar = await _userManagementService.ValidarRegistro(confirmar);
                if (!validar.Success) {
                    TempData["ErrorMessageConfirm"] =validar.Message;
                }
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

               
                var usuario = await _policyExecutor.ExecutePolicyAsync(
                    () => _unitOfWork.UserRepository.ObtenerUsuarioPorId(usuarioAEditarId));

                if ( usuario == null)
                {
                    TempData["ErrorMessage"] =  "Usuario no encontrado";
                    _logger.LogWarning("Intento de editar usuario inexistente con ID {UserId}", id);
                    return RedirectToAction(nameof(Index));
                }

               
            
                var viewModel = _mapper.Map<UsuarioEditViewModel>(usuario);
                // Marcamos si es edición propia
                viewModel.EsEdicionPropia = usuarioAEditarId == userIdClaim;            
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
     
        public async Task<ActionResult> Edit(UsuarioEditViewModel userVM)
        {
            try
            {             
                         
                if (!ModelState.IsValid)
                {
                    return View(userVM);
                }
                var result= await _userManagementService.EditarUsuarioAsync(userVM);
               
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return View(userVM);
                }
                if (User.IsAdministrador())
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
               
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
                var usuario = await _policyExecutor.ExecutePolicyAsync(() => _unitOfWork.UserRepository.ObtenerUsuarioPorId(id));
                if (usuario == null)
                {
                    _logger.LogCritical("Se intento manipular la url por el usuario: " + _currentUserAccessor.GetCurrentUserId());
                    return RedirectToAction(nameof(Index));
                }
                var viewModel = new EliminarUsuarioViewModel
                {
                    Id = usuario.Id,
                    NombreCompleto = usuario.NombreCompleto,
                    Email = usuario.Email,
                    Direccion = usuario.Direccion,
                    Ciudad = usuario.Ciudad,
                    CodigoPostal = usuario.CodigoPostal,
                    FechaNacimiento = usuario.FechaNacimiento,
                    FechaRegistro = usuario.FechaRegistro
                };

                return View(viewModel);
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
        // Este metodo se llaman desde el script alta-baja-usuario.js
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
        // Este metodo se llaman desde el script alta-baja-usuario.js
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

        //[HttpPost("alert")]
        //public async Task<IActionResult> SendAlert([FromBody] string msg)
        //{
        //    var success = await _notificationService.SendWhatsAppNotificationAsync(msg);
        //    return success ? Ok("Enviado") : BadRequest("Error al enviar");
        //}
        private void CargarRolesEnViewData()
        {
            var roles = _policyExecutor.ExecutePolicy(() => _unitOfWork.AdminRepository.ObtenerRoles());
            ViewData["Roles"] = new SelectList(roles, "Id", "Nombre");
        }
    }
}
