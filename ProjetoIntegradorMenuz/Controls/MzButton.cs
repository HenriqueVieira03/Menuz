using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace ProjetoIntegradorMenuz.Controls
{
    public enum ButtonVariant { Primary, Secondary, Outline, Ghost, Danger, Subtle }

    /// <summary>
    /// Botão com ícone e/ou texto, no padrão visual do app.
    /// Ex.: &lt;controls:MzButton Icon="plus" Text="Novo produto" Variant="Primary" Command="{Binding NewProductCommand}" /&gt;.
    /// Sem Text vira botão quadrado só de ícone (voltar, fechar, +, −).
    /// As cores vêm dos tokens do tema (DynamicResource), então acompanham a troca de cor/tema.
    /// </summary>
    public class MzButton : ContentView
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text), typeof(string), typeof(MzButton), null, propertyChanged: (b, _, _) => ((MzButton)b).Rebuild());

        public static readonly BindableProperty IconProperty = BindableProperty.Create(
            nameof(Icon), typeof(string), typeof(MzButton), null, propertyChanged: (b, _, _) => ((MzButton)b).Rebuild());

        public static readonly BindableProperty VariantProperty = BindableProperty.Create(
            nameof(Variant), typeof(ButtonVariant), typeof(MzButton), ButtonVariant.Outline, propertyChanged: (b, _, _) => ((MzButton)b).ApplyVariant());

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command), typeof(ICommand), typeof(MzButton), null, propertyChanged: OnCommandChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter), typeof(object), typeof(MzButton), null, propertyChanged: (b, _, _) => ((MzButton)b).UpdateEnabled());

        public static readonly BindableProperty SizeProperty = BindableProperty.Create(
            nameof(Size), typeof(double), typeof(MzButton), 40d, propertyChanged: (b, _, _) => ((MzButton)b).Rebuild());

        public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
            nameof(IsLoading), typeof(bool), typeof(MzButton), false, propertyChanged: (b, _, _) => ((MzButton)b).Rebuild());

        private readonly Border _frame = new();
        private readonly MzIcon _icon = new() { Size = 18 };
        private readonly Label _label = new()
        {
            FontFamily = "OpenSansSemibold",
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.NoWrap
        };
        private readonly ActivityIndicator _spinner = new() { WidthRequest = 16, HeightRequest = 16, IsRunning = true };
        private readonly HorizontalStackLayout _row = new()
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        public event EventHandler? Clicked;

        public MzButton()
        {
            _frame.StrokeShape = new RoundRectangle { CornerRadius = 8 };
            _frame.Padding = new Thickness(14, 0);
            _frame.Content = _row;
            Content = _frame;

            var tap = new TapGestureRecognizer();
            tap.Tapped += OnTapped;
            _frame.GestureRecognizers.Add(tap);

            // Resposta ao toque: leve redução enquanto pressionado.
            var pointer = new PointerGestureRecognizer();
            pointer.PointerPressed += (_, _) => { if (IsEnabled) _frame.Opacity = 0.8; };
            pointer.PointerReleased += (_, _) => _frame.Opacity = 1;
            pointer.PointerExited += (_, _) => _frame.Opacity = 1;
            _frame.GestureRecognizers.Add(pointer);

            HorizontalOptions = LayoutOptions.Start;
            VerticalOptions = LayoutOptions.Center;
            ApplyVariant();
            Rebuild();
        }

        public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
        public ButtonVariant Variant { get => (ButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
        public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
        public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

        /// <summary>Altura do botão (e largura, quando é só ícone).</summary>
        public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

        public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }

        private bool IconOnly => string.IsNullOrEmpty(Text);

        private void Rebuild()
        {
            // O mesmo layout é reaproveitado: ícone e texto nunca ficam com dois "pais" ao mudar Text/Icon.
            var row = _row;
            row.Clear();

            if (IsLoading) row.Add(_spinner);
            else if (!string.IsNullOrEmpty(Icon))
            {
                _icon.Name = Icon;
                _icon.Size = Size >= 48 ? 20 : 18;
                row.Add(_icon);
            }
            if (!IconOnly)
            {
                _label.Text = Text;
                _label.FontSize = Size >= 48 ? 15 : 14;
                row.Add(_label);
            }

            _frame.HeightRequest = Size;
            _frame.WidthRequest = IconOnly ? Size : -1;
            _frame.Padding = IconOnly ? new Thickness(0) : new Thickness(Size >= 48 ? 20 : 14, 0);

            SemanticProperties.SetDescription(this, Text ?? Icon);
            AutomationProperties.SetName(_frame, Text ?? Icon);
        }

        private void ApplyVariant()
        {
            var (bg, fg, stroke) = Variant switch
            {
                ButtonVariant.Primary => ("MzPrimary", "MzOnPrimary", "MzPrimary"),
                ButtonVariant.Secondary => ("MzPrimarySoft", "MzPrimaryStrong", "MzPrimarySoft"),
                ButtonVariant.Ghost => ("MzTransparent", "MzTextMuted", "MzTransparent"),
                ButtonVariant.Danger => ("MzSurface", "MzDangerText", "MzDangerBorder"),
                ButtonVariant.Subtle => ("MzSurfaceAlt", "MzText", "MzSurfaceAlt"),
                _ => ("MzSurface", "MzText", "MzBorderStrong")
            };

            _frame.SetDynamicResource(Border.BackgroundColorProperty, bg);
            _frame.SetDynamicResource(Border.StrokeProperty, stroke);
            _frame.StrokeThickness = 1;
            _label.SetDynamicResource(Label.TextColorProperty, fg);
            _icon.SetDynamicResource(MzIcon.ColorProperty, fg);
            _spinner.SetDynamicResource(ActivityIndicator.ColorProperty, fg);
        }

        private static void OnCommandChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var button = (MzButton)bindable;
            if (oldValue is ICommand old) old.CanExecuteChanged -= button.OnCanExecuteChanged;
            if (newValue is ICommand cmd) cmd.CanExecuteChanged += button.OnCanExecuteChanged;
            button.UpdateEnabled();
        }

        private void OnCanExecuteChanged(object? sender, EventArgs e) => UpdateEnabled();

        private void UpdateEnabled()
        {
            var enabled = Command?.CanExecute(CommandParameter) ?? true;
            IsEnabled = enabled;
            Opacity = enabled ? 1 : 0.45;
        }

        private void OnTapped(object? sender, TappedEventArgs e)
        {
            if (!IsEnabled || IsLoading) return;
            Clicked?.Invoke(this, EventArgs.Empty);
            if (Command?.CanExecute(CommandParameter) == true)
                Command.Execute(CommandParameter);
        }
    }
}
