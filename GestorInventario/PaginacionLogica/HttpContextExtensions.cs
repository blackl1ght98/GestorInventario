using Microsoft.EntityFrameworkCore;

namespace GestorInventario.PaginacionLogica
{
    public static class HttpContextExtensions
    {
        /*Pasos para crear un metodo de extension:
         1. La clase donde se realice el metodo de extension y el metodo tienen que ser estaticos
         2. Para decirle a .NET que admita cualquier tipo de dato se le pone <T> (InsertarParametrosPaginacionRespuesta<T>)
         3. Decimos de que clase de .NET va a extender este se indica asi (this HttpContext context)
         4. Decimos a .NET que tipo de dato va a admitir en este caso (IQueryable<T> queryable)
         */
        /*¿Que hace Httpcontext?
         * Su principal funcion es poder manipular y poder hacer distintas tareas con las peticiones http,
         * su definicion puede ser la siguiente HttpContext es una clase de .NET con la cual puedes manipular
         * una peticion http antes de enviarse.
         */
        /*El metodo que estamos creando es una extension de HttpContext con lo cual poniendo esto 
         * await HttpContext.InsertarParametrosPaginacionRespuesta(queryable, paginacion.CantidadAMostrar); podemos
         * acceder al metodo.
         
         */
        public static async Task InsertarParametrosPaginacionRespuesta<T>(this HttpContext context, IQueryable<T> queryable, int cantidadRegistrosAMostrar)
        {
            //Si lo que vine en la peticion http viene vacio retorna esta excepcion
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            //Cuenta cuantos datos hay
            double conteo = await queryable.CountAsync();
            /*La función Math.Ceiling en .NET redondea un número hacia arriba al entero más cercano. 
             * Esta función es útil cuando necesitas redondear un número decimal hacia el entero más 
             * cercano que es mayor o igual al número dado.

            Por ejemplo, si tienes un número 2.4, Math.Ceiling(2.4) devolverá 3 porque 3 es el entero más pequeño que 
            es mayor o igual a 2.4. Si tienes un número -2.4, Math.Ceiling(-2.4) devolverá -2 porque -2 es 
            el entero más pequeño que es mayor o igual a -2.4.
             */
            double totalPaginas = Math.Ceiling(conteo / cantidadRegistrosAMostrar);
            //Agrega a la cabecera de la peticion http el numero total de paginas
            context.Response.Headers.Add("totalPaginas", totalPaginas.ToString());
        }
    }
}
