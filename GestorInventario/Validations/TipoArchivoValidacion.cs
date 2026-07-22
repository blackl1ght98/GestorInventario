using GestorInventario.Domain.enums.Archivos;
using System.ComponentModel.DataAnnotations;


namespace GestorInventario.Validations
{
    public class TipoArchivoValidacion : ValidationAttribute
    {
        private readonly HashSet<string> _tiposValidos;
        private readonly HashSet<string> _extensionesValidas;
        private readonly string _mensajeError;

        public TipoArchivoValidacion(GrupoTipoArchivo grupo)
        {
            (_tiposValidos, _extensionesValidas, _mensajeError) = grupo switch
            {
                GrupoTipoArchivo.Imagen => (
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "image/jpeg", "image/png", "image/gif", "image/webp"
                    },
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ".jpg", ".jpeg", ".png", ".gif", ".webp"
                    },
                    "Solo se permiten archivos de imagen (JPG, JPEG, PNG, GIF o WebP)."
                ),
                GrupoTipoArchivo.PDF => (
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "application/pdf"
                    },
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ".pdf"
                    },
                    "Solo se permiten archivos PDF."
                ),
                _ => (new HashSet<string>(), new HashSet<string>(), "Tipo de archivo no permitido.")
            };
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IFormFile file)
                return ValidationResult.Success;

            if (!_tiposValidos.Contains(file.ContentType) ||
                !_extensionesValidas.Contains(Path.GetExtension(file.FileName)))
            {
                return new ValidationResult(_mensajeError);
            }

            return ValidationResult.Success;
        }
    }
}
