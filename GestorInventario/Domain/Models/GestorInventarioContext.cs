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

    public virtual DbSet<DetalleHistorialPedido> DetalleHistorialPedidos { get; set; }

    public virtual DbSet<DetalleHistorialProducto> DetalleHistorialProductos { get; set; }

    public virtual DbSet<DetallePedido> DetallePedidos { get; set; }

    public virtual DbSet<HistorialPedido> HistorialPedidos { get; set; }

    public virtual DbSet<HistorialProducto> HistorialProductos { get; set; }

    public virtual DbSet<Monedum> Moneda { get; set; }

    public virtual DbSet<PayPalPaymentDetail> PayPalPaymentDetails { get; set; }

    public virtual DbSet<PayPalPaymentItem> PayPalPaymentItems { get; set; }

    public virtual DbSet<Pedido> Pedidos { get; set; }

    public virtual DbSet<Permiso> Permisos { get; set; }

    public virtual DbSet<PlanDetail> PlanDetails { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Proveedore> Proveedores { get; set; }

    public virtual DbSet<Rembolso> Rembolsos { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermiso> RolePermisos { get; set; }

    public virtual DbSet<SubscriptionDetail> SubscriptionDetails { get; set; }

    public virtual DbSet<UserSubscription> UserSubscriptions { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=GUILLERMO\\SQLEXPRESS;Initial Catalog=GestorInventario;User ID=sa;Password=SQL#1234;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DetalleHistorialPedido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DetalleH__3214EC07C0868139");

            entity.Property(e => e.EstadoPedido)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaPedido).HasColumnType("datetime");
            entity.Property(e => e.NumeroPedido)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.HistorialPedido).WithMany(p => p.DetalleHistorialPedidos)
                .HasForeignKey(d => d.HistorialPedidoId)
                .HasConstraintName("FK__DetalleHi__Histo__2F9A1060");

            entity.HasOne(d => d.Producto).WithMany(p => p.DetalleHistorialPedidos)
                .HasForeignKey(d => d.ProductoId)
                .HasConstraintName("FK__DetalleHi__Produ__3FD07829");
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
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC0787CADD9A");

            entity.Property(e => e.Accion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.Ip)
                .HasMaxLength(100)
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

        modelBuilder.Entity<PayPalPaymentDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3213E83F37FE3292");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.CreateTime)
                .HasColumnType("datetime")
                .HasColumnName("create_time");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.ExchangeRate)
                .HasColumnType("decimal(20, 10)")
                .HasColumnName("exchange_rate");
            entity.Property(e => e.Intent)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("intent");
            entity.Property(e => e.PayeeEmail)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("payee_email");
            entity.Property(e => e.PayeeMerchantId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payee_merchant_id");
            entity.Property(e => e.PayerEmail)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("payer_email");
            entity.Property(e => e.PayerFirstName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payer_first_name");
            entity.Property(e => e.PayerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payer_id");
            entity.Property(e => e.PayerLastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payer_last_name");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("payment_method");
            entity.Property(e => e.ProtectionEligibility)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("protection_eligibility");
            entity.Property(e => e.ReceivableAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("receivable_amount");
            entity.Property(e => e.ReceivableCurrency)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("receivable_currency");
            entity.Property(e => e.SaleCurrency)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("sale_currency");
            entity.Property(e => e.SaleId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sale_id");
            entity.Property(e => e.SaleState)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("sale_state");
            entity.Property(e => e.SaleTotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("sale_total");
            entity.Property(e => e.ShippingCity)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("shipping_city");
            entity.Property(e => e.ShippingCountryCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("shipping_country_code");
            entity.Property(e => e.ShippingLine1)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("shipping_line1");
            entity.Property(e => e.ShippingPostalCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("shipping_postal_code");
            entity.Property(e => e.ShippingRecipientName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("shipping_recipient_name");
            entity.Property(e => e.ShippingState)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("shipping_state");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TransactionFeeAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("transaction_fee_amount");
            entity.Property(e => e.TransactionFeeCurrency)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("transaction_fee_currency");
            entity.Property(e => e.TransactionsCurrency)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("transactions_currency");
            entity.Property(e => e.TransactionsShipping)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("transactions_shipping");
            entity.Property(e => e.TransactionsSubtotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("transactions_subtotal");
            entity.Property(e => e.TransactionsTotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("transactions_total");
            entity.Property(e => e.UpdateTime)
                .HasColumnType("datetime")
                .HasColumnName("update_time");
        });

        modelBuilder.Entity<PayPalPaymentItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PayPalPa__3213E83F77BE0112");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ItemCurrency)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("item_currency");
            entity.Property(e => e.ItemImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("item_image_url");
            entity.Property(e => e.ItemName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("item_name");
            entity.Property(e => e.ItemPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("item_price");
            entity.Property(e => e.ItemQuantity).HasColumnName("item_quantity");
            entity.Property(e => e.ItemSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("item_sku");
            entity.Property(e => e.ItemTax)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("item_tax");
            entity.Property(e => e.PayPalId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payPal_id");

            entity.HasOne(d => d.PayPal).WithMany(p => p.PayPalPaymentItems)
                .HasForeignKey(d => d.PayPalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PayPalPay__payPa__16644E42");
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC0775AB72C3");

            entity.Property(e => e.CaptureId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("captureID");
            entity.Property(e => e.Currency)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("currency");
            entity.Property(e => e.EstadoPedido)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaPedido).HasColumnType("datetime");
            entity.Property(e => e.NumeroPedido)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.OrderId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("orderID");
            entity.Property(e => e.RefundId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("refundId");
            entity.Property(e => e.Total)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("total");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("trackingNumber");
            entity.Property(e => e.Transportista)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UrlTracking)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Pedidos)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_IdUsuario");
        });

        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Permisos__3214EC0775746617");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PlanDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC0750705EBA");

            entity.HasIndex(e => e.PaypalPlanId, "UQ__PlanDeta__C8025F80D0AF9CCD").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PaypalPlanId)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ProductId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RegularFixedPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RegularIntervalUnit)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.SetupFee).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SetupFeeFailureAction)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TaxPercentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TrialFixedPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TrialIntervalUnit)
                .HasMaxLength(10)
                .IsUnicode(false);
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
            entity.Property(e => e.UpcCode)
                .HasMaxLength(20)
                .IsUnicode(false);

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

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Proveedores)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_IdUsuario_Provedor");
        });

        modelBuilder.Entity<Rembolso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC076ABC116C");

            entity.ToTable("rembolso");

            entity.Property(e => e.EmailCliente)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EstadoRembolso)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaRembolso).HasColumnType("datetime");
            entity.Property(e => e.MotivoRembolso)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NombreCliente)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NumeroPedido)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Rembolsos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_rembolso");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC077E50CB8D");

            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RolePermiso>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermisoId }).HasName("PK__RolePerm__A394C26868D0DE9C");

            entity.Property(e => e.Nombre)
                .HasMaxLength(5)
                .IsUnicode(false);

            entity.HasOne(d => d.Permiso).WithMany(p => p.RolePermisos)
                .HasForeignKey(d => d.PermisoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RolePermi__Permi__62AFA012");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermisos)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RolePermi__RoleI__61BB7BD9");
        });

        modelBuilder.Entity<SubscriptionDetail>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__Subscrip__9A2B24BDB4931DFA");

            entity.Property(e => e.SubscriptionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SubscriptionID");
            entity.Property(e => e.FinalPaymentTime).HasColumnType("datetime");
            entity.Property(e => e.LastPaymentAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.LastPaymentCurrency)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.LastPaymentTime).HasColumnType("datetime");
            entity.Property(e => e.NextBillingTime).HasColumnType("datetime");
            entity.Property(e => e.OutstandingBalance).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.OutstandingCurrency)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.PayerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PayerID");
            entity.Property(e => e.PlanId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PlanID");
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StatusUpdateTime).HasColumnType("datetime");
            entity.Property(e => e.SubscriberEmail)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SubscriberName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TrialFixedPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TrialIntervalUnit)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Plan).WithMany(p => p.SubscriptionDetails)
                .HasPrincipalKey(p => p.PaypalPlanId)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_planId");
        });

        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC077E0B3350");

            entity.ToTable("UserSubscription");

            entity.Property(e => e.NombreSusbcriptor)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PaypalPlanId)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.SubscriptionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SubscriptionID");

            entity.HasOne(d => d.Subscription).WithMany(p => p.UserSubscriptions)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SuscriptionID");

            entity.HasOne(d => d.User).WithMany(p => p.UserSubscriptions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserId");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC075EC55856");

            entity.Property(e => e.Ciudad)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ciudad");
            entity.Property(e => e.CodigoPostal)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("codigoPostal");
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
