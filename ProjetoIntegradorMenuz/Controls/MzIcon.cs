using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace ProjetoIntegradorMenuz.Controls
{
    /// <summary>
    /// Ícone vetorial de traço. Ex.: &lt;controls:MzIcon Name="bag" Size="20" Color="{DynamicResource MzText}" /&gt;.
    /// O desenho é escalado a partir da grade 24×24 sem engrossar o traço, então fica nítido em qualquer tamanho.
    /// </summary>
    public class MzIcon : ContentView
    {
        private static readonly PathGeometryConverter Converter = new();

        public static readonly new BindableProperty NameProperty = BindableProperty.Create(
            nameof(Name), typeof(string), typeof(MzIcon), null, propertyChanged: (b, _, _) => ((MzIcon)b).UpdateGeometry());

        public static readonly BindableProperty SizeProperty = BindableProperty.Create(
            nameof(Size), typeof(double), typeof(MzIcon), 20d, propertyChanged: (b, _, _) => ((MzIcon)b).UpdateSize());

        public static readonly BindableProperty ColorProperty = BindableProperty.Create(
            nameof(Color), typeof(Color), typeof(MzIcon), Colors.Black,
            propertyChanged: (b, _, n) => ((MzIcon)b)._path.Stroke = new SolidColorBrush((Color)n));

        private readonly Path _path = new()
        {
            Fill = null,
            Stroke = new SolidColorBrush(Colors.Black),
            StrokeThickness = 1.8,
            StrokeLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Aspect = Stretch.None,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start
        };

        public MzIcon()
        {
            Content = _path;
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Center;
            InputTransparent = true;
            UpdateSize();
        }

        /// <summary>Nome do ícone em <see cref="IconGeometry.Paths"/>.</summary>
        public new string? Name
        {
            get => (string?)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        private void UpdateGeometry() =>
            // Cada ícone ganha a sua própria geometria (não compartilhar a mesma instância entre controles).
            _path.Data = Name is { } name && IconGeometry.Paths.TryGetValue(name, out var data)
                ? Converter.ConvertFromInvariantString(data) as Geometry
                : null;

        private void UpdateSize()
        {
            WidthRequest = HeightRequest = Size;
            _path.WidthRequest = _path.HeightRequest = Size;
            var scale = Size / 24d;
            _path.RenderTransform = new ScaleTransform(scale, scale);
        }
    }
}
