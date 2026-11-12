namespace GestorInventario.Application.DTOS.User;

public class ConfirmEmail
{
    public int UserId { get; set; }
    public string? Token { get; set; }
    public string NuevoEmail { get; set; }
}