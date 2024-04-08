using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.MetodosExtension
{
    public static class ExisteEmail
    {
        public static Task<Usuario> EmailExists(this DbSet<Usuario> context, string email)
        {
            return context.FirstOrDefaultAsync(x => x.Email == email);
        }
    }
}
