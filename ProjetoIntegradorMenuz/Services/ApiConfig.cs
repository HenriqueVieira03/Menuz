namespace ProjetoIntegradorMenuz.Services
{
    public static class ApiConfig
    {
        // Android Emulator enxerga o localhost do PC em 10.0.2.2.
        // Em tablet físico, troque pelo IP do PC na rede Wi-Fi (ex.: http://192.168.0.10:5080/).
        public static string BaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5080/"
                : "http://localhost:5080/";

        /// <summary>
        /// Converte o ImageUrl do produto em endereço completo. Fotos enviadas pelo app ficam no servidor
        /// como caminho relativo ("uploads/abc.jpg"); links externos já vêm completos.
        /// </summary>
        public static Uri? ResolveImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return null;
            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absolute) &&
                (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
                return absolute;
            return Uri.TryCreate(new Uri(BaseUrl), imageUrl.TrimStart('/'), out var relative) ? relative : null;
        }
    }
}
