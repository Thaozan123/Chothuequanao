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
        public DbSet<Payment> Payments { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình Quan hệ cho RentalContract (Hợp đồng)
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

            // 2. Cấu hình Quan hệ cho RentalContractDetail (Chi tiết hợp đồng)
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

            // 3. CẤU HÌNH ĐỘ CHÍNH XÁC CHO TIỀN TỆ (BỔ SUNG QUAN TRỌNG)
            // Giúp tránh lỗi làm tròn khi tính toán tiền thuê và khuyến mãi
            
            // Bảng Product
            modelBuilder.Entity<Product>(entity => {
                entity.Property(e => e.RentalPricePerDay).HasPrecision(18, 2);
                entity.Property(e => e.Deposit).HasPrecision(18, 2);
                entity.Property(e => e.LateFeePerDay).HasPrecision(18, 2);
            });

            // Bảng Promotion
            modelBuilder.Entity<Promotion>(entity => {
                entity.Property(e => e.DiscountValue).HasPrecision(18, 2);
                entity.Property(e => e.MinOrderAmount).HasPrecision(18, 2);
            });

            // Bảng RentalContract
            modelBuilder.Entity<RentalContract>(entity => {
                entity.Property(e => e.SubTotal).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.DepositRequired).HasPrecision(18, 2);
                entity.Property(e => e.DepositPaid).HasPrecision(18, 2);
                entity.Property(e => e.ShippingFee).HasPrecision(18, 2);
            });

            // Bảng RentalContractDetail
            modelBuilder.Entity<RentalContractDetail>(entity => {
                entity.Property(e => e.SnapshotUnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.SubTotal).HasPrecision(18, 2);
                entity.Property(e => e.SnapshotDeposit).HasPrecision(18, 2);
            });

            // Bảng Payment
            modelBuilder.Entity<Payment>(entity => {
                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });
        }
    }
}