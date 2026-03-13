using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using static System.Collections.Specialized.BitVector32;

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

                var lstModel = await _db.Socio
                    .Include(x => x.SocioPerfil)
                    .OrderBy(x => x.Nome)
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
                var mensagemErro = $"ListGrid : {ex?.Message}";
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
                    message = "DELETE - Id deve ser maior que 0",
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
                var mensagemErro = $"ListGrid : {ex?.Message}";
                _logger.LogError(mensagemErro);

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = mensagemErro
                });
            }
        }

        /*
        [HttpPost]
        public ActionResult Create(Socio data)
        {
            try
            {
                dynamic response = new { bResult = false, message = string.Empty };

                if (string.IsNullOrEmpty(data.Nome))
                {
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Nome deve ser preenchido"
                    });
                }

                try
                {
                    var result = AsyncActionAPI(data, "Create");

                    if (result.GetType() == typeof(NotFoundObjectResult) ||
                         result.GetType() == typeof(BadRequestObjectResult))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = result?.ToString()
                        });
                }
                catch (Exception ex)
                {
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = ex?.Message?.ToString()
                    });
                }

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: "
                });
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        public ActionResult Edit(Socio data)
        {
            try
            {
                dynamic response = new { bResult = false, message = string.Empty };

                if (string.IsNullOrEmpty(data.Nome))
                {
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Nome deve ser preenchido"
                    });
                }

                try
                {
                    var result = AsyncActionAPI(data, "Edit");

                    if (result.GetType() == typeof(NotFoundObjectResult) ||
                         result.GetType() == typeof(BadRequestObjectResult))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = result?.ToString()
                        });
                }
                catch (Exception ex)
                {
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = ex?.Message?.ToString()
                    });
                }

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: "
                });
            }
            catch
            {
                return View();
            }
        }

        [HttpDelete]
        public ActionResult Delete(int id)
        {
            dynamic response = new { bResult = false, message = string.Empty };

            if (id < 1)
            {
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Id deve ser maior que 0"
                });
            }

            var model = new List<Socio>();

            try
            {
                var result = AsyncDeleteById(id);

                if (result.GetType() == typeof(NotFoundObjectResult) ||
                     result.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = result?.ToString()
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = ex?.Message?.ToString()
                });
            }

            return Ok(new
            {
                bResult = true,
                type = "OK",
                message = "SUCESSO ::: "
            });

            //return View();
        }

        */
        #endregion
    }
}
