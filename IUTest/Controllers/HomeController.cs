using IUTest.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
