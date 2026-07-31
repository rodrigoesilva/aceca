using Aceca.Adm.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text;

namespace Aceca.Adm.Controllers.Admin.Socio
{
    [Authorize(Roles = "Administracao")]
    public class SocioAniversarioController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioAniversarioController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;
        //

        #endregion

        public SocioAniversarioController(ILogger<SocioAniversarioController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
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
            return View("~/Views/Admin/Socio/SocioAniversario.cshtml");
        }

        #endregion

        #region GRID

        // Paginação no servidor (Dapper + LIMIT/OFFSET) — antes carregava todos os aniversários
        // de uma vez (ver auditoria de performance / piloto SocioLogAcesso).
        [HttpPost]
        public async Task<IActionResult> FiltrarDados([FromBody] Models.FilterDataGridSimples request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request inválido");

                var sqlFrom = new StringBuilder(@"
                FROM socio_aniversario sa
                INNER JOIN socios s ON sa.SocioId = s.id
                WHERE 1=1
                ");

                var parameters = new DynamicParameters();

                if (request.SomenteAtivos)
                {
                    sqlFrom.Append(" AND s.ativo = true");
                }

                if (!string.IsNullOrWhiteSpace(request.Search?.Value))
                {
                    sqlFrom.Append(@"
                        AND s.nome LIKE @SearchLike
                    ");
                    parameters.Add("@SearchLike", $"%{request.Search.Value.Trim()}%");
                }

                var totalSql = "SELECT COUNT(1) FROM socio_aniversario";
                var filteredSql = "SELECT COUNT(1) " + sqlFrom;

                var dataSql = $@"
                    SELECT
                        sa.id AS Id,
                        sa.SocioId AS SocioId,
                        sa.dia AS Dia,
                        sa.mes AS Mes,

                        s.nome AS NomeSocio,
                        s.ativo AS SocioAtivo

                    {sqlFrom}

                    ORDER BY s.nome
                    LIMIT @Limit OFFSET @Offset
                    ";

                parameters.Add("@Limit", request.Length);
                parameters.Add("@Offset", request.Start);

                using var conn = _db.Database.GetDbConnection();

                var total = await conn.ExecuteScalarAsync<int>(totalSql);
                var filtered = await conn.ExecuteScalarAsync<int>(filteredSql, parameters);
                var data = await conn.QueryAsync(dataSql, parameters);

                return Ok(new
                {
                    draw = request.Draw,
                    recordsTotal = total,
                    recordsFiltered = filtered,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro FiltrarDados");

                return BadRequest(new { error = true, message = ex.Message });
            }
        }

        #endregion

        #region CRUD JS

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.SocioAniversario model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model?.SocioId is null || model.SocioId <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Sócio deve ser selecionado",
                            data = model,
                            modelState = ModelState
                        });

                    if (string.IsNullOrEmpty(model?.Dia?.ToString()) || string.IsNullOrEmpty(model?.Mes?.ToString()))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Data Inválida",
                            data = model,
                            modelState = ModelState
                        });

                    var newModel = new Models.SocioAniversario
                    {
                        SocioId = model.SocioId,
                        Dia = model.Dia,
                        Mes = model.Mes,
                    };

                    _db.SocioAniversario.Add(newModel);
                    _db.SaveChanges();

                    model.Id = newModel?.Id;

                    if (model?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar",
                            data = model,
                            modelState = ModelState
                        });

                    return Ok(new
                    {
                        bResult = true,
                        type = "OK",
                        message = "SUCESSO ::: ",
                        data = model,
                        modelState = ModelState
                    });
                }

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Model Inválida",
                    data = model,
                    modelState = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors?.Select(e => e.ErrorMessage).ToArray()
                        )
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
                    message = mensagemErro,
                    data = model,
                    modelState = ModelState
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Models.SocioAniversario model)
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

                    if (string.IsNullOrEmpty(model?.Dia?.ToString()) || string.IsNullOrEmpty(model?.Mes?.ToString()))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Data Inválida"
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
                    modelState = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors?.Select(e => e.ErrorMessage).ToArray()
                        )
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
                {
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Id deve ser maior que 0"
                    });
                }

                var model = await _db.SocioAniversario.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                _db.SocioAniversario.Remove(model);
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
