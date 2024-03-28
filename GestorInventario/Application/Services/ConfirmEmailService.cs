using GestorInventario.Application.DTOs;
using GestorInventario.Interfaces.Application;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Services
{
    public class ConfirmEmailService:IConfirmEmailService
    {
        private readonly GestorInventarioContext _context;
        //Creamos el constructor

        public ConfirmEmailService(GestorInventarioContext context)
        {
            _context = context;

        }
        //Agregamos el metodo que esta en la interfaz junto a su DTOConfirmRegistrtion

        public async Task ConfirmEmail(DTOConfirmRegistration confirm)
        {
            //var usuarioUpdate = await _confimremail.ChangePassword(confirm.UserId);
            //Buscamos en base de datos si existe el usuario en base a su id
            var usuarioUpdate = _context.Usuarios.AsTracking().FirstOrDefault(x => x.Id == confirm.UserId);
            //ConfirmacionEmail esto lo establecemos a true una vez que el usuario haya confirmado su email

            usuarioUpdate.ConfirmacionEmail = true;
            //Llamamos al servicio _confirmationRegisterRepository encargado de gurdar y actualizar los datos
            _context.Usuarios.Update(usuarioUpdate);
            //Guardamos los cambios

            await _context.SaveChangesAsync();
        }
    }
}
