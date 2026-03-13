using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Aceca.Adm.Controllers.Admin.Fabrica
{
    public class FabricaController : Controller
    {
        #region variaveis

        private readonly ILogger<FabricaController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _appBaseUrl = string.Empty;
        //

        #endregion

        public FabricaController(ILogger<FabricaController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
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
            return View("~/Views/Admin/Fabrica/Fabrica.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {

                var lstModel = await _db.Fabrica
                    .Include(x =>x.FabricaFase)
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
