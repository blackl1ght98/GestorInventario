## Documentacion del metodo GenerarListaPaginas

### Descripci�n
El m�todo `GenerarListaPaginas` es responsable de generar una lista de p�ginas para la paginaci�n de un 
conjunto de datos. Este m�todo toma como entrada el n�mero total de elementos, el n�mero de elementos por p�gina y el n�mero de la p�gina actual. 
Devuelve una lista de n�meros de p�gina que se utilizar�n para la paginaci�n.
### Par�metros
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
Esta linea inicializa una nueva lista de objetos `PaginasModel` que se utilizar� para almacenar la informaci�n de las p�ginas generadas:
```csharp
var paginas = new List<PaginasModel>();
```
Esta linea realiza el calcula para determinar la p�gina anterior a la actual. Si la p�gina actual es mayor que 1, se resta 1 de la p�gina actual; 
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
- `for (int i = 1; i <= paginacion.TotalPaginas; i++)` : Este bucle itera desde 1 hasta el n�mero total de p�ginas.
- `if (i >= paginacion.PaginaActual - paginacion.Radio && i <= paginacion.PaginaActual + paginacion.Radio)` : Esta condici�n verifica si 
el n�mero de p�gina actual `i` est� dentro del rango definido por la p�gina actual menos el radio y la p�gina actual m�s el radio. Esto significa 
que solo se agregar�n las p�ginas que est�n dentro de este rango.
- `paginas.Add(new PaginasModel(i) { Activa = paginacion.PaginaActual == i });` : Si la condici�n anterior es verdadera, se agrega un nuevo objeto `PaginasModel`
A continuacion se va a explicar la siguiente linea de codigo que se encarga de agregar la pagina siguiente a la lista de paginas:
```csharp
 var paginaSiguiente = paginacion.PaginaActual < paginacion.TotalPaginas ? paginacion.PaginaActual + 1 : paginacion.TotalPaginas;
            paginas.Add(new PaginasModel(paginaSiguiente, paginacion.PaginaActual != paginacion.TotalPaginas, "Siguiente"));
            return paginas;
```
- `var paginaSiguiente = paginacion.PaginaActual < paginacion.TotalPaginas ? paginacion.PaginaActual + 1 : paginacion.TotalPaginas;` : Esta l�nea determina la p�gina siguiente.
- `paginas.Add(new PaginasModel(paginaSiguiente, paginacion.PaginaActual != paginacion.TotalPaginas, "Siguiente"));` : Esta l�nea agrega la p�gina siguiente a la lista de p�ginas.
- `return paginas;` : Finalmente, se devuelve la lista de p�ginas generadas.
## Explicacion de la clase PaginasModel
La clase `PaginasModel` es una representaci�n de una p�gina en la paginaci�n. Contiene propiedades que indican el n�mero de p�gina, si est� habilitada o no,
y si es la p�gina activa.
### Propiedades
- `Texto` : Representa el texto que se mostrar� para la p�gina (por ejemplo, el n�mero de p�gina o "Anterior"/"Siguiente").
- `Pagina` : Representa el n�mero de la p�gina.
- `Habilitada` : Indica si la p�gina est� habilitada o no.
- `Activa` : Indica si la p�gina es la p�gina activa en la paginaci�n.
### Constructores
- `public PaginasModel(int pagina, bool habilitada, string texto)` : Constructor que inicializa las propiedades de la p�gina, este es el constructor base.
- `public PaginasModel(int pagina, bool habilitada) : this(pagina, habilitada, pagina.ToString())` : Constructor que extiende del constructor base y establece el texto como el n�mero de p�gina, la extension
- que se hace es convertir a cadena de texto el numero de pagina
- `public PaginasModel(int pagina) : this(pagina, true) { }` : Constructor que establece la p�gina como habilitada por defecto, este constructor extiende
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
La clase `Paginacion` es una representaci�n de la configuraci�n de paginaci�n. Contiene propiedades que indican la 
p�gina actual, la cantidad de elementos a mostrar por p�gina, el total de p�ginas y el radio de p�ginas a mostrar 
alrededor de la p�gina actual.
### Propiedades
- `Pagina` : Representa la p�gina actual.
- `CantidadAMostrar` : Representa la cantidad de elementos a mostrar por p�gina.
- `TotalPaginas` : Representa el total de p�ginas.
- `PaginaActual` : Representa la p�gina actual.
- `Radio` : Representa el n�mero de p�ginas a mostrar alrededor de la p�gina actual.
