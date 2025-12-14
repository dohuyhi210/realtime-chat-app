using Microsoft.AspNetCore.Mvc;

namespace ChatClient.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home/Index
        public IActionResult Index()
        {
            return View();
        }
    }
}