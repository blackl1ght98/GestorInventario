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
using GestorInventario.MetodosExtension;
using System.Linq;


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

        [Authorize(Roles = "Administrador", Policy = "VerUsuarios")]
        public async Task<ActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                var claims = User.Claims.Select(c => $"{c.Type}: {c.Value}");
                var hasPermiso = User.HasClaim("permiso", "VerUsuarios");
                var isInRole = User.IsInRole("Administrador");
                _logger.LogInformation($"Claims del usuario: {string.Join(", ", claims)}");
                _logger.LogInformation($"Tiene permiso VerUsuarios: {hasPermiso}");
                _logger.LogInformation($"Es Administrador: {isInRole}");
                if (!hasPermiso || !isInRole)
                {
                    return Forbid(); // Forzar error para depurar
                }
                var queryable =  await ExecutePolicyAsync(() => _adminrepository.ObtenerUsuarios());             
                 ViewData["Buscar"] = buscar;
              
                if (!String.IsNullOrEmpty(buscar))
                {
                    queryable = queryable.Where(s => s.NombreCompleto.Contains(buscar));
                }
               
                 HttpContext.TotalPaginasLista(queryable, paginacion.CantidadAMostrar);
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                var usuarios = ExecutePolicy(() => queryable.PaginarLista(paginacion).ToList());
              
                
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
      
        public async Task<IActionResult> Create()
        {
            try
            {
                
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
                        if (User.IsInRole("administrador")&& User.Identity.IsAuthenticated) {
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            return RedirectToAction("Login", "Auth");
                        }
                       
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
        
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
               
                var user = await ExecutePolicyAsync(() => _adminrepository.ObtenerPorId(id));                          
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                }
              
                UsuarioEditViewModel viewModel = new UsuarioEditViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    NombreCompleto = user.NombreCompleto,
                    FechaNacimiento = user.FechaNacimiento,
                    Telefono = user.Telefono,
                    IdRol=user.IdRol,
                    Direccion = user.Direccion,
                    Ciudad=user.Ciudad,
                    codigoPostal=user.CodigoPostal
                };
                var roles = await ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
                ViewData["Roles"] = new SelectList(roles, "Id", "Nombre");


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
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult> Edit(UsuarioEditViewModel userVM)
        {
            try
            {
                
               
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
       
        [Authorize]
        public async Task<IActionResult> EditUserActual(int id)
        {
            try
            {
               

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
                        Direccion = user.Direccion,
                        ciudad = user.Ciudad,
                        codigoPostal= user.CodigoPostal

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
        [Authorize]
        public async Task<ActionResult> EditUserActual(EditarUsuarioActual userVM)
        {
            try
            {
                
              
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
    
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {             
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
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
               
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
        //Metodo que da de baja el usuario, este metodo se llaman desde el script alta-baja-usuario.js
        [HttpPost]
        public async Task<IActionResult> BajaUsuarioPost([FromBody] UsuarioRequest request)
        {
            var (success, errorMessage) = await ExecutePolicyAsync(() => _adminrepository.BajaUsuario(request.Id));
            if (success)
            {
                return Json(new { success = true });
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage;
                return Json(new { success = false, errorMessage = errorMessage });
            }
        }
        //Metodo que da de alta el usuario, este metodo se llaman desde el script alta-baja-usuario.js
        [HttpPost]
        public async Task<IActionResult> AltaUsuarioPost([FromBody] UsuarioRequest request)
        {
            var (success, errorMessage) = await ExecutePolicyAsync(() => _adminrepository.AltaUsuario(request.Id));
            if (success)
            {
                return Json(new { success = true });
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage;
                return Json(new { success = false, errorMessage = errorMessage });
            }
        }
        //Metodo que obtiene los roles y los usuarios que tienen ese rol
        public async Task<IActionResult> ObtenerRoles()
        {
            try
            {
                var roles = await ExecutePolicyAsync(() => _adminrepository.ObtenerRolesConUsuarios());
                return View(roles);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener los roles");
                return RedirectToAction("Error", "Home");
            }
        }
      
        public async Task<IActionResult> VerUsuariosPorRol(int id, [FromQuery] Paginacion paginacion)
        {
            try
            {
                
                var rol = (await ExecutePolicyAsync(() => _adminrepository.ObtenerRolesConUsuarios())).FirstOrDefault(r => r.Id == id);
                if (rol == null)
                {
                    TempData["Error"] = "Rol no encontrado.";
                    return RedirectToAction("ObtenerRoles");
                }

                var usuarios = rol.Usuarios; 

             
                HttpContext.TotalPaginasLista(usuarios, paginacion.CantidadAMostrar);
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);

                var usuariosPaginados = ExecutePolicy(() => usuarios.PaginarLista(paginacion).ToList());

            
                var todosLosRoles = await ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
                ViewBag.Roles = todosLosRoles;
                ViewBag.RolId = id;

               
                return View(usuariosPaginados);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, intenta de nuevo más tarde.";
                _logger.LogError(ex, "Error al obtener los usuarios del rol con Id {Id}", id);
                return RedirectToAction("Error", "Home");
            }
        }
       
        //Metodo para editar el rol se llama desde el script ver-usuario-rol.js
        [HttpPost]
        public async Task<IActionResult> CambiarRol([FromBody] CambiarRolRequestDTO request)
        {
            try
            {
              
                var usuario = await ExecutePolicyAsync(() => _adminrepository.ObtenerPorId(request.Id));
                if (usuario == null)
                {
                    return Json(new { success = false, errorMessage = "Usuario no encontrado." });
                }

               
                if (usuario.IdRolNavigation == null)
                {
                    return Json(new { success = false, errorMessage = "El rol actual del usuario no está definido." });
                }

                // Obtener todos los roles disponibles
                var roles = await ExecutePolicyAsync(() => _adminrepository.ObtenerRoles());
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
                await ExecutePolicy(() => _adminrepository.ActualizarRolUsuario(request.Id, nuevoRol.Id));
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
