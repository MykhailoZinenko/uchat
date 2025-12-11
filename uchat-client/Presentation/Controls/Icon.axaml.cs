using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace uchat_client.Presentation.Controls;

public class Icon : TemplatedControl
{
    public static readonly StyledProperty<string> IconKeyProperty =
        AvaloniaProperty.Register<Icon, string>(nameof(IconKey));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(Size), 24.0);

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<Icon, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<Icon, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(StrokeThickness), 0.0);

    public static readonly StyledProperty<StreamGeometry?> IconDataProperty =
        AvaloniaProperty.Register<Icon, StreamGeometry?>(nameof(IconData));

    static Icon()
    {
        IconKeyProperty.Changed.AddClassHandler<Icon>((icon, e) => icon.OnIconKeyChanged(e));
        SizeProperty.Changed.AddClassHandler<Icon>((icon, e) => icon.OnSizeChanged(e));
    }

    public string IconKey
    {
        get => GetValue(IconKeyProperty);
        set => SetValue(IconKeyProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public StreamGeometry? IconData
    {
        get => GetValue(IconDataProperty);
        private set => SetValue(IconDataProperty, value);
    }

    private void OnIconKeyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is string key && !string.IsNullOrEmpty(key))
        {
            if (Application.Current?.TryGetResource(key, null, out var resource) == true)
            {
                IconData = resource as StreamGeometry;
            }
        }
    }

    private void OnSizeChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is double size)
        {
            Width = size;
            Height = size;
        }
    }
}
