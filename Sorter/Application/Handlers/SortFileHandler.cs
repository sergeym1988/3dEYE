using MediatR;
using Microsoft.Extensions.Options;
using Sorter.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using static Sorter.Application.Commands.SortFileCommands;

/// <summary>
/// Handles sorting of large files by splitting into chunks, sorting them, and merging the results.
/// </summary>
public class SortFileHandler : IRequestHandler<SortFileCommand, bool>
{
    private readonly ILogger<SortFileHandler> _logger;
    private readonly FileSortingOptions _options;
    private readonly int _maxDegreeOfParallelism;

    // Record to hold line and its parsed sorting key
    private record LineWithKey(string Line, (string Text, int Number) Key);

    public SortFileHandler(ILogger<SortFileHandler> logger, IOptions<FileSortingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _maxDegreeOfParallelism = Environment.ProcessorCount;
    }

    /// <summary>
    /// Main handler method to sort the specified file.
    /// </summary>
    public async Task<bool> Handle(SortFileCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var fileId = request.FileId;
        var inputPath = Path.Combine(_options.InputDirectory, $"file_{fileId}.txt");
        var tempFolder = Path.Combine(_options.TempDirectory, fileId.ToString());
        var outputPath = Path.Combine(_options.OutputDirectory, $"file_{fileId}_sorted.txt");

        Directory.CreateDirectory(tempFolder);
        Directory.CreateDirectory(_options.OutputDirectory);

        try
        {
            var chunkSizeBytes = _options.ChunkSizeMb * 1024L * 1024L;

            var chunks = await SplitFileIntoChunksAsync(inputPath, tempFolder, chunkSizeBytes, cancellationToken);
            _logger.LogInformation("The file {FileId} was divided into chunks in {Elapsed:0.00} seconds", fileId, stopwatch.Elapsed.TotalSeconds);

            var sortedChunkPaths = new ConcurrentBag<string>();

            await Parallel.ForEachAsync(chunks, new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism }, async (chunkPath, ct) =>
            {
                var linesWithKeys = await ReadLinesWithKeysAsync(chunkPath, ct);
                var array = linesWithKeys.ToArray();
                Array.Sort(array, CompareByKeys);

                var sortedPath = chunkPath + "_sorted.txt";
                await WriteLinesAsync(sortedPath, array, ct);
                sortedChunkPaths.Add(sortedPath);
                File.Delete(chunkPath);
            });

            _logger.LogInformation("Chunks were sorted in {Elapsed:0.00} seconds", stopwatch.Elapsed.TotalSeconds);

            await MergeSortedChunksAsync(sortedChunkPaths.ToList(), outputPath, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("Sorted file {FileId} in {Elapsed:0.00} seconds", fileId, stopwatch.Elapsed.TotalSeconds);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sort file {FileId}", fileId);
            return false;
        }
        finally
        {
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, recursive: true);
        }
    }

    /// <summary>
    /// Splits the input file into smaller chunk files, each up to chunkSizeBytes in size.
    /// </summary>
    private async Task<List<string>> SplitFileIntoChunksAsync(string inputPath, string tempFolder, long chunkSizeBytes, CancellationToken ct)
    {
        var chunks = new List<string>();
        var buffer = new List<string>();
        long currentSize = 0;
        int index = 0;

        using var reader = new StreamReader(inputPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 81920);
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync();
            if (line == null) continue;

            buffer.Add(line);
            currentSize += Encoding.UTF8.GetByteCount(line + "\n");

            if (currentSize >= chunkSizeBytes)
            {
                var chunkPath = Path.Combine(tempFolder, $"chunk_{index++}.txt");
                await using var writer = new StreamWriter(chunkPath, false, Encoding.UTF8, 81920);
                foreach (var l in buffer)
                    await writer.WriteLineAsync(l);

                chunks.Add(chunkPath);
                buffer.Clear();
                currentSize = 0;
            }
        }

        if (buffer.Count > 0)
        {
            var chunkPath = Path.Combine(tempFolder, $"chunk_{index++}.txt");
            await using var writer = new StreamWriter(chunkPath, false, Encoding.UTF8, 81920);
            foreach (var l in buffer)
                await writer.WriteLineAsync(l);
            chunks.Add(chunkPath);
        }

        return chunks;
    }

    /// <summary>
    /// Reads all lines from a file and returns them with their parsed sorting keys.
    /// </summary>
    private async Task<List<LineWithKey>> ReadLinesWithKeysAsync(string filePath, CancellationToken ct)
    {
        var list = new List<LineWithKey>();
        using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 524288);
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync();
            if (line == null) continue;
            list.Add(new LineWithKey(line, GetSortKey(line)));
        }
        return list;
    }

    /// <summary>
    /// Compares two lines by their parsed sorting keys.
    /// </summary>
    private static int CompareByKeys(LineWithKey a, LineWithKey b)
    {
        var cmp = string.Compare(a.Key.Text, b.Key.Text, StringComparison.OrdinalIgnoreCase);
        return cmp != 0 ? cmp : a.Key.Number.CompareTo(b.Key.Number);
    }

    /// <summary>
    /// Writes sorted lines back to a file.
    /// </summary>
    private async Task WriteLinesAsync(string filePath, LineWithKey[] lines, CancellationToken ct)
    {
        await using var writer = new StreamWriter(filePath, false, Encoding.UTF8, 524288);
        foreach (var lineWithKey in lines)
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(lineWithKey.Line);
        }
    }

    /// <summary>
    /// Merges multiple sorted chunk files into the final sorted output file.
    /// </summary>
    private async Task MergeSortedChunksAsync(List<string> chunkPaths, string outputPath, CancellationToken ct)
    {
        var readers = new List<StreamReader>();
        try
        {
            foreach (var path in chunkPaths)
                readers.Add(new StreamReader(path, Encoding.UTF8, true, 81920));

            await using var writer = new StreamWriter(outputPath, false, Encoding.UTF8, 81920);
            var pq = new PriorityQueue<(string Line, int ReaderIndex, (string Text, int Number) Key), (string Text, int Number)>();

            for (int i = 0; i < readers.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var line = await readers[i].ReadLineAsync();
                if (line != null)
                {
                    var key = GetSortKey(line);
                    pq.Enqueue((line, i, key), key);
                }
            }

            while (pq.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                var (line, readerIndex, _) = pq.Dequeue();
                await writer.WriteLineAsync(line);

                var nextLine = await readers[readerIndex].ReadLineAsync();
                if (nextLine != null)
                {
                    var key = GetSortKey(nextLine);
                    pq.Enqueue((nextLine, readerIndex, key), key);
                }
            }
        }
        finally
        {
            foreach (var reader in readers)
                reader.Dispose();
        }
    }

    /// <summary>
    /// Extracts sorting key from a line: text part and integer prefix.
    /// </summary>
    private static (string Text, int Number) GetSortKey(string line)
    {
        var splitIndex = line.IndexOf('.');
        var numberPart = line[..splitIndex];
        var text = line[(splitIndex + 1)..];
        int.TryParse(numberPart, out var number);
        return (text.Trim(), number);
    }
}