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
    }
}
