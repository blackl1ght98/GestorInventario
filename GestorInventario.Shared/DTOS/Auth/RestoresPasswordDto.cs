using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Shared.DTOS.Auth
{
    public class RestoresPasswordDto
    {
        public int UserId { get; set; }
        public required string Token { get; set; }
        [Required]
        public  string? Password { get; set; }
        [Required]
        public  string? TemporaryPassword { get; set; }
       
    }
}
