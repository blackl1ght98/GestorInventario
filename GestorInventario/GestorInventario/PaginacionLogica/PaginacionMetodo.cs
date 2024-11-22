using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.PaginacionLogica
{
    public class PaginacionMetodo
    {
        public List<PaginasModel> GenerarListaPaginas(Paginacion paginacion)
        {
            var paginas = new List<PaginasModel>();
            var paginaAnterior = paginacion.PaginaActual > 1 ? paginacion.PaginaActual - 1 : 1;
            paginas.Add(new PaginasModel(paginaAnterior, paginacion.PaginaActual != 1, "Anterior"));

            /* Explicación del flujo de ejecución del bucle for:
             * 1º Inicializamos "i" en 1.
             * 2º Condición: comprobamos que "i" sea menor o igual al total de páginas.
             * 3º Incremento: "i" va incrementándose de 1 en 1 hasta que sea igual al total de páginas.
             * Condición if dentro del bloque for:
             * if (i >= paginaActual - radio && i <= paginaActual + radio) {}
             * 1º En la primera condición, comprobamos si "i" es mayor o igual que la página actual menos el radio.
             * 2º En la segunda condición, comprobamos si "i" es menor o igual que la página actual más el radio.
             * Acción dentro del if:
             * paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
             * Esta acción agrega a la lista solo las páginas que cumplen con ambas condiciones del if.
             */

            /* Explicación práctica:
             * Supongamos que totalPaginas es 8, paginaActual es 5, y radio es 3:
             * El bucle for se traduce como: for (int i = 1; i <= 8; i++) { }
             * Durante la primera iteración (i = 1):
             *  - La condición if se traduce como: if (1 >= 5 - 3 && 1 <= 5 + 3)
             *  - Resultado: if (1 >= 2 && 1 <= 8)
             *  - Aquí, la primera condición no se cumple (1 >= 2 es falso), aunque la segunda sí (1 <= 8 es verdadero).
             *    Debido a que ambas condiciones deben ser verdaderas para que se ejecute el bloque if, la página 1 no se agrega a la lista dentro del bucle.
             *    Para manejar esto, la página 1 ya se había agregado antes del bucle como "Anterior", si es necesario.
             * 
             * Durante la segunda iteración (i = 2):
             *  - La condición if se traduce como: if (2 >= 5 - 3 && 2 <= 5 + 3)
             *  - Resultado: if (2 >= 2 && 2 <= 8)
             *  - Ambas condiciones son verdaderas, por lo tanto, la página 2 se agrega a la lista:
             *    paginas.Add(new PaginasModel(2) { Activa = paginaActual == i });
             *    La página 2 se agrega con la propiedad Activa en false, ya que paginaActual no es igual a 2. 
             *    Cuando i sea 5 (es decir, la página actual), Activa será true.
             * 
             * El bucle continúa hasta llegar a i = 8, agregando a la lista las páginas 2, 3, 4, 5, 6, 7, y 8.
             *
             * Aclaración: El radio es la cantidad de páginas mostradas a la izquierda y a la derecha de la página actual.
             */

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



    }
}
