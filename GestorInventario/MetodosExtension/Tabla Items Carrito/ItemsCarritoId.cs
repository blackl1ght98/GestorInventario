using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.MetodosExtension.Tabla_Items_Carrito
{
    public static  class ItemsCarritoId
    {
        public static Task<ItemsDelCarrito> ItemsCarritoIds(this DbSet<ItemsDelCarrito> context, int Id)
        {
            return context.FirstOrDefaultAsync(x => x.Id == Id);
        }
    }
}
