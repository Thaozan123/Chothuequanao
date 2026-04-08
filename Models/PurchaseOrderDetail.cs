namespace ChoThueQuanAo.Models
{
   public class PurchaseOrderDetail
   {
       public int Id { get; set; }
       public int PurchaseOrderId { get; set; }
       public int ProductId { get; set; }
       public int Quantity { get; set; }
       public int ReceivedQty { get; set; }
       public decimal UnitCost { get; set; }
   }
}
