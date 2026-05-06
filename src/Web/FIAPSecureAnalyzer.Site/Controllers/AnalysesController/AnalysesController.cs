using Microsoft.AspNetCore.Mvc;

public class AnalysesController : Controller
{
    public IActionResult Details(Guid id)
    {
        // depois vamos buscar da API
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return View();

        // depois vamos enviar para API

        return RedirectToAction("Dashboard", "Home");
    }
}