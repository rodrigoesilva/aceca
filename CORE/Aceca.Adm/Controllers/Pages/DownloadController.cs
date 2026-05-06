using Aceca.Adm.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;

namespace Aceca.Adm.Controllers.Pages.Download
{
    [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
    public class DownloadController : Controller
    {
        #region variaveis

        private readonly ILogger<DownloadController> _logger;
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
        //

        #endregion

        public DownloadController(ILogger<DownloadController> logger, 
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
            return View("~/Views/Pages/Download.cshtml");
        }

        #endregion

        #region GRID

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {
                var lstModel = await _db.Download
                    .Include(x => x.DownloadTipo)
                    .Include(x => x.Socio)
                    .OrderBy(x => x.DownloadTipoId)
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
                    arqUrlBase = $"{_urlBaseSite}/arquivos",
                    imgDefault = $"{_urlBaseSite}/assets/img/img_inexistente.jpg"
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

        #region CRUD JS

        [HttpPost]
        public async Task<IActionResult> Create(Models.Download model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (string.IsNullOrEmpty(model.Descricao))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Descricao deve ser preenchido"
                        });

                    var newModel = new Models.Download
                    {
                        DownloadTipoId = model.DownloadTipoId,
                        Titulo = !string.IsNullOrEmpty(model.Titulo) ? model.Titulo : null,
                        Nome = !string.IsNullOrEmpty(model.Nome) ? model.Nome : null,
                        Extensao = !string.IsNullOrEmpty(model.Extensao) ? model.Extensao : null,
                        Imagem = !string.IsNullOrEmpty(model.Imagem) ? model.Imagem : null,
                        Diretorio = !string.IsNullOrEmpty(model.Diretorio) ? model.Diretorio : null,
                        Descricao = !string.IsNullOrEmpty(model.Descricao) ? model.Descricao : null,
                        SocioId = model.SocioId,
                        Ativo = model.Ativo
                    };

                    _db.Download.Add(newModel);
                    _db.SaveChanges();

                    model.Id = newModel?.Id;

                    if (model?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar"
                        });

                    return Ok(new
                    {
                        bResult = true,
                        type = "OK",
                        message = "SUCESSO ::: ",
                        data = model,
                    });
                }

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Model Inválida",
                    data = model,
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

        [HttpPost]
        public async Task<IActionResult> Edit(Models.Download model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (string.IsNullOrEmpty(model.Descricao))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Descricao deve ser preenchido"
                        });

                    _db.Entry(model).State = EntityState.Modified;
                    _db.SaveChanges();

                    if (model?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar"
                        });

                    return Ok(new
                    {
                        bResult = true,
                        type = "OK",
                        message = "SUCESSO ::: ",
                        data = model,
                    });
                }

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Model Inválida",
                    data = model,
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

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id < 1)
                {
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Id deve ser maior que 0"
                    });
                }

                var model = await _db.Download.FindAsync(id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                _db.Download.Remove(model);
                _db.SaveChanges();

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = model,
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