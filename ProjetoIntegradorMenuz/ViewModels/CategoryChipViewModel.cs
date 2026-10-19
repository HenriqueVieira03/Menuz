using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.ViewModels
{
    public partial class CategoryChipViewModel : ObservableObject
    {
        public CategoryChipViewModel(Category category) => Category = category;

        public Category Category { get; }
        public string Name => Category.Name;

        public string Icon => Category.Name.ToLowerInvariant() switch
        {
            var n when n.Contains("lanche") => "🍔",
            var n when n.Contains("pizza") => "🍕",
            var n when n.Contains("bebida") => "🥤",
            var n when n.Contains("sobremesa") => "🍰",
            var n when n.Contains("salgado") => "🥟",
            var n when n.Contains("doce") => "🍪",
            _ => "🍽️"
        };

        [ObservableProperty]
        private bool isSelected;
    }
}
