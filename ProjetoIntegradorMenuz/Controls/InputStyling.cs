using Microsoft.Maui.Handlers;

namespace ProjetoIntegradorMenuz.Controls
{
    /// <summary>
    /// Remove a moldura/sublinhado nativos dos campos de texto: a moldura fica por conta do Border com
    /// estilo MzInput, igual em Windows e Android. Sem isso o Windows desenha a própria caixa (com cores
    /// do tema do sistema) e o Android desenha um sublinhado.
    /// </summary>
    public static class InputStyling
    {
        public static void Apply()
        {
#if WINDOWS
            static void Plain(Microsoft.UI.Xaml.Controls.Control control)
            {
                var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                var zero = new Microsoft.UI.Xaml.Thickness(0);

                control.BorderThickness = zero;
                control.Background = transparent;
                foreach (var key in new[]
                {
                    "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused",
                    "TextControlBackgroundDisabled", "TextControlBorderBrush", "TextControlBorderBrushPointerOver",
                    "TextControlBorderBrushFocused", "TextControlBorderBrushDisabled"
                })
                    control.Resources[key] = transparent;
                control.Resources["TextControlBorderThemeThickness"] = zero;
                control.Resources["TextControlBorderThemeThicknessFocused"] = zero;
            }

            EntryHandler.Mapper.AppendToMapping("MzPlain", (handler, _) => Plain(handler.PlatformView));
            EditorHandler.Mapper.AppendToMapping("MzPlain", (handler, _) => Plain(handler.PlatformView));

            // Picker (ComboBox): sem caixa própria e ocupando toda a largura da moldura.
            PickerHandler.Mapper.AppendToMapping("MzPlain", (handler, _) =>
            {
                var combo = handler.PlatformView;
                combo.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                combo.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                combo.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
            });

            // O CheckBox do WinUI reserva 120 px de largura mínima, afastando o texto ao lado.
            CheckBoxHandler.Mapper.AppendToMapping("MzCompact", (handler, _) => handler.PlatformView.MinWidth = 0);
#elif ANDROID
            static void Plain(Android.Widget.EditText view) =>
                view.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);

            EntryHandler.Mapper.AppendToMapping("MzPlain", (handler, _) => Plain(handler.PlatformView));
            EditorHandler.Mapper.AppendToMapping("MzPlain", (handler, _) => Plain(handler.PlatformView));
            PickerHandler.Mapper.AppendToMapping("MzPlain", (handler, _) => Plain(handler.PlatformView));
#endif
        }
    }
}
