using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOs.User
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
