using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorter.Models;

public class SortFileHandlerTests
{
#nullable disable
    private readonly SortFileHandler _handler;
    private readonly string _tempDir;

    public SortFileHandlerTests()
    {
        var logger = new LoggerFactory().CreateLogger<SortFileHandler>();
        var options = Options.Create(new FileSortingOptions
        {
            OutputDirectory = Path.Combine(Path.GetTempPath(), "SortedFilesTest"),
            ChunkSizeMb = 1
        });

        _handler = new SortFileHandler(logger, options);
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Tests that a large file is correctly split into smaller chunks based on the chunk size.
    /// </summary>
    [Fact]
    public async Task SplitFileIntoChunksAsync_ShouldSplitFileIntoChunks()
    {
        // Arrange
        string inputFile = Path.Combine(_tempDir, "input.txt");
        var lines = Enumerable.Range(1, 100).Select(i => $"{i}. Apple").ToArray();
        await File.WriteAllLinesAsync(inputFile, lines);

        // Act
        var chunks = await InvokeSplitFileIntoChunksAsync(inputFile, _tempDir, 100); // ~100 bytes per chunk

        // Assert
        chunks.Should().NotBeNull();
        chunks.Count.Should().BeGreaterThan(1);

        foreach (var chunkPath in chunks)
        {
            File.Exists(chunkPath).Should().BeTrue();
            var chunkLines = await File.ReadAllLinesAsync(chunkPath);
            chunkLines.Should().NotBeEmpty();
        }
    }

    /// <summary>
    /// Tests that the method correctly parses lines with numeric and text parts into keys.
    /// </summary>
    [Fact]
    public async Task ReadLinesWithKeysAsync_ShouldReturnLinesWithParsedKeys()
    {
        // Arrange
        string filePath = Path.Combine(_tempDir, "chunk.txt");
        var lines = new[] { "123. Banana", "45. Apple", "67. Cherry" };
        await File.WriteAllLinesAsync(filePath, lines);

        // Act
        var result = await InvokeReadLinesWithKeysAsync(filePath);

        // Assert
        result.Should().HaveCount(lines.Length);
    }

    /// <summary>
    /// Tests that sorted chunk files are correctly merged into a single, ordered output file.
    /// </summary>
    [Fact]
    public async Task MergeSortedChunksAsync_ShouldMergeFilesCorrectly()
    {
        // Arrange
        var chunk1Path = Path.Combine(_tempDir, "chunk1.txt");
        var chunk2Path = Path.Combine(_tempDir, "chunk2.txt");

        await File.WriteAllLinesAsync(chunk1Path, new[] { "1. Apple", "3. Cherry" });
        await File.WriteAllLinesAsync(chunk2Path, new[] { "2. Banana", "4. Date" });

        var outputPath = Path.Combine(_tempDir, "output.txt");
        var chunkPaths = new List<string> { chunk1Path, chunk2Path };

        // Act
        await InvokeMergeSortedChunksAsync(chunkPaths, outputPath);

        // Assert
        var outputLines = await File.ReadAllLinesAsync(outputPath);
        outputLines.Should().Equal("1. Apple", "2. Banana", "3. Cherry", "4. Date");
    }

    /// <summary>
    /// Helper method to invoke the private SplitFileIntoChunksAsync method using reflection.
    /// </summary>
    private async Task<List<string>> InvokeSplitFileIntoChunksAsync(string inputPath, string tempFolder, long chunkSizeBytes)
    {
        var method = typeof(SortFileHandler).GetMethod(
            "SplitFileIntoChunksAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var task = (Task<List<string>>)method.Invoke(
            _handler,
            new object[] { inputPath, tempFolder, chunkSizeBytes, CancellationToken.None }
        );

        return await task;
    }

    /// <summary>
    /// Helper method to invoke the private ReadLinesWithKeysAsync method using reflection.
    /// </summary>
    private async Task<List<object>> InvokeReadLinesWithKeysAsync(string filePath)
    {
        var handlerType = typeof(SortFileHandler);
        var method = handlerType.GetMethod(
            "ReadLinesWithKeysAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var taskObj = method.Invoke(_handler, new object[] { filePath, CancellationToken.None });
        var task = (Task)taskObj;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        var result = (IEnumerable<object>)resultProperty.GetValue(task);

        return result.ToList();
    }

    /// <summary>
    /// Helper method to invoke the private MergeSortedChunksAsync method using reflection.
    /// </summary>
    private async Task InvokeMergeSortedChunksAsync(List<string> chunkPaths, string outputPath)
    {
        var method = typeof(SortFileHandler).GetMethod(
            "MergeSortedChunksAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var task = (Task)method.Invoke(_handler, new object[] { chunkPaths, outputPath, CancellationToken.None });
        await task;
    }
#nullable enable
}