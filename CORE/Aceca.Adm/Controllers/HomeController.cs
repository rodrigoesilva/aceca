using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aceca.Adm.Controllers
{
    [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult Inicio()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("AccessDenied", "Auth");

            return View("~/Views/Home/Index.cshtml");
        }

    }
}