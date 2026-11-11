namespace GestorInventario.Application.DTOS.User;

public class UserDtoViewmodel
{
    public int Id { get; set; }
    public bool BajaUsuario { get; set; }
    public string NombreCompleto { get; set; }
    public string Email { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string NombreRol { get; set; }
    
}