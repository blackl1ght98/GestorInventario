public List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual, int? radio = 3)
        {
**Inicializa una lista vac�a que contendr� los objetos PaginasModel.**
  var paginas = new List<PaginasModel>();

  **Determina la p�gina anterior a la actual:**
  Si la p�gina actual es mayor que 1, la p�gina anterior ser� una menos que la actual.
  De lo contrario, si estamos en la primera p�gina, la p�gina anterior tambi�n ser� 1.
  Esto se utiliza para determinar la navegaci�n "Anterior".             
  **var paginaAnterior = (paginaActual > 1) ? paginaActual - 1 : 1;**

  **Agregar la primera pagina a la lista:**
  Agrega el objeto PaginasModel para la opci�n "Anterior":
  Este objeto permitir� al usuario navegar a la p�gina anterior.
  La opci�n "Anterior" solo se activa si no estamos en la primera p�gina.
 **paginas.Add(new PaginasModel(paginaAnterior, paginaActual != 1, "Anterior"));**

 **Explicaci�n bucle for:**
  Agrega las p�ginas que deben ser visibles en la barra de paginaci�n:
  Recorre desde la p�gina 1 hasta la p�gina final (totalPaginas).
  Solo se agregan las p�ginas que est�n dentro del rango del radio especificado.
  El radio define cu�ntas p�ginas a la izquierda y derecha de la actual deben mostrarse.

  for (int i = 1; i <= totalPaginas; i++)
   {
   **Explicacion de la condicion:**
   Verifica si la p�gina actual est� dentro del rango de visibilidad:
   Si la p�gina (i) est� dentro del rango definido por (paginaActual - radio) y (paginaActual + radio),
   entonces se agrega a la lista de p�ginas.
   Adem�s, si (i) es igual a la p�gina actual, esa p�gina se marca como activa
   Por ejemplo, si paginaActual es 5 y el radio es 3, entonces se mostrar�n las p�ginas de 2 a 8 (5 - 3 = 2 y 5 + 3 = 8).

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
   Ahora que tenemos estos datos veamos como es el flujo de ejecuci�n del bucle for
   for (int i = 1; i <= totalPaginas; i++){}
   Un bucle for tiene 3 parametros:
- Inicializaci�n: int i =1: normalmente se inicializa en 1 pero admite otros valores
- Condici�n: i <= totalPaginas; : esta es la codici�n que va a seguir el bucle en este caso la condici�n es 1 <= 8;
- Incremento: i++; : es las veces que se va a incrementar en base a la condicion.
**Condici�n dentro del bucle for:**
 if (i >= paginaActual - radio && i <= paginaActual + radio)
      {
        paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
      }
Esta condici�n es la que va creando las p�ginas que ve el usuario y la va almacenando en la lista que hemos creado que la almacena la variable **paginas**.

**Acci�n dentro del condicional if:**
paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
Aqui mientras que **paginaActual** no sea igual a **i** esa pagina no estara activa.

**Explicaci�n practica del bucle:**

for (int i = 1; i <= totalPaginas; i++)
{
 if (i >= paginaActual - radio && i <= paginaActual + radio)
      {
        paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
      }
}
**Primera iteraci�n:**
for (int i = 1; i <= 8; i++)
{
 if (i >= 5 - 3 && i <= 5 + 3)
 if (i >= 2 && i <= 8)
  {
     paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
  }
}
Aqui vemos que la primera iteraci�n no se cumple porque **if (i >= 2 && i <= 8)** i es 1 y aqui hace i es mayor o igual a 2 no es mayor y no es igual la segunda condici�n **i <= 8**
1<=8 aqui 1 es menor o igual que 8 si es menor por lo tanto la segunda condici�n si se cumple pero la primera no como estas dos condiciones lo tenemos puesto en un unico if y para 
separar estas condiciones usamos el operador **AND (&&)** ambas condiciones tienen que ser verdaderas por lo tanto en la primera iteraci�n la primera pagina no se agrega pero no pasa nada
ya que hemos cubierto esto con la segunda variable en este metodo **paginaAnterior**.

**Segunda iteraci�n:**
for (int i = 2; i <= 8; i++)
{
 if (i >= 5 - 3 && i <= 5 + 3)
 if (i >= 2 && i <= 8)
  {
     paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
  }
}
Aqui vemos que la segunda iteraci�n si se cumple porque ** if (i >= 2 && i <= 8)** aqui **i** es igual 2 y este 2 se ha obtenido por el incremento al ser la segunda iteracion i vale 2
porque se ha incrementado y esto se traduce como ** if (2 >= 2 && 2 <= 8)** aqui como hemos dicho si se cumple porque 2 es igual a 2 y 2 es menor a 8 por lo tando en la segunda iteraci�n 
si entra en el bloque if:
 paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
que esto tendria estos valores en la segunda iteracion:
paginas.Add(new PaginasModel(2) { Activa = 5 == 2 }); aqu� activa es false porque no es igual a la pagina actual

**Avancemos a la quinta iteracion**
**Quinta iteraci�n:**
for (int i = 5; i <= 8; i++)
{
 if (i >= 5 - 3 && i <= 5 + 3)
 if (i >= 2 && i <= 8)
  {
     paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
  }
}
Aqui vemos que la condici�n si se cumple porque  **if (i >= 2 && i <= 8)** aqui **i** es igual a 5 porque es la quinta iteraci�n, la condicion se cumple porque 5 >=2 && 5<=8 por lo tanto
si entra en el bloque if:
 paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
que esto tendria estos valores en la segunda iteracion:
paginas.Add(new PaginasModel(5) { Activa = 5 == 5 }); aqu� activa es true por lo tanto es la pagina activa

Aqui continuaria igual hasta la pagina 8.
 
**Determina la p�gina siguiente a la actual:**
Si la p�gina actual es menor que el total de p�ginas, la siguiente p�gina ser� una m�s.
De lo contrario, si estamos en la �ltima p�gina, la p�gina siguiente tambi�n ser� la �ltima.
Esto se utiliza para determinar la navegaci�n "Siguiente".
**var paginaSiguiente = (paginaActual < totalPaginas) ? paginaActual + 1 : totalPaginas;**
Agrega el objeto PaginasModel para la opci�n "Siguiente":
Este objeto permitir� al usuario navegar a la p�gina siguiente.
La opci�n "Siguiente" solo se activa si no estamos en la �ltima p�gina.
**paginas.Add(new PaginasModel(paginaSiguiente, paginaActual != totalPaginas, "Siguiente"));**
Retorna la lista de p�ginas generada, que incluye la navegaci�n "Anterior", "Siguiente" y las p�ginas visibles.
**return paginas;**
}