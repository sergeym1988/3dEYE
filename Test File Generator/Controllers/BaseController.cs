using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TestFileGenerator.Models;

namespace TestFileGenerator.Controllers
{
    /// <summary>
    /// The base controller providing logger and mediator dependencies.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    public class BaseController : Controller
    {
        /// <summary>
        /// The logger instance.
        /// </summary>
        protected readonly ILogger<BaseController> _logger;

        /// <summary>
        /// The mediator instance.
        /// </summary>
        protected readonly IMediator _mediator;

        /// <summary>
        /// The options instance.
        /// </summary>
        protected readonly FileGenerationOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="mediator">The mediator instance.</param>
        public BaseController(ILogger<BaseController> logger, IMediator mediator, IOptions<FileGenerationOptions> options)
        {
            _logger = logger;
            _mediator = mediator;
            _options = options.Value;
        }
    }
}
