using Heladeria.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Data
{
    public class HeladeriaDbContext : DbContext
    {
        public HeladeriaDbContext(DbContextOptions<HeladeriaDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<SesionUsuario> SesionesUsuario => Set<SesionUsuario>();
        public DbSet<HistorialSesion> HistorialSesiones => Set<HistorialSesion>();
        public DbSet<ResumenVentasUsuarioDia> ResumenVentasUsuarioDia => Set<ResumenVentasUsuarioDia>();
        public DbSet<ProductoPresentacion> ProductoPresentaciones => Set<ProductoPresentacion>();
        public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
        public DbSet<ArticuloInventario> ArticulosInventario => Set<ArticuloInventario>();
        public DbSet<ExistenciaInventario> ExistenciasInventario => Set<ExistenciaInventario>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<Compra> Compras => Set<Compra>();
        public DbSet<CompraDetalle> CompraDetalles => Set<CompraDetalle>();
        public DbSet<LoteInventario> LotesInventario => Set<LoteInventario>();
        public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
        public DbSet<Caja> Cajas => Set<Caja>();
        public DbSet<Venta> Ventas => Set<Venta>();
        public DbSet<DetalleVenta> DetalleVentas => Set<DetalleVenta>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<RecetaDetalle> RecetaDetalles => Set<RecetaDetalle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuarios", "dbo");

                entity.HasKey(u => u.UsuarioId);

                entity.Property(u => u.NombreCompleto)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(u => u.NombreUsuario)
                    .HasMaxLength(60)
                    .IsRequired();

                entity.Property(u => u.PasswordHash)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(u => u.Rol)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(u => u.Activo)
                    .HasDefaultValue(true);

                entity.Property(u => u.FechaCreacion)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasIndex(u => u.NombreUsuario)
                    .IsUnique();
            });

            modelBuilder.Entity<SesionUsuario>(entity =>
            {
                entity.ToTable("SesionesUsuario", "dbo");

                entity.HasKey(s => s.SesionId);

                entity.Property(s => s.SesionId)
                    .HasDefaultValueSql("NEWID()");

                entity.Property(s => s.FechaHoraInicio)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(s => s.MotivoCierre)
                    .HasMaxLength(40);

                entity.Property(s => s.Equipo)
                    .HasMaxLength(128);

                entity.Property(s => s.DireccionIP)
                    .HasMaxLength(45);

                entity.HasOne(s => s.Usuario)
                    .WithMany(u => u.Sesiones)
                    .HasForeignKey(s => s.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(s => new
                {
                    s.UsuarioId,
                    s.FechaHoraInicio
                });
            });

            modelBuilder.Entity<HistorialSesion>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_HistorialSesiones", "dbo");
            });

            modelBuilder.Entity<ResumenVentasUsuarioDia>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_ResumenVentasUsuarioDia", "dbo");
            });

            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.ToTable("Categorias", "dbo");

                entity.HasKey(c => c.CategoriaId);

                entity.Property(c => c.Nombre)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(c => c.Descripcion)
                    .HasMaxLength(250);

                entity.Property(c => c.Activa)
                    .HasDefaultValue(true);

                entity.Property(c => c.FechaCreacion)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasIndex(c => c.Nombre)
                    .IsUnique();
            });

            modelBuilder.Entity<Producto>(entity =>
            {
                entity.ToTable("Productos", "dbo");

                entity.HasKey(p => p.ProductoId);

                entity.Property(p => p.Nombre)
                    .HasMaxLength(120)
                    .IsRequired();

                entity.Property(p => p.Sabor)
                    .HasMaxLength(80);

                entity.Property(p => p.Descripcion)
                    .HasMaxLength(300);

                entity.Property(p => p.Activo)
                    .HasDefaultValue(true);

                entity.Property(p => p.FechaCreacion)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasOne(p => p.Categoria)
                    .WithMany(c => c.Productos)
                    .HasForeignKey(p => p.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductoPresentacion>(entity =>
            {
                entity.ToTable("ProductoPresentaciones", "dbo");

                entity.HasKey(p => p.ProductoPresentacionId);

                entity.Property(p => p.NombrePresentacion)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(p => p.Tamano)
                    .HasMaxLength(50);

                entity.Property(p => p.CodigoInterno)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(p => p.CodigoBarras)
                    .HasMaxLength(80);

                entity.Property(p => p.PrecioVenta)
                    .HasColumnType("decimal(12,2)");

                entity.Property(p => p.CostoEstimado)
                    .HasColumnType("decimal(12,2)")
                    .HasDefaultValue(0m);

                entity.Property(p => p.Activa)
                    .HasDefaultValue(true);

                entity.Property(p => p.FechaCreacion)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasOne(p => p.Producto)
                    .WithMany(p => p.Presentaciones)
                    .HasForeignKey(p => p.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => p.CodigoInterno)
                    .IsUnique();

                entity.HasIndex(p => p.CodigoBarras)
                    .IsUnique()
                    .HasFilter("[CodigoBarras] IS NOT NULL");
            });

            modelBuilder.Entity<UnidadMedida>(entity =>
            {
                entity.ToTable("UnidadesMedida", "dbo");

                entity.HasKey(u => u.UnidadMedidaId);

                entity.Property(u => u.Nombre)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(u => u.Abreviatura)
                    .HasMaxLength(12)
                    .IsRequired();
            });

            modelBuilder.Entity<ArticuloInventario>(entity =>
            {
                entity.ToTable("ArticulosInventario", "dbo");

                entity.HasKey(a => a.ArticuloInventarioId);

                entity.Property(a => a.Nombre)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(a => a.TipoArticulo)
                    .HasMaxLength(25)
                    .IsRequired();

                entity.Property(a => a.StockMinimo)
                    .HasColumnType("decimal(14,3)");

                entity.Property(a => a.Activo)
                    .HasDefaultValue(true);

                entity.Property(a => a.FechaCreacion)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasOne(a => a.UnidadMedida)
                    .WithMany()
                    .HasForeignKey(a => a.UnidadMedidaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.ProductoPresentacion)
                    .WithOne()
                    .HasForeignKey<ArticuloInventario>(
                        a => a.ProductoPresentacionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ExistenciaInventario>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_ExistenciasInventario", "dbo");

                entity.Property(e => e.StockMinimo)
                    .HasColumnType("decimal(14,3)");

                entity.Property(e => e.ExistenciaActual)
                    .HasColumnType("decimal(14,3)");
            });

            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.ToTable("Proveedores", "dbo");

                entity.HasKey(p => p.ProveedorId);

                entity.Property(p => p.Nombre)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(p => p.RNC)
                    .HasMaxLength(20);

                entity.Property(p => p.Telefono)
                    .HasMaxLength(30);

                entity.Property(p => p.Correo)
                    .HasMaxLength(150);

                entity.Property(p => p.Direccion)
                    .HasMaxLength(250);

                entity.Property(p => p.Activo)
                    .HasDefaultValue(true);

                entity.Property(p => p.FechaCreacion)
                    .HasDefaultValueSql("SYSDATETIME()");
            });

            modelBuilder.Entity<Compra>(entity =>
            {
                entity.ToTable("Compras", "dbo");

                entity.HasKey(c => c.CompraId);

                entity.Property(c => c.NumeroCompra)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.HasIndex(c => c.NumeroCompra)
                    .IsUnique();

                entity.Property(c => c.FechaCompra)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(c => c.Estado)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(c => c.Subtotal)
                    .HasColumnType("decimal(14,2)");

                entity.Property(c => c.Descuento)
                    .HasColumnType("decimal(14,2)");

                entity.Property(c => c.Total)
                    .HasColumnType("decimal(14,2)");

                entity.Property(c => c.Observaciones)
                    .HasMaxLength(500);

                entity.HasOne(c => c.Proveedor)
                    .WithMany()
                    .HasForeignKey(c => c.ProveedorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(c => c.Detalles)
                    .WithOne(d => d.Compra)
                    .HasForeignKey(d => d.CompraId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CompraDetalle>(entity =>
            {
                entity.ToTable("CompraDetalles", "dbo");

                entity.HasKey(d => d.CompraDetalleId);

                entity.Property(d => d.Cantidad)
                    .HasColumnType("decimal(14,3)");

                entity.Property(d => d.CostoUnitario)
                    .HasColumnType("decimal(12,4)");

                entity.Property(d => d.Descuento)
                    .HasColumnType("decimal(14,2)");

                entity.Property(d => d.Importe)
                    .HasColumnType("decimal(14,2)");

                entity.HasOne(d => d.ArticuloInventario)
                    .WithMany()
                    .HasForeignKey(d => d.ArticuloInventarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LoteInventario>(entity =>
            {
                entity.ToTable("LotesInventario", "dbo");

                entity.HasKey(l => l.LoteInventarioId);

                entity.Property(l => l.CodigoLote)
                    .HasMaxLength(80);

                entity.Property(l => l.FechaVencimiento)
                    .HasColumnType("date");

                entity.Property(l => l.FechaIngreso)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(l => l.CantidadInicial)
                    .HasColumnType("decimal(14,3)");

                entity.Property(l => l.CantidadActual)
                    .HasColumnType("decimal(14,3)");

                entity.Property(l => l.CostoUnitario)
                    .HasColumnType("decimal(12,4)");

                entity.Property(l => l.Activo)
                    .HasDefaultValue(true);

                entity.HasOne<ArticuloInventario>()
                    .WithMany()
                    .HasForeignKey(l => l.ArticuloInventarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<CompraDetalle>()
                    .WithMany()
                    .HasForeignKey(l => l.CompraDetalleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(l => l.CompraDetalleId)
                    .IsUnique()
                    .HasFilter("[CompraDetalleId] IS NOT NULL");
            });

            modelBuilder.Entity<MovimientoInventario>(entity =>
            {
                entity.ToTable("MovimientosInventario", "dbo");

                entity.HasKey(m => m.MovimientoInventarioId);

                entity.Property(m => m.TipoMovimiento)
                    .HasMaxLength(25)
                    .IsRequired();

                entity.Property(m => m.Direccion)
                    .HasColumnType("char(1)")
                    .IsRequired();

                entity.Property(m => m.Cantidad)
                    .HasColumnType("decimal(14,3)");

                entity.Property(m => m.CostoUnitario)
                    .HasColumnType("decimal(12,4)")
                    .HasDefaultValue(0m);

                entity.Property(m => m.FechaMovimiento)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(m => m.ReferenciaTipo)
                    .HasMaxLength(30);

                entity.Property(m => m.Observaciones)
                    .HasMaxLength(300);

                entity.HasOne<LoteInventario>()
                    .WithMany()
                    .HasForeignKey(m => m.LoteInventarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<ArticuloInventario>()
                    .WithMany()
                    .HasForeignKey(m => m.ArticuloInventarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(m => m.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Caja>(entity =>
            {
                entity.ToTable("Cajas", "dbo");

                entity.HasKey(c => c.CajaId);

                entity.Property(c => c.Codigo)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(c => c.Nombre)
                    .HasMaxLength(80)
                    .IsRequired();
            });

            modelBuilder.Entity<Venta>(entity =>
            {
                entity.ToTable("Ventas", "dbo");

                entity.HasKey(v => v.VentaId);

                entity.Property(v => v.NumeroVenta)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(v => v.FechaVenta)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(v => v.Estado)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(v => v.Subtotal)
                    .HasColumnType("decimal(14,2)");

                entity.Property(v => v.Descuento)
                    .HasColumnType("decimal(14,2)");

                entity.Property(v => v.Total)
                    .HasColumnType("decimal(14,2)");

                entity.Property(v => v.Observaciones)
                    .HasMaxLength(500);

                entity.HasOne(v => v.Usuario)
                    .WithMany()
                    .HasForeignKey(v => v.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Caja)
                    .WithMany()
                    .HasForeignKey(v => v.CajaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(v => v.Detalles)
                    .WithOne(d => d.Venta)
                    .HasForeignKey(d => d.VentaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(v => v.Pagos)
                    .WithOne(p => p.Venta)
                    .HasForeignKey(p => p.VentaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                entity.ToTable("DetalleVentas", "dbo");

                entity.HasKey(d => d.DetalleVentaId);

                entity.Property(d => d.NombreProducto)
                    .HasMaxLength(120)
                    .IsRequired();

                entity.Property(d => d.NombrePresentacion)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(d => d.CodigoInterno)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(d => d.Cantidad)
                    .HasColumnType("decimal(12,3)");

                entity.Property(d => d.PrecioUnitario)
                    .HasColumnType("decimal(12,2)");

                entity.Property(d => d.CostoUnitario)
                    .HasColumnType("decimal(12,4)");

                entity.Property(d => d.Descuento)
                    .HasColumnType("decimal(14,2)");

                entity.Property(d => d.ImporteLinea)
                    .HasColumnType("decimal(14,2)");

                entity.HasOne(d => d.ProductoPresentacion)
                    .WithMany()
                    .HasForeignKey(d => d.ProductoPresentacionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Pago>(entity =>
            {
                entity.ToTable("Pagos", "dbo");

                entity.HasKey(p => p.PagoId);

                entity.Property(p => p.MetodoPago)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(p => p.Importe)
                    .HasColumnType("decimal(14,2)");

                entity.Property(p => p.Referencia)
                    .HasMaxLength(100);

                entity.Property(p => p.FechaPago)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(p => p.MontoRecibido)
                    .HasColumnType("decimal(14,2)");

                entity.Property(p => p.Cambio)
                    .HasColumnType("decimal(14,2)")
                    .HasDefaultValue(0m);
            });

            modelBuilder.Entity<RecetaDetalle>(entity =>
            {
                entity.ToTable("RecetaDetalles", "dbo");

                entity.HasKey(r => r.RecetaDetalleId);

                entity.Property(r => r.CantidadPorUnidad)
                    .HasColumnType("decimal(14,4)");

                entity.HasOne(r => r.ProductoPresentacion)
                    .WithMany()
                    .HasForeignKey(r => r.ProductoPresentacionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ArticuloInventario)
                    .WithMany()
                    .HasForeignKey(r => r.ArticuloInventarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new
                {
                    r.ProductoPresentacionId,
                    r.ArticuloInventarioId
                })
                .IsUnique();
            });

        }
    }
}
