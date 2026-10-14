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
    }
}
