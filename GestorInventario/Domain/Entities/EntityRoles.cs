namespace GestorInventario.Domain.Entities
{
    public class EntityRoles
    {
        public int Id { get; private set; }
        public string NombreRol { get; private set; } 
        public EntityRoles() { }

        public EntityRoles(int id, string nombreRol)
        {
            Id = id;
            NombreRol = nombreRol;
        }
    }
}
