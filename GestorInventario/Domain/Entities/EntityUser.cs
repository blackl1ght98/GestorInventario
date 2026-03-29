namespace GestorInventario.Domain.Entities
{
    /// <summary>
    /// Entidad de dominio que representa un Usuario en el negocio.
    /// Esta clase NO depende de Entity Framework.
    /// </summary>
    public class EntityUser
    {
        public int Id { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string NombreCompleto { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
        public byte[] Salt { get; private set; } = Array.Empty<byte>();
        public bool ConfirmacionEmail { get; private set; }
        public bool BajaUsuario { get; private set; }
        public int IdRol { get; private set; }
        public DateTime? FechaRegistro { get; private set; }
        public string? TemporaryPassword { get; private set; }
        public string? EmailVerificationToken { get; private set; }
        public string? CodigoPostal { get; private set; }
        public string? Ciudad { get; private set; }
        public string Direccion { get; private set; }
        public string? Telefono { get; private set; }
        public DateTime? FechaNacimiento { get; private set; }
        // Constructor vacío necesario para AutoMapper
        public EntityUser() { }

        // Constructor principal para crear un nuevo usuario
        public EntityUser(string email, string nombreCompleto, string password, byte[] salt, int idRol, DateTime fechaNacimiento,
                          string? codigoPostal = null, string? ciudad = null, string direccion=null, string telefono= null)
        {
            Email = email;
            NombreCompleto = nombreCompleto;
            Password = password;
            Salt = salt;
            IdRol = idRol;
            CodigoPostal = codigoPostal;
            Ciudad = ciudad;

            ConfirmacionEmail = false;
            BajaUsuario = false;
            FechaRegistro = DateTime.UtcNow;
            Direccion = direccion;
            Telefono = telefono;
            FechaNacimiento = fechaNacimiento;
        }

        // Métodos de comportamiento
        public void EstablecerPassword(string password, byte[] salt)
        {
            Password = password;
            Salt = salt;
        }

        public void EstablecerEmailVerificationToken(string token)
        {
            EmailVerificationToken = token;
        }

        public void ConfirmarEmail() => ConfirmacionEmail = true;
        public void DarDeBaja() => BajaUsuario = true;
        public void DarDeAlta() => BajaUsuario = false;
        public void CambiarRol(int nuevoRolId) => IdRol = nuevoRolId;
    }
}