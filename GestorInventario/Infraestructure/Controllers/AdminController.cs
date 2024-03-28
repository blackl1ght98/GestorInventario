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

namespace GestorInventario.Infraestructure.Controllers
{
    public class AdminController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly IEmailService _emailService;
        private readonly HashService _hashService;
        private readonly IConfirmEmailService _confirmEmailService;

        public AdminController(GestorInventarioContext context, IEmailService emailService, HashService hashService, IConfirmEmailService confirmEmailService)
        {
            _context = context;
            _emailService = emailService;
            _hashService = hashService;
            _confirmEmailService = confirmEmailService;
        }

        public IActionResult Index()
        {
            ViewData["Roles"] = new SelectList(_context.Roles, "Id", "Nombre");
            var usuarios = _context.Usuarios.Include(x => x.IdRolNavigation).ToList();
            return View(usuarios);
        }
        [HttpPost]
        public IActionResult UpdateRole(int id, int newRole)
        {
            var user = _context.Usuarios.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            //crea el desplegable
            ViewData["Roles"] = new SelectList(_context.Roles, "Id", "Nombre");
            //Le asigna el rol al usuario
            user.IdRol = newRole;
            //Actualiza en base de datos 
            _context.Usuarios.Update(user);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Create()
        {
            //Sirve para obtener los datos del desplegable
            ViewData["Roles"] = new SelectList(_context.Roles, "Id", "Nombre");

            return View();
        }


        [HttpPost]
        //Sirve para que no se altere la informacion del formulario
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            //Esto toma en cuenta las validaciones puestas en UserViewModel
            if (ModelState.IsValid)
            {
                // Verificar si el email ya existe en la base de datos
                var existingUser = _context.Usuarios.FirstOrDefault(u => u.Email == model.Email);
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
                    //Rol = "user",
                    NombreCompleto = model.NombreCompleto,
                    FechaNacimiento = model.FechaNacimiento,
                    Telefono = model.Telefono,
                    Direccion = model.Direccion,
                    FechaRegistro = DateTime.Now
                };
                //Sirve para crear el desplegable
                ViewData["Roles"] = new SelectList(_context.Roles, "Id", "Nombre");
                //Agrega el usuario a base de datos
                _context.Add(user);
                //Guarda los cambios
                await _context.SaveChangesAsync();
                //Envia el correo electronico al usuario para que confirme su email
                await _emailService.SendEmailAsyncRegister(new DTOEmail
                {
                    ToEmail = model.Email
                });
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
        [AllowAnonymous]
        [Route("AdminController/ConfirmRegistration/{UserId}/{Token}")]
        public async Task<IActionResult> ConfirmRegistration(DTOConfirmRegistration confirmar)
        {

            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == confirmar.UserId);
            if (usuarioDB.ConfirmacionEmail != false)
            {
                return BadRequest("Usuario ya validado con anterioridad");
            }

            if (usuarioDB.EnlaceCambioPass != confirmar.Token)
            {
                return BadRequest("Token no valido");
            }
            await _confirmEmailService.ConfirmEmail(new DTOConfirmRegistration
            {
                UserId = confirmar.UserId
            });
            return RedirectToAction("Index", "Admin");
        }
        
