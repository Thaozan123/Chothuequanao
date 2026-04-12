using ChoThueQuanAo.Models;

namespace ChoThueQuanAo.ViewModels
{
    public class CartItemViewModel
    {
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
    }
}
