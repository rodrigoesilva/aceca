using Aceca.Adm.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Aceca.Adm.Controllers.Admin.Socio
{
    public class SocioEnderecoController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioEnderecoController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _appBaseUrl = string.Empty;
        //

        #endregion

        public SocioEnderecoController(ILogger<SocioEnderecoController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;

            _appBaseUrl = _appConfiguration["App:Url"]!;
        }

        #region Index

        public ActionResult Index()
        {
            return View("~/Views/Admin/Socio/SocioEndereco.cshtml");
        }

        #endregion

        #region GRID

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {

                var lstModel = await _db.SocioEndereco
                     .Include(x => x.Socio)
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

        #endregion

        #region CRUD JS

        [HttpPost]
        public async Task<IActionResult> Create(Models.SocioEndereco model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (string.IsNullOrEmpty(model.Endereco))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Endereço Inválido"
                        });

                    var newModel = new Models.SocioEndereco
                    {
                        SocioId = model.SocioId,
                        Endereco = !string.IsNullOrEmpty(model.Endereco) ? model.Endereco : null,
                        Numero = !string.IsNullOrEmpty(model.Numero) ? model.Numero : null,
                        Complemento = !string.IsNullOrEmpty(model.Complemento) ? model.Complemento : null,
                        Bairro = !string.IsNullOrEmpty(model.Bairro) ? model.Bairro : null,
                        Cidade = !string.IsNullOrEmpty(model.Cidade) ? model.Cidade : null,
                        Estado = !string.IsNullOrEmpty(model.Estado) ? model.Estado : null,
                        CEP = !string.IsNullOrEmpty(model.CEP) ? model.CEP : null,
                    };

                    _db.SocioEndereco.Add(newModel);
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
        public async Task<IActionResult> Edit(Models.SocioEndereco model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model?.Id < 1)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Sócio não identificado"
                        });

                    if (string.IsNullOrEmpty(model.Endereco))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Endereço Inválido"
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

                var model = await _db.SocioEndereco.FindAsync(id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                _db.SocioEndereco.Remove(model);
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
