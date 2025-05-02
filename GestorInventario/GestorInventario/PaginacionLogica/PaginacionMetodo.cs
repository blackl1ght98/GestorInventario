using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.PaginacionLogica
{
    public class PaginacionMetodo
    {
        //NOTA: Si tienen dudas sobre constructores encadenados mirar explicacion logica paginacion.md
        public List<PaginasModel> GenerarListaPaginas(Paginacion paginacion)
        {
            var paginas = new List<PaginasModel>();
            var paginaAnterior = paginacion.PaginaActual > 1 ? paginacion.PaginaActual - 1 : 1;
            paginas.Add(new PaginasModel(paginaAnterior, paginacion.PaginaActual != 1, "Anterior"));

           

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
