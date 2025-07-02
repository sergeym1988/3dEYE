using MediatR;

namespace TestFileGenerator.Application.Commands
{
    /// <summary>
    /// Contains commands related to file generation.
    /// </summary>
    public class FileGeneratorCommands
    {
        /// <summary>
        /// Command to generate a file with specified ID and size in megabytes.
        /// </summary>
        /// <param name="FileId">The unique identifier of the file.</param>
        /// <param name="FileSizeInMb">The size of the file to generate in megabytes.</param>
        public record GenerateFileCommand(Guid FileId, long FileSizeInMb) : IRequest<bool>;
    }
}