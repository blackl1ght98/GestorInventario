using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface ICarritoRepository
    {
        Task<Carrito> ObtenerCarritoUsuario(int userId);
     
        Task<List<ItemsDelCarrito>> ObtenerItemsDelCarritoUsuario(int userIdcarrito);
        Task<ItemsDelCarrito> ItemsDelCarrito(int Id);
        Task<(bool, string,string)> Pagar(string moneda, int userId);
        IQueryable<ItemsDelCarrito> ObtenerItems(int id);
        Task<List<Monedum>> ObtenerMoneda();
        Task<(bool, string)> Incremento(int id);
        Task<(bool, string)> Decremento(int id);
        Task<(bool, string)> EliminarProductoCarrito(int id);
    }
}
