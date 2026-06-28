namespace GestorInventario.Shared.DTOS.User
{
    public class ConfirmRegistrationDto
    {
        public int UserId { get; set; }
        public string? Token { get; set; }
    }
}
