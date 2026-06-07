namespace ShareXMac.Models;

public enum AnnotationTool { Arrow, Rectangle, Text }

public abstract record Annotation(string Color, double StrokeWidth = 0);

public record ArrowAnnotation(
    double X1, double Y1, double X2, double Y2,
    string Color, double StrokeWidth = 3)
    : Annotation(Color, StrokeWidth);

public record RectAnnotation(
    double X, double Y, double Width, double Height,
    string Color, double StrokeWidth = 3)
    : Annotation(Color, StrokeWidth);

public record TextAnnotation(
    double X, double Y, string Text,
    string Color, double FontSize = 18)
    : Annotation(Color);
