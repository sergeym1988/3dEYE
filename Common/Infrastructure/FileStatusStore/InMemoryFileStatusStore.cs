using Common.Application.Interfaces;
using Common.Models;
using System.Collections.Concurrent;

namespace Common.Infrastructure.FileStatusStore
{
    /// <summary>
    /// In-memory implementation of the file status store using a thread-safe dictionary.
    /// </summary>
    public class InMemoryFileStatusStore : IFileStatusStore
    {
        private readonly ConcurrentDictionary<Guid, FileStatusEnum> _store = new();

        /// <summary>
        /// Sets the status for the file with the specified identifier.
        /// </summary>
        public void SetStatus(Guid id, FileStatusEnum status)
        {
            _store[id] = status;
        }

        /// <summary>
        /// Gets the status of the file by its identifier. Returns "NotFound" if the id does not exist.
        /// </summary>
        public string GetStatus(Guid id)
        {
            return _store.TryGetValue(id, out var status)
                ? status.ToString()
                : FileStatusEnum.NotFound.ToString();
        }
    }
}
