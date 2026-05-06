using FIAPSecureSystem.Site.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FIAPSecureAnalyzer.Site.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

    }
}
