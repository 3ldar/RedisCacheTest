using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using RedisTest.ActionFilters;

namespace RedisTest.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [OutputChache]
        [HttpGet]
        public async Task<DateTime> GetDate(int id)
        {
            return DateTime.Now;
        }

        [OutputChache(VaryByParams = new[] {"id" })]
        [HttpGet("{id}")]
        public async Task<Party> GetPartyById(int id)
        {
            return new Party { Id = id,
            Name = "Abuzer"+ id,
            Created = DateTime.Now};
        }
    }
}
