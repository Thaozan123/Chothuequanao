namespace ChoThueQuanAo.Models
{
   public class RentalContractDetail
   {
       public int Id { get; set; }
       public int RentalContractId { get; set; }
       public int ProductId { get; set; }
       public string SelectedSize { get; set; } = "";
       public int Quantity { get; set; }
       public int NumberOfDays { get; set; }
       public decimal SnapshotUnitPrice { get; set; }
       public decimal SnapshotDeposit { get; set; }
       public decimal SubTotal { get; set; }
       public bool SizeChanged { get; set; }
       public string? OriginalSize { get; set; }
       public DateTime? SizeChangedAt { get; set; }
   }
}
