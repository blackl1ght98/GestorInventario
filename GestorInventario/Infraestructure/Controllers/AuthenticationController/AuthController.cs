using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.DTOS.User;
using GestorInventario.Application.Services.Authentication.Strategies.Login;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.Usuarios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;



namespace GestorInventario.Infraestructure.Controllers.AuthenticationController
{
    public class AuthController : Controller
    {

           
        private readonly ITokenService _tokenService;          
        private readonly ILogger<AuthController> _logger;       
        private readonly IPolicyExecutor _policyExecutor;
        private readonly ICarritoService _carritoService;
        private readonly ICurrentUserAccessor _current;
        private readonly IAuthService _authService;
        private readonly ICacheService _cache;   
        private readonly ILoginGenerator _loginGenerator;
        public AuthController(
         ITokenService tokenService,  
         ICurrentUserAccessor currentUser,
         ILogger<AuthController> logger,   
         IPolicyExecutor policyExecutor,  
         ICarritoService carritoService, 
         IAuthService authService,
         ICacheService cache,
    
         ILoginGenerator factory
        )
        {
           
            _tokenService = tokenService;                
            _logger = logger;     
            _policyExecutor = policyExecutor;
            _carritoService = carritoService;
            _current= currentUser;
            _authService = authService;
            _cache = cache;
         
            _loginGenerator = factory;
        }
        
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {

            if (_current.IsAuthenticated())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

     
       
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                foreach (var cookie in Request.Cookies) { Response.Cookies.Delete(cookie.Key); }
            
                var dto= new LoginDto { Email = model.Email, Password=model.Password };
                // 1. Obtenemos la estrategia según la configuración
                var result = await _loginGenerator.AuthenticateAsync(dto);

                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(model);
                }

                // 2. Si la estrategia indica es la que  requiere MFA....
                if (result.Data.RequiresMfa)
                {
                    Response.Cookies.Append("mfa_pending", result.Data.User.Id.ToString(), new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        Expires = DateTime.UtcNow.AddMinutes(5)
                    });
                    TempData["LoginModel"] = JsonConvert.SerializeObject(model);
                    return RedirectToAction("VerifyMfa", "Auth");
                }

