using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.MetodosExtension
{
    public  class PaginacionMetodo
    {
        public List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual, int radio=3)
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


    }
}
