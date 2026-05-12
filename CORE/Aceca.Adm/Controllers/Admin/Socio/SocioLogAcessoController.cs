using Aceca.Adm.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Aceca.Adm.Controllers.Admin.Usuario
{
    public class SocioLogAcessoController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioLogAcessoController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;
        //

        #endregion

        public SocioLogAcessoController(ILogger<SocioLogAcessoController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;
        }

        #region CRUD JS

        public ActionResult Index()
        {

            return View("~/Views/Admin/Socio/SocioLogAcesso.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {
                var lstModel = await _db.SocioLogAcesso
                    .Include(x => x.Socio)
                    .Include(x => x.Socio.SocioPerfil)
                    .OrderByDescending(x => x.UltimoLogin)
                    .AsNoTracking()
                    .ToListAsync();

                if (lstModel.Count <= 0)
                {
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - VAZIO - lstResult",
                        message = "listagem em branco",
                        data = lstModel
                    });
                }

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = lstModel,
                });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";

                _logger.LogError(mensagemErro);

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = mensagemErro
                });
            }
        }
        #endregion
    }
}
