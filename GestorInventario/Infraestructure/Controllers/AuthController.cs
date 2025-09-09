using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.Services;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.user;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;



namespace GestorInventario.Infraestructure.Controllers
{
    public class AuthController : Controller
    {

        private readonly HashService _hashService;
        private readonly IEmailService _emailService;
        private readonly TokenService _tokenService;
        private readonly IAuthRepository _authRepository;        
        private readonly ILogger<AuthController> _logger;       
        private readonly PolicyExecutor _policyExecutor;
        public AuthController(HashService hashService, IEmailService emailService, TokenService tokenService, IAuthRepository adminRepository,
              ILogger<AuthController> logger,   PolicyExecutor executor)
        {
            _hashService = hashService;
            _emailService = emailService;
            _tokenService = tokenService;
            _authRepository = adminRepository;         
            _logger = logger;     
            _policyExecutor = executor;
        }
        
        [AllowAnonymous]
        [HttpGet]
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
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Si ya está autenticado, redirige
                if (User.Identity.IsAuthenticated)
                    return RedirectToAction("Index", "Home");

                // Eliminar cookies existentes para mayor seguridad
                foreach (var cookie in Request.Cookies)
                {
                    Response.Cookies.Delete(cookie.Key);
                }

                // Buscar usuario
                var (user,mensaje) = await _policyExecutor.ExecutePolicyAsync(() => _authRepository.Login(model.Email));
                if (user == null)
                {
                    ModelState.AddModelError("", "El email y/o la contraseña son incorrectos.");
                    return View(model);
                }

                // Verificar confirmación de correo
                if (!user.ConfirmacionEmail)
                {
                    ModelState.AddModelError("", "Por favor, confirma tu correo electrónico antes de iniciar sesión.");
                    return View(model);
                }

                // Verificar si el usuario está dado de baja
                if (user.BajaUsuario)
                {
                    ModelState.AddModelError("", "Su usuario ha sido dado de baja, contacte con el administrador.");
                    return View(model);
                }

                // Validar contraseña
                var resultadoHash = _hashService.Hash(model.Password, user.Salt);
                if (user.Password != resultadoHash.Hash)
                {
                    ModelState.AddModelError("", "El email y/o la contraseña son incorrectos.");
                    return View(model);
                }

                // Autenticación exitosa - generar tokens
                var tokenResponse = await _tokenService.GenerarToken(user);

                Response.Cookies.Append("auth", tokenResponse.Token, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Domain = "localhost",
                    Secure = true,
                    Expires = DateTime.UtcNow.AddMinutes(10)
                });

                Response.Cookies.Append("refreshToken", tokenResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Domain = "localhost",
                    Secure = true,
                    Expires = DateTime.UtcNow.AddHours(24)
                });

               

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el login");
                TempData["ConectionError"] = "El servidor tardó mucho en responder, inténtelo de nuevo más tarde.";
                return RedirectToAction("Error", "Home");
            }
        }


      
        [AllowAnonymous]
        [HttpGet]
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
               await _authRepository.EliminarCarritoActivo();
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
       

        [Authorize(Roles = "Administrador")]
        [HttpGet("reset-password/{email}")]
        public async Task<IActionResult> ResetPassword(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
                {
                    TempData["ErrorMessage"] = "El correo electrónico no es válido.";
                    return RedirectToAction("Index", "Admin");
                }

                var (success, error, userEmail) = await _emailService.SendEmailAsyncResetPassword(new EmailDto
                {
                    ToEmail = email
                });
                if (success)
                {
                    _logger.LogInformation("Email de restablecimiento de contraseña enviado con éxito");
                    return View("ResetPassword", userEmail); 
                }

                _logger.LogError("Error al enviar el email: {error}", error);
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                TempData["ConnectionError"] = "El servidor tardó mucho en responder, inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al enviar el email de restablecimiento");
                return RedirectToAction("Error", "Home");
            }
        }

       [AllowAnonymous]
       [HttpGet("auth/restore-password/{UserId}/{Token}")]
        public async Task<IActionResult> RestorePassword(int UserId, string Token)
        {
            var (success, mensaje, modelo) = await _authRepository.PrepareRestorePassModel(UserId, Token);
            if (!success)
            {
                TempData["ErrorMessage"] = mensaje;
                return RedirectToAction("ResetPasswordOlvidada");
            }
            return View(modelo);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RestorePasswordUser(RestoresPasswordDto cambio)
        {
            if (!ModelState.IsValid)
            {
                return View("RestorePassword", cambio);
            }

            var (success,mensaje,modelo) = await _authRepository.PrepareRestorePassModel(cambio.UserId, cambio.Token);
            if (!success)
            {
                TempData["ErrorMessage"] =mensaje;
                return RedirectToAction("ResetPasswordOlvidada");
            }

            try
            {
                var (exito, errorMessage) = await _policyExecutor.ExecutePolicyAsync(() => _authRepository.SetNewPasswordAsync(cambio));
                if (exito)
                {
                    if ((User.Identity?.IsAuthenticated ?? false) && User.IsAdministrador())
                        return RedirectToAction("Index", "Admin");

                    return RedirectToAction(nameof(Login));
                }
                else
                {
                    TempData["ErrorMessage"] = errorMessage;
                    return View("RestorePassword", cambio);
                }
            }
            catch (Exception ex)
            {
                TempData["ConnectionError"] = "El servidor tardó mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al recuperar la contraseña");
                return RedirectToAction("Error", "Home");
            }
        }


        [HttpGet]
        public  IActionResult ResetPasswordOlvidada()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResetPasswordOlvidada(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
                {
                    TempData["ErrorMessage"] = "El correo electrónico no es válido.";
                    return View();
                }

                var (success, error, _) = await _emailService.SendEmailAsyncResetPassword(new EmailDto
                {
                    ToEmail = email
                });
                if (success)
                {
                    _logger.LogInformation("Correo enviado con éxito");
                    TempData["Success"] = "Se ha enviado un correo con instrucciones para restablecer tu contraseña. Por favor, revisa tu bandeja de correo o spam.";
                    return View();
                }

                _logger.LogError("Error al enviar el correo: {error}", error);
                TempData["ErrorMessage"] = error;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ConnectionError"] = "El servidor tardó mucho en responder, inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al enviar el email de restablecimiento");
                return RedirectToAction("Error", "Home");
            }
        }
       
      
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string passwordAnterior, string passwordActual)
        {

            var (succes, errorMessage) = await _policyExecutor.ExecutePolicyAsync(() => _authRepository.ChangePassword(passwordAnterior, passwordActual));
            if (succes)
            {
                if (User.Identity.IsAuthenticated && User.IsAdministrador())
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction(nameof(Login));
                }
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage;
                return View(nameof(ChangePassword));
            }

        }                
    }
}