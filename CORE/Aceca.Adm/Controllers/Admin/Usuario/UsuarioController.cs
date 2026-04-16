using Aceca.Adm.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Aceca.Adm.Controllers.Admin.Usuario
{
    public class UsuarioController : Controller
    {
        #region variaveis

        private readonly ILogger<UsuarioController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _appBaseUrl = string.Empty;
        //

        #endregion

        public UsuarioController(ILogger<UsuarioController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
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

            return View("~/Views/Admin/Usuario/Usuario.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {

                var lstModel = await _db.Usuario
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
                {/*
                    _logger.LogInformation(
                    $"{lstModel} graus Fahrenheit = " +
                    $"{resultado.Celsius} graus Celsius = " +
                    $"{resultado.Kelvin} graus Kelvin");
                return resultado;
                    */
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
        public async Task<IActionResult> Create(Models.Usuario model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    return Ok(new
                    {/*
                        _logger.LogInformation(
                        $"{lstModel} graus Fahrenheit = " +
                        $"{resultado.Celsius} graus Celsius = " +
                        $"{resultado.Kelvin} graus Kelvin");
                    return resultado;
                        */
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
        public async Task<IActionResult> Edit(Models.Usuario model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    #region Usuario

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

                    var newModelSocio = new Models.Socio
                    {
                        Id = model?.Socio?.Id,
                        SocioPerfilId = model?.Socio?.SocioPerfilId = model?.Socio?.SocioPerfilId > 0 ? model?.Socio?.SocioPerfilId : 5, //socio
                        Nome = model?.Socio?.Nome,
                        MostrarSite = model?.Socio?.MostrarSite != null ? model?.Socio?.MostrarSite : true,
                        Ativo = model?.Ativo,
                    };

                    _db.Entry(newModelSocio).State = EntityState.Modified;
                    _db.SaveChanges();

                    model?.SocioId = newModelSocio?.Id;

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
                var model = await _db.Usuario.FindAsync(id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                _db.Usuario.Remove(model);
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
