using JsonMonitor.Core.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace JsonMonitor.Core.Services;

/// <summary>
/// Implementation of JSON file service that handles file I/O operations.
/// Uses System.Text.Json for deserialization with proper error handling.
/// </summary>
public class JsonFileService : IJsonFileService
{
    private readonly ILogger<JsonFileService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonFileService(ILogger<JsonFileService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    public async Task<RootData?> ReadJsonFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("File path is null or empty");
            return null;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("JSON file does not exist at path: {FilePath}", filePath);
                return null;
            }

            var rawContent = await File.ReadAllTextAsync(filePath, cancellationToken);

            var rootData = JsonSerializer.Deserialize<RootData>(rawContent, _jsonOptions);

            if (rootData == null)
            {
                _logger.LogWarning("Deserialized JSON data is null from file: {FilePath}", filePath);
                return null;
            }

            return rootData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON from file: {FilePath}", filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading JSON file: {FilePath}", filePath);
            return null;
        }
    }

    public DateTime GetLastWriteTime(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return DateTime.MinValue;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                return DateTime.MinValue;
            }

            return File.GetLastWriteTime(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last write time for file: {FilePath}", filePath);
            return DateTime.MinValue;
        }
    }
}