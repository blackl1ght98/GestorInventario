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
        public static void AddEntity<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Add(entity);
            context.SaveChanges();
        }

        public static void DeleteEntity<T>(this DbContext context, T entity) where T : class
        {
            context.Set<T>().Remove(entity);
            context.SaveChanges();
        }
        // IEnumerable<T> es una interfaz que define un método (GetEnumerator) que expone un enumerador,
        // que soporta una iteración simple sobre una colección de un tipo específico. Puedes pensar en 
        // IEnumerable<T> como la "forma más simple" de una colección. No tiene métodos para agregar o 
        // eliminar elementos, ni tiene índices. Solo te permite iterar sobre los elementos de la colección.

        // List<T> es una clase que implementa la interfaz IEnumerable<T>, pero también proporciona 
        // funcionalidades adicionales. List<T> representa una lista fuertemente tipada de objetos a los 
        // que se puede acceder por índice. Proporciona métodos para buscar, ordenar y manipular listas.

        public static void DeleteRangeEntity<T>(this DbContext context, IEnumerable<T> entities) where T : class
        {
            context.Set<T>().RemoveRange(entities);
            context.SaveChanges();
        }
    }
}
