namespace GestorInventario.PaginacionLogica
{
    public class PaginasModel
    {
        /*La encadenación de constructores es una técnica en la programación orientada a 
         * objetos donde un constructor en una clase llama a otro constructor en la misma clase 
         * para inicializar la instancia de la clase.
          En C#, esto se hace utilizando la palabra clave this seguida de los argumentos del constructor 
        que se quiere invocar. Esto permite reutilizar el código de inicialización en varios constructores 
        y proporciona una forma de tener múltiples constructores con diferentes números de parámetros.
         */
        /*public class Ejemplo
{
    private int valor1;
    private int valor2;

    // Constructor base que inicializa todas las propiedades
    public Ejemplo(int valor1, int valor2)
    {
        this.valor1 = valor1;
        this.valor2 = valor2;
    }

    // Constructor que llama al constructor base con 'valor1' y un valor predeterminado para 'valor2'
    public Ejemplo(int valor1) : this(valor1, 0) { }
}
*/
        public string Texto { get; set; }
        public int Pagina { get; set; }
        //La propiedad habilitada se refiere a la pagina de si esta o no habilitada
        public bool Habilitada { get; set; } = true;
        public bool Activa { get; set; } = false;
        /*Para realizar el encadenamiento de constructores se necesitan cumplir unos requisitos:
         * 1.Tener unas propiedades.
         * 2.Realizar el constructor padre.
         * 3.Una vez que se tiene el constructor padre se puede realizar la encadenacion de constructores:
         * public PaginasModel(int pagina, bool habilitada) : this(pagina, habilitada, pagina.ToString()) { }
         * aqui tenemos un ejemplo de esto: en la primera parte del constructor tenemos 2 propiedades
         * que estan en el constructor padre y para realizar la encadenacion de constructores se pone 
         * : this(pagina, habilitada, pagina.ToString()) los parametros que pongas tienen que tener el mismo
         *  nombre que proporciona el constructor padre y el mismo tipo, el motivo por el cual se pone 
         *  pagina.ToString() es que el ultimo parametro del constructor padre recibe texto y pagina es un numero.
         *    IMPORTANTE: la encadenacion de constructores
         * no puede tener mas parametros de los que tenga el constructor padre, esto se aplica a la parte del this() que si
         * el padre tiene 3 parametros tu en la parte del this() no puedes pasar 4. Dicho esto actua como si fuese
         * un constructor unico pero en realidad son como 2 constructores la primera parte del constructor 
         * puede tener mas parametros que los que ofrece el padre pero la parte del this() tiene que ser si o si el 
         * mismo numero de parametros que el padre tenga y de el mismo tipo
         
       

         */
        /*public class Ejemplo
            {
            private int valor1;
            private int valor2;

    // Constructor base que inicializa todas las propiedades
    public Ejemplo(int valor1, int valor2)
    {
        this.valor1 = valor1;
        this.valor2 = valor2;
    }

    // Este constructor tiene un parámetro adicional, pero solo pasa 'valor1' y 'valor2' a 'this()'
    public Ejemplo(int valor1, int valor2, int valor3) : this(valor1, valor2)
    {
        // Aquí puedes hacer algo con 'valor3'
    }
}

         */
        public PaginasModel(int pagina, bool habilitada, string texto)
        {
            Pagina = pagina;
            Habilitada = habilitada;
            Texto = texto;
        }
        /*

            public PaginasModel(int pagina, bool habilitada) : this(pagina, habilitada, pagina.ToString()) { }
            Este constructor toma dos argumentos: pagina y habilitada. Luego, llama al constructor que toma tres 
            argumentos (pagina, habilitada y texto) utilizando la palabra clave this. El tercer argumento, texto, se 
            establece como la representación en cadena del número de página (pagina.ToString()). Por lo tanto, si 
            creas una instancia con este constructor, el texto se establecerá automáticamente como el número de página.     
         */
        //Contructor que llama al constructor padre la primera parte del constructor solo se le pasan 2 parametro
        //pagina y habilitada y este constructor de dos parametros se encadena al constructor padre que recibe 3 
        //parametros el ultimo parametro el constructor padre marca que tiene que ser string pero pagina es int
        //este es el motivo por el cual a pagina se le pone ToString()



        public PaginasModel(int pagina, bool habilitada) : this(pagina, habilitada, pagina.ToString()) { }
        /*

            public PaginasModel(int pagina) : this(pagina, true) { }
            Este constructor solo toma un argumento: pagina. Luego, llama al constructor que toma dos argumentos 
            (pagina y habilitada) utilizando la palabra clave this. El segundo argumento, habilitada, se establece 
            como true. Por lo tanto, si creas una instancia con este constructor, habilitada se establecerá automáticamente
            como true y el texto se establecerá automáticamente como el número de página. 
        */
      //Constructor que llama al constructor de 2 parametro pagina y habilitada y este constructor de 1 parametro 
      //se encadena al constructor que recibe 2 parametros se establece pagina que es un numero y si esta habilitada que 
      //en este caso es true
        public PaginasModel(int pagina) : this(pagina, true) { }

    }
}/*
  public class Persona
{
    public string Nombre { get; set; }
    public int Edad { get; set; }
    public string Ciudad { get; set; }

    // Constructor base que inicializa todas las propiedades
    public Persona(string nombre, int edad, string ciudad)
    {
        Nombre = nombre;
        Edad = edad;
        Ciudad = ciudad;
    }

    // Constructor que llama al constructor base con 'nombre', 'edad' y un valor predeterminado para 'ciudad'
    public Persona(string nombre, int edad) : this(nombre, edad, "Desconocida") { }

    // Constructor que llama al constructor que toma dos argumentos con 'nombre' y un valor predeterminado para 'edad'
    public Persona(string nombre) : this(nombre, 0) { }
}
En este ejemplo, si creas una instancia de Persona utilizando el tercer constructor y proporcionas solo el nombre, 
la edad se inicializará automáticamente a 0 y la ciudad a "Desconocida" gracias al encadenamiento de constructores.
  
  */