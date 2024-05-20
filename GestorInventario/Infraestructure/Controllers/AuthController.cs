using GestorInventario.Application.Services;
using GestorInventario.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GestorInventario.Domain.Models;
using GestorInventario.Application.DTOs;
using GestorInventario.Interfaces.Application;
using GestorInventario.MetodosExtension;
using GestorInventario.Interfaces.Infraestructure;

namespace GestorInventario.Infraestructure.Controllers
{
    public class AuthController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly HashService _hashService;
        private readonly IEmailService _emailService;
        private readonly TokenService _tokenService;
        private readonly IAdminRepository _adminRepository;
        private readonly IAdminCrudOperation _adminCrudOperation;
        private readonly ILogger<AuthController> _logger;
        public AuthController(GestorInventarioContext context, HashService hashService, IEmailService emailService, TokenService tokenService, IAdminRepository adminRepository, IAdminCrudOperation adminCrudOperation,
              ILogger<AuthController> logger)
        {
            _context = context;
            _hashService = hashService;
            _emailService = emailService;
            _tokenService = tokenService;
            _adminRepository = adminRepository;
            _adminCrudOperation = adminCrudOperation;
            _logger = logger;
        }  
        [AllowAnonymous]
        public IActionResult Login()
        {
           
                if (User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Index", "Home");
                }
                return View();
        }

      
        [AllowAnonymous]
        [HttpPost]
       
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    var user = await _adminRepository.Login(model.Email);

