using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Categoria na navegação do cardápio (lista lateral no tablet, abas no celular).</summary>
    public partial class CategoryChipViewModel : ObservableObject
    {
        public CategoryChipViewModel(Category category) => Category = category;

        public Category Category { get; }
        public string Name => Category.Name;
        public int Count => Category.Products.Count;

        [ObservableProperty]
        private bool isSelected;
    }
}
