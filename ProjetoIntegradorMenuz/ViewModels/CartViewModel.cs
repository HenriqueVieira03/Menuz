using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;

namespace ProjetoIntegradorMenuz.ViewModels
{
    public partial class CartViewModel : ObservableObject
    {
        public CartViewModel(CartService cart) => Cart = cart;

        public CartService Cart { get; }

        [RelayCommand]
        private void Increase(CartItem? item)
        {
            if (item is not null) Cart.Increase(item);
        }

        [RelayCommand]
        private void Decrease(CartItem? item)
        {
            if (item is not null) Cart.Decrease(item);
        }

        [RelayCommand]
        private void Remove(CartItem? item)
        {
            if (item is not null) Cart.Remove(item);
        }

        [RelayCommand]
        private void Clear() => Cart.Clear();
    }
}