                    if (user != null)
                    {
                        // Comprobar si el correo electrónico ha sido confirmado
                        if (!user.ConfirmacionEmail)
                        {
                            ModelState.AddModelError("", "Por favor, confirma tu correo electrónico antes de iniciar sesión.");
                            return View(model);
                        }

                        //Se llama al servicio hash service
                        var resultadoHash = _hashService.Hash(model.Password, user.Salt);
                        //Si la contraseña que se introduce es igual a la que hay en base de datos se procede al login
                        if (user.Password == resultadoHash.Hash)
                        {
                            // Generar el token
                            var tokenResponse = await _tokenService.GenerarToken(user);

                            // Guardar el token en una cookie
                            Response.Cookies.Append("auth", tokenResponse.Token, new CookieOptions
                            {
                                HttpOnly = true,
                                SameSite = SameSiteMode.Strict,
                               
                                Secure = true,
                                Expires=null
                            });

                            // Crear una identidad de usuario y firmar al usuario
                            //Los claims se pueden definir como las caracteristicas de ese usuario  o como se identifica en el sistema
                            //y cuando se identifique como se va a llamar, que email tiene etc
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Email, user.Email),
                                new Claim(ClaimTypes.Role, user.IdRolNavigation.Nombre),
                                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                             };
                            //Representa la identidad del usuario logueado
                            /*El segundo argumento del constructor ClaimsIdentity es el esquema de autenticación que se utilizó 
                             * para autenticar al usuario. En este caso, estás utilizando 
                             * CookieAuthenticationDefaults.AuthenticationScheme, lo que indica que el usuario fue autenticado 
                             * usando cookies.
                             */
                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            //Marca al usuario como autenticado en base a la informacion de los claims
                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            ModelState.AddModelError("", "El email y/o la contraseña son incorrectos.");
                            return View(model);
                        }
                    }

                    return View(model);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el login");
                return BadRequest("Error al realizar el login intentelo de nuevo mas tarde o contacte con el administrador");
            }
        }
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Obtiene las cookies del navegador.
                var cookieCollection = Request.Cookies;

                // Recorre todas las cookies y las elimina.
                foreach (var cookie in cookieCollection)
                {
                    Response.Cookies.Delete(cookie.Key);
                }

                // Cierra la sesión.
                await HttpContext.SignOutAsync();

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar sesion");
                return BadRequest("Error al cerrar sesion intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LogoutScript()
        {
            try
            {
                // Obtiene las cookies del navegador.
                var cookieCollection = Request.Cookies;

                // Recorre todas las cookies y las elimina.
                foreach (var cookie in cookieCollection)
                {
                    Response.Cookies.Delete(cookie.Key);
                }

                // Cierra la sesión.
                await HttpContext.SignOutAsync();

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar sesion");
                return BadRequest("Error al cerrar sesion intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
        }


        //[AllowAnonymous]
        //public async Task<IActionResult> Logout()
        //{
        //    //Elimina de las cookies del navegador las cookie del usuario
        //    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        //    return RedirectToAction("Index", "Home");
        //}
        //Este metodo toma el email por ruta y ademas envia un email al usuario con lo que tiene que hacer para resetear la contraseña
        [Route("AuthController/ResetPassword/{email}")]
        //Esto le muestra una vista al administrador
        public async Task<IActionResult> ResetPassword(string email)
        {
            try
            {
                var usuarioDB = await _adminRepository.ExisteEmail(email);
                //var usuarioDB= await _context.Usuarios.EmailExists(email);
                //var usuarioDB = await _context.Usuarios.AsTracking().FirstOrDefaultAsync(x => x.Email == email);
                // Generar una contraseña temporal
                await _emailService.SendEmailAsyncResetPassword(new DTOEmail
                {
                    ToEmail = email
                });
                return View(usuarioDB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener y enviar el email");
                return BadRequest("Error al mostrar la vista de restauracion de contraseña intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
           
        }
        //De ese email que se ha enviado del enlace tomamos el id de usuario y el token que no es token lo que genera es un
        //identificador unico, este identificador se genero cuando el usuario se registro, con esto nos asguramos de que la contraseña
        //se cambia para el usuario correcto
        [AllowAnonymous]
        [Route("AuthController/RestorePassword/{UserId}/{Token}")]
        public async Task<IActionResult> RestorePassword(DTORestorePass cambio)
        {
            try
            {
                //Se busca al usuario por Id
                var usuarioDB = await _adminRepository.ObtenerPorId(cambio.UserId);
                //var usuarioDB= await _context.Usuarios.ExistUserId(cambio.UserId);
                //var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == cambio.UserId);
                if (usuarioDB.Email == null)
                {
                    return BadRequest("Email no encontrado");
                }
                //Si el enlace que se genero en el registro se altero ese enlace no sera valido
                if (usuarioDB.EnlaceCambioPass != cambio.Token)
                {
                    return BadRequest("Token no valido");
                }
                if (usuarioDB.FechaEnlaceCambioPass < DateTime.Now && usuarioDB.FechaExpiracionContrasenaTemporal<DateTime.Now)
                {
                    TempData["error"] = "El enlace y contraseña temporal ha expirado, solicite otro";
                    usuarioDB.FechaEnlaceCambioPass = null;
                    usuarioDB.FechaExpiracionContrasenaTemporal = null;
                    usuarioDB.TemporaryPassword = null;
                    await _context.SaveChangesAsync();
                }
                // Crear un nuevo objeto DTORestorePass y establecer las propiedades apropiadas
                var restorePass = new DTORestorePass
                {
                    UserId = usuarioDB.Id,
                    Token = usuarioDB.EnlaceCambioPass,
                    // Puedes establecer otras propiedades aquí si es necesario
                };

                // Pasar el objeto DTORestorePass a la vista
                return View(restorePass);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar el formulario de restauracion de contraseña");
                return BadRequest("Error al mostrar el formulario intentelo de nuevo mas tarde si el problema persiste contacte con el administrador");
            }
           
        }
        //En la vista para restaurar la contraseña llamamos a este metodo para que la contraseña sea restaurada
        //esto es la logica que hay detras del formulario para restaurar la contraseña
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RestorePasswordUser(DTORestorePass cambio)
        {
            try
            {
                var usuarioDB = await _adminRepository.ObtenerPorId(cambio.UserId);

                // var usuarioDB = await _context.Usuarios.ExistUserId(cambio.UserId);

                //Busca al usuario por Id
                //var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == cambio.UserId);
                //Si la id es nula devuelve el siguiente error
                if (usuarioDB == null)
                {
                    return BadRequest("Usuario no encontrado");
                }
                // Comprobar si la contraseña es nula 
                if (string.IsNullOrEmpty(cambio.Password))
                {
                    return BadRequest("La contraseña no puede estar vacía");
                }
               
                //Se usa el servicio hashService para cifrar la contraseña
                var resultadoHashTemp = _hashService.Hash(cambio.TemporaryPassword);
                //Se genera un hash para esa contraseña
                usuarioDB.TemporaryPassword = resultadoHashTemp.Hash;
                //Se genera un salt para esa contraseña
                usuarioDB.Salt = resultadoHashTemp.Salt;
                //Si la contraseña temporal que hay en base de datos es distinta a la que se ha proporcionado por correo o el salt es distinto
                //al que hay en base de datos da error 

                if (usuarioDB.TemporaryPassword != resultadoHashTemp.Hash || usuarioDB.Salt != resultadoHashTemp.Salt)
                {
                    return BadRequest("La contraseña temporal no es válida");
                }
                //si todo ha ido bien se actualiza con la contraseña temporal 
                _adminCrudOperation.UpdateOperation(usuarioDB);
                //_context.Usuarios.Update(usuarioDB);
                //Se guarda en base de datos
                //await _context.SaveChangesAsync();
                //Se usa el servicio hash service para hashear la contraseña proporcionada por el usuario
                var resultadoHash = _hashService.Hash(cambio.Password);
                //Se asigna un hash a la contraseña que proporciono el usuario
                usuarioDB.Password = resultadoHash.Hash;
                //Se asigna un salt a la contraseña que proporciono el usuario

                usuarioDB.Salt = resultadoHash.Salt;

                // Guardar los cambios en la base de datos
                _adminCrudOperation.UpdateOperation(usuarioDB);

                //_context.Usuarios.Update(usuarioDB);
                //await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Admin");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar la contraseña");
                return BadRequest("Error al restaurar la contraseña intentelo de nuevo mas tarde o contacte con el administrador si el problema persiste");
            }
            
        }

    }
}
