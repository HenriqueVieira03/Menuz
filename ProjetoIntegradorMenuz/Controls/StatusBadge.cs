using Microsoft.Maui.Controls.Shapes;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.Controls
{
    /// <summary>
    /// Selo de status do pedido (ponto + texto) com as cores do tema.
    /// Ex.: &lt;controls:StatusBadge Status="{Binding Status}" Text="{Binding StatusText}" /&gt;.
    /// Sem Text, usa o nome padrão do status.
    /// </summary>
    public class StatusBadge : ContentView
    {
        public static readonly BindableProperty StatusProperty = BindableProperty.Create(
            nameof(Status), typeof(OrderStatus), typeof(StatusBadge), OrderStatus.Pending, propertyChanged: (b, _, _) => ((StatusBadge)b).Update());

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text), typeof(string), typeof(StatusBadge), null, propertyChanged: (b, _, _) => ((StatusBadge)b).Update());

        private readonly Border _frame = new()
        {
            Padding = new Thickness(8, 3, 10, 3),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 6 }
        };
        private readonly Ellipse _dot = new() { WidthRequest = 6, HeightRequest = 6, VerticalOptions = LayoutOptions.Center };
        private readonly Label _label = new() { FontFamily = "OpenSansSemibold", FontSize = 12, VerticalOptions = LayoutOptions.Center };

        public StatusBadge()
        {
            _frame.Content = new HorizontalStackLayout { Spacing = 6, Children = { _dot, _label } };
            Content = _frame;
            HorizontalOptions = LayoutOptions.Start;
            VerticalOptions = LayoutOptions.Center;
            Update();
        }

        public OrderStatus Status { get => (OrderStatus)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }
        public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

        private void Update()
        {
            var (bg, fg, name) = Status switch
            {
                OrderStatus.Pending => ("MzPrimarySoft", "MzPrimaryStrong", "Novo"),
                OrderStatus.Preparing => ("MzWarningSoft", "MzWarningText", "Em preparo"),
                OrderStatus.Ready => ("MzSuccessSoft", "MzSuccessText", "Pronto"),
                OrderStatus.Delivered => ("MzSurfaceAlt", "MzTextMuted", "Entregue"),
                _ => ("MzDangerSoft", "MzDangerText", "Cancelado")
            };

            _frame.SetDynamicResource(Border.BackgroundColorProperty, bg);
            _label.SetDynamicResource(Label.TextColorProperty, fg);
            _dot.SetDynamicResource(Shape.FillProperty, fg);
            _label.Text = string.IsNullOrEmpty(Text) ? name : Text;
        }
    }
}
