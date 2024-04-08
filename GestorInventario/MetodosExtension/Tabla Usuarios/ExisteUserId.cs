using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.MetodosExtension
{
    public static class ExisteUserId
    {
        public static  Task<Usuario> ExistUserId(this DbSet<Usuario> context, int UserId)
        {
            return  context.FirstOrDefaultAsync(x => x.Id == UserId);
        }
    }
}
