using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.user
{
    public class RestorePasswordViewModel
    {
        [Required(ErrorMessage = "El ID de usuario es requerido")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; }

        [Required(ErrorMessage = "La contraseña temporal es requerida")]
        [Display(Name = "Contraseña Temporal")]
        public string TemporaryPassword { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña")]
        public string Password { get; set; }

        
    }
}
