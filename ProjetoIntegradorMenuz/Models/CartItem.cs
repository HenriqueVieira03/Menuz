using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjetoIntegradorMenuz.Models
{
    /// <summary>
    /// Linha do carrinho: um produto com um conjunto de escolhas (tamanho, adicionais, retiradas e observação).
    /// O mesmo produto com escolhas diferentes vira outra linha.
    /// </summary>
    public partial class CartItem : ObservableObject
    {
        public CartItem(
            Product product,
            int quantity = 1,
            ProductSize? size = null,
            IEnumerable<ProductAddon>? addons = null,
            IEnumerable<ProductIngredient>? removedIngredients = null,
            string? notes = null)
        {
            Product = product;
            Quantity = quantity;
            Size = size;
            Addons = (addons ?? Enumerable.Empty<ProductAddon>()).OrderBy(a => a.Name).ToList();
            RemovedIngredients = (removedIngredients ?? Enumerable.Empty<ProductIngredient>()).OrderBy(i => i.Name).ToList();
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        }

        public Product Product { get; }
        public ProductSize? Size { get; }
        public IReadOnlyList<ProductAddon> Addons { get; }
        public IReadOnlyList<ProductIngredient> RemovedIngredients { get; }
        public string? Notes { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Subtotal))]
        private int quantity;

        /// <summary>Mesma regra da API: preço base + acréscimo do tamanho + adicionais.</summary>
        public decimal UnitPrice => Product.BasePrice + (Size?.PriceDelta ?? 0) + Addons.Sum(a => a.Price);
        public decimal Subtotal => UnitPrice * Quantity;

        public string DisplayName => Size is null ? Product.Name : $"{Product.Name} ({Size.Name})";

        /// <summary>Resumo das escolhas, ex.: "+ Bacon · sem Cebola · Obs: bem passado".</summary>
        public string OptionsText => FormatOptions(Addons.Select(a => a.Name), RemovedIngredients.Select(i => i.Name), Notes);
        public bool HasOptions => OptionsText.Length > 0;

        /// <summary>Duas linhas com as mesmas escolhas são somadas no carrinho.</summary>
        public bool HasSameChoicesAs(CartItem other) =>
            Product.Id == other.Product.Id
            && Size?.Id == other.Size?.Id
            && Addons.Select(a => a.Id).SequenceEqual(other.Addons.Select(a => a.Id))
            && RemovedIngredients.Select(i => i.Id).SequenceEqual(other.RemovedIngredients.Select(i => i.Id))
            && string.Equals(Notes, other.Notes, StringComparison.OrdinalIgnoreCase);

        public static string FormatOptions(IEnumerable<string> addons, IEnumerable<string> removed, string? notes)
        {
            var parts = new List<string>();
            parts.AddRange(addons.Select(a => $"+ {a}"));
            parts.AddRange(removed.Select(r => $"sem {r}"));
            if (!string.IsNullOrWhiteSpace(notes)) parts.Add($"Obs: {notes}");
            return string.Join(" · ", parts);
        }
    }
}
