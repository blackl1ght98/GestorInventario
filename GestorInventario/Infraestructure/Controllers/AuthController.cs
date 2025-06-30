using GestorInventario.Application.Services;
using GestorInventario.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GestorInventario.Application.DTOs;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Application.Politicas_Resilencia;
using Microsoft.Extensions.Caching.Memory;


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
        private readonly IMemoryCache _memoryCache;

        public AuthController(HashService hashService, IEmailService emailService, TokenService tokenService, IAuthRepository adminRepository,
              ILogger<AuthController> logger, PolicyHandler policy, IMemoryCache memory)
        {
            _hashService = hashService;
            _emailService = emailService;
            _tokenService = tokenService;
            _authRepository = adminRepository;
            _PolicyHandler = policy;
            _logger = logger;
            _memoryCache = memory;
        }
        //Metodo para mostrar la vista de login
        [AllowAnonymous]
        public IActionResult Login()
        {

            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        //Metodo para realizar el login
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
                        // Eliminar cookies existentes para mayor seguridad
                        var cookieCollection = Request.Cookies;
                        foreach (var cookie in cookieCollection)
                        {
                            Response.Cookies.Delete(cookie.Key);
                        }
                    }

                    var user = await ExecutePolicyAsync(() => _authRepository.Login(model.Email));
                    if (user != null)
                    {
                        if (!user.ConfirmacionEmail)
                        {
                            ModelState.AddModelError("", "Por favor, confirma tu correo electrónico antes de iniciar sesión.");
                            return View(model);
                        }
                        if (user.BajaUsuario)
                        {
                            ModelState.AddModelError("", "Su usuario ha sido dado de baja, contacte con el administrador.");
                            return View(model);
                        }

                        var resultadoHash = _hashService.Hash(model.Password, user.Salt);
                        if (user.Password == resultadoHash.Hash)
                        {


                            // Generar y devolver el auth token
                            var tokenResponse = await _tokenService.GenerarToken(user);

                            Response.Cookies.Append("auth", tokenResponse.Token, new CookieOptions
                            {
                                HttpOnly = true,
                                SameSite = SameSiteMode.Lax,
                                Domain = "localhost",
                                Secure = true,
                                Expires = DateTime.UtcNow.AddMinutes(10)
                            });
                            // Guardar el token de refresco en una cookie
                            Response.Cookies.Append("refreshToken", tokenResponse.RefreshToken, new CookieOptions
                            {
                                HttpOnly = true,
                                SameSite = SameSiteMode.Lax,
                                Domain = "localhost",
                                Secure = true,
                                Expires = DateTime.UtcNow.AddDays(7) 
                            });
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Email, user.Email),
                                new Claim(ClaimTypes.Role, user.IdRolNavigation.Nombre),
                                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                             };

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
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
                TempData["ConectionError"] = "El servidor tardó mucho en responder, inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al realizar el login");
                return RedirectToAction("Error", "Home");
            }
        }


        //Metodo que cierra sesion
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
        //Metodo para hacer el logout desde el script site.js
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LogoutScript()
        {
            try
            {
               
                var cookieCollection = Request.Cookies;
                Console.WriteLine("El metodo se ejecuta");
               
                foreach (var cookie in cookieCollection)
                {
                    Response.Cookies.Delete(cookie.Key);
                }
               
              
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

        //CUANDO ES ADMIN ENTRA AQUI
        [Route("AuthController/ResetPassword/{email}")]
        public async Task<IActionResult> ResetPassword(string email)
        {
            try
            {
                var usuarioDB = await ExecutePolicyAsync(() => _authRepository.ExisteEmail(email));
                // Generar una contraseña temporal
                var (succes,error)= await _emailService.SendEmailAsyncResetPassword(new DTOEmail
                {
                    ToEmail = email
                });
                if (succes)
                {
                    _logger.LogInformation("Email de restablecion de contraseña enviado con exito");
                    return View(usuarioDB);
                }
                else
                {
                    _logger.LogError("Error al enviar el email");
                    return RedirectToAction("Error", "Home");
                }
                
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener y enviar el email");
                return RedirectToAction("Error", "Home");
            }

        }
        
        [AllowAnonymous]
        [Route("AuthController/RestorePassword/{UserId}/{Token}")]
        public async Task<IActionResult> RestorePassword(DTORestorePass cambio)
        {
            try
            {
                
                var usuarioDB = await ExecutePolicyAsync(() => _authRepository.ObtenerPorId(cambio.UserId));
                var (success, errorMessage) = await ExecutePolicyAsync(() => _authRepository.RestorePass(cambio));
                if (success)
                {
                    var restorePass = new DTORestorePass
                    {
                        UserId = usuarioDB.Id,
                        Token = usuarioDB.EnlaceCambioPass,

                    };
                    return View(restorePass);
                }
                else
                {
                    TempData["ErrorMessage"] = errorMessage;
                    return RedirectToAction(nameof(RestorePassword), new { UserId = usuarioDB.Id, Token = usuarioDB.EnlaceCambioPass });

                }
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar el formulario de restauracion de contraseña");
                return RedirectToAction("Error", "Home");
            }

        }
       
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RestorePasswordUser(DTORestorePass cambio)
        {
            try
            {

                var (success, errorMessage) = await ExecutePolicyAsync(() => _authRepository.ActualizarPass(cambio));
                if (success)
                {

                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    TempData["ErrorMessage"] = errorMessage;
                    return View("RestorePassword", cambio);
                }
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al recuperar la contraseña");
                return RedirectToAction("Error", "Home");
            }
        }
      
       
       //CUANDO ES USUARIO NORMAL ENTRA AQUI
        public async Task<IActionResult> ResetPasswordOlvidada()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPasswordOlvidada(string email)
        {
            try
            {
                var usuarioDB = await ExecutePolicyAsync(() => _authRepository.ExisteEmail(email));
               
                var (success, error) = await _emailService.SendEmailAsyncResetPassword(new DTOEmail
                {
                    ToEmail = email
                });
                if (success)
                {

                    _logger.LogInformation("correo enviado con exito");
                }
                else {
                    _logger.LogError("Ocurrio un error al enviar el correo");
                    return RedirectToAction("Error", "Home");
                }
                TempData["Succes"] = "Email eviado con exito por favor mire en su bandeja de correo o spam";
                return View(usuarioDB);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener y enviar el email");
                return RedirectToAction("Error", "Home");
            }

        }

        public async Task<IActionResult> ChangePassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string passwordAnterior, string passwordActual)
        {

            var (succes, errorMessage) = await ExecutePolicyAsync(() => _authRepository.ChangePassword(passwordAnterior, passwordActual));
            if (succes)
            {
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage;
                return View(nameof(ChangePassword));
            }

        }
        /*
             Func<Task<T>>:   encapsula un método que no tiene parámetros y devuelve un valor del tipo especificado por Task<T>
         */
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