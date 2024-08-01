using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Validations
{
    public class PesoArchivoValidacion : ValidationAttribute
    {
        private readonly int pesoMaximoEnMegaBytes;
        //PesoArchivoValidacion(int PesoMaximoEnMegaBytes): recibe por parametro un numero a nuestra eleccion
        public PesoArchivoValidacion(int PesoMaximoEnMegaBytes)
        {
            pesoMaximoEnMegaBytes = PesoMaximoEnMegaBytes;
        }

        // value representa al archivo, en este caso nuestra foto
        //ValidationContext validationContext es parte del metodo IsValid pero no se usa
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            //Si es null (no viene foto) damos el ok para que se guarde en este caso el producto sin imagen
            if (value == null)
            {
                //Se guardaria el producto sin imagen
                //Pasa la validacion porque puede interesarnos guardar un producto sin imagen
                return ValidationResult.Success;
            }

            // IFormFile es el dato tal y como entra desde la post
            IFormFile formFile = value as IFormFile;
            //Esta segunda comprobacion revisa si la foto viene como formFile. Si viene como formFile
            //sigue hacia delante para revisar el tamaño. Si no pasamos la validacion pero el producto no guarda imagen
            if (formFile == null)
            {
                return ValidationResult.Success;
            }

            // Si  el archivo subido sobrepasa el tamaño devolvemos un error
            if (formFile.Length > pesoMaximoEnMegaBytes * 1024 * 1024)
            {
                return new ValidationResult($"El peso del archivo no debe ser mayor a {pesoMaximoEnMegaBytes}mb");
            }

            // Si hemos llegado hasta aquí es que todo ha ido bien y el archivo cumple con el tamaño especificado en el DTO
            return ValidationResult.Success;
        }
    }
}
