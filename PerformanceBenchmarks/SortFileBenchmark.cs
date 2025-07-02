using BenchmarkDotNet.Attributes;
using GenerateFileHandler.Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sorter.Models;
using TestFileGenerator.Models;
using static Sorter.Application.Commands.SortFileCommands;
using static TestFileGenerator.Application.Commands.FileGeneratorCommands;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 1, invocationCount: 1)]
public class SortFileBenchmark
{
#nullable disable
    private SortFileHandler _sortHandler;
    private TestFileGenerator.Application.Handlers.GenerateFileHandler _generateHandler;
    private IFruitProvider _fruitProvider;
    private string _inputFilePath;
    private string _outputDirPath;
    private Guid _fileId;
#nullable enable

    [Params(1000)]
    public long FileSizeInMb { get; set; }

    [Params(100)]
    public int SortChunkSizeMb { get; set; }

    /// <summary>
    /// Sets up the environment before benchmarking:
    /// generates a test file, prepares directories,
    /// and initializes handlers for file generation and sorting.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _fileId = Guid.NewGuid();

        _outputDirPath = Path.Combine(Path.GetTempPath(), $"SortedFilesOutput_{Guid.NewGuid()}");
        Directory.CreateDirectory(_outputDirPath);

        var loggerGen = new NullLogger<TestFileGenerator.Application.Handlers.GenerateFileHandler>();

        var genOptions = Options.Create(new FileGenerationOptions
        {
            OutputDirectory = Path.GetTempPath(),
            ChunkSizeMb = 10
        });

        string contentRootPath = AppContext.BaseDirectory;
        _fruitProvider = new BenchmarkFruitProvider(contentRootPath);

        _generateHandler = new TestFileGenerator.Application.Handlers.GenerateFileHandler(_fruitProvider, genOptions, loggerGen);

        var generateCommand = new GenerateFileCommand(_fileId, FileSizeInMb);
        _generateHandler.Handle(generateCommand, CancellationToken.None).GetAwaiter().GetResult();

        _inputFilePath = Path.Combine(genOptions.Value.OutputDirectory, $"file_{_fileId}.txt");
        if (!File.Exists(_inputFilePath))
        {
            throw new FileNotFoundException($"Сгенерированный входной файл не найден: {_inputFilePath}");
        }

        var loggerSort = new NullLogger<SortFileHandler>();

        var sortOptions = Options.Create(new FileSortingOptions
        {
            OutputDirectory = _outputDirPath,
            ChunkSizeMb = SortChunkSizeMb,
        });

        _sortHandler = new SortFileHandler(loggerSort, sortOptions);

        var sorterExpectedInputPath = Path.Combine(AppContext.BaseDirectory, "../GeneratedFiles", $"file_{_fileId}.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(sorterExpectedInputPath));

        File.Copy(_inputFilePath, sorterExpectedInputPath, true);

        _inputFilePath = sorterExpectedInputPath;
    }

    /// <summary>
    /// Cleans up generated files and directories after benchmarking.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_inputFilePath))
        {
            File.Delete(_inputFilePath);
        }

        var sortedFilePath = Path.Combine(_outputDirPath, $"file_{_fileId}_sorted.txt");
        if (File.Exists(sortedFilePath))
        {
            File.Delete(sortedFilePath);
        }

        if (Directory.Exists(_outputDirPath))
        {
            Directory.Delete(_outputDirPath, true);
        }
    }

    /// <summary>
    /// Benchmark method that performs the sorting operation asynchronously.
    /// Returns true if sorting was successful.
    /// </summary>
    [Benchmark]
    public async Task<bool> SortFileAsync()
    {
        var sortCommand = new SortFileCommand(_fileId);

        return await _sortHandler.Handle(sortCommand, CancellationToken.None);
    }
}