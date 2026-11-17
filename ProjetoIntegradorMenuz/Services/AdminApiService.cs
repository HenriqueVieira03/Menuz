using System.Net;
using System.Net.Http.Json;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.Services
{
    /// <summary>
    /// Chamadas da gerência (/api/admin e pedidos do dia). O token é enviado pelo HttpClient compartilhado
    /// (cabeçalho Authorization definido pelo AuthService no login).
    /// Erros: <see cref="StaffAccessException"/> (401/403), <see cref="OrderRejectedException"/> (400/409, com a mensagem da API)
    /// e HttpRequestException/TaskCanceledException (rede).
    /// </summary>
    public class AdminApiService
    {
        private readonly HttpClient _http;

        public AdminApiService(HttpClient http) => _http = http;

        // ---- Indicadores e pedidos ----

        public Task<Dashboard> GetDashboardAsync(DateTime fromUtc, DateTime toUtc) =>
            GetAsync<Dashboard>($"api/admin/dashboard?from={Iso(fromUtc)}&to={Iso(toUtc)}");

        public Task<List<OrderDto>> GetOrdersAsync(DateTime fromUtc, DateTime toUtc) =>
            GetAsync<List<OrderDto>>($"api/orders?includeFinished=true&from={Iso(fromUtc)}&to={Iso(toUtc)}");

        public Task<OrderDto> SetOrderStatusAsync(int orderId, OrderStatus status) =>
            SendAsync<OrderDto>(HttpMethod.Put, $"api/orders/{orderId}/status", new UpdateOrderStatusRequest { Status = status });

        // ---- Usuários da equipe ----

        public Task<List<UserAdmin>> GetUsersAsync() => GetAsync<List<UserAdmin>>("api/admin/users");

        public Task<UserAdmin> CreateUserAsync(SaveUserRequest request) =>
            SendAsync<UserAdmin>(HttpMethod.Post, "api/admin/users", request);

        public Task<UserAdmin> UpdateUserAsync(int id, SaveUserRequest request) =>
            SendAsync<UserAdmin>(HttpMethod.Put, $"api/admin/users/{id}", request);

        // ---- Contas das mesas ----

        public Task<List<OpenBill>> GetOpenBillsAsync() => GetAsync<List<OpenBill>>("api/admin/bills/open");

        public Task<BillDto> CloseBillAsync(int billId) =>
            SendAsync<BillDto>(HttpMethod.Post, $"api/admin/bills/{billId}/close", null);

        // ---- Categorias ----

        public Task<List<CategoryAdmin>> GetCategoriesAsync() => GetAsync<List<CategoryAdmin>>("api/admin/categories");

        public Task<CategoryAdmin> CreateCategoryAsync(SaveCategoryRequest request) =>
            SendAsync<CategoryAdmin>(HttpMethod.Post, "api/admin/categories", request);

        public Task<CategoryAdmin> UpdateCategoryAsync(int id, SaveCategoryRequest request) =>
            SendAsync<CategoryAdmin>(HttpMethod.Put, $"api/admin/categories/{id}", request);

        public Task DeleteCategoryAsync(int id) => SendAsync<object>(HttpMethod.Delete, $"api/admin/categories/{id}", null);

        // ---- Produtos ----

        public Task<List<ProductAdmin>> GetProductsAsync() => GetAsync<List<ProductAdmin>>("api/admin/products");

        public Task<ProductAdmin> GetProductAsync(int id) => GetAsync<ProductAdmin>($"api/admin/products/{id}");

        public Task<ProductAdmin> CreateProductAsync(SaveProductRequest request) =>
            SendAsync<ProductAdmin>(HttpMethod.Post, "api/admin/products", request);

        public Task<ProductAdmin> UpdateProductAsync(int id, SaveProductRequest request) =>
            SendAsync<ProductAdmin>(HttpMethod.Put, $"api/admin/products/{id}", request);

        public Task<ProductAdmin> SetAvailabilityAsync(int id, bool isAvailable) =>
            SendAsync<ProductAdmin>(HttpMethod.Put, $"api/admin/products/{id}/availability", new { isAvailable });

        public Task DeleteProductAsync(int id) => SendAsync<object>(HttpMethod.Delete, $"api/admin/products/{id}", null);

        // ---- Imagens ----

        /// <summary>Envia a foto escolhida (JPG/PNG/WEBP até 5 MB). Retorna o caminho relativo a gravar no produto.</summary>
        public async Task<string> UploadImageAsync(Stream content, string fileName)
        {
            using var form = new MultipartFormDataContent();
            var file = new StreamContent(content);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GuessContentType(fileName));
            form.Add(file, "file", fileName);

            var response = await _http.PostAsync("api/admin/images", form);
            await ThrowIfErrorAsync(response);

            var result = await response.Content.ReadFromJsonAsync<ImageUploadResult>();
            return result!.Path;
        }

        private static string GuessContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        private sealed class ImageUploadResult
        {
            public string Path { get; set; } = string.Empty;
        }

        // ---- Infra ----

        private static string Iso(DateTime utc) => Uri.EscapeDataString(utc.ToUniversalTime().ToString("O"));

        private async Task<T> GetAsync<T>(string url)
        {
            var response = await _http.GetAsync(url);
            await ThrowIfErrorAsync(response);
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body)
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null) request.Content = JsonContent.Create(body);

            var response = await _http.SendAsync(request);
            await ThrowIfErrorAsync(response);

            if (response.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(object))
                return default!;
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        private static async Task ThrowIfErrorAsync(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new StaffAccessException("Sua sessão expirou. Saia e entre novamente.");
                case HttpStatusCode.Forbidden:
                    throw new StaffAccessException("Seu perfil não tem permissão para esta ação.");
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Conflict:
                    var message = (await response.Content.ReadAsStringAsync()).Trim().Trim('"');
                    throw new OrderRejectedException(string.IsNullOrWhiteSpace(message) ? "Operação recusada pelo servidor." : message);
            }

            response.EnsureSuccessStatusCode();
        }
    }
}
