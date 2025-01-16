using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface IChangePassService
    {
        Task ChangePassId(Usuario usuarioDB, string newPass);

    }
}
