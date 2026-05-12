using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;

namespace Aceca.Adm.Controllers.Admin.Socio
{
    public class SocioSegurancaController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioSegurancaController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;
        private readonly HelperExtensionsController _helperController;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;
        //

        #endregion

        public SocioSegurancaController(ILogger<SocioSegurancaController> logger, AppDbContext db
            , IWebHostEnvironment env, IConfiguration cfg
            , HelperExtensionsController helperController)
        { 
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;
            _helperController = helperController;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;
        }

        #region CRUD JS

        public ActionResult Index()
        {

            return View("~/Views/Admin/Socio/SocioSeguranca.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {

                var lstModel = await _db.SocioSeguranca
                    .Include(x => x.Socio)
                    .Include(x => x.Socio.SocioPerfil)
                    .OrderBy(x => x.Socio.Nome)
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

        
        [HttpPost]
        public async Task<IActionResult> Create(Models.SocioSeguranca model)
        {
            try
            {
                if (ModelState.IsValid)
                {
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
        public async Task<IActionResult> Edit(Models.SocioSeguranca model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    #region SocioSeguranca

                    if (string.IsNullOrEmpty(model?.Email))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Email deve ser preenchido"

                        });

                    _db.Entry(model).State = EntityState.Modified;
                    _db.SaveChanges();

                    model?.Id = model?.Id;

                    if (model?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar Socio"
                        });

                    #endregion

                    #region Socio

                    if (model.SocioId < 1)
                    {
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Id deve ser maior que 0"
                        });
                    }

                    var newModelSocio = new Models.SocioSeguranca
                    {
                        Id = model?.Socio?.Id,
                        Email = model?.Email,                        
                        NomeUsuario = model?.Socio?.Nome,
                        ResetPasswordToken = null,
                        ResetPasswordTokenExpiry = null,
                        UltimoLogin = DateTime.UtcNow,
                    };


                    // Atualiza senha
                    using (MD5 md5Hash = MD5.Create())
                    {
                        string strTempPass = _helperController.GenerateStringPassword(8);

                        string hash = _helperController.GenerateMD5HashPassword(md5Hash, strTempPass);
                        newModelSocio.Senha = hash;
                        newModelSocio.SenhaAberta = strTempPass;
                        newModelSocio.SenhaAtualizada = false;
                    }

                    _db.Entry(newModelSocio).State = EntityState.Modified;
                    _db.SaveChanges();

                    model?.Id = newModelSocio?.Id;

                    if (newModelSocio?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar Socio"
                        });

                    #endregion

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
            if (id < 1)
            {
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Id deve ser maior que 0"
                });
            }

            try
            {
                var model = await _db.SocioSeguranca.FindAsync(id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                _db.SocioSeguranca.Remove(model);
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
