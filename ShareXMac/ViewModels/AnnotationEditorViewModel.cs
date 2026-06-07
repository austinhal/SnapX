using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Models;

namespace ShareXMac.ViewModels;

public partial class AnnotationEditorViewModel : ObservableObject
{
    private readonly Bitmap _originalBitmap;
    private Point? _dragStart;

    [ObservableProperty] private AnnotationTool _currentTool = AnnotationTool.Arrow;
    [ObservableProperty] private string         _currentColor = "#FF0000";
    [ObservableProperty] private double         _strokeWidth  = 3;
    [ObservableProperty] private Annotation?    _inProgressAnnotation;

    public AnnotationLayer Layer { get; } = new();
    public Bitmap OriginalBitmap => _originalBitmap;

    public event Action? DoneRequested;
    public event Action? CancelRequested;
    public event Action<byte[]>? Completed;

    public AnnotationEditorViewModel(byte[] imageData)
    {
        using var ms = new MemoryStream(imageData);
        _originalBitmap = new Bitmap(ms);
    }

    partial void OnCurrentToolChanged(AnnotationTool value)
    {
        OnPropertyChanged(nameof(IsArrowTool));
        OnPropertyChanged(nameof(IsRectTool));
        OnPropertyChanged(nameof(IsTextTool));
    }

    public bool IsArrowTool
    {
        get => CurrentTool == AnnotationTool.Arrow;
        set { if (value) CurrentTool = AnnotationTool.Arrow; }
    }

    public bool IsRectTool
    {
        get => CurrentTool == AnnotationTool.Rectangle;
        set { if (value) CurrentTool = AnnotationTool.Rectangle; }
    }

    public bool IsTextTool
    {
        get => CurrentTool == AnnotationTool.Text;
        set { if (value) CurrentTool = AnnotationTool.Text; }
    }

    public void StartDrag(Point p) => _dragStart = p;

    public void UpdateDrag(Point p)
    {
        if (_dragStart == null) return;
        InProgressAnnotation = MakeAnnotation(_dragStart.Value, p);
    }

    public void EndDrag(Point p)
    {
        if (_dragStart == null) return;
        var a = MakeAnnotation(_dragStart.Value, p);
        if (a != null) Layer.Add(a);
        _dragStart = null;
        InProgressAnnotation = null;
    }

    public void PlaceText(Point p, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Layer.Add(new TextAnnotation(p.X, p.Y, text, CurrentColor));
    }

    public void Complete(byte[] flattenedBytes) => Completed?.Invoke(flattenedBytes);

    [RelayCommand]
    public void Undo()
    {
        InProgressAnnotation = null;
        _dragStart = null;
        Layer.Undo();
    }

    [RelayCommand]
    public void Done() => DoneRequested?.Invoke();

    [RelayCommand]
    public void Cancel() => CancelRequested?.Invoke();

    [RelayCommand]
    public void SetColor(string color) => CurrentColor = color;

    private Annotation? MakeAnnotation(Point start, Point end) => CurrentTool switch
    {
        AnnotationTool.Arrow     => new ArrowAnnotation(start.X, start.Y, end.X, end.Y, CurrentColor, StrokeWidth),
        AnnotationTool.Rectangle => new RectAnnotation(start.X, start.Y, end.X - start.X, end.Y - start.Y, CurrentColor, StrokeWidth),
        _                        => null // Text is placed via PlaceText, not drag
    };
}
