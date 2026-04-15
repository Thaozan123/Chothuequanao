using System.Collections.Generic;

namespace ChoThueQuanAo.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = "";
        public int? CategoryId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Material { get; set; }
        public string Size { get; set; } = "";
        public decimal RentalPricePerDay { get; set; }
        public decimal LateFeePerDay { get; set; }
        public decimal Deposit { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = "Available";
        public int StockQuantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ProductCategory? Category { get; set; }
        public ICollection<RentalContractDetail>? RentalContractDetails { get; set; }

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        
        public int ImportedQuantity { get; set; }
    }
}
