using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Usuarios
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public byte[] Salt { get; set; } = null!;

    public int IdRol { get; set; }

    public bool ConfirmacionEmail { get; set; }

    public bool BajaUsuario { get; set; }

    public DateTime? FechaExpiracionContrasenaTemporal { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public DateTime FechaNacimiento { get; set; }

    public string Telefono { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string? TemporaryPassword { get; set; }

    public string CodigoPostal { get; set; } = null!;

    public string Ciudad { get; set; } = null!;

    public string? EmailVerificationToken { get; set; }

    public byte[]? ResetTokenSalt { get; set; }

    public bool? ResetTokenUsed { get; set; }

    public string? PendingEmail { get; set; }

    public virtual Roles IdRolNavigation { get; set; } = null!;

    public virtual ICollection<Notificacion> Notificacion { get; set; } = new List<Notificacion>();

    public virtual ICollection<Pedidos> Pedidos { get; set; } = new List<Pedidos>();

    public virtual ICollection<Proveedores> Proveedores { get; set; } = new List<Proveedores>();

    public virtual ICollection<Rembolso> Rembolso { get; set; } = new List<Rembolso>();

    public virtual ICollection<UserSubscription> UserSubscription { get; set; } = new List<UserSubscription>();
}
