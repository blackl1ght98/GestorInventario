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
      //Al usar la modularización se queda todo mas separado y el codigo se queda mas limpio
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
        /* Explicación de cómo se crea la paginación:
          * 1º Pasamos los datos que se van a paginar: 
          *    var queryable = await ExecutePolicyAsync(() => _adminrepository.ObtenerUsuarios());
          *    En esta variable se almacena la fuente de información, que es una consulta que aún no se ha ejecutado completamente.
          * 
          * 2º Calculamos el número total de páginas usando el método de extensión que hemos creado:
          *    HttpContext.TotalPaginas(queryable, paginacion.CantidadAMostrar); 
          *    Este método necesita dos parámetros: la fuente de información (`queryable`) y la cantidad de registros a mostrar por página (`paginacion.CantidadAMostrar`). 
          *    Calcula el total de páginas usando `Math.Ceiling` para redondear hacia arriba, y almacena el resultado en la cabecera de la respuesta HTTP bajo el nombre "totalPaginas".
          *    Para recuperar este valor, usamos: 
          *    var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString(); 
          * 
          * 3º Aplicamos la paginación llamando al método de extensión creado:
          *    var usuarios = ExecutePolicy(() => queryable.PaginarLista(paginacion).ToList());
          *    Este método se encarga de omitir (`Skip`) y tomar (`Take`) los registros necesarios para la página actual. Se nutre de la consulta inicial `queryable`, 
          *    que devuelve un `IQueryable<T>`, permitiendo que la paginación se ejecute eficientemente en la base de datos.
          * 
          * 4º Generamos la lista de páginas para la navegación llamando a otro método de extensión:
          *    ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina); 
          *    Este método toma dos parámetros: el número total de páginas y la página actual, y genera la lista de páginas que se mostrará en la interfaz de usuario.
          * 
          * 5º Finalmente, devolvemos los datos paginados:
          *    var usuarios = ExecutePolicy(() => queryable.PaginarLista(paginacion).ToList());
          *    Este es el resultado final que se pasa a la vista, mostrando los registros correspondientes a la página seleccionada.
          */
        [Authorize(Roles ="administrador")]
        /// <summary>
        /// Define un contrato que representa el resultado de un método de acción en un controlador.
        /// </summary>
        /// <remarks>
        /// - <b>IActionResult</b> es una interfaz que define el contrato para los resultados de las acciones, 
        /// permitiendo una mayor flexibilidad para devolver diferentes tipos de respuestas (redirecciones, vistas, errores, etc.).
        /// - <b>ActionResult</b> es una clase concreta que implementa <b>IActionResult</b>, 
        /// y se usa comúnmente para devolver resultados genéricos como vistas o redirecciones.
        /// Ambos pueden usarse de manera intercambiable, pero <b>IActionResult</b> ofrece más flexibilidad.
        /// </remarks>

        public async Task<ActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {

               
                var queryable =  await ExecutePolicyAsync(() => _adminrepository.ObtenerUsuarios());
               
                //var queryable = _adminrepository.ObtenerUsuarios();
                 ViewData["Buscar"] = buscar;
              
                if (!String.IsNullOrEmpty(buscar))
                {
                    queryable = queryable.Where(s => s.NombreCompleto.Contains(buscar));
                }
               
                 HttpContext.TotalPaginasLista(queryable, paginacion.CantidadAMostrar);
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                var usuarios = ExecutePolicy(() => queryable.PaginarLista(paginacion).ToList());
                //Obtiene los datos de la cabecera que hace esta peticion
                
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
      //Metodo que actualiza el rol del usuario
        [HttpPost]
        [Authorize(Roles = "administrador")]
        /// <summary>
        /// Define un contrato que representa el resultado de un método de acción en un controlador.
        /// </summary>
        /// <remarks>
        /// IActionResult permite que un método de acción devuelva diferentes tipos de respuestas,
        /// como redirecciones, vistas, errores o contenido directo, dependiendo de la lógica de la acción.
        /// </remarks>

        public async Task<IActionResult> UpdateRole(int id, int newRole)
        {
            try
            {
                
                //Uso de la separacion de tuplas
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
                //Obtener los datos del desplegable
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
        //Metodo que crea el usuario
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
        //Metodo que se encarga de confirmar la cuenta del usuario
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
        //Metodo para obtener los datos del usuario a editar
        [Authorize(Roles = "administrador")]
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
                    Direccion = user.Direccion
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
        //Metodo encargado de editar el usuario
        [HttpPost]
        [Authorize(Roles = "administrador")]
        public async Task<ActionResult> Edit(UsuarioEditViewModel userVM)
        {
            try
            {
                
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
        //Metodo para obtener los datos a editar del usuario actual
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
      //Metodo que edita el usuario actual
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> EditUserActual(EditarUsuarioActual userVM)
        {
            try
            {
                
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
        //Metodo que obtiene los datos del usuario a editar
        [Authorize(Roles = "administrador")]
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
        //Metodo que elimina el usuario
        [HttpPost, ActionName("DeleteConfirmed")]
        [Authorize(Roles = "administrador")]
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
       //Metodo que da de baja el usuario
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
        //Metodo que da de alta el usuario
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
        public class UsuarioRequest
        {
            public int Id { get; set; }
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
