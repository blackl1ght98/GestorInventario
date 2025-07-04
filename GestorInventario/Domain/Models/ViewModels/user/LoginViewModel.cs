using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Domain.Models.ViewModels.user
{
    public class LoginViewModel
    {
        //Para que en la vista se muestre un mensaje de error es necesario poner 2 cosas una el texto
        //del error en el modelo de la vista y dos ponerlo de esta manera en la vista  <span asp-validation-for="Email" class="text-danger"></span>
        /*Lo explico mas facil si tu pones solo esto <span asp-validation-for="Email" class="text-danger"></span>
         * en la vista el mensaje de error no se muestra porque en el modelo de la vista no esta explicado con una 
         * analogia seria como que tu tienes una manguera de agua y donde conectarla pero no tienes agua pues algo parecido
         * esto <span asp-validation-for="Email" class="text-danger"></span> seria la manguera de agua y donde se conecta
         * y esto [Required(ErrorMessage = "El correo electrónico es requerido.")] seria el agua si estan las dos cosas funcionan
  

*/
        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "La contraseña es requerida.")]

        public string Password { get; set; }
    }
}
