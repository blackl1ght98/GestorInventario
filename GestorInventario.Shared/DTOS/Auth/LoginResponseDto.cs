namespace GestorInventario.Shared.DTOS.Auth
{
   
    public class LoginResponseDto
    {
        public int Id { get; set; }
        public required string Token { get; set; }
        public string? Rol { get; set; }     
        public  string? RefreshToken { get; set; }
    }
}
