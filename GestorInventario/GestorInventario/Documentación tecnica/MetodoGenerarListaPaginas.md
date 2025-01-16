public List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual, int? radio = 3)
        {
**Inicializa una lista vacía que contendrá los objetos PaginasModel.**
  var paginas = new List<PaginasModel>();

  **Determina la página anterior a la actual:**
  Si la página actual es mayor que 1, la página anterior será una menos que la actual.
  De lo contrario, si estamos en la primera página, la página anterior también será 1.
  Esto se utiliza para determinar la navegación "Anterior".             
  **var paginaAnterior = (paginaActual > 1) ? paginaActual - 1 : 1;**

  **Agregar la primera pagina a la lista:**
  Agrega el objeto PaginasModel para la opción "Anterior":
  Este objeto permitirá al usuario navegar a la página anterior.
  La opción "Anterior" solo se activa si no estamos en la primera página.
 **paginas.Add(new PaginasModel(paginaAnterior, paginaActual != 1, "Anterior"));**

 **Explicación bucle for:**
  Agrega las páginas que deben ser visibles en la barra de paginación:
  Recorre desde la página 1 hasta la página final (totalPaginas).
  Solo se agregan las páginas que están dentro del rango del radio especificado.
  El radio define cuántas páginas a la izquierda y derecha de la actual deben mostrarse.

  for (int i = 1; i <= totalPaginas; i++)
   {
   **Explicacion de la condicion:**
   Verifica si la página actual está dentro del rango de visibilidad:
   Si la página (i) está dentro del rango definido por (paginaActual - radio) y (paginaActual + radio),
   entonces se agrega a la lista de páginas.
   Además, si (i) es igual a la página actual, esa página se marca como activa
   Por ejemplo, si paginaActual es 5 y el radio es 3, entonces se mostrarán las páginas de 2 a 8 (5 - 3 = 2 y 5 + 3 = 8).

   if (i >= paginaActual - radio && i <= paginaActual + radio)
      {
        paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
      }
   }
   **Ejemplo practico para comprender mejor el funcionamiento del bucle for:**
     
   **Supongamos que tenemos estos datos:** 
   *"totalPaginas= 8"*
   *"paginaActual=5"*
   *"radio= 3"* 
   **Veamos como se manejan estos datos:**
   Ahora que tenemos estos datos veamos como es el flujo de ejecución del bucle for
   for (int i = 1; i <= totalPaginas; i++){}
   Un bucle for tiene 3 parametros:
- Inicialización: int i =1: normalmente se inicializa en 1 pero admite otros valores
- Condición: i <= totalPaginas; : esta es la codición que va a seguir el bucle en este caso la condición es 1 <= 8;
- Incremento: i++; : es las veces que se va a incrementar en base a la condicion.
**Condición dentro del bucle for:**
 if (i >= paginaActual - radio && i <= paginaActual + radio)
      {
        paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
      }
Esta condición es la que va creando las páginas que ve el usuario y la va almacenando en la lista que hemos creado que la almacena la variable **paginas**.

**Acción dentro del condicional if:**
paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
Aqui mientras que **paginaActual** no sea igual a **i** esa pagina no estara activa.

**Explicación practica del bucle:**

for (int i = 1; i <= totalPaginas; i++)
{
 if (i >= paginaActual - radio && i <= paginaActual + radio)
      {
        paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
      }
}
**Primera iteración:**
for (int i = 1; i <= 8; i++)
{
 if (i >= 5 - 3 && i <= 5 + 3)
 if (i >= 2 && i <= 8)
  {
     paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
  }
}
Aqui vemos que la primera iteración no se cumple porque **if (i >= 2 && i <= 8)** i es 1 y aqui hace i es mayor o igual a 2 no es mayor y no es igual la segunda condición **i <= 8**
1<=8 aqui 1 es menor o igual que 8 si es menor por lo tanto la segunda condición si se cumple pero la primera no como estas dos condiciones lo tenemos puesto en un unico if y para 
separar estas condiciones usamos el operador **AND (&&)** ambas condiciones tienen que ser verdaderas por lo tanto en la primera iteración la primera pagina no se agrega pero no pasa nada
ya que hemos cubierto esto con la segunda variable en este metodo **paginaAnterior**.

**Segunda iteración:**
for (int i = 2; i <= 8; i++)
{
 if (i >= 5 - 3 && i <= 5 + 3)
 if (i >= 2 && i <= 8)
  {
     paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
  }
}
Aqui vemos que la segunda iteración si se cumple porque ** if (i >= 2 && i <= 8)** aqui **i** es igual 2 y este 2 se ha obtenido por el incremento al ser la segunda iteracion i vale 2
porque se ha incrementado y esto se traduce como ** if (2 >= 2 && 2 <= 8)** aqui como hemos dicho si se cumple porque 2 es igual a 2 y 2 es menor a 8 por lo tando en la segunda iteración 
si entra en el bloque if:
 paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
que esto tendria estos valores en la segunda iteracion:
paginas.Add(new PaginasModel(2) { Activa = 5 == 2 }); aquí activa es false porque no es igual a la pagina actual

**Avancemos a la quinta iteracion**
**Quinta iteración:**
for (int i = 5; i <= 8; i++)
{
 if (i >= 5 - 3 && i <= 5 + 3)
 if (i >= 2 && i <= 8)
  {
     paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
  }
}
Aqui vemos que la condición si se cumple porque  **if (i >= 2 && i <= 8)** aqui **i** es igual a 5 porque es la quinta iteración, la condicion se cumple porque 5 >=2 && 5<=8 por lo tanto
si entra en el bloque if:
 paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
que esto tendria estos valores en la segunda iteracion:
paginas.Add(new PaginasModel(5) { Activa = 5 == 5 }); aquí activa es true por lo tanto es la pagina activa

Aqui continuaria igual hasta la pagina 8.
 
**Determina la página siguiente a la actual:**
Si la página actual es menor que el total de páginas, la siguiente página será una más.
De lo contrario, si estamos en la última página, la página siguiente también será la última.
Esto se utiliza para determinar la navegación "Siguiente".
**var paginaSiguiente = (paginaActual < totalPaginas) ? paginaActual + 1 : totalPaginas;**
Agrega el objeto PaginasModel para la opción "Siguiente":
Este objeto permitirá al usuario navegar a la página siguiente.
La opción "Siguiente" solo se activa si no estamos en la última página.
**paginas.Add(new PaginasModel(paginaSiguiente, paginaActual != totalPaginas, "Siguiente"));**
Retorna la lista de páginas generada, que incluye la navegación "Anterior", "Siguiente" y las páginas visibles.
**return paginas;**
}