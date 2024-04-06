namespace GestorInventario.PaginacionLogica
{
    public static class QueryableExtensions
    {
        /*Métodos de extensión: Los métodos de extensión te permiten “agregar” métodos a tipos existentes 
         * sin crear un nuevo tipo derivado, recompilar o modificar de alguna otra manera el tipo original.
         * Son métodos estáticos, pero se llaman como si fueran métodos de instancia en el tipo extendido.
         * En este caso Paginar pasaria a formar parte de IQueryable
         */
        /*IQueryable: IQueryable y IQueryable<T> son abstracciones que encapsulan las expresiones LINQ; 
         * estas expresiones son utilizadas por un proveedor LINQ - como el utilizado por Entity Framework - 
         * para convertir estas expresiones en SQL que será enviado a la base de datos, IQueryable es útil cuando 
         * se trabaja con orígenes de datos remotos como una base de datos, ya que permite construir consultas de 
         * manera eficiente. En pocas palabras admite cualquier consulta sobre una tabla*/
        /*HttpContext: HttpContext encapsula toda la información sobre una solicitud HTTP individual y su respuesta. 
         * Una instancia de HttpContext se inicializa cuando se recibe una solicitud HTTP*/
        //--------------------------------------------------------------------------------------------------
        /*Para hacer un metodo de extension es decir que este dentro de una de las clases de .NET tenemos que hacer
         que esa clase sea estatica(public static class QueryableExtensions), si la clase es estatica el metodo tambien
        lo tiene que ser que en este caso es Paginar.
        Esta clase Paginar cuenta con las caracteristicas siguientes:
        Admite cualquier tipo de dato debido a que se le pone <T>.
        Aqui lo que hacemos es establecer que tipo de dato va a manejar en este caso es IQueryable para que
        el metodo Paginar use IQueryable a este es necesario marcarlo con IQueryable<T> aqui decimos que el metodo
        estatico a crear va a admitir algo de tipo IQueryable<T> y para asignar IQueryable<T> al metodo Paginar
        este metodo tambien tiene que tener el marcador Paginar<T> aqui  decimos que nuestro metodo va ha admitir cualquier
        tipo de dato que sea IQueryable<T>.
        Para poder "llamar" a Paginar desde otra clase usando una variable le tenemos que decir a .NET que paginar es
        un metodo de extension de IQueryable para ello se pone de la manera siquiente:
        public static IQueryable<T> Paginar<T>(this IQueryable<T> queryable) con el this le decimos que el metodo
        Paginar extiende de IQueryable<T> de esta manera cuando un dato sea de tipo IQueryable se puede llamar a este metodo.
        Ejemplo  para entenderlo:
        Aqui hacemos una consulta a la base de datos obteniendo todos los usuarios con sus roles, pero tiene una caracteristica
        le decimos a .NET que esta consulta es de tipo IQueryable<T> y como se hace pues cuando terminas de poner la consulta
        al final lo pones como .AsQueryable(). con esto convierte el tipo de dato que devuelve sin esto seria una consulta a 
        base de datos sencilla que se puede iterar como una lista, y sin ese .AsQueryable() seria de tipo IEnumerable que maneja
        listas y arrays.
          var queryable = _context.Usuarios.Include(x => x.IdRolNavigation).AsQueryable();
        aqui vemos un ejemplo claro del gran uso que se le puede dar a un metodo de extension, HttpContext inicialmente no tiene
        el metodo al que se esta llamando pero como se a creado un metodo de extencion ahora si lo tiene 
           await HttpContext.InsertarParametrosPaginacionRespuesta(queryable, paginacion.CantidadAMostrar);
        aqui igual como la variable queryable se ha convertido a un tipo de dato IQueryable<T>, llamando a la variable
        queryable nos permite acceder al metodo paginar 
            var usuarios = queryable.Paginar(paginacion).ToList();
*/
        public static IQueryable<T> Paginar<T>(this IQueryable<T> queryable, Paginacion paginacion)
        {
            // Calculamos cuántos elementos debemos omitir en la consulta.
            // Esto se hace multiplicando el número de la página por la cantidad de elementos que queremos mostrar en cada página.
            // Luego restamos la cantidad de elementos a mostrar porque las páginas se cuentan desde 1 y no desde 0.
            int skip = (paginacion.Pagina - 1) * paginacion.CantidadAMostrar;

            // Usamos el método Skip para omitir los elementos que ya hemos contado.
            // Luego usamos Take para seleccionar la cantidad de elementos que queremos mostrar.
            // De esta manera, obtenemos una "página" de resultados de nuestra consulta.
            return queryable.Skip(skip).Take(paginacion.CantidadAMostrar);
        }

    }
}