                // 3. Si NO requiere MFA, generamos tokens directamente
                var tokenResponse = await _tokenService.GenerarToken(result.Data.User);
                Response.Cookies.Append("auth", tokenResponse.Token, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Secure = true,
                    Expires = DateTime.UtcNow.AddMinutes(10)
                });
                Response.Cookies.Append("refreshToken", tokenResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Secure = true,
                    Expires = DateTime.UtcNow.AddHours(24)
                });

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en proceso de autenticación");
                return RedirectToAction("Error", "Home");
            }
        }
       
        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerifyMfa()
        {
            var pendingUserId = Request.Cookies["mfa_pending"];
            if (string.IsNullOrEmpty(pendingUserId)) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyMfa(string codigo)
        {
            var pendingUserId = Request.Cookies["mfa_pending"];
            if (string.IsNullOrEmpty(pendingUserId)) return RedirectToAction("Login");

            // 1. LOGICA DE REINTENTOS
            string attemptsKey = $"MFA_Attempts_{pendingUserId}";
            var attemptsStr = await _cache.GetStringAsync(attemptsKey);
            int attempts = string.IsNullOrEmpty(attemptsStr) ? 0 : int.Parse(attemptsStr);

            if (attempts >= 3)
            {

                await _cache.RemoveAsync($"MFA_{pendingUserId}");
                await _cache.RemoveAsync(attemptsKey);
                Response.Cookies.Delete("mfa_pending");

                TempData["ErrorMessage"] = "Demasiados intentos fallidos. Por seguridad, reinicie el proceso de login.";
                return RedirectToAction("Login");
            }

            // Recuperamos el modelo de TempData para la vista en caso de error
            var modelJson = TempData["LoginModel"] as string;
            LoginDto model = null;
            if (!string.IsNullOrEmpty(modelJson))
            {
                model = JsonConvert.DeserializeObject<LoginDto>(modelJson);
            }

            // 2. Validar código de la caché
            var cachedCode = await _cache.GetStringAsync($"MFA_{pendingUserId}");

            if (string.IsNullOrEmpty(codigo) || cachedCode != codigo)
            {

                attempts++;
                await _cache.SetStringAsync(attemptsKey, attempts.ToString(), TimeSpan.FromMinutes(5));

                ModelState.AddModelError("", $"Código incorrecto. Le quedan {3 - attempts} intentos.");


                TempData["LoginModel"] = modelJson;

                if (model == null) return RedirectToAction(nameof(Login));

                var viewmodel = new LoginViewModel { Email = model.Email, Password = model.Password };
                return View(viewmodel);
            }

            // 3. CÓDIGO CORRECTO: Generamos los tokens 
            try
            {
                // Limpiamos el contador de intentos ya que tuvo éxito
                await _cache.RemoveAsync(attemptsKey);

                var user = await _policyExecutor.ExecutePolicyAsync(() => _authService.Login(model.Email, model));
                var tokenResponse = await _tokenService.GenerarToken(user.Data);

                // Creamos las cookies
                Response.Cookies.Append("auth", tokenResponse.Token, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Secure = true,
                    Expires = DateTime.UtcNow.AddMinutes(10)
                });
                Response.Cookies.Append("refreshToken", tokenResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Secure = true,
                    Expires = DateTime.UtcNow.AddHours(24)
                });

                // 4. Limpiar cookie temporal y código de caché
                Response.Cookies.Delete("mfa_pending");
                await _cache.RemoveAsync($"MFA_{pendingUserId}");

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando tokens tras MFA");
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
               await _carritoService.EliminarCarritoActivoAsync();
                
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


        [Authorize(Roles ="Administrador")]
        [HttpGet("reset-password/{email}")]
        public async Task<IActionResult> ResetPassword(string email)
        {
            try
            {
                var userEmail = await _authService.EnviarCorreoResetAsync(email);

                if (!userEmail.Success)
                {
                    _logger.LogError("Ocurrio un error al eviar el correo electronico", userEmail);
                    return RedirectToAction("Index", "Admin");
                }

                return View("ResetPassword", userEmail.Data);
            }
            catch (Exception ex)
            {
                TempData["ConnectionError"] = "El servidor tardó mucho en responder, inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al enviar el email de restablecimiento");
                return RedirectToAction("Error", "Home");
            }
        }

       
        [HttpGet("auth/restore-password/{UserId}/{Token}")]
        public async Task<IActionResult> RestorePassword(int UserId, string Token)
        {
            var  modelo = await _authService.PrepareRestorePassModel(UserId, Token);
            if (!modelo.Success || modelo.Data is null)
            {
                _logger.LogCritical("La URL a intentado ser manipulada por el usuario ", UserId);
                return RedirectToAction("ResetPasswordOlvidada");
            }
            // Convertimos el DTO a ViewModel
            var viewModel = new RestorePasswordViewModel
            {
                UserId = modelo.Data.UserId,
                Token = modelo.Data.Token,
                TemporaryPassword = string.Empty,  
                Password = string.Empty,
                
            };
            return View(viewModel);
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestorePasswordUser(RestorePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("RestorePassword", model); 
            }

            var result = await _authService.PrepareRestorePassModel(model.UserId, model.Token);

            if (!result.Success)
            {
                _logger.LogCritical("La URL fue manipulada por el usuario: ", model.UserId);
                return RedirectToAction(nameof(Login));
            }

            // Preparar DTO para el cambio (con la nueva contraseña)
            var cambio = new RestoresPasswordDto
            {
                UserId = model.UserId,
                Token = model.Token,
                TemporaryPassword = model.TemporaryPassword,
                Password = model.Password
            };

            try
            {
                var setResult = await _authService.SetNewPasswordAsync(cambio);

                if (setResult.Success)
                {
                    _logger.LogInformation("Contraseña restablecida con exito para el usuario: ", cambio.UserId);
                    return RedirectToAction("Login", "Auth");
                }
                else
                {
                   _logger.LogCritical(setResult.Message);
                    return RedirectToAction(nameof(Logout));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar la contraseña");
                TempData["ConnectionError"] = "El servidor tardó mucho en responder, inténtelo de nuevo más tarde";
                return RedirectToAction("Error", "Home");
            }
        }


        [HttpGet]
    
        public  IActionResult ResetPasswordOlvidada()
        {
            return View();
        }
    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordOlvidada(string email)
        {
            try
            {
                var  userEmail = await _authService.EnviarCorreoResetAsync(email);

                if (!userEmail.Success)
                {
                    TempData["ErrorMessage"] = userEmail.Message;
                    return RedirectToAction(nameof(ResetPasswordOlvidada));
                }

                return View("ResetPassword", userEmail.Data);
            }
            catch (Exception ex)
            {
                TempData["ConnectionError"] = "El servidor tardó mucho en responder, inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al enviar el email de restablecimiento");
                return RedirectToAction("Error", "Home");
            }
        }
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string passwordAnterior, string passwordActual)
        {

            var resultado = await _policyExecutor.ExecutePolicyAsync(() => _authService.ChangePassword(passwordAnterior, passwordActual));
            if (resultado.Success)
            {
                if ( User.IsAdministrador())
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
                TempData["ErrorMessage"] = resultado.Message;
                return View(nameof(ChangePassword));
            }

        }
    }
}