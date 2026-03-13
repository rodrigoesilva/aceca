using Aceca.Adm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Aceca.Adm.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Inicio()
        {
            return View("~/Views/Home/Index.cshtml");
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        //



        //[HttpGet("GetMaterials")]
        //public async Task<IActionResult> GetProductsAsync()
        [Authorize(Policy = "RequireAdministratorRole1")]
        public ActionResult GetMaterials()
        {
            return Redirect("https://www.uol.cox.br");
        }


        //[HttpPost("CreateMaterial")]
        //public async Task<IActionResult> CreateProductAsync()
        public ActionResult CreateMaterial()
        {
            return Redirect("https://www.globo.com");
        }
    }
}