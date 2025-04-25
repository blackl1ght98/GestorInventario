using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOs
{
    public class DTORestorePass
    {
        public int UserId { get; set; }
        public string Token { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string TemporaryPassword { get; set; }
        public string? email { get; set; }
    }
}
