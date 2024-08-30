using GestorInventario.Application.DTOs;
using GestorInventario.Interfaces.Application;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using GestorInventario.MetodosExtension;

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
           await _context.UpdateEntityAsync(usuarioUpdate);
        }
    }
}
