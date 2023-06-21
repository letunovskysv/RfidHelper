namespace SmartMinex.Web
{
    using Microsoft.AspNetCore.Mvc;

    public class ValuesController : Controller
    {
        // GET: api/<ValuesController>
        [HttpGet]
        public IEnumerable<string> Get()
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
