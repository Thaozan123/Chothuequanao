using ChoThueQuanAo.Models;
using Microsoft.EntityFrameworkCore;

namespace ChoThueQuanAo.Data
{
   public class AppDbContext : DbContext
   {
       public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
       {
       }

       public DbSet<User> Users { get; set; }
       public DbSet<ProductCategory> ProductCategories { get; set; }
       public DbSet<Product> Products { get; set; }
       public DbSet<ProductMaintenanceLog> ProductMaintenanceLogs { get; set; }
       public DbSet<Promotion> Promotions { get; set; }
       public DbSet<RentalContract> RentalContracts { get; set; }
       public DbSet<RentalContractDetail> RentalContractDetails { get; set; }
       public DbSet<Inspection> Inspections { get; set; }
       public DbSet<DeliveryOrder> DeliveryOrders { get; set; }
       public DbSet<Payment> Payments { get; set; }
       public DbSet<Supplier> Suppliers { get; set; }
       public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
       public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }

       protected override void OnModelCreating(ModelBuilder modelBuilder)
       {
           modelBuilder.Entity<Product>()
               .HasOne(p => p.Category)
               .WithMany()
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.SetNull);
       }
   }
}
