using GestorInventario.Application.DTOs;
using GestorInventario.Interfaces.Application;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Services
{
    public class ChangePassService : IChangePassService
    {
        private readonly HashService _hashService;
        private readonly GestorInventarioContext _context;


        //Creamos el contructor
        public ChangePassService(HashService hashService, GestorInventarioContext context)
        {
            _hashService = hashService;
            _context = context;


        }
        //Agregamos el metodo task que esta en la interfaz con el DTOCambioPassPorId para poder cambiar la contraseña
        //una vez logueados, este dto dispone de los datos que se necesitan junto a la base de datos
        public async Task ChangePassId(Usuario usuarioDB, string newPass)
        {
            // Ya no necesitas buscar al usuario porque ya lo tienes

            // Llamamos al servicio _hashService este servicio tiene un metodo Hash  al cual se le pasa la 
            // nueva contraseña que cree el usuario para que la cifre
            var resultadoHash = _hashService.Hash(newPass);
            // A la contraseña que cree el usuario se cifra con el hash y se le asigna un salt
            usuarioDB.Password = resultadoHash.Hash;
            usuarioDB.Salt = resultadoHash.Salt;
            _context.Usuarios.Update(usuarioDB);
            // Guardamos los cambios en base de datos
            await _context.SaveChangesAsync();
        }

    }
}
