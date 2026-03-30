using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Reflection;

namespace Aceca.Adm.Controllers.Admin.Socio
{
    public class SocioController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _appBaseUrl = string.Empty;
        //

        #endregion

        public SocioController(ILogger<SocioController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
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
            return View("~/Views/Admin/Socio/Socio.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {
                /*
                var lstModel = await _db.Socio
                    .Include(x => x.SocioPerfil)
                    .OrderBy(x => x.Nome)
                    .AsNoTracking()
                    .ToListAsync();
                */

                var result = from s in _db.Socio
                             join sa in _db.SocioAniversario on s.Id equals sa.SocioId
                             join sc in _db.SocioContato on s.Id equals sc.SocioId
                             join se in _db.SocioEndereco on s.Id equals se.SocioId
                             join sp in _db.SocioPerfil on s.SocioPerfilId equals sp.Id
                             orderby s.Nome
                             select new
                             {
                                 Socio = s,
                                 SocioAniversario = sa,
                                 SocioContato = sc,
                                 SocioEndereco = se,
                                 SocioPerfil = sp,
                             };


                var lstModel = await result.AsNoTracking().ToListAsync();

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
        public async Task<IActionResult> GetFullById(int id)
        {
            if (id < 1)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "GetFullById - Id deve ser maior que 0",
                    data = id
                });

            try
            {
                var result = from s in _db.Socio // Table 1
                             join sa in _db.SocioAniversario on s.Id equals sa.SocioId
                             join sc in _db.SocioContato on s.Id equals sc.SocioId
                             join se in _db.SocioEndereco on s.Id equals se.SocioId
                             join sf in _db.SocioFinanceiro on s.Id equals sf.SocioId
                             join sp in _db.SocioPerfil on s.SocioPerfilId equals sp.Id
                             where s.Id == id
                             select new
                             {
                                 Socio = s,
                                 SocioAniversario = sa,
                                 SocioContato = sc,
                                 SocioEndereco = se,
                                 SocioFinanceiro = sf,
                                 SocioPerfil = sp,
                             };


                var lstModel = await result.AsNoTracking().ToListAsync();

                if (lstModel.Count <= 0)
                {
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - VAZIO - lstResult",
                        message = "GetById - Model ID Invalido",
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
                    data = lstModel.FirstOrDefault(),
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
        public async Task<IActionResult> Create(VMSocio model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    #region Socio

                    if (string.IsNullOrEmpty(model.Nome))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Nome deve ser preenchido"
                        });

                    var newModel = new Models.Socio
                    {
                        SocioPerfilId = model.SocioPerfilId = model.SocioPerfilId > 0 ? model.SocioPerfilId : 5, //socio
                        Nome = model.Nome,
                        MostrarSite = model.MostrarSite != null ? model.MostrarSite : true,
                        Ativo = model.Ativo,
                    };

                    _db.Socio.Add(newModel);
                    _db.SaveChanges();

                    model.Id = newModel?.Id;

                    if (newModel?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Cadastrar Socio"
                        });

                    #endregion

                    #region SocioContato

                    if (string.IsNullOrEmpty(model?.Email))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Email deve ser preenchido"

                        });

                    var resulCreateSocioContato = await Create_SocioContato(model);

                    if (resulCreateSocioContato.GetType() == typeof(NotFoundObjectResult) ||
                               resulCreateSocioContato.GetType() == typeof(NotFoundResult) ||
                               resulCreateSocioContato.GetType() == typeof(BadRequestObjectResult) ||
                               resulCreateSocioContato.GetType() == typeof(BadRequestResult))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Cadastrar Socio Contato",
                            data = model
                        });

                    var objJsonResulCreateSocioContatoReturnApi = ((ObjectResult)resulCreateSocioContato).Value;

                    #endregion

                    #region SocioEndereco

                    var resulCreateSocioEndereco = await Create_SocioEndereco(model);

                    if (resulCreateSocioEndereco.GetType() == typeof(NotFoundObjectResult) ||
                               resulCreateSocioEndereco.GetType() == typeof(NotFoundResult) ||
                               resulCreateSocioEndereco.GetType() == typeof(BadRequestObjectResult) ||
                               resulCreateSocioEndereco.GetType() == typeof(BadRequestResult))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Cadastrar Socio Contato",
                            data = model
                        });

                    var objJsonResulCreateSocioEnderecoReturnApi = ((ObjectResult)resulCreateSocioEndereco).Value;

                    #endregion

                    #region SocioAniversario

                    var resulCreateSocioAniversario = await Create_SocioAniversario(model);

                    if (resulCreateSocioAniversario.GetType() == typeof(NotFoundObjectResult) ||
                               resulCreateSocioAniversario.GetType() == typeof(NotFoundResult) ||
                               resulCreateSocioAniversario.GetType() == typeof(BadRequestObjectResult) ||
                               resulCreateSocioAniversario.GetType() == typeof(BadRequestResult))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Cadastrar Socio Contato",
                            data = model
                        });

                    var objJsonResulCreateSocioAniversarioReturnApi = ((ObjectResult)resulCreateSocioAniversario).Value;

                    #endregion

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
        public async Task<IActionResult> Edit(VMSocio model)
        {

            try
            {
                if (ModelState.IsValid)
                {
                    if (string.IsNullOrEmpty(model.Nome))
                    {
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Nome deve ser preenchido"
                        });
                    }

                    //model = await _db.Socio.FindAsync(id);

                    // Mark the entity state as modified
                    _db.Entry(model).State = EntityState.Modified;
                    _db.SaveChanges();
                    return RedirectToAction("Index");
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
                var model = await _db.Socio.FindAsync(id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                _db.Socio.Remove(model);
                _db.SaveChanges();

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

        #region Socio Derivacao
        public async Task<IActionResult> Create_SocioContato(VMSocio model)
        {
            try
            {
                if (!IsValidEmailUsingMailAddress(model?.Email?.Trim()?.ToLower()))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Formato de Email Inválido"
                    });

                var newModel = new SocioContato
                {
                    SocioId = model.Id,
                    DDI =  model.DDI > 0 ? model.DDI : 55,
                    DDD =  !string.IsNullOrEmpty(model.Telefone) ? Convert.ToInt16(model.Telefone.Split(")")[0].Replace("(", string.Empty)) : null,
                    Telefone = !string.IsNullOrEmpty(model.Telefone) ? Convert.ToInt32(model.Telefone.Split(")")[1].Replace("-", string.Empty)) : null,
                    Email = model?.Email?.Trim()?.ToLower(),
                };

                _db.SocioContato.Add(newModel);
                _db.SaveChanges();

                var newSocioContatoId = newModel?.Id;

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
        public async Task<IActionResult> Create_SocioEndereco(VMSocio model)
        {
            try
            {
                var newModel = new SocioEndereco
                {
                    SocioId = model.Id,
                    Endereco = model.Endereco,
                    Numero = model.Numero,
                    Complemento = model.Complemento,
                    Bairro = model.Bairro,
                    Cidade = model.Cidade,
                    Estado = model.Estado,
                    CEP = !string.IsNullOrEmpty(model.CEP) ? model.CEP.Replace("-", string.Empty) : string.Empty,
                };

                _db.SocioEndereco.Add(newModel);
                _db.SaveChanges();

                var newSocioEnderecoId = newModel?.Id;

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
        public async Task<IActionResult> Create_SocioAniversario(VMSocio model)
        {
            try
            {
                var newModel = new SocioAniversario
                {
                    SocioId = model.Id,
                    Dia = !string.IsNullOrEmpty(model.DataAniversario) ? Convert.ToInt32(model.DataAniversario.Split("/")[0]) : null,
                    Mes = !string.IsNullOrEmpty(model.DataAniversario) ? Convert.ToInt32(model.DataAniversario.Split("/")[1]) : null,
                };

                _db.SocioAniversario.Add(newModel);
                _db.SaveChanges();

                var newSocioAniversarioId = newModel?.Id;

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

        #region Validador Email
        public bool IsValidEmailUsingMailAddress(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                return false;
            try
            {
                var mailAddress = new MailAddress(emailAddress);
                // Optional extra check to ensure the original input matches the parsed address,
                // preventing issues with inputs like "John Doe" <john@doe.com>.
                return mailAddress.Address == emailAddress;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        #endregion
    }
}
