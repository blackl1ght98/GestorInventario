namespace GestorInventario.Application.DTOs
{
    public class CreateRoleDTO
    {
        public string NombreRol { get; set; }
        public List<PermisoDTO> Permisos { get; set; }
        public List<int> PermisoIds { get; set; }
    }

    public class PermisoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
    }
    public class NewPermisoDTO
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
    }
}
