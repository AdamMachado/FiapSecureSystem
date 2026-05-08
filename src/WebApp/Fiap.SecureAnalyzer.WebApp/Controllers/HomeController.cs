using Fiap.SecureSystem.Site.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Fiap.SecureAnalyzer.Site.Controllers;

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
