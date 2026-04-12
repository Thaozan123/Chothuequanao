using ChoThueQuanAo.Models;
using System.Collections.Generic;

namespace ChoThueQuanAo.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = new Product();
        public string Advice { get; set; } = string.Empty;
        public List<SizeChartEntry> SizeChart { get; set; } = new List<SizeChartEntry>();
        public string ColorAdvice { get; set; } = string.Empty;
        public string SizeRecommendation { get; set; } = string.Empty;
    }

    public class SizeChartEntry
    {
        public string Region { get; set; } = string.Empty;
        public string Chart { get; set; } = string.Empty;
        
        // Chi tiết size theo từng loại quốc gia
        public List<SizeDetail> SizeDetails { get; set; } = new List<SizeDetail>();
    }

    public class SizeDetail
    {
        public string Size { get; set; } = string.Empty;  // S, M, L, XL, etc.
        public string ChestMeasurement { get; set; } = string.Empty;  // e.g., "85-90cm"
        public string WeightRange { get; set; } = string.Empty;  // e.g., "45kg-52kg"
        public string SleeveLength { get; set; } = string.Empty;  // e.g., "58cm"
        public string OtherMeasurements { get; set; } = string.Empty;  // e.g., "41cm (vai)"
    }
}
