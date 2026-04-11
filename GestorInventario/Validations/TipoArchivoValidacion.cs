using GestorInventario.enums;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Validations
{
    public class TipoArchivoValidacion : ValidationAttribute
    {
        private readonly string[] _tiposValidos;

        public TipoArchivoValidacion(GrupoTipoArchivo grupo)
        {
            _tiposValidos = grupo switch
            {
                GrupoTipoArchivo.Imagen => new[] { "image/jpeg", "image/png", "image/gif", "image/webp" },
                GrupoTipoArchivo.PDF => new[] { "application/pdf" },
                _ => Array.Empty<string>()
            };
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IFormFile file)
                return ValidationResult.Success;

            if (!_tiposValidos.Contains(file.ContentType.ToLowerInvariant()))
            {
                return new ValidationResult("Solo se permiten archivos de imagen (JPG, JPEG, PNG, GIF o WebP).");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
            {
                return new ValidationResult("Solo se permiten archivos de imagen (JPG, JPEG, PNG, GIF o WebP).");
            }

            return ValidationResult.Success;
        }
    }
}
