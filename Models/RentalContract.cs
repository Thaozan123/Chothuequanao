namespace ChoThueQuanAo.Models
{
   public class RentalContract
   {
       public int Id { get; set; }
       public string ContractCode { get; set; } = "";
       public int CustomerId { get; set; }
       public int? StaffId { get; set; }
       public string ContractType { get; set; } = "Online";
       public DateTime StartDate { get; set; }
       public DateTime ExpectedReturnDate { get; set; }
       public DateTime? ActualReturnDate { get; set; }
       public string Status { get; set; } = "PendingDeposit";
       public int? PromotionId { get; set; }
       public decimal SubTotal { get; set; }
       public decimal DiscountAmount { get; set; }
       public decimal ShippingFee { get; set; }
       public decimal TotalAmount { get; set; }
       public decimal DepositRequired { get; set; }
       public decimal DepositPaid { get; set; }
       public string? Notes { get; set; }
       public DateTime CreatedAt { get; set; }
   }
}
