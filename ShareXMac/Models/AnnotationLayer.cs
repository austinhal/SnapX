namespace ShareXMac.Models;

public class AnnotationLayer
{
    private readonly List<Annotation> _items = new();

    public IReadOnlyList<Annotation> Items => _items;
    public int Count => _items.Count;

    public event Action? Changed;

    public void Add(Annotation a)
    {
        _items.Add(a);
        Changed?.Invoke();
    }

    public bool Undo()
    {
        if (_items.Count == 0) return false;
        _items.RemoveAt(_items.Count - 1);
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        _items.Clear();
        Changed?.Invoke();
    }
}
