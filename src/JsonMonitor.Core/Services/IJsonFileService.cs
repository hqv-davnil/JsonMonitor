using JsonMonitor.Core.Models;

namespace JsonMonitor.Core.Services;

/// <summary>
/// Interface for JSON file operations.
/// </summary>
public interface IJsonFileService
{
    /// <summary>
    /// Asynchronously reads and deserializes JSON data from the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The deserialized root data object, or null if file doesn't exist or is invalid.</returns>
    Task<RootData?> ReadJsonFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last write time of the specified file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The last write time, or DateTime.MinValue if file doesn't exist.</returns>
    DateTime GetLastWriteTime(string filePath);
}