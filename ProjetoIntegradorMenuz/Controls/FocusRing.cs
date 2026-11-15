namespace ProjetoIntegradorMenuz.Controls
{
    /// <summary>
    /// Realça a moldura (Border pai) de um campo quando ele recebe foco.
    /// Ligado nos estilos implícitos de Entry/Editor/Picker em Menuz.xaml:
    /// todo campo colocado dentro de um Border com o estilo MzInput ganha o realce sem código extra.
    /// </summary>
    public static class FocusRing
    {
        public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached(
            "IsEnabled", typeof(bool), typeof(FocusRing), false, propertyChanged: OnIsEnabledChanged);

        public static bool GetIsEnabled(BindableObject view) => (bool)view.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(BindableObject view, bool value) => view.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not VisualElement element) return;

            element.Focused -= OnFocused;
            element.Unfocused -= OnUnfocused;
            if ((bool)newValue)
            {
                element.Focused += OnFocused;
                element.Unfocused += OnUnfocused;
            }
        }

        private static void OnFocused(object? sender, FocusEventArgs e) => Apply(sender, focused: true);
        private static void OnUnfocused(object? sender, FocusEventArgs e) => Apply(sender, focused: false);

        private static void Apply(object? sender, bool focused)
        {
            // A moldura pode estar logo acima do campo ou envolver um Grid com prefixo (ex.: "R$" + Entry).
            Border? border = null;
            var parent = (sender as Element)?.Parent;
            for (var level = 0; level < 3 && parent is not null; level++, parent = parent.Parent)
                if (parent is Border b && IsInputFrame(b)) { border = b; break; }
            if (border is null) return;

            var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
            var key = focused
                ? (dark ? "MzPrimaryDark" : "MzPrimaryLight")
                : (dark ? "MzBorderDark" : "MzInputBorderLight");

            if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
                border.Stroke = color;
            border.StrokeThickness = focused ? 2 : 1;
        }

        /// <summary>O Border usa o estilo MzInput (ou um estilo baseado nele)?</summary>
        private static bool IsInputFrame(Border border)
        {
            if (Application.Current?.Resources.TryGetValue("MzInput", out var value) != true || value is not Style input)
                return false;

            for (var style = border.Style; style is not null; style = style.BasedOn)
                if (ReferenceEquals(style, input)) return true;
            return false;
        }
    }
}
