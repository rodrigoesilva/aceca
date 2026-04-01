using Microsoft.AspNetCore.Mvc;

namespace Aceca.Adm.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Sorry, the page you requested could not be found.";
                    return View("NotFound");
                default:
                    return View("Error");
            }
        }
    }
}
