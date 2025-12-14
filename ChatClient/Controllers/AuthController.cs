using Microsoft.AspNetCore.Mvc;

namespace ChatClient.Controllers
{
    public class AuthController : Controller
    {
        // GET: /Auth/Login
        public IActionResult Login()
        {
            return View();
        }

        // GET: /Auth/Register
        public IActionResult Register()
        {
            return View();
        }
    }
}