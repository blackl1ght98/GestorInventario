using Microsoft.EntityFrameworkCore;

namespace GestorInventario.MetodosExtension
{
    public static class HttpContextExtensions
    {
        
        public static async Task TotalPaginas<T>(this HttpContext context, IQueryable<T> queryable, int cantidadRegistrosAMostrar)
        {
          
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

           //Numero total de registros de base de  datos
            double conteo = await queryable.CountAsync();

          //Math.Ceiling() metodo de c# para redondear el mas proximo pero hacia arriba.
          //por ejemplo tenemos 254 registros de base de datos y la cantidad de registros ha motrar es de 3 por pagina pues esto divide 254/3=84,666666666666666
          //pues esta funcion redondea y pone 85 paginas
            double totalPaginas = Math.Ceiling(conteo / cantidadRegistrosAMostrar);

            context.Response.Headers.Add("totalPaginas", totalPaginas.ToString());
        }
        public static void TotalPaginasLista<T>(this HttpContext context, IEnumerable<T> enumerable, int cantidadRegistrosAMostrar)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            double conteo = enumerable.Count();
            double totalPaginas = Math.Ceiling(conteo / cantidadRegistrosAMostrar);

            context.Response.Headers.Add("totalPaginas", totalPaginas.ToString());
        }

    }
}
