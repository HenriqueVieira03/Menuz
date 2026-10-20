using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;

namespace ProjetoIntegradorMenuz.ViewModels
{
    public partial class CartViewModel : ObservableObject
    {
        private readonly MenuApiService _api;

        public CartViewModel(CartService cart, MenuApiService api)
        {
            Cart = cart;
            _api = api;
        }

        public CartService Cart { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotSubmitting))]
        private bool isSubmitting;

        public bool IsNotSubmitting => !IsSubmitting;

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

        [RelayCommand]
        private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");

        /// <summary>Envia o pedido à API. Retorna null em caso de sucesso, ou a mensagem de erro.</summary>
        public async Task<string?> SubmitOrderAsync()
        {
            if (IsSubmitting) return "Envio em andamento.";

            IsSubmitting = true;
            try
            {
                await _api.CreateOrderAsync(Cart.Items.ToList());
                Cart.Clear();
                return null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return "Não foi possível enviar o pedido. Verifique a conexão com o servidor e tente novamente.";
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}