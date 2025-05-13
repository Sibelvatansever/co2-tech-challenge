using Microsoft.AspNetCore.Mvc;

namespace TechChallenge.Emissions.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Emissions API is alive...");
        }
    }
}