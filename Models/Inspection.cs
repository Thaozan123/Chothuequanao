namespace ChoThueQuanAo.Models
{
   public class Inspection
   {
       public int Id { get; set; }
       public int RentalContractId { get; set; }
       public int InspectedBy { get; set; }
       public string InspectionType { get; set; } = "";
       public string Condition { get; set; } = "Good";
       public string? Description { get; set; }
       public decimal CleaningFee { get; set; }
       public decimal RepairFee { get; set; }
       public decimal CompensationFee { get; set; }
       public string? ImageEvidence { get; set; }
       public DateTime InspectedAt { get; set; }
   }
}
