using FIAPSecureAnalyzer.Site.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FIAPSecureAnalyzer.Site.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Details(Guid analysisId)
        {
            return View();
        }
    }
}
