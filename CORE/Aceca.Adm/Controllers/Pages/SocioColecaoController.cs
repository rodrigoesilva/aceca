using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Dapper;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers.Pages
{
    [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
    public class SocioColecaoController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioColecaoController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;

        private readonly string _ftpBaseUrl = string.Empty;
        private readonly string _ftpHost = string.Empty;
        private readonly string _ftpUser = string.Empty;
        private readonly string _ftpPass = string.Empty;

        private readonly bool _bIsLocalHost = false;


        #endregion

        public SocioColecaoController(ILogger<SocioColecaoController> logger,
            AppDbContext db,
            IWebHostEnvironment env,
            IConfiguration cfg,
            IMemoryCache cache)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;
            _cache = cache;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;

            _ftpHost = _appConfiguration["Ftp:Host"]!;
            _ftpUser = _appConfiguration["Ftp:User"]!;
            _ftpPass = _appConfiguration["Ftp:Pass"]!;
            _ftpBaseUrl = _appConfiguration["Ftp:Path"]!;
        }

        #region Index
        public ActionResult Index()
        {
            return View("~/Views/Admin/Socio/SocioColecao.cshtml");
        }

        #endregion

        [HttpPost]
        public async Task<IActionResult> ActionColecao(int itemId, int actionId, int socioId, bool isPerfil)
        {
            switch ((EColecaoAcao)actionId)
            {
                case EColecaoAcao.ColecaoDelete:
                    break;
                case EColecaoAcao.ColecaoIncluir:
                    AdicionarOuAtualizarItemAsync(socioId, itemId, 1, false, false, false);
                    break;
                case EColecaoAcao.ColecaoInteresse:
                    break;
                case EColecaoAcao.ColecaoNaoQuero:
                    break;
                case EColecaoAcao.ColecaoTroca:
                    break;
                case EColecaoAcao.ColecaoVenda:
                    break;
                default:
                    break;
            }

            return Ok();
        }

        public async Task<IActionResult> AdicionarOuAtualizarItemAsync(int socioId, int itemId, int quantidade, bool troca, bool venda, bool interesse)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    
                    //var lstModel = await context.SociosColecao.AsNoTracking().FirstOrDefaultAsync();

                    var registro = await context.SociosColecao
                         .Include(x => x.Socio)
                         .AsNoTracking()
                         .Where(x =>
                            x.SocioId == socioId &&
                            x.MarcaId == itemId)
                         .FirstOrDefaultAsync();

                    if (registro == null)
                    {
                        registro = new SocioColecao
                        {
                            SocioId = socioId,
                            MarcaId = itemId,
                            Quantidade = quantidade,
                            Possui = true,
                            DisponivelTroca = troca,
                            DisponivelVenda = venda,
                            Interesse = interesse
                        };

                        context.SociosColecao.Add(registro);
                    }
                    else
                    {
                        registro.Quantidade = quantidade;
                        registro.DisponivelTroca = troca;
                        registro.DisponivelVenda = venda;
                        registro.Interesse = interesse;

                        context.SociosColecao.Update(registro);
                    }

                    await context.SaveChangesAsync();

                }

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: "
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
    }
}