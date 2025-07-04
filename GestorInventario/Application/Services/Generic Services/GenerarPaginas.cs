using GestorInventario.PaginacionLogica;

namespace GestorInventario.Application.Services
{
    public class GenerarPaginas
    {
        //Si tienen dudas mirar documentacion tecnica de este metodo
        public List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual, int? radio = 3)
        {

            var paginas = new List<PaginasModel>();

            var paginaAnterior = (paginaActual > 1) ? paginaActual - 1 : 1;
            paginas.Add(new PaginasModel(paginaAnterior, paginaActual != 1, "Anterior"));
            for (int i = 1; i <= totalPaginas; i++)
            {

                if (i >= paginaActual - radio && i <= paginaActual + radio)
                {
                    paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
                }
            }

            var paginaSiguiente = (paginaActual < totalPaginas) ? paginaActual + 1 : totalPaginas;
            paginas.Add(new PaginasModel(paginaSiguiente, paginaActual != totalPaginas, "Siguiente"));
            return paginas;
        }
        public List<PaginasModel> GenerarListaPaginas(Paginacion paginacion)
        {
            var paginas = new List<PaginasModel>();
            int totalPaginas = paginacion.TotalPaginas;
            int paginaActual = paginacion.Pagina;
            int radio = paginacion.Radio; // Usamos el valor directamente, ya que no puede ser null

            // Agregar página anterior
            var paginaAnterior = paginaActual > 1 ? paginaActual - 1 : 1;
            paginas.Add(new PaginasModel(paginaAnterior, paginaActual != 1, "Anterior"));

            // Generar páginas dentro del rango del radio
            for (int i = 1; i <= totalPaginas; i++)
            {
                if (i >= paginaActual - radio && i <= paginaActual + radio)
                {
                    paginas.Add(new PaginasModel(i) { Activa = paginaActual == i });
                }
            }

            // Agregar página siguiente
            var paginaSiguiente = paginaActual < totalPaginas ? paginaActual + 1 : totalPaginas;
            paginas.Add(new PaginasModel(paginaSiguiente, paginaActual != totalPaginas, "Siguiente"));

            return paginas;
        }
    }

}

