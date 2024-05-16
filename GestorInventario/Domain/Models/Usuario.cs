using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public byte[] Salt { get; set; } = null!;

    public int IdRol { get; set; }

    public bool ConfirmacionEmail { get; set; }

    public bool BajaUsuario { get; set; }

    public string? EnlaceCambioPass { get; set; }

    public DateTime? FechaEnlaceCambioPass { get; set; }

    public DateTime? FechaExpiracionContrasenaTemporal { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public DateTime? FechaNacimiento { get; set; }

    public string? Telefono { get; set; }

    public string Direccion { get; set; } = null!;

    public DateTime? FechaRegistro { get; set; }

    public string? TemporaryPassword { get; set; }

    public virtual ICollection<Carrito> Carritos { get; set; } = new List<Carrito>();

    public virtual ICollection<HistorialPedido> HistorialPedidos { get; set; } = new List<HistorialPedido>();

    public virtual ICollection<HistorialProducto> HistorialProductos { get; set; } = new List<HistorialProducto>();

    public virtual Role IdRolNavigation { get; set; } = null!;

    public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
