using GestorInventario.PaginacionLogica;

namespace GestorInventario.Application.Services
{
    public class GenerarPaginas
    {
        public  List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual)
        {
            //A la variable paginas le asigna una lista de PaginasModel
            var paginas = new List<PaginasModel>();

            // Si estamos en una página que no es la primera (por ejemplo, la página 3),
            // y queremos ir a la página anterior, comprobamos si la página actual es mayor que 1.
            // Si es así, restamos 1 a la página actual para obtener la página anterior (3 - 1 = 2).
            // Si ya estamos en la primera página, la página anterior sigue siendo 1.
            var paginaAnterior = (paginaActual > 1) ? paginaActual - 1 : 1;

            // Se crea un nuevo objeto PaginasModel para la página "anterior".
            // Si la página actual no es la primera, se puede ir a la página anterior.
            // Si ya estamos en la primera página, el botón "anterior" se desactiva.
            // Se utiliza el constructor de PaginasModel que toma tres argumentos.
            paginas.Add(new PaginasModel(paginaAnterior, paginaActual != 1, "Anterior"));

            // Este bucle crea las páginas.
            // Comienza con i = 1 y continúa mientras i sea menor o igual que el total de páginas.
            for (int i = 1; i <= totalPaginas; i++)
            {
                // Para cada iteración, se añade una nueva página a la lista 'paginas'.
                // Se utiliza el constructor de PaginasModel que toma un solo argumento: el número de página (i).
                // Además, se establece la propiedad 'Activa' de la página a 'true' si la página actual es igual a i.
                paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
            }


            // Se calcula el número de la página "siguiente".
            // Si aún no estamos en la última página, es simplemente la página actual más uno.
            // Si ya estamos en la última página, la página "siguiente" sigue siendo la última página.
            var paginaSiguiente = (paginaActual < totalPaginas) ? paginaActual + 1 : totalPaginas;

            // Se añade un nuevo objeto PaginasModel para la página "siguiente" a la lista 'paginas'.
            // Se utiliza el constructor de PaginasModel que toma tres argumentos: el número de página, el estado de habilitación y el texto.
            // El botón "siguiente" solo está habilitado si no estamos en la última página.
            paginas.Add(new PaginasModel(paginaSiguiente, paginaActual != totalPaginas, "Siguiente"));


            return paginas;
        }
    }
}
