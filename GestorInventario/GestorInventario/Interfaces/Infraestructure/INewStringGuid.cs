using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infrastructure
{
    public interface INewStringGuid
    {
        Task SaveNewStringGuid(Usuario operation);
    }
}
