using Aceca.Adm.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Aceca.Adm.Controllers.Admin.Usuario
{
    public class UsuarioLogController : Controller
    {
        #region variaveis

        private readonly ILogger<UsuarioLogController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _appBaseUrl = string.Empty;
        //

        #endregion

        public UsuarioLogController(ILogger<UsuarioLogController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;

            _appBaseUrl = _appConfiguration["App:Url"]!;
        }

        #region CRUD JS

        public ActionResult Index()
        {

            return View("~/Views/Admin/Usuario/UsuarioLog.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {
                var lstModel = await _db.UsuarioLog
                    .Include(x => x.Usuario)
                    .Include(x => x.Usuario.Socio)
                    .Include(x => x.Usuario.Socio.SocioPerfil)
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
