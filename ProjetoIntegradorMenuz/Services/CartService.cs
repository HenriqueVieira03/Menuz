using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.Services
{
    /// <summary>Estado local do carrinho (em memória) até o envio do pedido.</summary>
    public partial class CartService : ObservableObject
    {
        public ObservableCollection<CartItem> Items { get; } = new();

        [ObservableProperty]
        private int totalCount;

        [ObservableProperty]
        private decimal totalPrice;

        /// <summary>Adiciona a linha; se já existir uma com as mesmas escolhas, soma a quantidade.</summary>
        public void Add(CartItem item)
        {
            var existing = Items.FirstOrDefault(i => i.HasSameChoicesAs(item));
            if (existing is null)
                Items.Add(item);
            else
                existing.Quantity += item.Quantity;

            Recalculate();
        }

        public void Increase(CartItem item)
        {
            item.Quantity++;
            Recalculate();
        }

        public void Decrease(CartItem item)
        {
            if (item.Quantity <= 1)
                Items.Remove(item);
            else
                item.Quantity--;

            Recalculate();
        }

        public void Remove(CartItem item)
        {
            Items.Remove(item);
            Recalculate();
        }

        public void Clear()
        {
            Items.Clear();
            Recalculate();
        }

        private void Recalculate()
        {
            TotalCount = Items.Sum(i => i.Quantity);
            TotalPrice = Items.Sum(i => i.Subtotal);
        }
    }
}
