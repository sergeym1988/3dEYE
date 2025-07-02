using MediatR;

namespace Sorter.Application.Commands
{
    /// <summary>
    /// Contains commands related to file sorting operations.
    /// </summary>
    public static class SortFileCommands
    {
        /// <summary>
        /// Command to request sorting of a file by its identifier.
        /// </summary>
        /// <param name="FileId">Unique identifier of the file to sort.</param>
        public record SortFileCommand(Guid FileId) : IRequest<bool>;
    }
}
