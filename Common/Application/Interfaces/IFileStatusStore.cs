using Common.Models;

namespace Common.Application.Interfaces
{
    /// <summary>
    /// Interface for storing and retrieving the status of a file by its identifier.
    /// </summary>
    public interface IFileStatusStore
    {
        /// <summary>
        /// Sets the status for the file with the specified identifier.
        /// </summary>
        void SetStatus(Guid id, FileStatusEnum status);

        /// <summary>
        /// Gets the status of the file by its identifier.
        /// </summary>
        string GetStatus(Guid id);
    }
}
