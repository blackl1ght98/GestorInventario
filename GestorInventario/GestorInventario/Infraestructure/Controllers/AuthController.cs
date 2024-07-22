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
using GestorInventario.Application.Politicas_Resilencia;

namespace GestorInventario.Infraestructure.Controllers
{
    public class AuthController : Controller
    {
      
        private readonly HashService _hashService;
        private readonly IEmailService _emailService;
        private readonly TokenService _tokenService;
        private readonly IAuthRepository _authRepository;
        private readonly PolicyHandler _PolicyHandler;


        private readonly ILogger<AuthController> _logger;
        public AuthController( HashService hashService, IEmailService emailService, TokenService tokenService, IAuthRepository adminRepository,
              ILogger<AuthController> logger, PolicyHandler policy)
        {
           
            _hashService = hashService;
            _emailService = emailService;
            _tokenService = tokenService;
            _authRepository = adminRepository;
            _PolicyHandler= policy;
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
                    if (!User.Identity.IsAuthenticated)
                    {
                        // Obtiene las cookies del navegador.
                        var cookieCollection = Request.Cookies;

                        // Recorre todas las cookies y las elimina.
                        foreach (var cookie in cookieCollection)
                        {
                            Response.Cookies.Delete(cookie.Key);
                        }
                        
                    }
                    var user = await ExecutePolicyAsync(()=> _authRepository.Login(model.Email)) ;

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
                                SameSite = SameSiteMode.None,
                                Domain="localhost",
                                Secure = true,  // Asegúrate de que esto esté configurado correctamente
                                Expires = null  // Opcionalmente, puedes establecer una fecha de caducidad
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
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al realizar el login");
                return RedirectToAction("Error", "Home");
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
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al cerrar sesion");
                return RedirectToAction("Error", "Home");
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
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al cerrar sesion");
                return RedirectToAction("Error", "Home");
            }
        }
        //Este metodo toma el email por ruta y ademas envia un email al usuario con lo que tiene que hacer para resetear la contraseña
        [Route("AuthController/ResetPassword/{email}")]
        //Esto le muestra una vista al administrador
        public async Task<IActionResult> ResetPassword(string email)
        {
            try
            {
                var usuarioDB = await ExecutePolicyAsync(()=> _authRepository.ExisteEmail(email)) ;
                // Generar una contraseña temporal
                await _emailService.SendEmailAsyncResetPassword(new DTOEmail
                {
                    ToEmail = email
                });
                return View(usuarioDB);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener y enviar el email");
                return RedirectToAction("Error", "Home");
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
                var usuarioDB = await ExecutePolicyAsync(()=> _authRepository.ObtenerPorId(cambio.UserId));       
               var (success, errorMessage)= await ExecutePolicyAsync(()=>_authRepository.RestorePass(cambio)) ;
                if (success)
                {
                    TempData["SuccessMessage"] = "Exito";

                }
                else 
                {
                    TempData["ErrorMessage"] = errorMessage;

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
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar el formulario de restauracion de contraseña");
                return RedirectToAction("Error", "Home");
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
                
                var (success,errorMessage) = await ExecutePolicyAsync(()=> _authRepository.ActualizarPass(cambio)) ;
                if (success)
                {
                    TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";

                }
                else
                {
                    TempData["ErrorMessage"] = errorMessage;
                    return View("RestorePassword", cambio);
                }
             

                return RedirectToAction("Index", "Admin");

            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al recuperar la contraseña");
                return RedirectToAction("Error", "Home");
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
