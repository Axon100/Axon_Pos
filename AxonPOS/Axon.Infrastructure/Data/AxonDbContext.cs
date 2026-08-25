using Axon.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

using Axon.Domain.Entities;

namespace Axon.Infrastructure.Data
{
    public class AxonDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserSession> UserSessions { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<UnitOfMeasure> UnitsOfMeasure { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<SaleLineItem> SaleLineItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
        public DbSet<StockMovement> StockMovements { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<UserPermission> UserPermissions { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Return> Returns { get; set; } = null!;
        public DbSet<ReturnLineItem> ReturnLineItems { get; set; } = null!;
        public DbSet<SystemSetting> SystemSettings { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        public AxonDbContext(DbContextOptions<AxonDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure decimals
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,4)");
            }

            // Entity Configurations
            modelBuilder.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithMany(p => p.Roles)
                .UsingEntity(j => j.ToTable("RolePermissions"));

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Invoice)
                .WithOne(i => i.Sale)
                .HasForeignKey<Invoice>(i => i.SaleId);

            modelBuilder.Entity<Return>()
                .HasOne(r => r.Sale)
                .WithMany(s => s.Returns)
                .HasForeignKey(r => r.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReturnLineItem>()
                .HasOne(rli => rli.SaleLineItem)
                .WithMany()
                .HasForeignKey(rli => rli.SaleLineItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Default Role
            modelBuilder.Entity<Role>().HasData(new Role
            {
                Id = 1,
                Name = "Administrator",
                Description = "System Administrator with full access",
                CreatedAt = new System.DateTimeOffset(2026, 1, 1, 0, 0, 0, System.TimeSpan.Zero)
            });

            // Seed Default Admin User
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "admin123", // In production, this should be properly hashed
                RoleId = 1,
                IsActive = true,
                CreatedAt = new System.DateTimeOffset(2026, 1, 1, 0, 0, 0, System.TimeSpan.Zero)
            });

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AxonDbContext).Assembly);
        }

        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // Soft delete logic
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
                }
            }
        }
    }
}
