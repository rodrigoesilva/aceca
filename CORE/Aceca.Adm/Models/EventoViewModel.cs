namespace Aceca.Adm.Models
{
    // Um card da seção "Eventos & Encontros" da página institucional (Views/HomeController.Web
    // -> Web/Index.cshtml). Hoje a lista é montada fixa no controller; a ideia é que no futuro
    // vire uma consulta ao banco sem precisar tocar no .cshtml - só trocar como essa lista é
    // preenchida.
    public class EventoViewModel
    {
        public string Tipo { get; set; } = "upcoming"; // "upcoming" ou "past" (ver filterEvents no JS)
        public string CorGradienteInicio { get; set; } = "";
        public string CorGradienteFim { get; set; } = "";
        public bool GrayscaleFundo { get; set; }
        public string ImagemUrl { get; set; } = "";
        public int ImagemLargura { get; set; }
        public int ImagemAltura { get; set; }
        public string BadgeTexto { get; set; } = "";
        public string BadgeClasse { get; set; } = "badge-upcoming"; // "badge-upcoming" ou "badge-past"
        public string Data { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Descricao { get; set; } = "";
    }
}
