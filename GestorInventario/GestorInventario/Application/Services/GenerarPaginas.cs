using GestorInventario.PaginacionLogica;

namespace GestorInventario.Application.Services
{
    public class GenerarPaginas
    {
        public List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual, int? radio = 3)
        {
            // Inicializa una lista de PaginasModel
            var paginas = new List<PaginasModel>();

            /* Calcula la página anterior:
             * Si la página actual es mayor que 1, la página anterior será una menos que la actual.
             * Ejemplo: Si la página actual es 3, la página anterior será 2 (3 - 1).
             */
            var paginaAnterior = (paginaActual > 1) ? paginaActual - 1 : 1;

            /* Agrega un modelo de página para la navegación "Anterior".
             * Usamos el constructor de PaginasModel con la página anterior calculada.
             * La navegación "Anterior" se muestra si la página actual no es la primera.
             */
            paginas.Add(new PaginasModel(paginaAnterior, paginaActual != 1, "Anterior"));

            /* Crea las páginas visibles en base al radio:
             * Recorre desde 1 hasta el total de páginas.
             * Si el índice está dentro del rango del radio alrededor de la página actual, se agrega a la lista.
             */
            for (int i = 1; i <= totalPaginas; i++)
            {
                /* Si el índice está dentro del rango del radio (página actual ± radio), se agrega a la lista.
                 * Ejemplo: Si la página actual es 7 y el radio es 3, se mostrarán las páginas 4 a 10.
                 */
                if (i >= paginaActual - radio && i <= paginaActual + radio)
                {
                    paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
                }
            }

            /* Calcula la página siguiente:
             * Si la página actual es menor que el total de páginas, la página siguiente será una más que la actual.
             * Ejemplo: Si la página actual es 1 y hay más páginas, la siguiente será 2 (1 + 1).
             */
            var paginaSiguiente = (paginaActual < totalPaginas) ? paginaActual + 1 : totalPaginas;

            /* Agrega un modelo de página para la navegación "Siguiente".
             * Usamos el constructor de PaginasModel con la página siguiente calculada.
             * La navegación "Siguiente" se muestra si la página actual no es la última.
             */
            paginas.Add(new PaginasModel(paginaSiguiente, paginaActual != totalPaginas, "Siguiente"));

            return paginas;
        }
    }
}
