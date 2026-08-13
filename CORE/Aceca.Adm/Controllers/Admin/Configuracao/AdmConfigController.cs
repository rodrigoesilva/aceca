using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Aceca.Adm.Controllers.Admin.Configuracao
{
    [Authorize(Roles = "Administracao")]
    public class AdmConfigController : Controller
    {
        #region variaveis

        private readonly ILogger<AdmConfigController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;

        #endregion

        public AdmConfigController(ILogger<AdmConfigController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;
        }

        #region Index

        public ActionResult Index()
        {
            return View("~/Views/Admin/Configuracao/AdmConfig.cshtml");
        }

        #endregion

        #region GRID

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {
                var lstModel = await _db.AdmConfig
                    .AsNoTracking()
                    .OrderBy(x => x.Parametro)
                    .ToListAsync();

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = lstModel
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

        #region CRUD

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdmConfig model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model?.Parametro))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Parâmetro deve ser preenchido" });

                if (string.IsNullOrWhiteSpace(model.Descricao))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Descrição deve ser preenchida" });

                // Parametro é a chave lógica do registro (usada por quem for ler a
                // configuração pelo nome) - nunca pode se repetir.
                var jaExiste = await _db.AdmConfig.AnyAsync(x => x.Parametro == model.Parametro.Trim());

                if (jaExiste)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Já existe uma configuração com esse Parâmetro" });

                var novoModel = new AdmConfig
                {
                    Parametro = model.Parametro.Trim(),
                    Descricao = model.Descricao.Trim(),
                    Valor = model.Valor?.Trim(),
                    Ativo = model.Ativo
                };

                _db.AdmConfig.Add(novoModel);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = novoModel
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdmConfig model)
        {
            try
            {
                if (model == null || model.Id <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Id deve ser maior que 0" });

                if (string.IsNullOrWhiteSpace(model.Descricao))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Descrição deve ser preenchida" });

                var existente = await _db.AdmConfig.FirstOrDefaultAsync(x => x.Id == model.Id);

                if (existente == null)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Registro não encontrado" });

                // Parametro é a chave lógica do registro - o front trava o campo na edição
                // (não confiado sozinho); o valor enviado pelo cliente é sempre ignorado aqui.
                existente.Descricao = model.Descricao.Trim();
                existente.Valor = model.Valor?.Trim();
                existente.Ativo = model.Ativo;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = existente
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id < 1)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Id deve ser maior que 0" });

                var model = await _db.AdmConfig.FirstOrDefaultAsync(x => x.Id == id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                _db.AdmConfig.Remove(model);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = model
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
