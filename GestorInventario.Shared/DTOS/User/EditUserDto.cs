using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Shared.DTOS.User
{
    public class EditUserDto
    {
        public int Id { get; set; }

        public required string Email { get; set; }

      
        public required string NombreCompleto { get; set; }

        public DateTime? FechaNacimiento { get; set; }

    
        public string? Telefono { get; set; }

       
        public required string Direccion { get; set; }

       
        public required string Ciudad { get; set; }
      
        public required string CodigoPostal { get; set; }
        public bool EsEdicionPropia { get; set; }
    }
}
