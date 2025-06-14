
 ### Encadenaci�n de Constructores en C#
La encadenaci�n de constructores es una t�cnica en la **programaci�n orientada a objetos** donde un constructor en una clase llama a otro constructor 
en la misma clase para inicializar la instancia de la clase. En C#, esto se hace utilizando la palabra clave `this` seguida de los argumentos del 
constructor que se quiere invocar. Esto permite reutilizar el c�digo de inicializaci�n en varios constructores y proporciona una forma de tener 
m�ltiples constructores con diferentes n�meros de par�metros.

### Ejemplo:
```csharp
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


    public Ejemplo(int valor1) : this(valor1, 0) { }
}
````
### Explicacion del Ejemplo:
- **Constructor base**: `Ejemplo(int valor1, int valor2)` es el constructor principal que inicializa las propiedades `valor1` y `valor2`.
- **Constructor encadenado**: `Ejemplo(int valor1) : this(valor1, 0)` es un constructor adicional que llama al constructor base, pasando `valor1` y 
un valor predeterminado de `0` para `valor2`. Esto permite crear una instancia de la clase con solo un argumento, utilizando el constructor base para 
la inicializaci�n.
- **Uso de `this`**: La palabra clave `this` se utiliza para referirse al constructor base y pasar los argumentos necesarios. Esto evita la duplicaci�n de
codigo y asegura que todas las propiedades se inicialicen correctamente.
- **Ventajas**: La encadenaci�n de constructores mejora la legibilidad y el mantenimiento del c�digo, ya que permite reutilizar la l�gica de
- inicializaci�n y evita la duplicaci�n de c�digo. Adem�s, facilita la creaci�n de instancias de la clase con diferentes configuraciones sin
- tener que escribir m�ltiples constructores con l�gica similar.

### Requisitos para Realizar la Encadenaci�n de Constructores:
- **Definir una clase**: Crear una clase que contenga las propiedades y m�todos necesarios.
- **Definir un constructor base**: Crear un constructor que inicialice las propiedades de la clase.
- **Definir constructores adicionales**: Crear constructores adicionales que llamen al constructor base utilizando la palabra clave `this`.
- **Inicializar propiedades**: Asegurarse de que todas las propiedades necesarias se inicialicen correctamente en el constructor base.
- **Usar la palabra clave `this`**: Utilizar `this` para referirse al constructor base y pasar los argumentos necesarios.
- **Evitar duplicaci�n de c�digo**: Asegurarse de que la l�gica de inicializaci�n no se duplique en los constructores adicionales.
- **Probar la clase**: Crear instancias de la clase utilizando diferentes constructores para asegurarse de que se inicializan correctamente.


