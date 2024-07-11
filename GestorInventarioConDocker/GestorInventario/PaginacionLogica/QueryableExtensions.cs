namespace GestorInventario.PaginacionLogica
{
    public static class QueryableExtensions
    {



        
        //Para cualquier duda mirar documentos MetodosExtension.txt y glosario.txt
        public static IQueryable<T> Paginar<T>(this IQueryable<T> queryable, Paginacion paginacion)
        {
          
            int skip = (paginacion.Pagina - 1) * paginacion.CantidadAMostrar;

     
            return queryable.Skip(skip).Take(paginacion.CantidadAMostrar);
        }


    }
}
