namespace GestorInventario.PaginacionLogica
{
    public static class QueryableExtensions
    {       
        public static IQueryable<T> Paginar<T>(this IQueryable<T> queryable, Paginacion paginacion)
        {
          
            int skip = (paginacion.Pagina - 1) * paginacion.CantidadAMostrar;

     
            return queryable.Skip(skip).Take(paginacion.CantidadAMostrar);
        }
        /*
          * Ejemplo práctico:
          * 
          * Supongamos que tenemos 16 elementos y queremos mostrar 3 elementos por página. 
          * Vamos a ver cómo funciona el método `PaginarLista` para diferentes páginas:
          * 
          * Página 1:
          * - Cálculo de `skip`:
          *   - Fórmula: `int skip = (paginacion.Pagina - 1) * paginacion.CantidadAMostrar`
          *   - Valores: `paginacion.Pagina = 1`, `paginacion.CantidadAMostrar = 3`
          *   - Cálculo: `(1 - 1) * 3 = 0`
          *   - Resultado: `skip = 0`
          * 
          * - Método `Skip` y `Take`:
          *   - `Skip(0)`: No omite ningún elemento.
          *   - `Take(3)`: Toma los primeros 3 elementos.
          *   - Resultado: Devuelve los elementos 1, 2 y 3.
          * 
          * Página 2:
          * - Cálculo de `skip`:
          *   - Fórmula: `int skip = (paginacion.Pagina - 1) * paginacion.CantidadAMostrar`
          *   - Valores: `paginacion.Pagina = 2`, `paginacion.CantidadAMostrar = 3`
          *   - Cálculo: `(2 - 1) * 3 = 1 * 3 = 3`
          *   - Resultado: `skip = 3`
          * 
          * - Método `Skip` y `Take`:
          *   - `Skip(3)`: Omite los primeros 3 elementos.
          *   - `Take(3)`: Toma los siguientes 3 elementos.
          *   - Resultado: Devuelve los elementos 4, 5 y 6.
          * 
          * Página 3:
          * - Cálculo de `skip`:
          *   - Fórmula: `int skip = (paginacion.Pagina - 1) * paginacion.CantidadAMostrar`
          *   - Valores: `paginacion.Pagina = 3`, `paginacion.CantidadAMostrar = 3`
          *   - Cálculo: `(3 - 1) * 3 = 2 * 3 = 6`
          *   - Resultado: `skip = 6`
          * 
          * - Método `Skip` y `Take`:
          *   - `Skip(6)`: Omite los primeros 6 elementos.
          *   - `Take(3)`: Toma los siguientes 3 elementos.
          *   - Resultado: Devuelve los elementos 7, 8 y 9.
          * 
          * Resumen:
          * - `Skip`: Omite los primeros `skip` elementos en la colección. Para la página 1, no omite elementos (es `0`). Para la página 2, omite los primeros 3 elementos, y para la página 3, omite los primeros 6 elementos.
          * - `Take`: Toma la cantidad de elementos especificada por `paginacion.CantidadAMostrar`, que corresponde al número de elementos que deseas mostrar en la página actual.
          * 
          * Cada página muestra los elementos correctos basándose en el cálculo de cuántos elementos se deben omitir y cuántos deben ser tomados.
          */
        public static IEnumerable<T> PaginarLista<T>(this IEnumerable<T> enumerable, Paginacion paginacion)
        {

            int skip = (paginacion.Pagina - 1) * paginacion.CantidadAMostrar;
            return enumerable.Skip(skip).Take(paginacion.CantidadAMostrar);
        }

    }
}
