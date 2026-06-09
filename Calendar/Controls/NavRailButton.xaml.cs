using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Calendar.Controls;

public sealed partial class NavRailButton : UserControl
{
    // Higher-contrast inactive colours, per theme, so icons stay readable on a
    // translucent/tinted rail.
    private static readonly SolidColorBrush MutedLight = new(Color.FromArgb(0xFF, 0x4D, 0x4D, 0x50));
    private static readonly SolidColorBrush MutedDark = new(Color.FromArgb(0xFF, 0xD2, 0xD2, 0xD6));
    private static readonly SolidColorBrush HoverBrush = new(Color.FromArgb(0x18, 0x80, 0x80, 0x80));
    private static readonly SolidColorBrush ClearBrush = new(Colors.Transparent);

    public event EventHandler? Selected;

    public NavRailButton()
    {
        InitializeComponent();
        ActualThemeChanged += (_, _) => UpdateVisual();
        UpdateVisual();
    }

    private SolidColorBrush Muted => ActualTheme == ElementTheme.Dark ? MutedDark : MutedLight;

    public string Section { get; set; } = string.Empty;

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(NavRailButton),
            new PropertyMetadata("", OnAnyChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(NavRailButton),
            new PropertyMetadata("", OnAnyChanged));

    public Brush? Accent
    {
        get => (Brush?)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }
    public static readonly DependencyProperty AccentProperty =
        DependencyProperty.Register(nameof(Accent), typeof(Brush), typeof(NavRailButton),
            new PropertyMetadata(null, OnAnyChanged));

    public Brush? Tint
    {
        get => (Brush?)GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }
    public static readonly DependencyProperty TintProperty =
        DependencyProperty.Register(nameof(Tint), typeof(Brush), typeof(NavRailButton),
            new PropertyMetadata(null, OnAnyChanged));

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(NavRailButton),
            new PropertyMetadata(false, OnAnyChanged));

    private static void OnAnyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((NavRailButton)d).UpdateVisual();

    private void UpdateVisual()
    {
        IconElement.Glyph = Glyph ?? string.Empty;
        LabelElement.Text = Label ?? string.Empty;

        if (IsSelected)
        {
            Root.Background = Tint ?? ClearBrush;
            IconElement.Foreground = Accent ?? Muted;
            LabelElement.Foreground = Accent ?? Muted;
            LabelElement.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        }
        else
        {
            Root.Background = ClearBrush;
            IconElement.Foreground = Muted;
            LabelElement.Foreground = Muted;
            LabelElement.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
        }
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e) => Selected?.Invoke(this, EventArgs.Empty);

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!IsSelected) Root.Background = HoverBrush;
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!IsSelected) Root.Background = ClearBrush;
    }
}
