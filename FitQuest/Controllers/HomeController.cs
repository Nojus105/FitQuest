using Microsoft.AspNetCore.Mvc;

namespace FitQuest.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Welcome()
        {
            return View();
        }
    }
}