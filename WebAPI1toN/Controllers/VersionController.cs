using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace WebAPI1toN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly ILogger<VersionController> _logger;
        //private readonly LogOptions? _config;
        //private bool useStopwatch = false;

        public VersionController([NotNull] ILogger<VersionController> logger)
        {
            //_config = config.Value;
            _logger = logger;

        }
        /// <summary>
        /// Version
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<string> Get()
        {

            Version? v = Assembly.GetExecutingAssembly().GetName().Version;
            var msg = v!.ToString();

            //_logger.LogInformation("Get - Version {0} ElapsedTime = {1} mSecs", msg, _ts.TotalMilliseconds);

            if (!string.IsNullOrEmpty(msg))
                return new string[] { msg };

            return new string[] { "Unknown" };
        }

    }
}

