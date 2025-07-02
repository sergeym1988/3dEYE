using BenchmarkDotNet.Attributes;
using GenerateFileHandler.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestFileGenerator.Models;
using static TestFileGenerator.Application.Commands.FileGeneratorCommands;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 1, invocationCount: 1)]
public class FileGenerationBenchmark
{
#nullable disable
    private TestFileGenerator.Application.Handlers.GenerateFileHandler _generateHandler;
    private IFruitProvider _fruitProvider;
    private string _outputDir;
    private Guid _fileId;
    private GenerateFileCommand _command;
#nullable enable

    [Params(50, 100, 500)]
    public int ChunkSizeMb { get; set; }

    [Params(1000)]
    public long FileSizeInMb { get; set; }

    /// <summary>
    /// Sets up the environment before running the benchmark:
    /// creates output directory, initializes logging, options,
    /// fruit provider and the file generation handler with commands.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), $"GeneratedFiles");
        Directory.CreateDirectory(_outputDir);

        _fileId = Guid.NewGuid();

        var loggerGen = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TestFileGenerator.Application.Handlers.GenerateFileHandler>();

        var options = Options.Create(new FileGenerationOptions
        {
            OutputDirectory = _outputDir,
            ChunkSizeMb = ChunkSizeMb
        });

        string contentRootPath = AppContext.BaseDirectory;
        _fruitProvider = new BenchmarkFruitProvider(contentRootPath);

        _generateHandler = new TestFileGenerator.Application.Handlers.GenerateFileHandler(_fruitProvider, options, loggerGen);

        _command = new GenerateFileCommand(_fileId, FileSizeInMb);
    }

    /// <summary>
    /// Cleans up the generated files and directories after benchmark completion.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_outputDir))
            Directory.Delete(_outputDir, true);
    }

    /// <summary>
    /// Benchmark method that generates the file asynchronously.
    /// Deletes existing file if present before generating a new one.
    /// Returns true if generation succeeded.
    /// </summary>
    [Benchmark]
    public async Task<bool> GenerateFileAsync()
    {
        var filePath = Path.Combine(_outputDir, $"file_{_fileId}.txt");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return await _generateHandler.Handle(_command, CancellationToken.None);
    }
}