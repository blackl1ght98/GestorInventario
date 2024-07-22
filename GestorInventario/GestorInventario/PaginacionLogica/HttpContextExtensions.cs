using Microsoft.EntityFrameworkCore;

namespace GestorInventario.PaginacionLogica
{
    public static class HttpContextExtensions
    {
        //Si tienen dudas sobre como crear un metodo de extension o una explicacion de como es este esta en el documeto MetodosExtension.txt
        public static async Task InsertarParametrosPaginacionRespuesta<T>(this HttpContext context, IQueryable<T> queryable, int cantidadRegistrosAMostrar)
        {
          
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

           
            double conteo = await queryable.CountAsync();

         
            double totalPaginas = Math.Ceiling(conteo / cantidadRegistrosAMostrar);

            context.Response.Headers.Add("totalPaginas", totalPaginas.ToString());
        }

    }
}
