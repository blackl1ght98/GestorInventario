

namespace GestorInventario.Application.DTOS.Auth
{
    public record RestoresPasswordDto
    {
        public int UserId { get; init; }
        public required string Token { get; init; }
        
       
    }
}
