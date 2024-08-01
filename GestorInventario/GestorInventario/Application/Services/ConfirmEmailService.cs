using GestorInventario.Application.DTOs;
using GestorInventario.Interfaces.Application;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Services
{
    public class ConfirmEmailService:IConfirmEmailService
    {
        private readonly GestorInventarioContext _context;
        public ConfirmEmailService(GestorInventarioContext context)
        {
            _context = context;

        }
        

        public async Task ConfirmEmail(DTOConfirmRegistration confirm)
        {
          
            var usuarioUpdate = _context.Usuarios.AsTracking().FirstOrDefault(x => x.Id == confirm.UserId);

            usuarioUpdate.ConfirmacionEmail = true;
            _context.Usuarios.Update(usuarioUpdate);

            await _context.SaveChangesAsync();
        }
    }
}
