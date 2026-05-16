using GestorInventario.Application.DTOs.User;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Infraestructure.Controllers.PedidosControllers
{
    [Authorize(Roles = "Administrador")]
    public class SupplierReassignmentController : Controller
    {
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IUnitOfWork _unitOfWork;

        public SupplierReassignmentController(IPolicyExecutor policyExecutor, IUnitOfWork unitOfWork)
        {
            _policyExecutor = policyExecutor;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerInfoReasignacion(int id)
        {
            var usuario = await _policyExecutor.ExecutePolicyAsync(() =>
                _unitOfWork.AdminRepository.ObtenerUsuarioConProveedoresYPedidosAsync(id));

            if (usuario is null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            if (!usuario.Proveedores.Any())
                return Json(new { success = false, message = "Este usuario no tiene proveedores asignados" });

            var usuarios = _unitOfWork.AdminRepository.ObtenerUsuarios()
                .Where(u => u.Id != id)
                .Select(u => new { u.Id, u.NombreCompleto })
                .ToList();

            return Json(new { success = true, usuarios });
        }
        [HttpPost]
        public async Task<IActionResult> ReasignarProveedores([FromBody] ReasignarProveedoresDto request)
        {
            var result = await _policyExecutor.ExecutePolicyAsync(() =>
                _unitOfWork.AdminRepository.ReasignarProveedoresAsync(
                    request.UsuarioOrigenId, request.UsuarioDestinoId));

            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
