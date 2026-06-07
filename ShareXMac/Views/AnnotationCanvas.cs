using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ShareXMac.Models;

namespace ShareXMac.Views;

public class AnnotationCanvas : Control
{
    public static readonly StyledProperty<Bitmap?> SourceProperty =
        AvaloniaProperty.Register<AnnotationCanvas, Bitmap?>(nameof(Source));

    public static readonly StyledProperty<AnnotationLayer?> LayerProperty =
        AvaloniaProperty.Register<AnnotationCanvas, AnnotationLayer?>(nameof(Layer));

    public static readonly StyledProperty<Annotation?> InProgressProperty =
        AvaloniaProperty.Register<AnnotationCanvas, Annotation?>(nameof(InProgress));

    static AnnotationCanvas()
    {
        AffectsRender<AnnotationCanvas>(SourceProperty, InProgressProperty);
        AffectsMeasure<AnnotationCanvas>(SourceProperty);
        // Layer mutations fire Changed event; the window code-behind calls InvalidateVisual() there.
    }

    public Bitmap? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public AnnotationLayer? Layer
    {
        get => GetValue(LayerProperty);
        set => SetValue(LayerProperty, value);
    }

    public Annotation? InProgress
    {
        get => GetValue(InProgressProperty);
        set => SetValue(InProgressProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
        => Source?.Size ?? availableSize;

    protected override Size ArrangeOverride(Size finalSize)
        => Source?.Size ?? finalSize;

    public override void Render(DrawingContext ctx)
    {
        if (Source != null)
        {
            var rect = new Rect(Source.Size);
            ctx.DrawImage(Source, rect);
        }

        if (Layer != null)
            foreach (var a in Layer.Items)
                DrawAnnotation(ctx, a);

        if (InProgress != null)
            DrawAnnotation(ctx, InProgress);
    }

    private static void DrawAnnotation(DrawingContext ctx, Annotation a)
    {
        switch (a)
        {
            case ArrowAnnotation arrow:
                DrawArrow(ctx, arrow);
                break;

            case RectAnnotation rect:
                var pen = MakePen(a);
                var r = new Rect(
                    new Point(Math.Min(rect.X, rect.X + rect.Width),
                              Math.Min(rect.Y, rect.Y + rect.Height)),
                    new Size(Math.Abs(rect.Width), Math.Abs(rect.Height)));
                ctx.DrawRectangle(null, pen, r);
                break;

            case TextAnnotation text:
                var brush = new SolidColorBrush(Color.Parse(text.Color));
                var ft = new FormattedText(
                    text.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    text.FontSize,
                    brush);
                ctx.DrawText(ft, new Point(text.X, text.Y));
                break;
        }
    }

    private static void DrawArrow(DrawingContext ctx, ArrowAnnotation a)
    {
        var pen   = MakePen(a);
        var start = new Point(a.X1, a.Y1);
        var end   = new Point(a.X2, a.Y2);
        ctx.DrawLine(pen, start, end);

        double dx = a.X2 - a.X1;
        double dy = a.Y2 - a.Y1;
        if (Math.Abs(dx) < 0.5 && Math.Abs(dy) < 0.5) return; // too short for arrowhead

        double angle     = Math.Atan2(dy, dx);
        const double len = 14;
        const double spread = Math.PI / 6; // 30°

        ctx.DrawLine(pen, end,
            new Point(a.X2 - len * Math.Cos(angle - spread),
                      a.Y2 - len * Math.Sin(angle - spread)));
        ctx.DrawLine(pen, end,
            new Point(a.X2 - len * Math.Cos(angle + spread),
                      a.Y2 - len * Math.Sin(angle + spread)));
    }

    private static Pen MakePen(Annotation a)
        => new(new SolidColorBrush(Color.Parse(a.Color)), a.StrokeWidth);
}
