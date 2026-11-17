namespace ProjetoIntegradorMenuz.Controls
{
    /// <summary>
    /// Ícones de traço (estilo Feather/Lucide, licença MIT/ISC) desenhados numa grade de 24×24.
    /// Use pelo nome no controle <see cref="MzIcon"/>: &lt;controls:MzIcon Name="cart" /&gt;.
    /// Para incluir um ícone novo, adicione aqui o "d" do SVG original (viewBox 0 0 24 24, só traço).
    /// </summary>
    public static class IconGeometry
    {
        public static readonly IReadOnlyDictionary<string, string> Paths = new Dictionary<string, string>
        {
            // Navegação e ações
            ["arrow-left"] = "M19 12H5 M12 19l-7-7 7-7",
            ["arrow-right"] = "M5 12h14 M12 5l7 7-7 7",
            ["chevron-right"] = "M9 18l6-6-6-6",
            ["chevron-down"] = "M6 9l6 6 6-6",
            ["plus"] = "M12 5v14 M5 12h14",
            ["minus"] = "M5 12h14",
            ["x"] = "M18 6L6 18 M6 6l12 12",
            ["check"] = "M20 6L9 17l-5-5",
            ["refresh"] = "M23 4v6h-6 M1 20v-6h6 M3.51 9a9 9 0 0 1 14.85-3.36L23 10 M1 14l4.64 4.36A9 9 0 0 0 20.49 15",
            ["edit"] = "M17 3a2.83 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z",
            ["trash"] = "M3 6h18 M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6 M10 11v6 M14 11v6 M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2",
            ["upload"] = "M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4 M17 8l-5-5-5 5 M12 3v12",
            ["log-out"] = "M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4 M16 17l5-5-5-5 M21 12H9",
            ["log-in"] = "M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4 M10 17l5-5-5-5 M15 12H3",

            // Objetos
            ["bag"] = "M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z M3 6h18 M16 10a4 4 0 0 1-8 0",
            ["receipt"] = "M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6 M16 13H8 M16 17H8 M10 9H8",
            ["lock"] = "M5 11h14a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2z M7 11V7a5 5 0 0 1 10 0v4",
            ["image"] = "M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M8.5 10a1.5 1.5 0 1 0 0-3a1.5 1.5 0 0 0 0 3z M21 15l-5-5L5 21",
            ["user"] = "M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2 M12 11a4 4 0 1 0 0-8a4 4 0 0 0 0 8z",
            ["users"] = "M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8a4 4 0 0 0 0 8z M23 21v-2a4 4 0 0 0-3-3.87 M16 3.13a4 4 0 0 1 0 7.75",
            ["grid"] = "M3 3h7v7H3z M14 3h7v7h-7z M14 14h7v7h-7z M3 14h7v7H3z",
            ["chart"] = "M18 20V10 M12 20V4 M6 20v-6",
            ["list"] = "M8 6h13 M8 12h13 M8 18h13 M3 6h.01 M3 12h.01 M3 18h.01",
            ["clock"] = "M12 22a10 10 0 1 0 0-20a10 10 0 0 0 0 20z M12 6v6l4 2",
            ["book"] = "M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z",
            ["monitor"] = "M4 3h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M8 21h8 M12 17v4",
            ["hash"] = "M4 9h16 M4 15h16 M10 3L8 21 M16 3l-2 18",
            ["droplet"] = "M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z",
            ["card"] = "M3 4h18a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z M1 10h22",
            ["tag"] = "M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z M7 7h.01",
            ["inbox"] = "M22 12h-6l-2 3h-4l-2-3H2 M5.45 5.11L2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z",
            ["utensils"] = "M3 2v7a2 2 0 0 0 2 2h4a2 2 0 0 0 2-2V2 M7 2v20 M21 15V2a5 5 0 0 0-5 5v6a2 2 0 0 0 2 2h3 M21 15v7",
            ["wifi-off"] = "M1 1l22 22 M16.72 11.06A10.94 10.94 0 0 1 19 12.55 M5 12.55a10.94 10.94 0 0 1 5.17-2.39 M10.71 5.05A16 16 0 0 1 22.58 9 M1.42 9a15.91 15.91 0 0 1 4.7-2.88 M8.53 16.11a6 6 0 0 1 6.95 0 M12 20h.01",

            // Estados
            ["alert"] = "M12 22a10 10 0 1 0 0-20a10 10 0 0 0 0 20z M12 8v4 M12 16h.01",
            ["check-circle"] = "M22 11.08V12a10 10 0 1 1-5.93-9.14 M22 4L12 14.01l-3-3",
            ["eye-off"] = "M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94 M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19 M14.12 14.12a3 3 0 1 1-4.24-4.24 M1 1l22 22",
        };
    }
}
