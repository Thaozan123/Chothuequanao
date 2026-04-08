namespace ChoThueQuanAo.Models
{
   public class DeliveryOrder
   {
       public int Id { get; set; }
       public int RentalContractId { get; set; }
       public int? AssignedStaffId { get; set; }
       public string DeliveryType { get; set; } = "Delivery";
       public string Address { get; set; } = "";
       public string RecipientName { get; set; } = "";
       public string RecipientPhone { get; set; } = "";
       public DateTime ScheduledAt { get; set; }
       public DateTime? ActualAt { get; set; }
       public string Status { get; set; } = "Pending";
       public decimal ShippingFee { get; set; }
       public string? FailureReason { get; set; }
       public DateTime CreatedAt { get; set; }
   }
}
