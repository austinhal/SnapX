using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.HistoryLib;
using ShareXMac.Services;
using System.Diagnostics;

namespace ShareXMac.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly HistoryService _service;
    private List<HistoryItem> _allItems = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private HistoryItem? _selectedItem;

    public ObservableCollection<HistoryItem> FilteredItems { get; } = new();

    public event Action? CloseRequested;

    public HistoryViewModel(HistoryService service)
    {
        _service = service;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public void LoadItems()
    {
        _allItems = _service.GetItems();
        _allItems.Reverse(); // newest first
        ApplyFilter();
    }

    public async Task LoadItemsAsync()
    {
        _allItems = await _service.GetItemsAsync();
        _allItems.Reverse();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();
        string q = SearchText.Trim().ToLowerInvariant();
        foreach (var item in _allItems)
        {
            if (string.IsNullOrEmpty(q)
                || (item.FileName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.URL?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                FilteredItems.Add(item);
            }
        }
    }

    [RelayCommand]
    private void OpenSelected()
    {
        if (SelectedItem?.FilePath is { } path && File.Exists(path))
            Process.Start(new ProcessStartInfo("open")
                { UseShellExecute = false, ArgumentList = { "-R", path } });
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
