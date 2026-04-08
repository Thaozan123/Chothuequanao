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

       public ProductCategory? Category { get; set; }
   }
}