        public async Task<ActionResult> Edit(int id)
        {
            // Obtienes el usuario de la base de datos
            Usuario user = await _context.Usuarios.FindAsync(id);

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
        //Cuando quieres editar algo de tu modelo de base de datos pero no quieres que se puedan editar determinados campos
        //lo que se realiza es una vista del modelo para especificar que campos se quieren cambiar (UsuarioEditViewModel)
        [HttpPost]
        public async Task<ActionResult> Edit(UsuarioEditViewModel userVM)
        {
            //Si el modelo es valido:
            if (ModelState.IsValid)
            {
                // Obtiene la id del usuario a editar
                var user = await _context.Usuarios.FindAsync(userVM.Id);

                if (user == null)
                {
                    return NotFound("Usuario no encontrado");
                }
                // Mapea los datos del ViewModel a la entidad
                user.NombreCompleto = userVM.NombreCompleto;
                user.FechaNacimiento = userVM.FechaNacimiento;
                user.Telefono = userVM.Telefono;
                user.Direccion = userVM.Direccion;
                //Esto ocurre cuando el usuario cambia de email 
                if (user != null && user.Email != userVM.Email)
                {
                    //La confirmacion de email se le cambia a false
                    user.ConfirmacionEmail = false;
                    //El nuevo email se asigna a base de datos
                    user.Email = userVM.Email;
                    _context.Usuarios.Update(user);
                    await _context.SaveChangesAsync();
                    //Se envia un emain para confirmar el nuevo correo
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
                    /*El método Entry en el contexto de Entity Framework Core se utiliza para obtener un objeto 
                     * que puede usarse para configurar y realizar acciones en una entidad que está siendo rastreada por el contexto.*/
                    /*

    Update(user): Este método marca la entidad y todas sus propiedades como modificadas. Esto significa que cuando llamas a SaveChangesAsync(), 
                    Entity Framework generará un comando SQL UPDATE que actualizará todas las columnas de la entidad en la base de datos, 
                    independientemente de si cambiaron o no.

    Entry(user).State = EntityState.Modified: Este método marca la entidad como modificada, pero no todas las propiedades. Cuando llamas a 
                    SaveChangesAsync(), Entity Framework generará un comando SQL UPDATE que sólo actualizará las columnas de la entidad que 
                    realmente cambiaron.
*/
                    _context.Entry(user).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                //esta excepcion es lanzada cuando varios usuarios modifican al mismo tiempo los datos. Por ejemplo
                //tenemos un usuario llamado A que esta modificando los datos y todavia no ha guardado esos cambios pero
                //tenemos un usuario B que tiene que modificar los datos que esta modificando el usuario A y el usuario B 
                //guarda los datos antes que el A por lo tanto al usuario A tener datos antiguos se produce esta excepcion al usuario
                //A no tener los datos actuales
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound("Usuario no encontrado");
                    }
                    else
                    {
                        // Recarga los datos del usuario desde la base de datos
                        _context.Entry(user).Reload();

                        // Intenta guardar de nuevo
                        _context.Entry(user).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToAction("Index");
            }
            return View(userVM);
        }

        private bool UserExists(int Id)
        {

            return _context.Usuarios.Any(e => e.Id == Id);
        }
        
        public async Task<IActionResult> Delete(int id)
        {

            //Consulta a base de datos en base a la id del usuario
            var user = await _context.Usuarios.Include(p=>p.Pedidos).FirstOrDefaultAsync(m => m.Id == id);
            //Si no hay cervezas muestra el error 404
            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }
            //Llegados ha este punto hay cervezas por lo tanto se muestran las cervezas
            return View(user);
        }


        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        //Para que detecte la id del usuario es necesario poner el mismo nombre que se ponga en la vista en la
        //parte del asp-for del formulario tenemos Id por lo tanto aqui tambien hay que ponerlo
        //El asp-for si tu ahi le pasar Id lo que va a ir a buscar es algo que sea Id si se pone en minuscula no lo encuentra
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            //Busca al usuario en base de datos
            var user = await _context.Usuarios.Include(p=>p.Pedidos).FirstOrDefaultAsync(m => m.Id == Id);
            if (user == null)
            {
                return BadRequest();
            }
            if (user.Pedidos.Any())
            {
                TempData["ErrorMessage"] = "El usuario no se puede eliminar porque tiene pedidos asociados.";
                //En caso de que el proveedor tenga productos asociados se devuelve al usuario a la vista Delete y se
                //muestra el mensaje informandole.
                //A esta reedireccion se le pasa la vista Delete y al metodo que contiene esa vista la id del proveedor
                return RedirectToAction(nameof(Delete), new { id = Id });
            }
            //Elimina el usuario y guarda los cambios
            _context.Usuarios.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
