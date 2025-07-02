using FluentAssertions;
using GenerateFileHandler.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestFileGenerator.Models;
using static TestFileGenerator.Application.Commands.FileGeneratorCommands;

public class SimpleGenerateFileHandlerTests
{
    private readonly string _outputDir;

    public SimpleGenerateFileHandlerTests()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), "TestGen_" + Guid.NewGuid());
        Directory.CreateDirectory(_outputDir);
    }

    [Fact]
    public async Task Should_CreateFile_WhenCommandIsValid()
    {
        // Arrange
        var fruits = new TestFruitProvider();
        var options = Options.Create(new FileGenerationOptions
        {
            OutputDirectory = _outputDir,
            ChunkSizeMb = 1
        });

        var handler = new TestFileGenerator.Application.Handlers.GenerateFileHandler(fruits, options, NullLogger.Instance);
        var fileId = Guid.NewGuid();

        // Act
        var result = await handler.Handle(new GenerateFileCommand(fileId, 1), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, $"file_{fileId}.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task Should_ReturnFalse_WhenOutputDirectoryIsInvalid()
    {
        // Arrange
        var fruits = new TestFruitProvider();
        var options = Options.Create(new FileGenerationOptions
        {
            OutputDirectory = "Z:\\invalid_dir",
            ChunkSizeMb = 1
        });

        var handler = new TestFileGenerator.Application.Handlers.GenerateFileHandler(fruits, options, NullLogger.Instance);
        var fileId = Guid.NewGuid();

        // Act
        var result = await handler.Handle(new GenerateFileCommand(fileId, 1), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    // Fake fruit provider without mocks
    private class TestFruitProvider : IFruitProvider
    {
        public List<string> GetFruits() => new() { "Apple", "Banana" };
    }

    private class NullLogger : Microsoft.Extensions.Logging.ILogger<TestFileGenerator.Application.Handlers.GenerateFileHandler>
    {
        public static readonly NullLogger Instance = new();

        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    ~SimpleGenerateFileHandlerTests()
    {
        try
        {
            if (Directory.Exists(_outputDir))
                Directory.Delete(_outputDir, true);
        }
        catch { }
    }
}