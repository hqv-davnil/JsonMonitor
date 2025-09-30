using System.Text.Json.Serialization;

namespace JsonMonitor.Core.Models;

/// <summary>
/// Represents a data item from the JSON file with INotifyPropertyChanged support.
/// </summary>
public class DataItem : ModelBase
{
    private string _name = string.Empty;
    private string _value = string.Empty;
    private DateTime _timestamp;

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    [JsonPropertyName("value")]
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }
}