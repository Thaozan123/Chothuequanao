namespace ChoThueQuanAo.Models
{
   public class PurchaseOrder
   {
       public int Id { get; set; }
       public string OrderCode { get; set; } = "";
       public int SupplierId { get; set; }
       public string Status { get; set; } = "Pending";
       public decimal TotalAmount { get; set; }
       public DateTime OrderDate { get; set; }
       public DateTime? ReceivedDate { get; set; }
       public int? CreatedBy { get; set; }
   }
}
