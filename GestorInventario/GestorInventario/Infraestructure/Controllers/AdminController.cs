using GestorInventario.Application.DTOs;
using GestorInventario.Application.Services;
using GestorInventario.Interfaces.Application;
using GestorInventario.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestorInventario.PaginacionLogica;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Domain.Models.ViewModels;
using System.Security.Claims;


namespace GestorInventario.Infraestructure.Controllers
{
    public class AdminController : Controller
    {
      
        private readonly IEmailService _emailService;
        private readonly HashService _hashService;
        private readonly IConfirmEmailService _confirmEmailService;
        private readonly ILogger<AdminController> _logger;
        private readonly IAdminRepository _adminrepository;
        private readonly GenerarPaginas _generarPaginas;
        private readonly PolicyHandler _PolicyHandler;   
       
        public AdminController( IEmailService emailService, HashService hashService, IConfirmEmailService confirmEmailService, 
            ILogger<AdminController> logger, IAdminRepository adminRepository,  GenerarPaginas generarPaginas, 
             PolicyHandler policy)
        {
           
            _emailService = emailService;
            _hashService = hashService;
            _confirmEmailService = confirmEmailService;
            _logger = logger;
            _adminrepository = adminRepository;
            _generarPaginas = generarPaginas;
            _PolicyHandler = policy;
          
        }
        public async Task<ActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {

                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var queryable =  await ExecutePolicyAsync(() => _adminrepository.ObtenerUsuarios());
               
                //var queryable = _adminrepository.ObtenerUsuarios();
                 ViewData["Buscar"] = buscar;
              
                if (!String.IsNullOrEmpty(buscar))
                {
                    queryable = queryable.Where(s => s.NombreCompleto.Contains(buscar));
                }
                //Accedemos al metodo de extension creado pasandole la fuente de informacion(queryable) y las paginas a mostrar
                 HttpContext.InsertarParametrosPaginacionRespuestaLista(queryable, paginacion.CantidadAMostrar);
                var usuarios = ExecutePolicy(() => queryable.PaginarLista(paginacion).ToList());
                //Obtiene los datos de la cabecera que hace esta peticion
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                //Crea las paginas que el usuario ve.
                ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);
                var roles = await ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
                ViewData["Roles"] = new SelectList(roles, "Id", "Nombre");

