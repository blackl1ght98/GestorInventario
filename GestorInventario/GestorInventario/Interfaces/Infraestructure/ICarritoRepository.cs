﻿using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface ICarritoRepository
    {
        Task<Carrito> ObtenerCarrito(int userId);
        Task<List<ItemsDelCarrito>> ObtenerItemsCarrito(int userIdcarrito);
        Task<List<ItemsDelCarrito>> ConvertirItemsAPedido(int userIdcarrito);
        Task<ItemsDelCarrito> ItemsDelCarrito(int Id);
     
        Task<(bool, string,string)> Pagar(string moneda, int userId);
        IQueryable<ItemsDelCarrito> ObtenerItems(int id);
        Task<List<Monedum>> ObtenerMoneda();
        Task<(bool, string)> Incremento(int id);
        Task<(bool, string)> Decremento(int id);
        Task<(bool, string)> EliminarProductoCarrito(int id);
    }
}