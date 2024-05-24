using GestorInventario.Application.DTOs;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GestorInventario.PaginacionLogica;
using GestorInventario.MetodosExtension;
using GestorInventario.Interfaces.Infraestructure;

namespace GestorInventario.Infraestructure.Controllers
{
    public class AdminController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly IEmailService _emailService;
        private readonly HashService _hashService;
        private readonly IConfirmEmailService _confirmEmailService;
        private readonly ILogger<AdminController> _logger;
        private readonly IAdminRepository _adminrepository;
        private readonly IAdminCrudOperation _admincrudoperation;
        private readonly GenerarPaginas _generarPaginas;
        private readonly PolicyCircuitBreaker _policyCircuitBraker;
        public AdminController( IEmailService emailService, HashService hashService, IConfirmEmailService confirmEmailService, 
            ILogger<AdminController> logger, IAdminRepository adminRepository, IAdminCrudOperation admincrudoperation, GenerarPaginas generarPaginas, 
            GestorInventarioContext context, PolicyCircuitBreaker policy)
        {
           
            _emailService = emailService;
            _hashService = hashService;
            _confirmEmailService = confirmEmailService;
            _logger = logger;
            _adminrepository = adminRepository;
            _admincrudoperation = admincrudoperation;
            _generarPaginas = generarPaginas;
            _context = context;
            _policyCircuitBraker = policy;
        }
        public async Task<ActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
               
