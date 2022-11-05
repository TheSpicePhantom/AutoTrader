using Microsoft.AspNetCore.Mvc;

namespace AutoTrader.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return new OkObjectResult("TEST");
        }
    }
}
