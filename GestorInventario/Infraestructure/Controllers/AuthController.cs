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

namespace GestorInventario.Infraestructure.Controllers
{
    public class AuthController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly HashService _hashService;
        private readonly IEmailService _emailService;
        private readonly TokenService _tokenService;
        public AuthController(GestorInventarioContext context, HashService hashService, IEmailService emailService, TokenService tokenService)
        {
            _context = context;
            _hashService = hashService;
            _emailService = emailService;
            _tokenService = tokenService;
        }

        public IActionResult Index()
        {
            return View();
        }
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        //[AllowAnonymous]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login(LoginViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var user = await _context.Usuarios.Include(x => x.IdRolNavigation).FirstOrDefaultAsync(u => u.Email == model.Email);

        //        if (user != null)
        //        {
        //            // Comprobar si el correo electrónico ha sido confirmado
        //            if (!user.ConfirmacionEmail)
        //            {
        //                //Con esto creas un error personalizado
        //                ModelState.AddModelError("", "Por favor, confirma tu correo electrónico antes de iniciar sesión.");
        //                return View(model);
        //            }
        //            //Se llama al servicio hash service
        //            var resultadoHash = _hashService.Hash(model.Password, user.Salt);
        //            //Si la contraseña que se introduce es igual a la que hay en base de datos se procede al login
        //            if (user.Password == resultadoHash.Hash)
        //            {
        //                // Crear una lista de reclamaciones (claims) para el usuario
        //                var claims = new List<Claim>
        //                 {
        //                     new Claim(ClaimTypes.Name, user.Email),
        //                    new Claim(ClaimTypes.Role, user.IdRolNavigation.Nombre),

        //                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        //                };

        //                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);


        //                var principal = new ClaimsPrincipal(identity);

        //                // Iniciar sesión con el principal del usuario
        //                // Esto establece la cookie de autenticación en el navegador del usuario
        //                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        //                return RedirectToAction("Index", "Home");
        //            }
        //            else
        //            {
        //                ModelState.AddModelError("", "El email y/o la contraseña son incorrectos.");
        //                return View(model);
        //            }
        //        }

        //        return View(model);
        //    }

        //    return View(model);
        //}
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Usuarios.Include(x => x.IdRolNavigation).FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null)
                {
                    // Comprobar si el correo electrónico ha sido confirmado
                    if (!user.ConfirmacionEmail)
                    {
                        //Con esto creas un error personalizado
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
                        Response.Cookies.Append("auth", tokenResponse.Token, new CookieOptions { 
                            HttpOnly = true, 
                            SameSite= SameSiteMode.Strict,
                            MaxAge= TimeSpan.FromMinutes(60),
                            Secure = true,
                            
                        });

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
        [AllowAnonymous]
        public IActionResult Logout()
        {
            // Elimina la cookie "auth" del navegador.
            Response.Cookies.Delete("auth");
            return RedirectToAction("Index", "Home");
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
            var usuarioDB= await _context.Usuarios.EmailExists(email);
            //var usuarioDB = await _context.Usuarios.AsTracking().FirstOrDefaultAsync(x => x.Email == email);
            // Generar una contraseña temporal
            await _emailService.SendEmailAsyncResetPassword(new DTOEmail
            {
                ToEmail = email
            });
            return View(usuarioDB);
        }
        //De ese email que se ha enviado del enlace tomamos el id de usuario y el token que no es token lo que genera es un
        //identificador unico, este identificador se genero cuando el usuario se registro, con esto nos asguramos de que la contraseña
        //se cambia para el usuario correcto
        [AllowAnonymous]
        [Route("AuthController/RestorePassword/{UserId}/{Token}")]
        public async Task<IActionResult> RestorePassword(DTORestorePass cambio)
        {
            //Se busca al usuario por Id
            var usuarioDB= await _context.Usuarios.ExistUserId(cambio.UserId);
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
        //En la vista para restaurar la contraseña llamamos a este metodo para que la contraseña sea restaurada
        //esto es la logica que hay detras del formulario para restaurar la contraseña
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RestorePasswordUser(DTORestorePass cambio)
        {
            var usuarioDB = await _context.Usuarios.ExistUserId(cambio.UserId);

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
            _context.Usuarios.Update(usuarioDB);
            //Se guarda en base de datos
            await _context.SaveChangesAsync();
            //Se usa el servicio hash service para hashear la contraseña proporcionada por el usuario
            var resultadoHash = _hashService.Hash(cambio.Password);
            //Se asigna un hash a la contraseña que proporciono el usuario
            usuarioDB.Password = resultadoHash.Hash;
            //Se asigna un salt a la contraseña que proporciono el usuario

            usuarioDB.Salt = resultadoHash.Salt;

            // Guardar los cambios en la base de datos
            _context.Usuarios.Update(usuarioDB);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Admin");
        }

    }
}
