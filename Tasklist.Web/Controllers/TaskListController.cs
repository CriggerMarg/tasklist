using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tasklist.Background;
using Tasklist.Models;

namespace Tasklist.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TaskListController : ControllerBase
    {
        private readonly ILogger<TaskListController> _logger;
        private readonly IProcessRepository _processRepository;

        public TaskListController(ILogger<TaskListController> logger, IProcessRepository processRepository)
        {
            _logger = logger;
            _processRepository = processRepository;
        }

        [HttpGet]
        public IEnumerable<ProcessInformation> Get()
        {
            return _processRepository.ProcessInformation?.ToList();
        }
    }
}
