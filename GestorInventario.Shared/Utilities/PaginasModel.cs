namespace GestorInventario.Shared.Utilities
{
    
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