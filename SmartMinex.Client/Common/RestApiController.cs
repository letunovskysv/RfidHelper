namespace SmartMinex.Web
{
    using Microsoft.AspNetCore.Mvc;
    using SmartMinex.Runtime;

    [ApiController]
    [Route("api/data")]
    public class RestApiController : ControllerBase
    {
        readonly IRuntime _rtm;

        public RestApiController(IRuntime runtime)
        {
            _rtm = runtime;
        }

        /// <summary> http://localhost:8000/api/data/readtags </summary>
        [HttpGet("[action]")]
        public IEnumerable<string> ReadTags()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<ValuesController>/5
        [HttpGet("*")]
        public string Get(string id)
        {
            return "value";
        }
    }
}
