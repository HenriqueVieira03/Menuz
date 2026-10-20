using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;

namespace ProjetoIntegradorMenuz.ViewModels
{
    public partial class KitchenViewModel : ObservableObject
    {
        private readonly MenuApiService _api;

        public KitchenViewModel(MenuApiService api) => _api = api;

        public ObservableCollection<OrderCardViewModel> Orders { get; } = new();

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string lastSyncText = "Ainda não sincronizado";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorMessage;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>Busca os pedidos na API e reconcilia a lista (sem recriar os cards existentes).</summary>
        [RelayCommand]
        private async Task SyncAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var incoming = await _api.GetActiveOrdersAsync();
                var ids = incoming.Select(o => o.Id).ToHashSet();

                for (var i = Orders.Count - 1; i >= 0; i--)
                    if (!ids.Contains(Orders[i].Id))
                        Orders.RemoveAt(i);

                foreach (var dto in incoming) // a API devolve do mais antigo para o mais novo
                {
                    var existing = Orders.FirstOrDefault(o => o.Id == dto.Id);
                    if (existing is null)
                        Orders.Add(new OrderCardViewModel(dto));
                    else
                        existing.Update(dto);
                }

                LastSyncText = $"Sincronizado às {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ErrorMessage = "Não foi possível sincronizar. Verifique se a API está em execução.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AdvanceAsync(OrderCardViewModel? card)
        {
            if (card is null) return;
            ErrorMessage = null;

            try
            {
                var updated = await _api.UpdateOrderStatusAsync(card.Id, card.NextStatus);

                if (updated.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
                    Orders.Remove(card);
                else
                    card.Update(updated);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ErrorMessage = "Não foi possível atualizar o pedido. Tente sincronizar novamente.";
            }
        }

        [RelayCommand]
        private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
    }
}