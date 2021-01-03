using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockStation.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BlockStation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger) {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<string> Get() {
            return "success";
        }

        [HttpGet]
        [Route("filter")]
        [ServiceFilter(typeof(LoginCheckFilter))]
        public ActionResult<string> LoginFiltered() {
            return "success";
        }
    }
}
