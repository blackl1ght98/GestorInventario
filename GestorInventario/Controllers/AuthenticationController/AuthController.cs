using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.TokenGeneration;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Application.Interfaces.Application.Services.Common;
using GestorInventario.Application.Interfaces.Application.Services.ShopCart;
using GestorInventario.Application.Interfaces.Infrastructure.RetryPolicy;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Extensions;
using GestorInventario.ViewModels.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


namespace GestorInventario.Controllers.AuthenticationController
{
    public class AuthController : Controller
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IShopCartService _carritoService;
        private readonly ICurrentUserAccessor _current;
        private readonly IAuthService _authService;
        private readonly ILoginGenerator _loginGenerator;

        public AuthController(
            ITokenService tokenService,
            ICurrentUserAccessor currentUser,
            ILogger<AuthController> logger,
            IPolicyExecutor policyExecutor,
            IShopCartService carritoService,
            IAuthService authService,
         
            ILoginGenerator factory
        )
        {
            _tokenService = tokenService;
            _logger = logger;
            _policyExecutor = policyExecutor;
            _carritoService = carritoService;
            _current = currentUser;
            _authService = authService;
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
                foreach (var cookie in Request.Cookies)
                {
                    Response.Cookies.Delete(cookie.Key);
                }

                var dto = new LoginDto { Email = model.Email, Password = model.Password };
                // 1. Obtenemos la estrategia según la configuración: MfaLoginStrategy o StandardLoginStrategy
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
                   
                    return RedirectToAction("VerifyMfa", "Auth");
                }

                // 3. Si NO requiere MFA, generamos tokens directamente
                var tokenResponse = await _tokenService.GenerarToken(result.Data.User);
                SetAuthCookies(tokenResponse.Token, tokenResponse.RefreshToken);
                var userId = result.Data.User.Id;
                var crearCarrito = await _carritoService.CrearCarritoUsuario(userId);
                if (!crearCarrito.Success)
                {
                    return RedirectToAction("Error", "Home");
                }

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
            if (string.IsNullOrEmpty(pendingUserId))
                return RedirectToAction(nameof(Login));

            var result = await _authService.CompletarMfaAsync(pendingUserId, codigo);

            if (!result.Success)
            {
                // Bloqueo por intentos → limpiar cookie y volver a login
                if (result.Message.Contains("intentos superado", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Cookies.Delete("mfa_pending");
                    return RedirectToAction(nameof(Login));
                }

                ModelState.AddModelError("", result.Message);
                return View(); 
            }

            var tokenResponse = await _tokenService.GenerarToken(result.Data);
            SetAuthCookies(tokenResponse.Token, tokenResponse.RefreshToken);
            Response.Cookies.Delete("mfa_pending");

            await _carritoService.CrearCarritoUsuario(result.Data.Id);

            return RedirectToAction("Index", "Home");
        }

        private void SetAuthCookies(string token, string refreshToken)
        { 
            Response.Cookies.Append("auth", token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true,
                Expires = DateTime.UtcNow.AddMinutes(10)
            });
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true,
                Expires = DateTime.UtcNow.AddHours(24)
            });
            
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
                _logger.LogError(ex, "Error al cerrar sesion");
                return RedirectToAction("Error", "Home");
            }
        }


        [Authorize(Policy = "EsAdministrador")]
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
                _logger.LogError(ex, "Error al enviar el email de restablecimiento");
                return RedirectToAction("Error", "Home");
            }
        }
        [HttpGet("auth/restore-password/{UserId}/{Token}")]
        public async Task<IActionResult> RestorePassword(int UserId, string Token)
        {
            var modelo = await _authService.PrepareRestorePassModel(UserId, Token);
        

            // Convertimos el DTO a ViewModel
            var viewModel = new RestorePasswordViewModel
            {
                UserId = modelo.Data.UserId,
                Token = modelo.Data.Token,
                Password = string.Empty,
                TemporaryPassword = string.Empty,
            };
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestorePasswordUser(RestorePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View("RestorePassword", model);

            var cambio = new SetNewPasswordDto
            {
                UserId = model.UserId,
                Token = model.Token,
                TemporaryPassword = model.TemporaryPassword,
                Password = model.Password
            };

            var setResult = await _authService.SetNewPasswordAsync(cambio);
            if (setResult.Success)
                return RedirectToAction(nameof(Login));

            TempData["ErrorMessage"] = setResult.Message;
            return View("RestorePassword", model); 
        }
        [HttpGet]
        public IActionResult ResetPasswordOlvidada()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordOlvidada(string email)
        {
            try
            {
                var userEmail = await _authService.EnviarCorreoResetAsync(email);
                if (!userEmail.Success)
                {
                    TempData["ErrorMessage"] = userEmail.Message;
                    return RedirectToAction(nameof(ResetPasswordOlvidada));
                }

                return View("ResetPassword", userEmail.Data);
            }
            catch (Exception ex)
            {
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
            var resultado =
                await _policyExecutor.ExecutePolicyAsync(() =>
                    _authService.ChangePassword(passwordAnterior, passwordActual));
            if (resultado.Success)
            {
                if (User.IsAdministrador())
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