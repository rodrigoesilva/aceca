using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Aceca.Adm.Controllers.Pages
{
    public class SocioColecaoController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioColecaoController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;
        //

        #endregion

        public SocioColecaoController(ILogger<SocioColecaoController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
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
            return View("~/Views/Admin/Socio/SocioColecao.cshtml");
        }

        #endregion

        #region GRID

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {

                var lstModel = await _db.SocioColecao
                    .Include(x => x.Socio)
                    .AsNoTracking()
                    .OrderBy(x => x.Socio.Nome)
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
        public async Task<IActionResult> Create(Models.SocioColecao model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.SocioId < 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "SocioId Inválido"
                        });

                    var newModel = new Models.SocioColecao
                    {
                        SocioId = model.SocioId,
                        MarcaId = model.MarcaId,
                        Quantidade = model.Quantidade,
                        Possui = true,
                        DisponivelTroca = model.DisponivelTroca,
                        DisponivelVenda = model.DisponivelVenda,
                        Interesse = model.Interesse
                    };

                    _db.SocioColecao.Add(newModel);
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
        public async Task<IActionResult> Edit(Models.SocioColecao model)
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

                    if (model.SocioId < 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "SocioId Inválido"
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

                var model = await _db.SocioColecao.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                _db.SocioColecao.Remove(model);
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


        public async Task AdicionarOuAtualizarItemAsync(int socioId,int itemId,int quantidade, bool troca,bool venda, bool interesse)
        {
            var registro = await _db.SocioColecao
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SocioId == socioId &&
                    x.MarcaId == itemId);

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

                _db.SocioColecao.Add(registro);
            }
            else
            {
                registro.Quantidade = quantidade;
                registro.DisponivelTroca = troca;
                registro.DisponivelVenda = venda;
                registro.Interesse = interesse;

                _db.SocioColecao.Update(registro);
            }

            await _db.SaveChangesAsync();
        }

        //Quem possui determinado item
        public async Task<List<SocioColecao>> QuemTem(int itemId)
        {
            var socios = await _db.SocioColecao
                .Where(x =>
                    x.MarcaId == itemId &&
                    x.Possui)
                .Include(x => x.Socio)
                .AsNoTracking()
                .ToListAsync();

            return socios;
        }

        //Quem quer trocar determinado item
        public async Task<List<SocioColecao>> QuemTroca(int itemId)
        {
            var troca = await _db.SocioColecao
                .AsNoTracking()
                .Where(x =>
                    x.MarcaId == itemId &&
                    x.DisponivelTroca)
                .ToListAsync();

            return troca;
        }

        //Itens desejados por um sócio
        public async Task<List<SocioColecao>> QqualPreciso(int socioId)
        {
            var desejos = await _db.SocioColecao
                .Where(x =>
                    x.SocioId == socioId &&
                    x.Interesse)
                .Include(x => x.Marca)
                .AsNoTracking()
                .ToListAsync();

            return desejos;
        }
    }
}