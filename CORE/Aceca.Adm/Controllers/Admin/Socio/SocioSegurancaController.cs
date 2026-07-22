using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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

        // Paginação no servidor (Dapper + LIMIT/OFFSET) — antes carregava todos os sócios
        // de uma vez (ver auditoria de performance / piloto SocioLogAcesso).
        [HttpPost]
        public async Task<IActionResult> FiltrarDados([FromBody] Models.FilterDataGridSimples request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request inválido");

                var sqlFrom = new StringBuilder(@"
                FROM socio_seguranca sg
                INNER JOIN socios s ON sg.SocioId = s.id
                LEFT JOIN socio_perfil sp ON s.socioPerfilId = sp.id
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
                            OR sg.nome_usuario LIKE @SearchLike
                            OR sg.Email LIKE @SearchLike
                        )
                    ");
                    parameters.Add("@SearchLike", $"%{request.Search.Value.Trim()}%");
                }

                var totalSql = "SELECT COUNT(1) FROM socio_seguranca";
                var filteredSql = "SELECT COUNT(1) " + sqlFrom;

                var dataSql = $@"
                    SELECT
                        sg.id AS Id,
                        sg.SocioId AS SocioId,
                        sg.nome_usuario AS NomeUsuario,
                        sg.Email AS Email,
                        sg.senha_aberta AS SenhaAberta,
                        sg.last_login AS UltimoLogin,

                        s.nome AS NomeSocio,
                        s.ativo AS SocioAtivo,
                        s.socioPerfilId AS SocioPerfilId,

                        sp.descricao AS SocioPerfilDescricao

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
        public async Task<IActionResult> Edit(Models.SocioSeguranca model, int? socioPerfilId, bool? ativo)
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

                    if (model?.Id is null || model.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar Socio"
                        });

                    // Atualiza somente os campos editáveis nesta tela.
                    // Senha, SenhaAberta, ResetPasswordToken e ResetPasswordTokenExpiry
                    // não fazem parte deste formulário e não devem ser tocados aqui.
                    var trackedUser = await _db.SocioSeguranca
                        .Include(x => x.Socio)
                        .FirstOrDefaultAsync(x => x.Id == model.Id);

                    if (trackedUser is null)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar Socio"
                        });

                    trackedUser.Email = model.Email.Trim().ToLowerInvariant();
                    trackedUser.NomeUsuario = model.NomeUsuario;

                    #endregion

                    #region Socio

                    if (trackedUser.Socio is null)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Sócio não identificado"
                        });

                    if (socioPerfilId.HasValue && socioPerfilId.Value > 0)
                        trackedUser.Socio.SocioPerfilId = socioPerfilId.Value;

                    // Ativo=false bloqueia o acesso do sócio de qualquer forma:
                    // impede novo login (AuthController) e encerra sessão já autenticada
                    // na próxima requisição (OnValidatePrincipal em Program.cs).
                    if (ativo.HasValue)
                        trackedUser.Socio.Ativo = ativo.Value;

                    #endregion

                    await _db.SaveChangesAsync();

                    return Ok(new
                    {
                        bResult = true,
                        type = "OK",
                        message = "SUCESSO ::: ",
                        data = trackedUser,
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
                var model = await _db.SocioSeguranca.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

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
