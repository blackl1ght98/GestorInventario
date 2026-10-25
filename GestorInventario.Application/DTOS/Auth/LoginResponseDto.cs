namespace GestorInventario.Application.DTOS.Auth
{
   
    public record LoginResponseDto
    {
        public int Id { get; init; }
        public required string Token { get; init; }
        public string? Rol { get; init; }     
        public  string? RefreshToken { get; init; }
    }
}
