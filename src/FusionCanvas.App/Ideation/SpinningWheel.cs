using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace FusionCanvas.App.Ideation;

public sealed class SpinningWheel : Control
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<SpinningWheel, bool>(nameof(IsActive));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<SpinningWheel, IBrush?>(nameof(Foreground), Brushes.Gray);

    private readonly DispatcherTimer _timer;
    private int _frame;

    public SpinningWheel()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _timer.Tick += (_, _) =>
        {
            _frame = (_frame + 1) % 8;
            InvalidateVisual();
        };
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsActiveProperty)
        {
            if (change.GetNewValue<bool>())
            {
                _timer.Start();
            }
            else
            {
                _timer.Stop();
                _frame = 0;
                InvalidateVisual();
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var center = Bounds.Center;
        var orbit = Math.Max(1, Math.Min(Bounds.Width, Bounds.Height) * 0.34);
        var dotRadius = Math.Max(1, Math.Min(Bounds.Width, Bounds.Height) * 0.09);
        var color = Foreground is ISolidColorBrush solid ? solid.Color : Colors.Gray;

        for (var index = 0; index < 8; index++)
        {
            var angle = Math.PI * 2 * index / 8;
            var opacityRank = (index - _frame + 8) % 8;
            var opacity = 1d - (opacityRank * 0.1);
            var brush = new SolidColorBrush(color, opacity);
            var point = new Point(
                center.X + Math.Cos(angle) * orbit,
                center.Y + Math.Sin(angle) * orbit);
            context.DrawEllipse(brush, null, point, dotRadius, dotRadius);
        }
    }
}
