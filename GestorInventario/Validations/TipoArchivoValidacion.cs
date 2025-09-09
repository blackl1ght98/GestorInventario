using GestorInventario.enums;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Validations
{
    public class TipoArchivoValidacion : ValidationAttribute
    {
        private readonly string[] tiposValidos;

        public TipoArchivoValidacion(string[] tiposValidos)
        {
            this.tiposValidos = tiposValidos;
        }
        //Para mirar los tipos buscar en internet mime types
        // Desde el DTO le especificamos qué tipo de archivo es el que vamos a elegir (en este caso imagen)
        public TipoArchivoValidacion(GrupoTipoArchivo grupoTipoArchivo)
        {
            if (grupoTipoArchivo == GrupoTipoArchivo.Imagen)
            {
                tiposValidos = new string[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            }
            if (grupoTipoArchivo == GrupoTipoArchivo.PDF)
            {
                tiposValidos = new string[] { "application/pdf" };
            }
        }

        // value representa al archivo
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            // IFormFile es el dato tal y como entra desde la post

            IFormFile formFile = value as IFormFile;

            if (formFile == null)
            {
                return ValidationResult.Success;
            }

            // ContentType deberá ser uno de los tiposValidos { "image/jpeg", "image/png", "image/gif" } para pasar la validación
            if (!tiposValidos.Contains(formFile.ContentType))
            {
                return new ValidationResult($"El tipo del archivo debe ser uno de los siguientes: {string.Join(", ", tiposValidos)}");
            }

            return ValidationResult.Success;
        }
    }
}
