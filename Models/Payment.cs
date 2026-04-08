namespace ChoThueQuanAo.Models
{
   public class Payment
   {
       public int Id { get; set; }
       public int RentalContractId { get; set; }
       public string PaymentType { get; set; } = "";
       public decimal Amount { get; set; }
       public string PaymentMethod { get; set; } = "Cash";
       public string Status { get; set; } = "Pending";
       public string? TransactionCode { get; set; }
       public DateTime PaymentDate { get; set; }
       public int? CreatedBy { get; set; }
   }
}
