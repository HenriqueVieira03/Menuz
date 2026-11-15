using System.Net.Http.Json;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.Services
{
    public class MenuApiService
    {
        private readonly HttpClient _http;

        public MenuApiService(HttpClient http) => _http = http;

        /// <summary>Consome GET /api/menu de forma assíncrona.</summary>
        public async Task<List<Category>> GetMenuAsync(CancellationToken ct = default)
        {
            var result = await _http.GetFromJsonAsync<List<Category>>("api/menu", ct);
            return result ?? new List<Category>();
        }

        /// <summary>POST /api/orders: grava o pedido do cliente para a cozinha.</summary>
        /// <exception cref="OrderRejectedException">A API recusou o pedido (ex.: item esgotado); a mensagem pode ser mostrada ao cliente.</exception>
        public async Task<OrderDto> CreateOrderAsync(int tableNumber, IEnumerable<CartItem> items, CancellationToken ct = default)
        {
            var request = new CreateOrderRequest
            {
                TableNumber = tableNumber,
                ClientSentAt = DateTime.UtcNow,
                Items = items.Select(i => new CreateOrderItemRequest
                {
                    ProductId = i.Product.Id,
                    Quantity = i.Quantity,
                    SizeId = i.Size?.Id,
                    AddonIds = i.Addons.Select(a => a.Id).ToList(),
                    RemovedIngredientIds = i.RemovedIngredients.Select(r => r.Id).ToList(),
                    Notes = i.Notes
                }).ToList()
            };

            var response = await _http.PostAsJsonAsync("api/orders", request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = (await response.Content.ReadAsStringAsync(ct)).Trim().Trim('"');
                throw new OrderRejectedException(string.IsNullOrWhiteSpace(message) ? "Pedido recusado pelo servidor." : message);
            }

            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct))!;
        }

        /// <summary>GET /api/orders: pedidos em andamento (painel da cozinha).</summary>
        public async Task<List<OrderDto>> GetActiveOrdersAsync(CancellationToken ct = default)
        {
            var result = await _http.GetFromJsonAsync<List<OrderDto>>("api/orders", ct);
            return result ?? new List<OrderDto>();
        }

        /// <summary>PUT /api/orders/{id}/status.</summary>
        public async Task<OrderDto> UpdateOrderStatusAsync(int orderId, OrderStatus status, CancellationToken ct = default)
        {
            var response = await _http.PutAsJsonAsync($"api/orders/{orderId}/status",
                new UpdateOrderStatusRequest { Status = status }, ct);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct))!;
        }
    }
}