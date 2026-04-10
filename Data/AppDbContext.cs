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
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<RentalContract> RentalContracts { get; set; }
        public DbSet<RentalContractDetail> RentalContractDetails { get; set; }
        public DbSet<Promotion> Promotions { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RentalContract>()
                .HasOne(rc => rc.Customer)
                .WithMany(u => u.CustomerContracts)
                .HasForeignKey(rc => rc.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RentalContract>()
                .HasOne(rc => rc.Staff)
                .WithMany(u => u.StaffContracts)
                .HasForeignKey(rc => rc.StaffId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RentalContract>()
                .HasOne(rc => rc.Promotion)
                .WithMany()
                .HasForeignKey(rc => rc.PromotionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RentalContractDetail>()
                .HasOne(d => d.RentalContract)
                .WithMany(rc => rc.RentalContractDetails)
                .HasForeignKey(d => d.RentalContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RentalContractDetail>()
                .HasOne(d => d.Product)
                .WithMany(p => p.RentalContractDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}