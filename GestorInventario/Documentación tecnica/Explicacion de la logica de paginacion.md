## Documentacion del metodo GenerarListaPaginas

### Descripción
El método `GenerarListaPaginas` es responsable de generar una lista de páginas para la paginación de un 
conjunto de datos. Este método toma como entrada el número total de elementos, el número de elementos por página y el número de la página actual. 
Devuelve una lista de números de página que se utilizarán para la paginación.
### Parámetros
- `Paginacion paginacion` :clase que contiene la configuracion de la paginacion.
### Metodo
```csharp
 public List<PaginasModel> GenerarListaPaginas(Paginacion paginacion)
        {
            var paginas = new List<PaginasModel>();
            var paginaAnterior = paginacion.PaginaActual > 1 ? paginacion.PaginaActual - 1 : 1;
            paginas.Add(new PaginasModel(paginaAnterior, paginacion.PaginaActual != 1, "Anterior"));

           

            for (int i = 1; i <= paginacion.TotalPaginas; i++)
            {
                if (i >= paginacion.PaginaActual - paginacion.Radio && i <= paginacion.PaginaActual + paginacion.Radio)
                {
                    paginas.Add(new PaginasModel(i) { Activa = paginacion.PaginaActual == i });
                }
            }
            var paginaSiguiente = paginacion.PaginaActual < paginacion.TotalPaginas ? paginacion.PaginaActual + 1 : paginacion.TotalPaginas;
            paginas.Add(new PaginasModel(paginaSiguiente, paginacion.PaginaActual != paginacion.TotalPaginas, "Siguiente"));
            return paginas;
        }
```
### PaginasModel
Clase usada para representar una pagina en la paginacion, contiene la configuracion necesaria de como se va a mostrar la pagina.
```csharp
 public class PaginasModel
    {
        public string Texto { get; set; }
        public int Pagina { get; set; }
        public bool Habilitada { get; set; } = true;//Indica si esta o no habilitada la pagina
        public bool Activa { get; set; } = false;
        public PaginasModel(int pagina, bool habilitada, string texto)
        {
            Pagina = pagina;
            Habilitada = habilitada;
            Texto = texto;
        }
        public PaginasModel(int pagina, bool habilitada) : this(pagina, habilitada, pagina.ToString()) { }
       
        public PaginasModel(int pagina) : this(pagina, true) { }

    }
```
### Explicacion de cada linea de codigo del metodo Generar ListaPaginas
Esta linea inicializa una nueva lista de objetos `PaginasModel` que se utilizará para almacenar la información de las páginas generadas:
```csharp
var paginas = new List<PaginasModel>();
```
Esta linea realiza el calcula para determinar la página anterior a la actual. Si la página actual es mayor que 1, se resta 1 de la página actual; 
de lo contrario, se establece en 1:
```csharp
 var paginaAnterior = paginacion.PaginaActual > 1 ? paginacion.PaginaActual - 1 : 1;
 ```
Esta linea agrega la configuracion de la pagina anterior a la lista de paginas, si la pagina actual no es la primera, 
quiere decir que la pagina anterior es habilitada:
```csharp
 paginas.Add(new PaginasModel(paginaAnterior, paginacion.PaginaActual != 1, "Anterior"));
 ```
A continuacion vamos a explicar el bucle for que se encarga de agregar las paginas a la lista de paginas:
```csharp
for (int i = 1; i <= paginacion.TotalPaginas; i++)
            {
                if (i >= paginacion.PaginaActual - paginacion.Radio && i <= paginacion.PaginaActual + paginacion.Radio)
                {
                    paginas.Add(new PaginasModel(i) { Activa = paginacion.PaginaActual == i });
                }
            }
```
- `for (int i = 1; i <= paginacion.TotalPaginas; i++)` : Este bucle itera desde 1 hasta el número total de páginas.
- `if (i >= paginacion.PaginaActual - paginacion.Radio && i <= paginacion.PaginaActual + paginacion.Radio)` : Esta condición verifica si 
el número de página actual `i` está dentro del rango definido por la página actual menos el radio y la página actual más el radio. Esto significa 
que solo se agregarán las páginas que estén dentro de este rango.
- `paginas.Add(new PaginasModel(i) { Activa = paginacion.PaginaActual == i });` : Si la condición anterior es verdadera, se agrega un nuevo objeto `PaginasModel`
A continuacion se va a explicar la siguiente linea de codigo que se encarga de agregar la pagina siguiente a la lista de paginas:
```csharp
 var paginaSiguiente = paginacion.PaginaActual < paginacion.TotalPaginas ? paginacion.PaginaActual + 1 : paginacion.TotalPaginas;
            paginas.Add(new PaginasModel(paginaSiguiente, paginacion.PaginaActual != paginacion.TotalPaginas, "Siguiente"));
            return paginas;
```
- `var paginaSiguiente = paginacion.PaginaActual < paginacion.TotalPaginas ? paginacion.PaginaActual + 1 : paginacion.TotalPaginas;` : Esta línea determina la página siguiente.
- `paginas.Add(new PaginasModel(paginaSiguiente, paginacion.PaginaActual != paginacion.TotalPaginas, "Siguiente"));` : Esta línea agrega la página siguiente a la lista de páginas.
- `return paginas;` : Finalmente, se devuelve la lista de páginas generadas.
## Explicacion de la clase PaginasModel
La clase `PaginasModel` es una representación de una página en la paginación. Contiene propiedades que indican el número de página, si está habilitada o no,
y si es la página activa.
### Propiedades
- `Texto` : Representa el texto que se mostrará para la página (por ejemplo, el número de página o "Anterior"/"Siguiente").
- `Pagina` : Representa el número de la página.
- `Habilitada` : Indica si la página está habilitada o no.
- `Activa` : Indica si la página es la página activa en la paginación.
### Constructores
- `public PaginasModel(int pagina, bool habilitada, string texto)` : Constructor que inicializa las propiedades de la página, este es el constructor base.
- `public PaginasModel(int pagina, bool habilitada) : this(pagina, habilitada, pagina.ToString())` : Constructor que extiende del constructor base y establece el texto como el número de página, la extension
- que se hace es convertir a cadena de texto el numero de pagina
- `public PaginasModel(int pagina) : this(pagina, true) { }` : Constructor que establece la página como habilitada por defecto, este constructor extiende
- del constructor anterior extendido y solo toma el numero de la pagina y luego como se ha dicho habilita la pagina.
### Clase paginacion
```csharp
 public class Paginacion
    {
        public int Pagina { get; set; } = 1;
      
        public int CantidadAMostrar { get; set; } = 6;//Cantidad de registros por pagina
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int Radio { get; set; } = 3;
    }
```
### Explicacion de la clase paginacion
La clase `Paginacion` es una representación de la configuración de paginación. Contiene propiedades que indican la 
página actual, la cantidad de elementos a mostrar por página, el total de páginas y el radio de páginas a mostrar 
alrededor de la página actual.
### Propiedades
- `Pagina` : Representa la página actual.
- `CantidadAMostrar` : Representa la cantidad de elementos a mostrar por página.
- `TotalPaginas` : Representa el total de páginas.
- `PaginaActual` : Representa la página actual.
- `Radio` : Representa el número de páginas a mostrar alrededor de la página actual.
