

namespace GestorInventario.Domain.Models;

public partial class Notificacion
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string Titulo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public bool Leida { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
