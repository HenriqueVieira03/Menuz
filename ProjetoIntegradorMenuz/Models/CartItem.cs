using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjetoIntegradorMenuz.Models
{
    public partial class CartItem : ObservableObject
    {
        public CartItem(Product product, int quantity = 1)
        {
            Product = product;
            Quantity = quantity;
        }

        public Product Product { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Subtotal))]
        private int quantity;

        public decimal UnitPrice => Product.BasePrice;
        public decimal Subtotal => UnitPrice * Quantity;
    }
}
