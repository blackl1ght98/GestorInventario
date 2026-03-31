namespace GestorInventario.Domain.Entities
{
    public class EntityProveedor
    {
        public int Id { get; private set; }
        public string Nombre { get; private set; }
        public string Direccion {  get; private set; }
        public string Contacto { get; private set; }
        public int IdUsuario { get; private set; }

        public EntityProveedor() { }
        public EntityProveedor(int id, string nombre, string direccion, string contacto, int idUsuario)
        {
            Id = id;
            Nombre = nombre;
            Direccion = direccion;
            Contacto = contacto;
            IdUsuario = idUsuario;
        }
    }
}
