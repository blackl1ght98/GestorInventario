using Microsoft.EntityFrameworkCore;


namespace GestorInventario.Infrastructure
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

      
        public static async Task<int> AddEntityAsync<T>(this DbContext context, T entity, bool saveImmediately = true) where T : class
        {
            var entry = context.Set<T>().Add(entity);
            if (saveImmediately)
                return await context.SaveChangesAsync();
            return 0; 
        }

        public static async Task DeleteEntityAsync<T>(this DbContext context, T entity, bool saveImmediately = true) where T : class
        {
            context.Set<T>().Remove(entity);
            if (saveImmediately) await context.SaveChangesAsync();
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
