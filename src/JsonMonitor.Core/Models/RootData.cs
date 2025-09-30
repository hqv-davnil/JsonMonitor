using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace JsonMonitor.Core.Models;

/// <summary>
/// Root data model representing the JSON file structure with INotifyPropertyChanged support.
/// </summary>
public class RootData : ModelBase
{
    private string _title = string.Empty;
    private DateTime _lastModified;
    private ObservableCollection<DataItem> _items = new();

    [JsonPropertyName("title")]
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    [JsonPropertyName("lastModified")]
    public DateTime LastModified
    {
        get => _lastModified;
        set => SetProperty(ref _lastModified, value);
    }

    [JsonPropertyName("items")]
    public ObservableCollection<DataItem> Items
    {
        get => _items;
        set => SetProperty(ref _items, value ?? new ObservableCollection<DataItem>());
    }
}