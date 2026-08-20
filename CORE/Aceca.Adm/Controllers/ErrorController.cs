using Aceca.Adm.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
                    // Error.cshtml é @model ErrorViewModel - sem passar o model aqui,
                    // Model.ShowRequestId no view dá NullReferenceException (é isso que
                    // aparecia nos e-mails de monitoramento em vez da tela de erro).
                    return View("Error", new ErrorViewModel
                    {
                        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                    });
            }
        }
    }
}