                var policy = _policyCircuitBraker.GetCircuitBreakerPolicy();
                var queryable = policy.Execute(() => _adminrepository.ObtenerUsuarios());
                //var queryable = _adminrepository.ObtenerUsuarios();        
                if (!String.IsNullOrEmpty(buscar))
                {
                    queryable = queryable.Where(s => s.NombreCompleto.Contains(buscar));
                }
                //Accedemos al metodo de extension creado pasandole la fuente de informacion(queryable) y las paginas a mostrar
                await HttpContext.InsertarParametrosPaginacionRespuesta(queryable, paginacion.CantidadAMostrar);
                var usuarios = policy.Execute(() => queryable.Paginar(paginacion).ToList());
                //Obtiene los datos de la cabecera que hace esta peticion
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                //Crea las paginas que el usuario ve.
                ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);
                ViewData["Roles"] = new SelectList(_adminrepository.ObtenerRoles(), "Id", "Nombre");
                return View(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los datos del usuario");
                return BadRequest("En estos momentos no se ha podido llevar a cabo la obtención de los usuarios. Inténtelo de nuevo más tarde o contacte con el administrador.");
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateRole(int id, int newRole)
        {
            try
            {
                var policyAsync = _policyCircuitBraker.GetCircuitBreakerPolicyAsync();
                var policy = _policyCircuitBraker.GetCircuitBreakerPolicy();
                var user = await policyAsync.ExecuteAsync(() => _adminrepository.ObtenerPorId(id));
                //var user = await _adminrepository.ObtenerPorId(id);
                if (user == null)
                {
                    return NotFound("El usuario al que intenta cambiar el rol no existe");
                }
                //crea el desplegable
                ViewData["Roles"] = new SelectList(policy.Execute(()=> _adminrepository.ObtenerRoles()), "Id", "Nombre");
                //Le asigna el rol al usuario
                user.IdRol = newRole;
                //Actualiza en base de datos 
                _admincrudoperation.UpdateOperation(user);
                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al actualizar el rol");
                return BadRequest("En estos momentos no se ha podido llevar a cabo la actualizacion del rol intentelo de nuevo mas tarde o cantacte con el administrador");
            }
           
        }
        public IActionResult Create()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Index", "Home");
                }
                var policy = _policyCircuitBraker.GetCircuitBreakerPolicy();
                //Sirve para obtener los datos del desplegable
                ViewData["Roles"] = new SelectList(policy.Execute(()=> _adminrepository.ObtenerRoles()), "Id", "Nombre");
                return View();

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al visualizar la vista de creacion de usuario");
                return BadRequest("En estos momentos no se ha podido llevar a cabo la visualizacion de la vista de creacion de usuario intentelo de nuevo mas tarde o cantacte con el administrador");
            }
          
        }


        [HttpPost]
        //Sirve para que no se altere la informacion del formulario
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            try
            {
                //Esto toma en cuenta las validaciones puestas en UserViewModel
                if (ModelState.IsValid)
                {
                    var policyAsync = _policyCircuitBraker.GetCircuitBreakerPolicyAsync();
                    var policy = _policyCircuitBraker.GetCircuitBreakerPolicy();
                    var existingUser = await policyAsync.ExecuteAsync(() => _adminrepository.ExisteEmail(model.Email));
                    //var existingUser=await _adminrepository.ExisteEmail(model.Email);
                    if (existingUser != null)
                    {
                        // Si el usuario ya existe, retornar a la vista con un mensaje de error
                        ModelState.AddModelError("Email", "Este email ya está registrado.");
                        return View(model);
                    }
                    //Hashea la contraseña
                    var resultadoHash = _hashService.Hash(model.Password);
                    var user = new Usuario()
                    {
                        Email = model.Email,
                        Password = resultadoHash.Hash,
                        Salt = resultadoHash.Salt,
                        IdRol = model.IdRol,
                        NombreCompleto = model.NombreCompleto,
                        FechaNacimiento = model.FechaNacimiento,
                        Telefono = model.Telefono,
                        Direccion = model.Direccion,
                        FechaRegistro = DateTime.Now
                    };
                    //Sirve para crear el desplegable
                    ViewData["Roles"] = new SelectList(policy.Execute(()=> _adminrepository.ObtenerRoles()), "Id", "Nombre");
                    //Agrega el usuario a base de datos
                    _admincrudoperation.AddOperation(user);
                    await _emailService.SendEmailAsyncRegister(new DTOEmail
                    {
                        ToEmail = model.Email
                    });
                    return RedirectToAction("Login", "Auth");
                }
                return View(model);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al realizar el registro del usuario");
                return BadRequest("En estos momentos no se ha podido llevar a cabo el registro del usuario intentelo de nuevo mas tarde o cantacte con el administrador");
            }
            
        }
        [AllowAnonymous]
        [Route("AdminController/ConfirmRegistration/{UserId}/{Token}")]
        public async Task<IActionResult> ConfirmRegistration(DTOConfirmRegistration confirmar)
        {
            try
            {
                var policyAsync=_policyCircuitBraker.GetCircuitBreakerPolicyAsync();
                var usuarioDB = await policyAsync.ExecuteAsync(() => _adminrepository.ObtenerPorId(confirmar.UserId));
               //var usuarioDB= await _adminrepository.ObtenerPorId(confirmar.UserId);
                if (usuarioDB.ConfirmacionEmail != false)
                {
                    return BadRequest("Usuario ya validado con anterioridad");
                }

                if (usuarioDB.EnlaceCambioPass != confirmar.Token)
                {
                    _logger.LogCritical("Intento de manipulacion del token por el usuario: " + usuarioDB.Id);
                    return BadRequest("Token no valido");
                }
                await _confirmEmailService.ConfirmEmail(new DTOConfirmRegistration
                {
                    UserId = confirmar.UserId
                });
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al confirmar la cuenta del usuario");
                return BadRequest("En estos momentos no se ha podido llevar a cabo la confirmacion de la cuenta intentelo de nuevo mas tarde o cantacte con el administrador");
            }
            
        }
        
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var policyAsync = _policyCircuitBraker.GetCircuitBreakerPolicyAsync();
                var user = await policyAsync.ExecuteAsync(() => _adminrepository.ObtenerPorId(id));
                // Obtienes el usuario de la base de datos
                if (user == null)
                {
                    return BadRequest("Usuario no encontrado");
                }
                // Creas un nuevo ViewModel y llenas sus propiedades con los datos del usuario
                UsuarioEditViewModel viewModel = new UsuarioEditViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    NombreCompleto = user.NombreCompleto,
                    FechaNacimiento = user.FechaNacimiento,
                    Telefono = user.Telefono,
                    Direccion = user.Direccion
                };
                // Pasas el ViewModel a la vista
                return View(viewModel);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al visualizar la vista de edicion de usuario");
                return BadRequest("En estos momentos no se ha podido llevar a cabo la visualizacion de la vista de edicion de usuario intentelo de nuevo mas tarde o cantacte con el administrador");
            }
           
        }
        //Cuando quieres editar algo de tu modelo de base de datos pero no quieres que se puedan editar determinados campos
        //lo que se realiza es una vista del modelo para especificar que campos se quieren cambiar (UsuarioEditViewModel)
        [HttpPost]
        public async Task<ActionResult> Edit(UsuarioEditViewModel userVM)
        {
            try
            {
                //Si el modelo es valido:
                if (ModelState.IsValid)
                {
                    var policyAsync = _policyCircuitBraker.GetCircuitBreakerPolicyAsync();
                    // Obtiene la id del usuario a editar
                    var user = await policyAsync.ExecuteAsync(() => _adminrepository.ObtenerPorId(userVM.Id));
                    //var user=  await _adminrepository.ObtenerPorId(userVM.Id);
                    if (user == null)
                    {
                        return NotFound("Usuario no encontrado");
                    }
                    // Mapea los datos del ViewModel a la entidad
                    user.NombreCompleto = userVM.NombreCompleto;
                    user.FechaNacimiento = userVM.FechaNacimiento;
                    user.Telefono = userVM.Telefono;
                    user.Direccion = userVM.Direccion;
                    if (user != null && user.Email != userVM.Email)
                    {
                        user.ConfirmacionEmail = false;
                        user.Email = userVM.Email;
                        _admincrudoperation.UpdateOperation(user);
                        await _emailService.SendEmailAsyncRegister(new DTOEmail
                        {
                            ToEmail = userVM.Email
                        });
                    }
                    else
                    {
                        //Si el usuario no cambia el email se queda igual
                        user.Email = userVM.Email;
                    }
                    try
                    {
                        //Marca la entidad Usuarios como modificada
                        /*
                         * El método Entry en el contexto de Entity Framework Core se utiliza para obtener un objeto 
                         * que puede usarse para configurar y realizar acciones en una entidad que está siendo rastreada por el contexto.*/
                    
                        _admincrudoperation.ModifyEntityState(user, EntityState.Modified);
                        await _admincrudoperation.SaveChangesAsync();

                        //await _context.SaveChangesAsync();
                    }
                   
                    catch (DbUpdateConcurrencyException)
                    {
                        if (user.Id==null)
                        {
                            return NotFound("Usuario no encontrado");
                        }
                        else
                        {
                            // Recarga los datos del usuario desde la base de datos
                            _admincrudoperation.ReloadEntity(user);
                            // Intenta guardar de nuevo
                            _admincrudoperation.ModifyEntityState(user, EntityState.Modified);
                           await _admincrudoperation.SaveChangesAsync();
                        }
                    }
                    return RedirectToAction("Index");
                }
                return View(userVM);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al editar la informacion del usuario");
                return BadRequest("En estos momentos no se ha podido llevar a cabo la edicion de los datos del usuario intentelo de nuevo mas tarde o cantacte con el administrador");
            }  
        }
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var policy = _policyCircuitBraker.GetCircuitBreakerPolicyAsync();
                //Consulta a base de datos en base a la id del usuario
                var user = await policy.ExecuteAsync(() => _adminrepository.UsuarioConPedido(id));
                //var user=  await _adminrepository.UsuarioConPedido(id);
                //Si no hay cervezas muestra el error 404
                if (user == null)
                {
                    return NotFound("Usuario no encontrado");
                }
                //Llegados ha este punto hay cervezas por lo tanto se muestran las cervezas
                return View(user);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al visualizar la vista de eliminacion del usuario");
                return BadRequest("En estos momentos no se ha podido llevar a cabo la visualizacion de la vista de  eliminacion de los datos del usuario intentelo de nuevo mas tarde o cantacte con el administrador");
            }
           
        }


        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        //Para que detecte la id del usuario es necesario poner el mismo nombre que se ponga en la vista en la
        //parte del asp-for del formulario tenemos Id por lo tanto aqui tambien hay que ponerlo
        //El asp-for si tu ahi le pasar Id lo que va a ir a buscar es algo que sea Id si se pone en minuscula no lo encuentra
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
                var policy = _policyCircuitBraker.GetCircuitBreakerPolicyAsync();
                var user = await policy.ExecuteAsync(() => _adminrepository.UsuarioConPedido(Id));
                if (user == null)
                {
                    return BadRequest();
                }
                if (user.Pedidos.Any())
                {
                    TempData["ErrorMessage"] = "El usuario no se puede eliminar porque tiene pedidos asociados.";
                    return RedirectToAction(nameof(Delete), new { id = Id });
                }
                if (user.HistorialPedidos.Any())
                {
                    TempData["ErrorMessage"] = "El usuario no se puede eliminar porque tiene historial de pedidos asociados.";
                    return RedirectToAction(nameof(Delete), new { id = Id });
                }
                //Elimina el usuario y guarda los cambios
                _admincrudoperation.DeleteOperation(user);
                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al eliminar un  usuario");
                return BadRequest("En estos momentos no se ha podido llevar a cabo la eliminacion de los datos del usuario intentelo de nuevo mas tarde o cantacte con el administrador");
            }
           
        }
    }
}
