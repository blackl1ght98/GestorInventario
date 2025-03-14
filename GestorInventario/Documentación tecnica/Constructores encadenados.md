
 **Encadenación de Constructores en C#**

La encadenación de constructores es una técnica en la **programación orientada a objetos** donde un constructor en una clase llama a otro constructor 
en la misma clase para inicializar la instancia de la clase. En C#, esto se hace utilizando la palabra clave `this` seguida de los argumentos del 
constructor que se quiere invocar. Esto permite reutilizar el código de inicialización en varios constructores y proporciona una forma de tener 
múltiples constructores con diferentes números de parámetros.

**Ejemplo:**

public class Ejemplo
{
    private int valor1;
    private int valor2;

**Este es el constructor base o constructor padre:**
    public Ejemplo(int valor1, int valor2)
    {
        this.valor1 = valor1;
        this.valor2 = valor2;
    }

**Ejemplo practico del uso de contructor encadenado:**
Constructor que llama al **constructor base** este contructor tiene  un parametro llamado **int valor1** y se concatena al contructor base usando la palabra clave `this` en la parte del
`this` recibe el **valor1** y un valor predeterminado para el **valor2**
    public Ejemplo(int valor1) : this(valor1, 0) { }
}

**Requisitos para Realizar la Encadenación de Constructores:**
  - **Tener unas propiedades**: Definir las propiedades necesarias en la clase.
  - **Realizar el constructor base o padre**: Crear el constructor principal que inicializa todas las propiedades necesarias.
  - **Realizar la encadenación de constructores**: Crear constructores adicionales que llamen al constructor base utilizando la palabra  clave `this`.

**Ejemplo Aplicado a `PaginasModel`:**

public class PaginasModel
{
**Propiedades que va a tener el contructor:**

public string Texto { get; set; }
public int Pagina { get; set; }
public bool Habilitada { get; set; } = true;
public bool Activa { get; set; } = false;

**Constructor base:**
En el contructor **base** inicializamos las propiedades anteriores.

public PaginasModel(int pagina, bool habilitada, string texto)
{
  Pagina = pagina;
  Habilitada = habilitada;
  Texto = texto;
}

Contructor encadenado que llama al **contructor base** este constructor recibe 2 parametros, ** int pagina y bool habilitada** y para que esto se considere que se esta haciendo uso
del encadenamiento de constructores hacemos uso de la palabra clave `this` y en esta parte llamamos a **todas las propiedades** del **contructor base**. Este constructor recibe el 
numero de pagina y indica si esa pagina esta o no habilitada. Luego en la parte del `this` recibe el numero de pagina, despues de si esta habilitada o no y por ultimo el numero de pagina
pero el numero en formato texto.
public PaginasModel(int pagina, bool habilitada) : this(pagina, habilitada, pagina.ToString()) { }

Otro constructor encadenado que llama al constructor anterior, este constructor encadenado primero recibe un parametro **int pagina** que esto es el numero de pagina, luego en la parte del 
`this` se le pasa la propiedad que almacena el numero de pagina y se establece la propiedad **habilitada** en true
public PaginasModel(int pagina) : this(pagina, true) { }
}

En el ejemplo que hemos visto hay una propiedad que no se esta usando que esta propiedad es **Activa** que no se use una propiedad no significa que esa propiedad no se vaya ha usar deja la 
posibilidad para una futura ampliacion y manipular de si la pagina esta activa o no.