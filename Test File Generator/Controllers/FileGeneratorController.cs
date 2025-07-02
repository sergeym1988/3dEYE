using Common.Application.Interfaces;
using Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TestFileGenerator.Models;
using static TestFileGenerator.Application.Commands.FileGeneratorCommands;

namespace TestFileGenerator.Controllers
{
    /// <summary>
    /// Controller responsible for generating test files and checking their generation status.
    /// </summary>
    [ApiController]
    [Route("api/generator")]
    [EnableRateLimiting("sliding")]
    public class FileGeneratorController : BaseController
    {
        private readonly IFileStatusStore _fileStatusStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileGeneratorController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="mediator">Mediator instance.</param>
        /// <param name="fileStatusStore">File status store instance.</param>
        public FileGeneratorController(
            ILogger<FileGeneratorController> logger,
            IMediator mediator,
            IFileStatusStore fileStatusStore,
            IOptions<FileGenerationOptions> options) : base(logger, mediator, options)
        {
            _fileStatusStore = fileStatusStore;
        }

        /// <summary>
        /// Initiates file generation in the background. The method returns immediately with the fileId.
        /// Use GET /status/{fileId} to track progress.
        /// </summary>
        [Route("")]
        [HttpPost]
        public IActionResult Generate(long fileSizeInMb)
        {
            if (fileSizeInMb <= 0 || fileSizeInMb > _options.MaxFileSizeMb)
            {
                return BadRequest($"File size must be between 1 and {_options.MaxFileSizeMb} MB.");
            }

            var fileId = Guid.NewGuid();
            _fileStatusStore.SetStatus(fileId, FileStatusEnum.InProgress);

            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _mediator.Send(new GenerateFileCommand(fileId, fileSizeInMb));
                    _fileStatusStore.SetStatus(fileId, result ? FileStatusEnum.Completed : FileStatusEnum.Failed);
                }
                catch
                {
                    _fileStatusStore.SetStatus(fileId, FileStatusEnum.Failed);
                }
            });

            return Ok(new { fileId });
        }

        /// <summary>
        /// Retrieves the current status of a file generation request by file ID.
        /// </summary>
        /// <param name="fileId">The unique identifier of the file.</param>
        /// <returns>Returns the file ID and its current status.</returns>
        [Route("status/{fileId}")]
        [HttpGet]
        public IActionResult GetStatus(Guid fileId)
        {
            var status = _fileStatusStore.GetStatus(fileId);
            return Ok(new { fileId, status });
        }
    }
}