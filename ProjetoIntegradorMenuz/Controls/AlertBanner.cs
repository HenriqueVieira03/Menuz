using Microsoft.Maui.Controls.Shapes;

namespace ProjetoIntegradorMenuz.Controls
{
    public enum AlertKind { Error, Warning, Info, Success }

    /// <summary>
    /// Faixa de aviso com ícone (erro, atenção, informação). Some sozinha quando Text está vazio.
    /// Ex.: &lt;controls:AlertBanner Kind="Error" Text="{Binding ErrorMessage}" /&gt;.
    /// </summary>
    public class AlertBanner : ContentView
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text), typeof(string), typeof(AlertBanner), null, propertyChanged: (b, _, _) => ((AlertBanner)b).Update());

        public static readonly BindableProperty KindProperty = BindableProperty.Create(
            nameof(Kind), typeof(AlertKind), typeof(AlertBanner), AlertKind.Error, propertyChanged: (b, _, _) => ((AlertBanner)b).Update());

        private readonly Border _frame = new()
        {
            Padding = new Thickness(12, 10),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 }
        };
        private readonly MzIcon _icon = new() { Size = 18, VerticalOptions = LayoutOptions.Start };
        private readonly Label _label = new() { FontSize = 13, LineBreakMode = LineBreakMode.WordWrap, VerticalOptions = LayoutOptions.Center };

        public AlertBanner()
        {
            var grid = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 10 };
            grid.Add(_icon, 0, 0);
            grid.Add(_label, 1, 0);
            _frame.Content = grid;
            Content = _frame;
            Update();
        }

        public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public AlertKind Kind { get => (AlertKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }

        private void Update()
        {
            var (bg, fg, icon) = Kind switch
            {
                AlertKind.Warning => ("MzWarningSoft", "MzWarningText", "alert"),
                AlertKind.Info => ("MzSurfaceAlt", "MzTextMuted", "alert"),
                AlertKind.Success => ("MzSuccessSoft", "MzSuccessText", "check-circle"),
                _ => ("MzDangerSoft", "MzDangerText", "alert")
            };

            _frame.SetDynamicResource(Border.BackgroundColorProperty, bg);
            _frame.SetDynamicResource(Border.StrokeProperty, bg);
            _label.SetDynamicResource(Label.TextColorProperty, fg);
            _icon.SetDynamicResource(MzIcon.ColorProperty, fg);
            _icon.Name = icon;
            _label.Text = Text;
            IsVisible = !string.IsNullOrWhiteSpace(Text);
        }
    }
}
