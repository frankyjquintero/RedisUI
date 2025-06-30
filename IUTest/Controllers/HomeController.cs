using Microsoft.AspNetCore.Mvc;

namespace IUTest.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
