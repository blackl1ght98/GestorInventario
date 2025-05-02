### Documentación sobre el Nuevo Switch en C#

En programación, una expresión es cualquier combinación de valores, variables, operadores y funciones que se evalúan para producir un nuevo valor.
C# introdujo una característica denominada "switch de expresión" en la versión 8.0. Esta nueva forma de switch permite evaluar una expresión y 
devolver un valor basado en patrones.

# Switch de Expresión

El switch de expresión funciona evaluando una variable y devolviendo un valor en función de la coincidencia con los patrones definidos. 
La sintaxis es más concisa y directa en comparación con el switch de control de flujo tradicional.

 Ejemplo:
```csharp
int day = 3;

string dayName = day switch
{
    1 => "Monday", 
    2 => "Tuesday",
    3 => "Wednesday",
    _ => "Unknown" 
};
````
En este ejemplo, `day` se evalúa y se asigna a `dayName` el valor correspondiente según el patrón que coincide. El `_` se utiliza como un comodín para
indicar cualquier otro valor que no coincida con los casos anteriores. En este caso, si `day` es 3, `dayName` será "Wednesday".

## Diferencias entre Switch de Control de Flujo y Switch de Expresión

# Switch de Control de Flujo:

El switch de control de flujo es una declaración que no devuelve un valor y se utiliza principalmente para ejecutar diferentes bloques de código 
en función de una condición.

Ejemplo:
````csharp
int day = 3;
string dayName;
switch (day)
{
    case 1:
        dayName = "Monday";
        break;
    case 2:
        dayName = "Tuesday";
        break;
    case 3:
        dayName = "Wednesday";
        break;
    default:
        dayName = "Unknown";
        break;
}
````
En este ejemplo, el switch de control de flujo evalúa `day` y ejecuta el bloque de código correspondiente.
El valor de `dayName` se asigna dentro de cada bloque de caso, y se utiliza la instrucción `break` para salir del switch después de ejecutar el bloque correspondiente.
## Ventajas del Switch de Expresión
1. **Concisión**: La sintaxis es más corta y clara, lo que facilita la lectura y el mantenimiento del código.
1. **Inmutabilidad**: El switch de expresión devuelve un valor directamente, lo que promueve un estilo de programación más funcional.
1. **Patrones Avanzados**: Permite el uso de patrones más complejos, como patrones de tipo y de propiedad, lo que proporciona una mayor flexibilidad en la coincidencia de casos.
1. **Menos Errores**: Al no requerir la instrucción `break`, se reduce el riesgo de errores relacionados con la omisión de esta instrucción, lo que puede 
llevar a comportamientos inesperados en el switch de control de flujo.
Estas son solo algunas de las ventajas que ofrece el switch de expresión en comparación con el switch de control de flujo tradicional.

## Conclusión
El switch de expresión es una característica poderosa y flexible que mejora la legibilidad y la mantenibilidad del código en C#.
Es una excelente opción para evaluar expresiones y devolver valores basados en patrones, lo que lo convierte en una herramienta valiosa para los desarrolladores.
Al fin y al cabo, la elección entre el switch de control de flujo y el switch de expresión dependerá del contexto y de las necesidades específicas del código que 
estés escribiendo, ya que ambos logran el mismo objetivo pero de maneras diferentes.