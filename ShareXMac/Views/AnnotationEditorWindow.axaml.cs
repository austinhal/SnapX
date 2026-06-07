using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ShareXMac.Models;
using ShareXMac.ViewModels;

namespace ShareXMac.Views;

public partial class AnnotationEditorWindow : Window
{
    private readonly AnnotationEditorViewModel _vm;
    private AnnotationCanvas _canvas = null!;
    private Border           _textBorder = null!;
    private TextBox          _textBox    = null!;
    private Point?           _pendingTextPoint;
    private bool             _dragging;

    public AnnotationEditorWindow(AnnotationEditorViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;

        _canvas     = this.FindControl<AnnotationCanvas>("AnnotationCanvasControl")!;
        _textBorder = this.FindControl<Border>("TextInputBorder")!;
        _textBox    = this.FindControl<TextBox>("TextInputBox")!;

        _canvas.PointerPressed  += OnCanvasPointerPressed;
        _canvas.PointerMoved    += OnCanvasPointerMoved;
        _canvas.PointerReleased += OnCanvasPointerReleased;

        _textBox.KeyDown += OnTextBoxKeyDown;

        vm.Layer.Changed += () => _canvas.InvalidateVisual();

        vm.DoneRequested   += FlattenAndClose;
        vm.CancelRequested += Close;

        this.FindControl<Button>("DoneButton")!.Click += (_, _) => vm.DoneCommand.Execute(null);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed) return;
        var p = e.GetPosition(_canvas);

        if (_vm.CurrentTool == AnnotationTool.Text)
        {
            ShowTextInput(p);
        }
        else
        {
            _dragging = true;
            _vm.StartDrag(p);
        }
        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        _vm.UpdateDrag(e.GetPosition(_canvas));
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        _vm.EndDrag(e.GetPosition(_canvas));
    }

    private void ShowTextInput(Point canvasPoint)
    {
        _pendingTextPoint = canvasPoint;
        Canvas.SetLeft(_textBorder, canvasPoint.X);
        Canvas.SetTop(_textBorder,  canvasPoint.Y);
        _textBorder.IsVisible        = true;
        _textBorder.IsHitTestVisible = true;
        var overlay = this.FindControl<Canvas>("TextOverlay")!;
        overlay.IsHitTestVisible = true;
        _textBox.Text = "";
        Dispatcher.UIThread.Post(() => _textBox.Focus());
    }

    private void CommitText()
    {
        if (_pendingTextPoint.HasValue)
            _vm.PlaceText(_pendingTextPoint.Value, _textBox.Text ?? "");
        HideTextInput();
    }

    private void HideTextInput()
    {
        _textBorder.IsVisible        = false;
        _textBorder.IsHitTestVisible = false;
        var overlay = this.FindControl<Canvas>("TextOverlay")!;
        overlay.IsHitTestVisible = false;
        _pendingTextPoint = null;
        _canvas.InvalidateVisual();
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  { CommitText();    e.Handled = true; }
        if (e.Key == Key.Escape) { HideTextInput(); e.Handled = true; }
    }

    private void FlattenAndClose()
    {
        var size      = _vm.OriginalBitmap.Size;
        var pixelSize = new PixelSize(Math.Max(1, (int)size.Width), Math.Max(1, (int)size.Height));
        using var rtb = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
        rtb.Render(_canvas);
        using var ms = new MemoryStream();
        rtb.Save(ms);
        _vm.Complete(ms.ToArray());
        Close();
    }
}
