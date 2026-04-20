using System;
using System.ComponentModel.DataAnnotations;

namespace ChoThueQuanAo.Models
{
    public class RentalContractDetail
    {
        public int Id { get; set; }
        public int RentalContractId { get; set; }
        public int ProductId { get; set; }
        public string SelectedSize { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public int NumberOfDays { get; set; }
        public decimal SnapshotUnitPrice { get; set; }
        public decimal SnapshotDeposit { get; set; }
        public decimal SubTotal { get; set; }
        public bool SizeChanged { get; set; } = false;
        public string? OriginalSize { get; set; }
        public DateTime? SizeChangedAt { get; set; }

        public RentalContract? RentalContract { get; set; }
        public Product? Product { get; set; }
    }
}