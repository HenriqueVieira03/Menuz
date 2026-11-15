using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjetoIntegradorMenuz.Services
{
    /// <summary>
    /// Número da mesa em que este tablet está. Fica salvo no aparelho (Preferences),
    /// então só precisa ser configurado uma vez pela equipe do restaurante.
    /// </summary>
    public partial class TableService : ObservableObject
    {
        private const string PreferenceKey = "menuz.table_number";
        public const int MaxTableNumber = 999; // mesmo limite da API

        public TableService()
        {
            var saved = Preferences.Default.Get(PreferenceKey, 0);
            tableNumber = saved is >= 1 and <= MaxTableNumber ? saved : null;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsConfigured))]
        [NotifyPropertyChangedFor(nameof(DisplayText))]
        [NotifyPropertyChangedFor(nameof(CartHint))]
        private int? tableNumber;

        public bool IsConfigured => TableNumber is not null;
        public string DisplayText => TableNumber is { } n ? $"Mesa {n}" : "Definir mesa";

        public string CartHint => TableNumber is { } n
            ? $"O pedido será enviado à cozinha para a Mesa {n}."
            : "A mesa será solicitada ao finalizar o pedido.";

        partial void OnTableNumberChanged(int? value)
        {
            if (value is null) Preferences.Default.Remove(PreferenceKey);
            else Preferences.Default.Set(PreferenceKey, value.Value);
        }

        /// <summary>Pergunta o número da mesa. Retorna true se ficou configurado.</summary>
        public async Task<bool> PromptAsync(Page page)
        {
            var input = await page.DisplayPromptAsync(
                "Número da mesa",
                "Informe a mesa onde este tablet está. Os pedidos chegam na cozinha com esse número.",
                accept: "Salvar",
                cancel: "Cancelar",
                placeholder: "Ex.: 12",
                maxLength: 3,
                keyboard: Keyboard.Numeric,
                initialValue: TableNumber?.ToString() ?? string.Empty);

            if (input is null) return IsConfigured; // cancelou

            if (int.TryParse(input.Trim(), out var n) && n is >= 1 and <= MaxTableNumber)
            {
                TableNumber = n;
                return true;
            }

            await page.DisplayAlertAsync("Mesa inválida", $"Digite um número de 1 a {MaxTableNumber}.", "OK");
            return IsConfigured;
        }
    }
}
