namespace SmartMinex.Web
{
    using Microsoft.AspNetCore.Mvc;
    using SmartMinex.Runtime;

    [ApiController]
    [Route("api/data")]
    public class RestApiController : ControllerBase
    {
        readonly TDispatcher _rtm;

        public RestApiController(TDispatcher runtime)
        {
            _rtm = runtime;
        }

        /// <summary> http://localhost:8000/api/data/readtags </summary>
        [HttpGet("[action]")]
        public async Task<RfidTag[]?> ReadTags()
        {
            var tags = await _rtm.ReadTagsAsync();
            return tags?.ToArray();
        }
    }
}
