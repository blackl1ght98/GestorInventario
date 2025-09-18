using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.user
{
    public class LoginViewModel
    {
        /**Para que en la vista se muestre un mensaje de error es necesario poner 2 cosas una el texto
        del error en el modelo de la vista y dos ponerlo de esta manera en la vista  <span asp-validation-for="Email" class="text-danger"></span>*/

        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public required string Email { get; set; }
        [Required(ErrorMessage = "La contraseña es requerida.")]

        public required string Password { get; set; }
    }
}
