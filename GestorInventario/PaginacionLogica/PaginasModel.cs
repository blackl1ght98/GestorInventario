namespace GestorInventario.PaginacionLogica
{
    //NOTA: Si tienen dudas sobre constructores encadenados mirar explicacion logica paginacion.md
    public class PaginasModel
    {
        public string Texto { get; set; }
        public int Pagina { get; set; }
        public bool Habilitada { get; set; } = true;
        public bool Activa { get; set; } = false;
        public PaginasModel(int pagina, bool habilitada, string texto)
        {
            Pagina = pagina;
            Habilitada = habilitada;
            Texto = texto;
        }
        public PaginasModel(int pagina, bool habilitada) : this(pagina, habilitada, pagina.ToString()) { }
       
        public PaginasModel(int pagina) : this(pagina, true) { }

    }
}