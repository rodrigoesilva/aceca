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
    public class SocioEnderecoController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioEnderecoController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;
        //

        #endregion

        public SocioEnderecoController(ILogger<SocioEnderecoController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
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
            return View("~/Views/Admin/Socio/SocioEndereco.cshtml");
        }

        #endregion

        #region GRID

        // Paginação no servidor (Dapper + LIMIT/OFFSET) — antes carregava todos os endereços
        // de uma vez (ver auditoria de performance / piloto SocioLogAcesso).
        [HttpPost]
        public async Task<IActionResult> FiltrarDados([FromBody] Models.FilterDataGridSimples request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request inválido");

                var sqlFrom = new StringBuilder(@"
                FROM socio_endereco se
                INNER JOIN socios s ON se.SocioId = s.id
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
                        AND (
                            s.nome LIKE @SearchLike
                            OR se.endereco LIKE @SearchLike
                            OR se.bairro LIKE @SearchLike
                            OR se.cidade LIKE @SearchLike
                            OR se.cep LIKE @SearchLike
                        )
                    ");
                    parameters.Add("@SearchLike", $"%{request.Search.Value.Trim()}%");
                }

                var totalSql = "SELECT COUNT(1) FROM socio_endereco";
                var filteredSql = "SELECT COUNT(1) " + sqlFrom;

                var dataSql = $@"
                    SELECT
                        se.id AS Id,
                        se.SocioId AS SocioId,
                        se.endereco AS Endereco,
                        se.numero AS Numero,
                        se.complemento AS Complemento,
                        se.bairro AS Bairro,
                        se.cidade AS Cidade,
                        se.estado AS Estado,
                        se.cep AS Cep,

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
        public async Task<IActionResult> Create(Models.SocioEndereco model)
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
                            message = "Sócio deve ser selecionado"
                        });

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
        [ValidateAntiForgeryToken]
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

                var model = await _db.SocioEndereco.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

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
}