using GestorInventario.Application.DTOs;
using GestorInventario.Interfaces.Application;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using GestorInventario.MetodosExtension;

namespace GestorInventario.Application.Services
{
    public class ChangePassService : IChangePassService
    {
        private readonly HashService _hashService;
        private readonly GestorInventarioContext _context;
        public ChangePassService(HashService hashService, GestorInventarioContext context)
        {
            _hashService = hashService;
            _context = context;
        }
     
        public async Task ChangePassId(Usuario usuarioDB, string newPass)
        {
            var resultadoHash = _hashService.Hash(newPass);
            usuarioDB.Password = resultadoHash.Hash;
            usuarioDB.Salt = resultadoHash.Salt;
           await _context.UpdateEntityAsync(usuarioDB);
        }

    }
}