                return View(usuarios);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener los datos del usuario");
                return RedirectToAction("Error", "Home"); 

            }
        }
      
        [HttpPost]
        public async Task<IActionResult> UpdateRole(int id, int newRole)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var (success, errorMessage) = await ExecutePolicyAsync(() => _adminrepository.EditarRol(id, newRole));
                if (success)
                {
                    TempData["SuccessMessage"] = "Rol cambiado";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = errorMessage;           
                }
                var roles = await ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
                ViewData["Roles"] = new SelectList(roles, "Id", "Nombre");

                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al actualizar el rol");
                return RedirectToAction("Error", "Home");
            }
           
        }
        public async Task<IActionResult> Create()
        {
            try
            {
                // Sirve para obtener los datos del desplegable
                var roles = await ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
                ViewData["Roles"] = new SelectList(roles, "Id", "Nombre");

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
               
                if (ModelState.IsValid)
                {
                    var (success, errorMessage) = await ExecutePolicyAsync(() => _adminrepository.CrearUsuario(model));
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Usuario creado con exito";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                    }
                    var roles = await ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
                    ViewData["Roles"] = new SelectList(roles, "Id", "Nombre");

                    return RedirectToAction("Login", "Auth");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al realizar el registro del usuario");
                return RedirectToAction("Error", "Home");
            }         
        }
        [AllowAnonymous]
        [Route("AdminController/ConfirmRegistration/{UserId}/{Token}")]
        public async Task<IActionResult> ConfirmRegistration(DTOConfirmRegistration confirmar)
        {
            try
            {
                
                var usuarioDB = await ExecutePolicyAsync(() => _adminrepository.ObtenerPorId(confirmar.UserId));
              
                if (usuarioDB.ConfirmacionEmail != false)
                {
                    TempData["ErrorMessage"] = "Usuario ya validado con anterioridad";
                    _logger.LogInformation($"El usuario con email {usuarioDB.Email} ha intentado confirmar su correo estando confirmado");
                }

                if (usuarioDB.EnlaceCambioPass != confirmar.Token)
                {
                    _logger.LogCritical("Intento de manipulacion del token por el usuario: " + usuarioDB.Id);
                   
                }
                await _confirmEmailService.ConfirmEmail(new DTOConfirmRegistration
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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var user = await ExecutePolicyAsync(() => _adminrepository.ObtenerPorId(id));                          
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                }
                // Creas un nuevo ViewModel y llenas sus propiedades con los datos del usuario
                UsuarioEditViewModel viewModel = new UsuarioEditViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    NombreCompleto = user.NombreCompleto,
                    FechaNacimiento = user.FechaNacimiento,
                    Telefono = user.Telefono,
                    IdRol=user.IdRol,
                    Direccion = user.Direccion
                };
                var roles = await ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
                ViewData["Roles"] = new SelectList(roles, "Id", "Nombre");


                // Pasas el ViewModel a la vista
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al visualizar la vista de edicion de usuario");
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
                //Si el modelo es valido:
                if (ModelState.IsValid)
                {
                    if (!User.Identity.IsAuthenticated)
                    {
                        return RedirectToAction("Login", "Auth");
                    }
                    var roles = await ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
                    ViewData["Roles"] = new SelectList(roles, "Id", "Nombre");


                    var (success,errorMessage) = await _adminrepository.EditarUsuario(userVM);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Usuario Actualizado con exito";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                    }
                    return RedirectToAction("Index");
                }
                
                return View(userVM);
            }
            catch(DbUpdateConcurrencyException ex)
            {
                var (success, errorMessage) = await _adminrepository.EditarUsuario(userVM);
                if (success)
                {
                    TempData["SuccessMessage"] = "Usuario Actualizado con exito";
                    return RedirectToAction(nameof(Index));
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
                _logger.LogError(ex, "Error al editar la informacion del usuario");
                return RedirectToAction("Error", "Home");
            }  
        }
        public async Task<IActionResult> EditUserActual(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var user = await ExecutePolicyAsync(() => _adminrepository.ObtenerPorId(usuarioId));

                    if (user == null)
                    {
                        TempData["ErrorMessage"] = "Usuario no encontrado";
                        return BadRequest("Usuario no encontrado");
                    }

                    // Crear el ViewModel
                    var viewModel = new EditarUsuarioActual
                    {
                        Email = user.Email,
                        NombreCompleto = user.NombreCompleto,
                        FechaNacimiento = user.FechaNacimiento,
                        Telefono = user.Telefono,
                        Direccion = user.Direccion
                    };

                    
                    return View(viewModel);
                }

            
                TempData["ErrorMessage"] = "Error al obtener el usuario";
                return BadRequest("Error al obtener el usuario");
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder. Inténtalo de nuevo más tarde.";
                _logger.LogError(ex, "Error al visualizar la vista de edición de usuario");
                return RedirectToAction("Error", "Home");
            }
        }
      
        [HttpPost]
        public async Task<ActionResult> EditUserActual(EditarUsuarioActual userVM)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                //Si el modelo es valido:
                if (ModelState.IsValid)
                {
                    if (!User.Identity.IsAuthenticated)
                    {
                        return RedirectToAction("Login", "Auth");
                    }

                    var (success, errorMessage) = await _adminrepository.EditarUsuarioActual(userVM);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Usuario Actualizado con exito";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                    }
                    return RedirectToAction("Index");
                }

                return View(userVM);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var (success, errorMessage) = await _adminrepository.EditarUsuarioActual(userVM);
                if (success)
                {
                    TempData["SuccessMessage"] = "Usuario Actualizado con exito";
                    return RedirectToAction(nameof(Index));
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
                _logger.LogError(ex, "Error al editar la informacion del usuario");
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
                
                var user = await ExecutePolicyAsync(() => _adminrepository.UsuarioConPedido(id));
              
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
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
        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var (success, message) = await ExecutePolicyAsync(() => _adminrepository.EliminarUsuario(Id));
                if (success)
                {
                    TempData["SuccessMessage"] = message;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = message;
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
       
        [HttpPost]
        public async Task<IActionResult> BajaUsuarioPost(int id)
        {
            var (success, errorMessage) = await ExecutePolicyAsync(() => _adminrepository.BajaUsuario(id));
            if (success)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, errorMessage = errorMessage });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AltaUsuarioPost(int id)
        {
            var (success, errorMessage) = await ExecutePolicyAsync(() => _adminrepository.AltaUsuario(id));
            if (success)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, errorMessage = errorMessage });
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
