using Common.Application.Interfaces;
using Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using static Sorter.Application.Commands.SortFileCommands;

namespace Sorter.Controllers
{
    /// <summary>
    /// Controller responsible for file sorting operations.
    /// </summary>
    [ApiController]
    [Route("api/sorter")]
    [EnableRateLimiting("sliding")]
    public class SorterController : BaseController
    {
        private readonly IFileStatusStore _fileStatusStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="SorterController"/> class.
        /// </summary>
        public SorterController(
            ILogger<SorterController> logger,
            IMediator mediator,
            IFileStatusStore fileStatusStore) : base(logger, mediator)
        {
            _fileStatusStore = fileStatusStore;
        }

        /// <summary>
        /// Starts sorting a file asynchronously.
        /// </summary>
        /// <param name="fileId">The identifier of the file to sort.</param>
        /// <returns>Returns HTTP 200 with fileId and status InProgress.</returns>
        [HttpPost("{fileId}")]
        public IActionResult Sort(Guid fileId)
        {
            _fileStatusStore.SetStatus(fileId, FileStatusEnum.InProgress);

            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _mediator.Send(new SortFileCommand(fileId));
                    _fileStatusStore.SetStatus(fileId, result ? FileStatusEnum.Completed : FileStatusEnum.Failed);
                }
                catch
                {
                    _fileStatusStore.SetStatus(fileId, FileStatusEnum.Failed);
                }
            });

            return Ok(new { fileId, status = "InProgress" });
        }

        /// <summary>
        /// Gets the current status of the file sorting process.
        /// </summary>
        /// <param name="fileId">The identifier of the file.</param>
        /// <returns>Returns HTTP 200 with fileId and status string.</returns>
        [HttpGet("status/{fileId}")]
        public IActionResult GetStatus(Guid fileId)
        {
            var status = _fileStatusStore.GetStatus(fileId);
            return Ok(new { fileId, status });
        }
    }
}
