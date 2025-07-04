﻿namespace GestorInventario.Application.DTOs
{
    //NO CAMBIAR-> INFORMACION QUE CONTIENE EL TOKEN QUE SE GENERA
    public class DTOLoginResponse
    {
        public int Id { get; set; }
        public string? Token { get; set; }
        public string? Rol { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public DateTime? FechaRegistro { get; set; }
    }
}
