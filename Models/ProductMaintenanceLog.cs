namespace ChoThueQuanAo.Models
{
   public class ProductMaintenanceLog
   {
       public int Id { get; set; }
       public int ProductId { get; set; }
       public string LogType { get; set; } = "Cleaning";
       public string Description { get; set; } = "";
       public decimal Cost { get; set; }
       public string StatusBefore { get; set; } = "";
       public string? StatusAfter { get; set; }
       public DateTime LogDate { get; set; }
       public int? CreatedBy { get; set; }
   }
}
