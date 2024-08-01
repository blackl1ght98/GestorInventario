using Microsoft.EntityFrameworkCore;

namespace GestorInventario.MetodosExtension
{
    public static class DbContextExtensions
    {
        /*Este es un metodo estatico que no devuelve nada es de tipo generico <T> y admite
         * cualquier tipo de datos.
         * DbContext es una clase de entity framework que representa el contexto de base de datos de manera
         * global.
         * T entity esto quire decir que admite cualquier tipo de entidad(tabla).
         * where T: class--> es una restriccion de tipo que asegura que este metodo solo pueda ser usado en clases
         */
        public static void UpdateEntity<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Update(entity);
            context.SaveChanges();
        }
        public static async Task UpdateEntityAsync<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Update(entity);
            await context.SaveChangesAsync();
        }
        public static void AddEntity<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Add(entity);
            context.SaveChanges();
        }
        public static async Task AddEntityAsync<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Add(entity);
            await context.SaveChangesAsync();
        }
        public static void DeleteEntity<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Remove(entity);
            context.SaveChanges();
        }
        public static async Task DeleteEntityAsync<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Remove(entity);
           await context.SaveChangesAsync();
        }

        public static void DeleteRangeEntity<T>(this DbContext context, IEnumerable<T> entities) where T : class
        {
            context.Set<T>().RemoveRange(entities);
            context.SaveChanges();
        }
        public static async Task DeleteRangeEntityAsync<T>(this DbContext context, IEnumerable<T> entities) where T : class
        {
            context.Set<T>().RemoveRange(entities);
           await context.SaveChangesAsync();
        }
        public static void ReloadEntity<T>(this DbContext context, T entity) where T : class
        {
            context.Entry(entity).Reload();
           
        }
        public static void EntityModified<T>(this DbContext context, T entity) where T : class
        {
            context.Entry(entity).State=EntityState.Modified;

        }
    }
}
