using GestorInventario.Infraestructure.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

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
       
      
        public static void UpdateEntity<T>(this DbContext context, T entity, bool saveImmediately = true) where T : class
        {
            context.Set<T>().Update(entity);
            if (saveImmediately) context.SaveChanges();
        }
        public static void UpdateRangeEntity<T>( this DbContext context,IEnumerable<T> entities, bool saveImmediately = true) where T : class
        {
            context.Set<T>().UpdateRange(entities);

            if (saveImmediately)
            {
                context.SaveChanges();
            }
        }

        public static void AddRangeEntity<T>(this DbContext context, IEnumerable<T> entities, bool saveImmediately = true) where T : class
        {
            context.Set<T>().AddRange(entities);

            if (saveImmediately)
            {
                context.SaveChanges();
            }
        }
        public static void DeleteEntity<T>(this DbContext context, T entity, bool saveImmediately = true) where T : class
        {
            context.Set<T>().Remove(entity);
            if (saveImmediately) context.SaveChanges();
        }
        public static void DeleteRangeEntity<T>(this DbContext context, IEnumerable<T> entities, bool saveImmediately = true) where T : class
        {
            context.Set<T>().RemoveRange(entities);
            if (saveImmediately) context.SaveChanges();
        }
        public static void ExecuteInTransaction<T>(this DbContext context, Action operation) where T : class
        {
            using var transaction = context.Database.BeginTransaction();
            try
            {
                operation();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public static void AddEntity<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Add(entity);
            context.SaveChanges();
        }
        public static void ReloadEntity<T>(this DbContext context, T entity) where T : class
        {
            context.Entry(entity).Reload();

        }
        public static TResult ExecuteInTransaction<T, TResult>(this DbContext context, Func<TResult> operation) where T : class
        {
            using var transaction = context.Database.BeginTransaction();
            try
            {
                var result = operation();
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public static void EntityModified<T>(this DbContext context, T entity) where T : class
        {
            context.Entry(entity).State = EntityState.Modified;

        }
        public static async Task UpdateEntityAsync<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Update(entity);
            await context.SaveChangesAsync();
        }
        public static async Task DeleteRangeEntityAsync<T>(this DbContext context, IEnumerable<T> entities, bool saveImmediately = true) where T : class
        {
            context.Set<T>().RemoveRange(entities);
            if (saveImmediately) await context.SaveChangesAsync();
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

        public static async Task<TResult> ExecuteInTransactionAsync<T, TResult>(this DbContext context, Func<Task<TResult>> operation) where T : class
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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
        public static async Task UpdateRangeEntityAsync<T>(this DbContext context,IEnumerable<T> entities,bool saveImmediately = true) where T : class
        {
            context.Set<T>().UpdateRange(entities);

            if (saveImmediately)
            {
                await context.SaveChangesAsync();
            }
        }
        public static async Task AddRangeEntityAsync<T>(this DbContext context, IEnumerable<T> entities, bool saveImmediately = true) where T : class
        {
            context.Set<T>().AddRange(entities);

            if (saveImmediately)
            {
                await context.SaveChangesAsync();
            }
        }
        public static async Task ExecuteInTransactionAsync<T>(this DbContext context,Func<Task> operation) where T : class
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public static async Task<(bool success, string message, int affectedRows)> UpdateEntityWithPolicyAsync<T>(this DbContext context,T entity,PolicyExecutor policyExecutor,
        bool saveImmediately = true) where T : class
        {
            return await policyExecutor.ExecutePolicyAsync(async () =>
            {
                try
                {
                    context.Set<T>().Update(entity);

                    if (saveImmediately)
                    {
                        var affected = await context.SaveChangesAsync();
                        return (true, "Entity updated successfully", affected);
                    }

                    return (true, "Operation completed (no save)", 0);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    return (false, "Concurrency conflict - data was modified by another user", 0);
                }
                catch (DbUpdateException ex)
                {
                    return (false, $"Database update error: {ex.InnerException?.Message ?? ex.Message}", 0);
                }
                catch (Exception ex)
                {
                    return (false, $"Unexpected error: {ex.Message}", 0);
                }
            });
        }
        public static async Task<(bool success, string message, int affectedRows)> AddEntityWithPolicyAsync<T>(this DbContext context,T entity,PolicyExecutor policyExecutor,
        bool saveImmediately = true) where T : class
        {
            return await policyExecutor.ExecutePolicyAsync(async () =>
            {
                try
                {
                    var entry = context.Set<T>().Add(entity);

                    if (saveImmediately)
                    {
                        var affected = await context.SaveChangesAsync();
                        return (true, "Entity added successfully", affected);
                    }

                    return (true, "Entity added (no save)", 0);
                }
                catch (DbUpdateException ex)
                {
                    return (false, $"Database error: {ex.InnerException?.Message ?? ex.Message}", 0);
                }
                catch (Exception ex)
                {
                    return (false, $"Add failed: {ex.Message}", 0);
                }
            });
        }

        public static async Task<(bool success, string message, int affectedRows)> DeleteEntityWithPolicyAsync<T>(this DbContext context,T entity,PolicyExecutor policyExecutor,
        bool saveImmediately = true) where T : class
        {
            return await policyExecutor.ExecutePolicyAsync(async () =>
            {
                try
                {
                    context.Set<T>().Remove(entity);

                    if (saveImmediately)
                    {
                        var affected = await context.SaveChangesAsync();
                        return (true, "Entity deleted successfully", affected);
                    }

                    return (true, "Entity marked for deletion (no save)", 0);
                }
                catch (DbUpdateException ex)
                {
                    return (false, $"Database error: {ex.InnerException?.Message ?? ex.Message}", 0);
                }
                catch (Exception ex)
                {
                    return (false, $"Delete failed: {ex.Message}", 0);
                }
            });
        }

        public static async Task<(bool success, string message, int affectedRows)> UpdateRangeEntityWithPolicyAsync<T>(this DbContext context,IEnumerable<T> entities,PolicyExecutor policyExecutor,
        bool saveImmediately = true) where T : class
        {
            return await policyExecutor.ExecutePolicyAsync(async () =>
            {
                try
                {
                    context.Set<T>().UpdateRange(entities);

                    if (saveImmediately)
                    {
                        var affected = await context.SaveChangesAsync();
                        return (true, $"Updated {affected} entities successfully", affected);
                    }

                    return (true, "Entities updated (no save)", 0);
                }
                catch (Exception ex)
                {
                    return (false, $"Update range failed: {ex.Message}", 0);
                }
            });
        }

        public static void ConvertirJson<T>(this StringContent context, T entity) where T : class
        {
            var json = JsonConvert.SerializeObject(entity);
            context=new StringContent(json, Encoding.UTF8, "application/json");

        }
    }
}
