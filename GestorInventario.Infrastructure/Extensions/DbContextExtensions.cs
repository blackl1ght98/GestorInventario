using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrastructure.Extensions
{
    public static class DbContextExtensions
    {
        

     
     
        public static void EntityModified<T>(this DbContext context, T entity) where T : class
        {
            context.Entry(entity).State = EntityState.Modified;

        }

        public static async Task UpdateEntityAsync<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Update(entity);
            await context.SaveChangesAsync();
        }

      
        public static async Task AddEntityAsync<T>(this DbContext context, T entity) where T : class
        {
              context.Set<T>().Add(entity);
              await context.SaveChangesAsync();
          
        }

        public static async Task DeleteEntityAsync<T>(this DbContext context, T entity) where T : class
        {
            
            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
        }
        public static async Task<bool> DeleteWithChildrenAsync<TParent, TChild>(
            this DbContext context, 
            TParent parent, 
            IEnumerable<TChild> children) 
            where TParent : class 
            where TChild : class
        {
            return await context.ExecuteInTransactionAsync(async () =>
            {
                // 1. Borrar los hijos primero si existen
                var childList = children?.ToList();
                if (childList != null && childList.Any())
                {
                    context.Set<TChild>().RemoveRange(childList);
                }

                // 2. Borrar el padre
                context.Set<TParent>().Remove(parent);

                // 3. Guardar todo en la base de datos de forma atómica
                await context.SaveChangesAsync();

                return true; // Devuelve el bool que espera ExecuteInTransactionAsync<TResult>
            });
        }
        public static async Task<TResult> ExecuteInTransactionAsync<TResult>(this DbContext context, Func<Task<TResult>> operation)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public static async Task DeleteRangeEntityAsync<T>(this DbContext context, IEnumerable<T> entities) where T : class
        {
            context.Set<T>().RemoveRange(entities);
           await context.SaveChangesAsync();
        }   
       
    }
}
