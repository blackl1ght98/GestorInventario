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
        /*Para hacer un metodo de extension es decir que este dentro de una de las clases de .NET tenemos que hacer
          que esa clase sea estatica(public static class HttpContextExtensions), si la clase es estatica el metodo tambien
          lo tiene que ser que en este caso es InsertarParametrosPaginacionRespuesta.
          Esta clase  cuenta con las caracteristicas siguientes:
          Lo primero que hacemos es establecer que va ha hacer nuestro metodo de extension que en este caso va a realizar una
         operacion  de tipo Task y nuestro metodo InsertarParametrosPaginacionRespuesta<T> gracias al marcador
        <T> va a poder manejar cualquier tipo de dato ya que <T> es un tipo de dato generico de c# para ajustar que tipo
        de dato va a manejar ponemos esto InsertarParametrosPaginacionRespuesta<T>(this HttpContext context, IQueryable<T> queryable ) 
       esto quiere decir que nuestro método puede operar sobre cualquier tipo de dato que pueda ser consultado (IQueryable<T>).
        Este metodo extiende de la clase HttpContext,InsertarParametrosPaginacionRespuesta<T>(this HttpContext context)
         cuando tu haces  un metodo de extension, gracias al this mas nombre de clase de .NET extiendes la funcionalidad 
        de esa clase en este caso agregamos una funcionalidad mas a la clase HttpContext de .NET esta clase es la encargada de poder manejar las
        operaciones http de una peticion.
        Despues de esto tenemos el tipo de dato que va a admitir nuestro metodo para
        ello ponemos IQueryable<T> queryable esto va a ha aceptar algo de tipo IQueryable. 
        Al metodo estatico que hemos creado
        le hemos puesto async porque vamos a manejar operaciones asincronas. Para llamar a este metodo ponemos:
        Aqui gracias al .AsQueryable() en la consulta lo convertirmos a algo de tipo IQueryable<T>
        var queryable = _context.Usuarios.Include(x => x.IdRolNavigation).AsQueryable();
        y como nuestro metodo es un metodo de extension de HttpContext poniendo HttpContext. podemos acceder a nuestro metodo
        que este metodo requiere dos parametro uno es la fuente de informacion, y el otro la cantidad de datos a mostrar
        por cada pagina. Para ello nuestro metodo cuenta cuantos registros hay en total y luego le asigna la cantidad de informacion
        que tiene que mostrar cada pagina, el numero de paginas lo almacenamos en la cabecera de la peticion http
        
        await HttpContext.InsertarParametrosPaginacionRespuesta(queryable, paginacion.CantidadAMostrar);


*/
        public static async Task InsertarParametrosPaginacionRespuesta<T>(this HttpContext context, IQueryable<T> queryable, int cantidadRegistrosAMostrar)
        {
            //Si lo que vine en la peticion http viene vacio retorna esta excepcion
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            //Cuenta cuantos registros hay en base de datos actualmente hay 6 registros
            double conteo = await queryable.CountAsync();
            /*La función Math.Ceiling en .NET redondea un número hacia arriba al entero más cercano. 
             * Esta función es útil cuando necesitas redondear un número decimal hacia el entero más 
             * cercano que es mayor o igual al número dado.

            Por ejemplo, si tienes un número 2.4, Math.Ceiling(2.4) devolverá 3 porque 3 es el entero más pequeño que 
            es mayor o igual a 2.4. Si tienes un número -2.4, Math.Ceiling(-2.4) devolverá -2 porque -2 es 
            el entero más pequeño que es mayor o igual a -2.4.
             */
            /*Toma esos 6 registros y los divide entre la cantidad de registros que hemos decidido mostrar 6/2=3*/
            double totalPaginas = Math.Ceiling(conteo / cantidadRegistrosAMostrar);
            //Agrega a la cabecera de la peticion http el numero total de paginas en este caso la peticion almacena 3 que
            //es la cantidad de paginas que va a ver el usuario
            context.Response.Headers.Add("totalPaginas", totalPaginas.ToString());
        }
    }
}
