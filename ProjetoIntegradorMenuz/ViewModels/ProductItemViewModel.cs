using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Item do cardápio na tela. A imagem só é baixada quando o card é exibido (lazy loading).</summary>
    public partial class ProductItemViewModel : ObservableObject
    {
        private bool _imageRequested;
        private int _flashId;

        public ProductItemViewModel(Product product) => Product = product;

        public Product Product { get; }
        public string Name => Product.Name;
        public string Description => Product.Description;
        public decimal Price => Product.BasePrice;

        [ObservableProperty]
        private ImageSource? picture;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ButtonText))]
        private bool isAdded;

        public string ButtonText => IsAdded ? "Adicionado ✓" : "Adicionar";

        public void LoadImage()
        {
            if (_imageRequested || string.IsNullOrWhiteSpace(Product.ImageUrl))
                return;

            _imageRequested = true;
            Picture = new UriImageSource
            {
                Uri = new Uri(Product.ImageUrl),
                CachingEnabled = true,
                CacheValidity = TimeSpan.FromDays(1)
            };
        }

        /// <summary>Feedback visual curto no botão após adicionar ao carrinho.</summary>
        public async Task FlashAddedAsync()
        {
            var id = ++_flashId;
            IsAdded = true;
            await Task.Delay(900);
            if (id == _flashId)
                IsAdded = false;
        }
    }
}
