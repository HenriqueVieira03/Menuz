using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Chip de filtro por categoria na aba Cardápio (Category = null representa "Todas").</summary>
    public partial class CategoryFilterViewModel : ObservableObject
    {
        public CategoryFilterViewModel(CategoryAdmin? category, int productCount)
        {
            Category = category;
            ProductCount = productCount;
        }

        public CategoryAdmin? Category { get; }
        public int ProductCount { get; }
        public bool IsAll => Category is null;
        public string Text => $"{Category?.Name ?? "Todas"} ({ProductCount})";

        [ObservableProperty]
        private bool isSelected;
    }

    /// <summary>Linha de produto na aba Cardápio.</summary>
    public partial class ManagerProductViewModel : ObservableObject
    {
        public ManagerProductViewModel(ProductAdmin product)
        {
            Product = product;
            isAvailable = product.IsAvailable;
        }

        public ProductAdmin Product { get; private set; }
        public int Id => Product.Id;
        public string Name => Product.Name;
        public string CategoryName => Product.CategoryName;
        public string PriceText => $"{Product.BasePrice:C2}";

        public string OptionsText
        {
            get
            {
                var parts = new List<string>();
                if (Product.Sizes.Count > 0) parts.Add(Plural(Product.Sizes.Count, "tamanho", "tamanhos"));
                if (Product.Addons.Count > 0) parts.Add(Plural(Product.Addons.Count, "adicional", "adicionais"));
                var removable = Product.Ingredients.Count(i => i.IsRemovable);
                if (removable > 0) parts.Add(Plural(removable, "ingrediente removível", "ingredientes removíveis"));
                return parts.Count == 0 ? "Sem opções" : string.Join(" · ", parts);
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AvailabilityText))]
        private bool isAvailable;

        [ObservableProperty]
        private bool isUpdating;

        public string AvailabilityText => IsAvailable ? "✓ Disponível" : "Esgotado";

        public void Update(ProductAdmin product)
        {
            Product = product;
            IsAvailable = product.IsAvailable;
            OnPropertyChanged(string.Empty); // atualiza todos os textos
        }

        private static string Plural(int n, string one, string many) => $"{n} {(n == 1 ? one : many)}";
    }

    /// <summary>Linha do ranking de mais vendidos.</summary>
    public record TopProductViewModel(int Position, string Name, string QuantityText, string RevenueText);
}
