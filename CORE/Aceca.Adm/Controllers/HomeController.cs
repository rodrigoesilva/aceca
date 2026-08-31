using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Aceca.Adm.Models;

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

        // Site institucional (público) - a classe inteira exige login por causa do
        // Inicio() (dashboard do sócio logado), mas essa página precisa ser vista por
        // qualquer visitante anônimo (é a porta de entrada / marketing).
        //
        // A lista de eventos é fixa por enquanto (mesmo conteúdo hoje publicado em
        // Web/index.html, o site estático) - a ideia é que no futuro vire uma consulta
        // ao banco sem precisar tocar em Web/Index.cshtml, só trocar como essa lista é
        // montada aqui.
        [AllowAnonymous]
        public IActionResult Web()
        {
            var eventos = new List<EventoViewModel>
            {
                new() {
                    Tipo = "upcoming", CorGradienteInicio = "#d8c5ff", CorGradienteFim = "#a870ee",
                    ImagemUrl = "/Web/img/encontro/2025_picarras.jpg", ImagemLargura = 900, ImagemAltura = 506,
                    BadgeClasse = "badge-upcoming", BadgeTexto = "Próximo",
                    Data = "23 JAN 2027", Titulo = "Encontro de Piçarras/SC 2027",
                    Descricao = "Teremos mais um grande e tradicional encontro na região Sul do país, reforçando a tradiçao ACECA no encontro de amigos e participantes."
                },
                new() {
                    Tipo = "past", CorGradienteInicio = "#7040a0", CorGradienteFim = "#a890c8", GrayscaleFundo = true,
                    ImagemUrl = "/Web/img/encontro/2025_sbc.jpg", ImagemLargura = 900, ImagemAltura = 423,
                    BadgeClasse = "badge-upcoming", BadgeTexto = "Próximo",
                    Data = "22 AGO 2026", Titulo = "Encontro de São Bernardo do Campo/SP 2026",
                    Descricao = "Prestigiado encontro conduzido pelo grupo de colecionadores e amigos na região metropolitana de São Paulo, e apoiado pela ACECA"
                },
                new() {
                    Tipo = "past", CorGradienteInicio = "#d8c5ff", CorGradienteFim = "#a870ee",
                    ImagemUrl = "/Web/img/encontro/encontro_padrao.png", ImagemLargura = 500, ImagemAltura = 500,
                    BadgeClasse = "badge-upcoming", BadgeTexto = "Realizado",
                    Data = "25 ABR 2026", Titulo = "Encontro de Tietê/SP 2026",
                    Descricao = "Tradicional Encontro ACECA no interior de SP, com atualizaçoes por vir"
                },
                new() {
                    Tipo = "past", CorGradienteInicio = "#c8b0e8", CorGradienteFim = "#9060c0", GrayscaleFundo = true,
                    ImagemUrl = "/Web/img/encontro/2026_santacruz.jpg", ImagemLargura = 900, ImagemAltura = 506,
                    BadgeClasse = "badge-past", BadgeTexto = "Realizado",
                    Data = "18 NOV 2025", Titulo = "Encontro de Santa Cruz do SUl/RS 2025",
                    Descricao = "Mais um grande e tradicional encontro na região Sul do país, reforçando a tradiçao ACECA"
                },
                new() {
                    Tipo = "past", CorGradienteInicio = "#7040a0", CorGradienteFim = "#a890c8", GrayscaleFundo = true,
                    ImagemUrl = "/Web/img/encontro/2025_sbc.jpg", ImagemLargura = 900, ImagemAltura = 423,
                    BadgeClasse = "badge-past", BadgeTexto = "Apoio",
                    Data = "16 AGO 2025", Titulo = "Encontro de São Bernardo do Campo/SP 2025",
                    Descricao = "Prestigiado encontro conduzido pelo grupo de colecionadores e amigos na região metropolitana de São Paulo, e apoioado pela ACECA"
                },
                new() {
                    Tipo = "past", CorGradienteInicio = "#c8b0e8", CorGradienteFim = "#9060c0", GrayscaleFundo = true,
                    ImagemUrl = "/Web/img/encontro/2025_tiete.png", ImagemLargura = 350, ImagemAltura = 297,
                    BadgeClasse = "badge-past", BadgeTexto = "Realizado",
                    Data = "05 ABR 2025", Titulo = "Encontro de Tietê/SP 2025",
                    Descricao = "Mais um encontro no tradicional interior de São Paulo. Com a participação de conhecidos e novos amigos ACECA"
                },
                new() {
                    Tipo = "past", CorGradienteInicio = "#b8a0d8", CorGradienteFim = "#8050b0", GrayscaleFundo = true,
                    ImagemUrl = "/Web/img/encontro/2025_picarras.jpg", ImagemLargura = 900, ImagemAltura = 506,
                    BadgeClasse = "badge-past", BadgeTexto = "Realizado",
                    Data = "31 JAN 2025", Titulo = "Encontro de Piçarras/SC 2025",
                    Descricao = "Marcando e prestigiando a nova gestão, encontro de amigos e participantes. Recorde histórico de peças expostas."
                },
            };

            return View("~/Web/Index.cshtml", eventos);
        }
        public IActionResult Inicio()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("AccessDenied", "Auth");

            return View("~/Views/Home/Home.cshtml");
        }

    }
}