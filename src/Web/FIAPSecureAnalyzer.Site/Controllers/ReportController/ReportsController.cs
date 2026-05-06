using FIAPSecureSystem.Site.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FIAPSecureSystem.Site.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Details(Guid analysisId)
        {
            return View();
        }
    }
}
