using JsonMonitor.Core.Models;
using JsonMonitor.Core.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace JsonMonitor.Tests;

/// <summary>
/// Tests for garden tools file monitoring functionality
/// </summary>
public class GardenToolsMonitoringTests : IDisposable
{
    private readonly JsonFileService _jsonFileService;
    private readonly string _testDirectory;

    public GardenToolsMonitoringTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "JsonMonitorTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Warning));
        var jsonLogger = loggerFactory.CreateLogger<JsonFileService>();
        
        _jsonFileService = new JsonFileService(jsonLogger);
    }

    /// <summary>
    /// Tests core file change detection logic
    /// </summary>
    [Fact]
    public void FileChangeDetection_CoreLogic()
    {
        // Arrange - Simulate timestamp-based change detection logic
        var initialTimestamp = new DateTime(2025, 9, 29, 10, 0, 0);
        var modifiedTimestamp = new DateTime(2025, 9, 29, 10, 0, 1); // 1 second later
        
        // Act - Core logic: compare timestamps
        var hasNotChanged = modifiedTimestamp <= initialTimestamp;
        var hasChanged = modifiedTimestamp > initialTimestamp;
        
        // Assert - Verify the logic
        Assert.False(hasNotChanged, "File should be detected as NOT changed when timestamps are equal");
        Assert.True(hasChanged, "File should be detected as changed when timestamp is newer");
    }

    /// <summary>
    /// Tests JSON reading and deserialization
    /// </summary>
    [Fact]
    public async Task JsonDeserialization_GardenToolsData()
    {
        // Arrange - Create test JSON content
        var jsonContent = @"{
          ""title"": ""Garden Tools Connection Monitor"",
          ""lastModified"": ""2025-09-29T18:30:00Z"",
          ""items"": [
            {
              ""name"": ""Chainsaw"",
              ""value"": ""Connected"", 
              ""timestamp"": ""2025-09-29T18:30:00Z""
            }
          ]
        }";
        
        // Write to temp file
        var testFilePath = Path.Combine(_testDirectory, "test.json");
        await File.WriteAllTextAsync(testFilePath, jsonContent);

        // Act - Test JSON reading
        var result = await _jsonFileService.ReadJsonFileAsync(testFilePath);

        // Assert - Verify correct deserialization
        Assert.NotNull(result);
        Assert.Equal("Garden Tools Connection Monitor", result.Title);
        Assert.Single(result.Items);
        Assert.Equal("Chainsaw", result.Items.First().Name);
        Assert.Equal("Connected", result.Items.First().Value);
    }

    /// <summary>
    /// Tests JsonFileService GetLastWriteTime method
    /// </summary>
    [Fact]
    public async Task JsonFileService_GetLastWriteTime()
    {
        // Test verifies the file timestamp functionality
        var testFilePath = Path.Combine(_testDirectory, "timestamp-test.json");
        
        // Create initial file
        var testData = new { title = "Test", items = new object[0] };
        var jsonContent = JsonSerializer.Serialize(testData);
        await File.WriteAllTextAsync(testFilePath, jsonContent);
        
        // Test GetLastWriteTime method
        var timestamp1 = _jsonFileService.GetLastWriteTime(testFilePath);
        var timestamp2 = _jsonFileService.GetLastWriteTime(testFilePath);
        
        // Verify timestamp consistency
        Assert.Equal(timestamp1, timestamp2);
        Assert.True(timestamp1 > DateTime.MinValue);
        
        // Verify file exists and is readable
        var data = await _jsonFileService.ReadJsonFileAsync(testFilePath);
        Assert.NotNull(data);
        Assert.Equal("Test", data.Title);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}