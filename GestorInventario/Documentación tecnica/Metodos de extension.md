### Documentación sobre los metodos de extension:
 ## ¿Que es un metodo de extension?
 Un metodo de extension es un metodo que extiende la funcionalidad de una clase existente sin modificar su codigo fuente. Esto se logra creando un 
 metodo estatico dentro de una clase estatica, lo cual permite agregar nuevas funcionalidades a clases existentes sin necesidad de herencia o modificacion 
 del codigo original.
 
## Pasos para crear un metodo de extension:
1. Crear una clase estatica que contenga el metodo de extension.
1. Definir el metodo como estatico y especificar el tipo de objeto que se va a extender como primer parametro, precedido por la palabra clave `this`.
1. Implementar la logica del metodo de extension.
1. Usar el metodo de extension en el contexto de la clase extendida.
1. Compilar y usar el metodo de extension en el proyecto.
## Ejemplo de un metodo de extension:
```csharp
public static class HttpContextExtensions
{
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
```
## Explicacion del ejemplo:
### ¿Que es HttpContext?
`HttpContext` es una clase que representa el contexto HTTP de una solicitud en ASP.NET Core. Proporciona acceso a información sobre la solicitud y 
la respuesta, como los encabezados, los parámetros de la consulta y el cuerpo de la solicitud.
### ¿Que es IQueryable?
`IQueryable` es una interfaz que representa una consulta que puede ser ejecutada en una fuente de datos. Permite realizar consultas LINQ sobre colecciones 
de datos que pueden ser evaluadas de manera diferida, lo que significa que la consulta no se ejecuta hasta que se itera sobre los resultados.
### ¿Que es CountAsync?
`CountAsync` es un metodo asincrono que se utiliza para contar el numero de elementos en una coleccion de datos. En este caso, se utiliza para contar
el numero total de elementos en la consulta `queryable`.
### ¿Que es Math.Ceiling?
`Math.Ceiling` es un metodo que redondea un numero decimal hacia arriba al entero mas cercano. En este caso, se utiliza para calcular el total de paginas
redondeando hacia arriba el resultado de la division entre el conteo total de elementos y la cantidad de registros a mostrar por pagina.
### ¿Que es context.Response.Headers?
`context.Response.Headers` es una propiedad que permite acceder a los encabezados de la respuesta HTTP. En este caso, se utiliza para agregar 
un nuevo encabezado llamado "totalPaginas" que contiene el total de paginas calculado.
### ¿Que es el metodo Add?
`Add` es un metodo que se utiliza para agregar un nuevo encabezado a la coleccion de encabezados de la respuesta. En este caso, se utiliza para agregar
el encabezado "totalPaginas" con el valor del total de paginas calculado.



