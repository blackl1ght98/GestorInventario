using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Validations
{
    public class PesoArchivoValidacion : ValidationAttribute
{
    private readonly int _pesoMaximoEnMb;

    public PesoArchivoValidacion(int pesoMaximoEnMb)
    {
        _pesoMaximoEnMb = pesoMaximoEnMb;
        ErrorMessage = $"El peso del archivo no debe superar los {_pesoMaximoEnMb} MB.";
    }

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
            return ValidationResult.Success; // null o no es archivo → OK

        if (file.Length > _pesoMaximoEnMb * 1024 * 1024)
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
}
