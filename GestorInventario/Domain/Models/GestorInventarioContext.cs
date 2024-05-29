using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Domain.Models;

public partial class GestorInventarioContext : DbContext
{
    public GestorInventarioContext()
    {
    }

    public GestorInventarioContext(DbContextOptions<GestorInventarioContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Carrito> Carritos { get; set; }

    public virtual DbSet<DetalleHistorialPedido> DetalleHistorialPedidos { get; set; }

    public virtual DbSet<DetalleHistorialProducto> DetalleHistorialProductos { get; set; }

    public virtual DbSet<DetallePedido> DetallePedidos { get; set; }

    public virtual DbSet<HistorialPedido> HistorialPedidos { get; set; }

    public virtual DbSet<HistorialProducto> HistorialProductos { get; set; }

    public virtual DbSet<ItemsDelCarrito> ItemsDelCarritos { get; set; }

    public virtual DbSet<Monedum> Moneda { get; set; }

    public virtual DbSet<Pedido> Pedidos { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Proveedore> Proveedores { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-2TL9C3O\\SQLEXPRESS;Initial Catalog=GestorInventario;Integrated Security=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Carrito>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Carrito__3214EC0769AB9246");

            entity.ToTable("Carrito");

            entity.Property(e => e.FechaCreacion).HasColumnType("datetime");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Carritos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK__Carrito__Usuario__0880433F");
        });

        modelBuilder.Entity<DetalleHistorialPedido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DetalleH__3214EC07C0868139");

            entity.HasOne(d => d.HistorialPedido).WithMany(p => p.DetalleHistorialPedidos)
                .HasForeignKey(d => d.HistorialPedidoId)
                .HasConstraintName("FK__DetalleHi__Histo__3F115E1A");

            entity.HasOne(d => d.Producto).WithMany(p => p.DetalleHistorialPedidos)
                .HasForeignKey(d => d.ProductoId)
                .HasConstraintName("FK__DetalleHi__Produ__662B2B3B");
        });

        modelBuilder.Entity<DetalleHistorialProducto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC074AE05DC4");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.NombreProducto)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HistorialProducto).WithMany(p => p.DetalleHistorialProductos)
                .HasForeignKey(d => d.HistorialProductoId)
                .HasConstraintName("FK__DetalleHi__Histo__1D7B6025");

            entity.HasOne(d => d.Producto).WithMany(p => p.DetalleHistorialProductos)
                .HasForeignKey(d => d.ProductoId)
                .HasConstraintName("FK__DetalleHi__Produ__1E6F845E");
        });

        modelBuilder.Entity<DetallePedido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DetalleP__3214EC07A64B3C50");

            entity.ToTable("DetallePedido");

            entity.HasOne(d => d.Pedido).WithMany(p => p.DetallePedidos)
                .HasForeignKey(d => d.PedidoId)
                .HasConstraintName("FK__DetallePe__Pedid__29221CFB");

            entity.HasOne(d => d.Producto).WithMany(p => p.DetallePedidos)
                .HasForeignKey(d => d.ProductoId)
                .HasConstraintName("FK__DetallePe__Produ__65370702");
        });

        modelBuilder.Entity<HistorialPedido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Historia__3214EC075C3E02EE");

            entity.Property(e => e.EstadoPedido)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaPedido).HasColumnType("datetime");
            entity.Property(e => e.NumeroPedido)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.HistorialPedidos)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_IdUsuario_HistorialPedidos");
        });

        modelBuilder.Entity<HistorialProducto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC07AFC517BA");

            entity.Property(e => e.Accion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.Ip)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Usuario).WithMany(p => p.HistorialProductos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_UsuarioId");
        });

        modelBuilder.Entity<ItemsDelCarrito>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ItemsDel__3214EC07EC167622");

            entity.ToTable("ItemsDelCarrito");

            entity.HasOne(d => d.Carrito).WithMany(p => p.ItemsDelCarritos)
                .HasForeignKey(d => d.CarritoId)
                .HasConstraintName("FK__ItemsDelC__Carri__208CD6FA");

            entity.HasOne(d => d.Producto).WithMany(p => p.ItemsDelCarritos)
                .HasForeignKey(d => d.ProductoId)
                .HasConstraintName("FK__ItemsDelC__Produ__6442E2C9");
        });

        modelBuilder.Entity<Monedum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Moneda__3214EC0783509138");

            entity.Property(e => e.Codigo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC0775AB72C3");

            entity.Property(e => e.EstadoPedido)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaPedido).HasColumnType("datetime");
            entity.Property(e => e.NumeroPedido)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Pedidos)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_IdUsuario");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC074C62FFC1");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Imagen).IsUnicode(false);
            entity.Property(e => e.NombreProducto)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdProveedor)
                .HasConstraintName("FK_IdProveedor");
        });

        modelBuilder.Entity<Proveedore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Proveedo__3214EC075BCA4372");

            entity.Property(e => e.Contacto)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NombreProveedor)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC077E50CB8D");

            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC075EC55856");

            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.EnlaceCambioPass).HasMaxLength(50);
            entity.Property(e => e.FechaEnlaceCambioPass).HasColumnType("datetime");
            entity.Property(e => e.FechaExpiracionContrasenaTemporal).HasColumnType("datetime");
            entity.Property(e => e.FechaNacimiento).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasColumnType("datetime");
            entity.Property(e => e.NombreCompleto).IsUnicode(false);
            entity.Property(e => e.Password).HasMaxLength(500);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TemporaryPassword).IsUnicode(false);

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IdRol");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
