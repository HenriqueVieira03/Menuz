using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Uma opção do popup (tamanho, adicional ou ingrediente) com estado de seleção.</summary>
    public partial class OptionViewModel : ObservableObject
    {
        public OptionViewModel(int id, string name, string priceText, bool isSelected)
        {
            Id = id;
            Name = name;
            PriceText = priceText;
            this.isSelected = isSelected;
        }

        public int Id { get; }
        public string Name { get; }
        public string PriceText { get; }
        public bool HasPrice => PriceText.Length > 0;

        [ObservableProperty]
        private bool isSelected;
    }

    /// <summary>
    /// Popup de personalização do produto: tamanho (escolha única), adicionais (vários),
    /// ingredientes removíveis (desmarcar = pedir sem), observação e quantidade.
    /// </summary>
    public partial class ProductCustomizationViewModel : ObservableObject
    {
        public ProductCustomizationViewModel(ProductItemViewModel item)
        {
            Item = item;
            var product = item.Product;

            // A API já devolve os tamanhos do mais barato para o mais caro: o primeiro vem marcado.
            Sizes = product.Sizes
                .Select((s, i) => new OptionViewModel(s.Id, s.Name, s.PriceDelta > 0 ? $"+ {s.PriceDelta:C2}" : string.Empty, i == 0))
                .ToList();

            Addons = product.Addons
                .Select(a => new OptionViewModel(a.Id, a.Name, a.Price > 0 ? $"+ {a.Price:C2}" : "grátis", false))
                .ToList();

            // Marcado = vai no lanche. Desmarcar significa "sem".
            Ingredients = product.Ingredients
                .Where(i => i.IsRemovable)
                .Select(i => new OptionViewModel(i.Id, i.Name, string.Empty, true))
                .ToList();

            foreach (var option in Addons)
                option.PropertyChanged += OnOptionChanged;
        }

        public ProductItemViewModel Item { get; }
        public Product Product => Item.Product;
        public string Name => Product.Name;
        public string Description => Product.Description;
        public ImageSource? Picture => Item.Picture;

        public IReadOnlyList<OptionViewModel> Sizes { get; }
        public IReadOnlyList<OptionViewModel> Addons { get; }
        public IReadOnlyList<OptionViewModel> Ingredients { get; }

        // Com um único tamanho não há o que escolher: a seção fica oculta, mas o tamanho é enviado.
        public bool HasSizes => Sizes.Count > 1;
        public bool HasAddons => Addons.Count > 0;
        public bool HasIngredients => Ingredients.Count > 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Total))]
        [NotifyPropertyChangedFor(nameof(AddButtonText))]
        private int quantity = 1;

        [ObservableProperty]
        private string notes = string.Empty;

        public int NotesMaxLength => 200; // mesmo limite da API

        public decimal UnitPrice =>
            Product.BasePrice
            + (SelectedSize?.PriceDelta ?? 0)
            + SelectedAddons.Sum(a => a.Price);

        public decimal Total => UnitPrice * Quantity;
        public string AddButtonText => $"Adicionar ao pedido · {Total:C2}";

        private ProductSize? SelectedSize
        {
            get
            {
                var id = Sizes.FirstOrDefault(s => s.IsSelected)?.Id;
                return Product.Sizes.FirstOrDefault(s => s.Id == id);
            }
        }

        private IEnumerable<ProductAddon> SelectedAddons
        {
            get
            {
                var ids = Addons.Where(a => a.IsSelected).Select(a => a.Id).ToHashSet();
                return Product.Addons.Where(a => ids.Contains(a.Id));
            }
        }

        [RelayCommand]
        private void SelectSize(OptionViewModel? size)
        {
            if (size is null) return;
            foreach (var s in Sizes)
                s.IsSelected = ReferenceEquals(s, size);
            NotifyPriceChanged();
        }

        [RelayCommand]
        private void Toggle(OptionViewModel? option)
        {
            if (option is not null)
                option.IsSelected = !option.IsSelected;
        }

        [RelayCommand]
        private void Increase()
        {
            if (Quantity < 50) Quantity++;
        }

        [RelayCommand]
        private void Decrease()
        {
            if (Quantity > 1) Quantity--;
        }

        /// <summary>Monta a linha do carrinho com as escolhas atuais.</summary>
        public CartItem BuildCartItem()
        {
            var removedIds = Ingredients.Where(i => !i.IsSelected).Select(i => i.Id).ToHashSet();

            return new CartItem(
                Product,
                Quantity,
                SelectedSize,
                SelectedAddons,
                Product.Ingredients.Where(i => removedIds.Contains(i.Id)),
                Notes);
        }

        private void OnOptionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptionViewModel.IsSelected))
                NotifyPriceChanged();
        }

        private void NotifyPriceChanged()
        {
            OnPropertyChanged(nameof(UnitPrice));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(AddButtonText));
        }
    }
}
