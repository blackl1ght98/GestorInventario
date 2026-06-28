using AutoMapper;
using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Shared.Utilities;
using GestorInventario.ViewModels.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Infraestructure.Controllers.AdminControllers
{
    public class RoleController : Controller
    {
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaginationHelper _paginationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RoleController> _logger;
        private readonly IMapper _mapper;

        public RoleController(IPolicyExecutor policyExecutor, IPaginationHelper paginationHelper, IUnitOfWork unitOfWork, ILogger<RoleController> logger, IMapper mapper)
        {
            _policyExecutor = policyExecutor;
            _paginationHelper = paginationHelper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> ObtenerRoles([FromQuery] Paginacion paginacion)
        {
            try
            {
                var queryable = _policyExecutor.ExecutePolicy(() => _unitOfWork.AdminRepository.ObtenerRoles());

                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() => _paginationHelper.PaginarAsync(queryable, paginacion));

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


                var usuariosQueryable = _policyExecutor.ExecutePolicy(() => _unitOfWork.AdminRepository.ObtenerUsuariosPorRol(id));
                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() => _paginationHelper.PaginarAsync(usuariosQueryable, paginacion));

                var todosLosRoles = _policyExecutor.ExecutePolicy(() => _unitOfWork.AdminRepository.ObtenerRoles());

                var viewModel = new UsuariosPorRolViewModel
                {
                    Usuarios = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginacion.Pagina,
                    RolId = id,
                    TodosLosRoles = todosLosRoles.ToList(),
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

                if (usuario == null)
                {
                    return Json(new { success = false, errorMessage = "Usuario no encontrado" });
                }

                if (usuario.IdRolNavigation == null)
                {
                    return Json(new { success = false, errorMessage = "El rol actual del usuario no está definido." });
                }

                // Obtener todos los roles disponibles
                var rolesDomainResult = _policyExecutor.ExecutePolicy(() => _unitOfWork.AdminRepository.ObtenerRoles()); ;

                if (rolesDomainResult == null || !rolesDomainResult.Any())
                {
                    return Json(new { success = false, errorMessage = "No se encontraron roles disponibles." });
                }

                // Mapear correctamente la lista
                var roles = _mapper.Map<List<Role>>(rolesDomainResult);

                // Buscar el nuevo rol
                var nuevoRol = roles.FirstOrDefault(r => r.Id == request.RolId);
                if (nuevoRol == null)
                {
                    return Json(new { success = false, errorMessage = "El rol seleccionado no existe." });
                }

                // Evitar cambio innecesario
                if (usuario.IdRol == nuevoRol.Id)
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
    }
}
