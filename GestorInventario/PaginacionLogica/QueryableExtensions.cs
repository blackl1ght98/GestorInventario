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
        Esta clase  cuenta con las caracteristicas siguientes:
        Lo primero que hacemos es establecer que tipo de dato va a manejar nuestro metodo Paginar, como 
        nuestro metodo es una consulta a base de datos lo ponemos que el tipo de dato a manejar va ha ser 
        IQueryable<T> la <T> al final de este tipo de dato quiere decir que  es un parámetro de tipo que 
        permite que nuestro método funcione con cualquier tipo de dato que cumpla con las restricciones de `IQueryable<T>,
        para decir tambien que nuestro metodo va a manejar cualquier tipo de dato se le pone este marcador<T> 
        a  Paginar<T>, y como lo que queremos es que cuando una varible sea del tipo IQueryable<T> y podamos 
        acceder a este metodo Paginar<T>, para que esto ocurra le tenemos que decir a .NET que Paginar<T> es
        una extension de IQueryable para conseguir esto en la parte de los parametros tenemos que poner esto
        Paginar<T>(this IQueryable<T> queryable) con esto ya le estamos diciendo a .NET que Paginar va a extender 
        de IQueryable<T> o lo que es lo mismo IQueryable<T> va a contar con un nuevo metodo que es Paginar<T>.
        Al hacer que nuestros metodos formen parte de las clases de .NET se le llama metodo de extension porque
        "agrega" una funcion nueva a metodos ya existentes. Para acceder a nuestro metodo se hace de esta manera:
        Declaramos nuestra variable y a esta variable se le asigna una consulta a base de datos  esto esta devolviendo
        un este tipo de dato IncludableQueryable<Usuario, Role>
        var queryable = _context.Usuarios.Include(x => x.IdRolNavigation);
        Para acceder a nuestro metodo paginar debemos convertir la informacion que devuelve esa variable a tipo IQueryable<T>
        esto se hace asi:
        var queryable = _context.Usuarios.Include(x => x.IdRolNavigation).AsQueryable();
        el .AsQueryable() convierte lo que devuelve una consulta a tipo IQueryable<T> como ya esta variable es de tipo
        IQueryable<T> podemos acceder a Paginar<T> de esta manera:
       var usuarios = queryable.Paginar(paginacion).ToList();
        como vemos con solo poner queryable. nos deja acceder a todos los metodos que tenga IQueryable. Y como nuestro
        metodo a pasado a formar parte de IQueryable pues podemos acceder y operar con el.
        De esta manera, `Paginar<T>` se convierte en una parte integral de `IQueryable<T>`, permitiéndonos paginar 
        fácilmente los resultados de nuestras consultas
*/
        /*Un metodo de extension es un metodo que extiende la funcionalidad de una clase, se define como un metodo
        estatico que tiene como primer parametro la propia clase, en este caso extiende la funcionalidad de IQueryable<T>
             var queryable = _context.Usuarios.Include(x => x.IdRolNavigation).AsQueryable();

         */
        public static IQueryable<T> Paginar<T>(this IQueryable<T> queryable, Paginacion paginacion)
        {
            // Calculamos cuántos elementos debemos omitir en la consulta.
            // Esto se hace multiplicando el número de la página por la cantidad de elementos que queremos mostrar en cada página.
            // Luego restamos la cantidad de elementos a mostrar porque las páginas se cuentan desde 1 y no desde 0.
            //Detecta en que pagina estamos en este caso en la pagina 1 asi que paginacion.Pagina es 1-1=0 y como la cantidad
            //de registros a mostrar es 2*0=0 no hay cambio de pagina.
            //Si nos movemos a la pagina 2, paginacion.Pagina vale 2-1=1 y lo multiplica por la cantidad de registros a mostrar
            //1*2=2 
            /*
    En la página 1, Skip(0) omite 0 registros y Take(2) toma los primeros 2 registros. Por lo tanto, se muestran 
    los registros 1 y 2.

    En la página 2, Skip(2) omite los primeros 2 registros y Take(2) toma los siguientes 2 registros. 
    Por lo tanto, se muestran los registros 3 y 4.

    En la página 3, Skip(4) omite los primeros 4 registros y Take(2) toma los siguientes 2 registros. 
    Por lo tanto, se muestran los registros 5 y 6.

             */
            int skip = (paginacion.Pagina - 1) * paginacion.CantidadAMostrar;

            // Usamos el método Skip para omitir los elementos que ya hemos contado.
            // Luego usamos Take para seleccionar la cantidad de elementos que queremos mostrar.
            // De esta manera, obtenemos una "página" de resultados de nuestra consulta.
            //Quita los 2 registros de la primera pagina y toma lo de la siguiente
            return queryable.Skip(skip).Take(paginacion.CantidadAMostrar);
        }

    }
}
