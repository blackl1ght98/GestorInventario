using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAuthRepository
    {
        Task<Usuario> Login(string email);
        Task<Usuario> ExisteEmail(string email);
        Task<Usuario> ObtenerPorId(int id);
        Task<(bool, string)> RestorePass(DTORestorePass cambio);
        Task<(bool, string)> ActualizarPass(DTORestorePass cambio);
        Task<(bool, string)> ChangePassword(string passwordAnterior, string passwordActual);
       
      
    }
}
