using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Sorter.Controllers
{
    /// <summary>
    /// The base controller providing logger and mediator access.
    /// </summary>
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
        /// Initializes a new instance of the <see cref="BaseController"/> class.
        /// </summary>
        public BaseController(ILogger<BaseController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }
    }
}