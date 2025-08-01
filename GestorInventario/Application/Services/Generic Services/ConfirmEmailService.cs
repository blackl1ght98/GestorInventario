﻿using GestorInventario.Interfaces.Application;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using GestorInventario.MetodosExtension;
using GestorInventario.Application.DTOs.User;

namespace GestorInventario.Application.Services
{
    public class ConfirmEmailService:IConfirmEmailService
    {
        private readonly GestorInventarioContext _context;
        public ConfirmEmailService(GestorInventarioContext context)
        {
            _context = context;

        }   
        public async Task ConfirmEmail(ConfirmRegistrationDto confirm)
        {
          
            var usuarioUpdate = _context.Usuarios.AsTracking().FirstOrDefault(x => x.Id == confirm.UserId);
            if (usuarioUpdate != null)
            {
                usuarioUpdate.ConfirmacionEmail = true;
                await _context.UpdateEntityAsync(usuarioUpdate);
            }
        }
    }
}
