using GenerateFileHandler.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using TestFileGenerator.Models;
using static TestFileGenerator.Application.Commands.FileGeneratorCommands;

namespace TestFileGenerator.Application.Handlers
{
    /// <summary>
    /// Handler responsible for generating large files composed of random fruit lines.
    /// </summary>
    public class GenerateFileHandler : IRequestHandler<GenerateFileCommand, bool>
    {
        private readonly IFruitProvider _fruitProvider;
        private readonly ILogger<GenerateFileHandler> _logger;
        private readonly FileGenerationOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="GenerateFileHandler"/>.
        /// </summary>
        /// <param name="fruitProvider">Provider for fruits.</param>
        /// <param name="options">Configuration options for file generation.</param>
        /// <param name="logger">Logger instance.</param>
        public GenerateFileHandler(IFruitProvider fruitProvider, IOptions<FileGenerationOptions> options, ILogger<GenerateFileHandler> logger)
        {
            _options = options.Value;
            _fruitProvider = fruitProvider;
            _logger = logger;
        }

        /// <summary>
        /// Handles the file generation request.
        /// </summary>
        /// <param name="request">The file generation command.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Returns true if generation succeeds; otherwise, false.</returns>
        public async Task<bool> Handle(GenerateFileCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!Directory.Exists(_options.OutputDirectory))
                    Directory.CreateDirectory(_options.OutputDirectory);

                long totalSizeBytes = request.FileSizeInMb * 1024L * 1024L;
                int fullChunks = (int)(totalSizeBytes / (_options.ChunkSizeMb * 1024L * 1024L));
                long lastChunkSize = totalSizeBytes % (_options.ChunkSizeMb * 1024L * 1024L);
                int totalChunks = fullChunks + (lastChunkSize > 0 ? 1 : 0);

                string tempDir = Path.Combine(_options.OutputDirectory, $"chunks_{request.FileId}");
                Directory.CreateDirectory(tempDir);

                Parallel.For(0, totalChunks, chunkIndex =>
                {
                    long chunkSize = chunkIndex < fullChunks
                        ? _options.ChunkSizeMb * 1024L * 1024L
                        : lastChunkSize;

                    GenerateChunk(chunkIndex, tempDir, chunkSize, cancellationToken);
                });

                string outputFile = Path.Combine(_options.OutputDirectory, $"file_{request.FileId}.txt");

                using (var output = new FileStream(outputFile, FileMode.Create))
                {
                    for (int i = 0; i < totalChunks; i++)
                    {
                        string chunkPath = Path.Combine(tempDir, $"chunk_{i}.txt");
                        using (var input = new FileStream(chunkPath, FileMode.Open))
                        {
                            input.CopyTo(output);
                        }
                    }
                }

                Directory.Delete(tempDir, true);

                stopwatch.Stop();
                _logger.LogInformation("File {FileId} generated in {ElapsedSeconds} sec", request.FileId, stopwatch.Elapsed.TotalSeconds);

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error generating file {FileId} after {ElapsedSeconds} sec", request.FileId, stopwatch.Elapsed.TotalSeconds);
                return false;
            }
        }

        /// <summary>
        /// Generates a chunk of the file with random lines.
        /// </summary>
        /// <param name="chunkIndex">Index of the chunk.</param>
        /// <param name="folder">Folder to save chunk.</param>
        /// <param name="chunkSizeBytes">Size of chunk in bytes.</param>
        /// <param name="token">Cancellation token.</param>
        private void GenerateChunk(int chunkIndex, string folder, long chunkSizeBytes, CancellationToken token)
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            var fruits = _fruitProvider.GetFruits().ToArray();

            string path = Path.Combine(folder, $"chunk_{chunkIndex}.txt");

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 262144, FileOptions.SequentialScan); // 256KB buffer
            using var writer = new StreamWriter(stream, Encoding.UTF8, 262144);

            var linesBuffer = new List<string>(1000);
            long approxWrittenBytes = 0;

            while (approxWrittenBytes < chunkSizeBytes && !token.IsCancellationRequested)
            {
                linesBuffer.Clear();
                long bufferBytes = 0;

                while (bufferBytes < 256 * 1024 && approxWrittenBytes + bufferBytes < chunkSizeBytes)
                {
                    long number = rnd.NextInt64(1, 10_000_000);
                    string fruit = fruits[rnd.Next(fruits.Length)];

                    string line = $"{number}. {fruit}";
                    linesBuffer.Add(line);

                    bufferBytes += Encoding.UTF8.GetByteCount(line) + 2; // +2 for \r\n or \n line ending
                }

                foreach (var line in linesBuffer)
                {
                    writer.WriteLine(line);
                }

                approxWrittenBytes += bufferBytes;
            }

            writer.Flush();
        }
    }
}