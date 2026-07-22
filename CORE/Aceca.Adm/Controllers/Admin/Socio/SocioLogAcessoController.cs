using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Aceca.Adm.Controllers.Admin.Socio
{
    public class SocioLogAcessoController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioLogAcessoController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;
        //

        #endregion

        public SocioLogAcessoController(ILogger<SocioLogAcessoController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;
        }

        #region CRUD JS

        public ActionResult Index()
        {

            return View("~/Views/Admin/Socio/SocioLogAcesso.cshtml");
        }

        // Piloto de paginação no servidor (ver auditoria de performance): antes carregava
        // a tabela inteira (só cresce, um registro por login) e paginava no navegador.
        [HttpPost]
        public async Task<IActionResult> FiltrarDados([FromBody] FilterDataGridSimples request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request inválido");

                var sqlFrom = new StringBuilder(@"
                FROM socio_log_acesso sla
                INNER JOIN socio_endereco se ON sla.socioEnderecoId = se.id
                INNER JOIN socios s ON se.socioId = s.id
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
                            OR sla.ip LIKE @SearchLike
                            OR sla.operadora LIKE @SearchLike
                            OR sla.cidade LIKE @SearchLike
                            OR sla.estado LIKE @SearchLike
                            OR se.cidade LIKE @SearchLike
                            OR se.estado LIKE @SearchLike
                            OR sla.browser LIKE @SearchLike
                            OR sla.os LIKE @SearchLike
                            OR sla.device LIKE @SearchLike
                        )
                    ");
                    parameters.Add("@SearchLike", $"%{request.Search.Value.Trim()}%");
                }

                var totalSql = "SELECT COUNT(1) FROM socio_log_acesso";
                var filteredSql = "SELECT COUNT(1) " + sqlFrom;

                var dataSql = $@"
                    SELECT
                        sla.id AS Id,
                        sla.ip AS Ip,
                        sla.os AS Os,
                        sla.browser AS Browser,
                        sla.device AS Device,
                        sla.operadora AS Operadora,
                        sla.cidade AS OrigemCidade,
                        sla.estado AS OrigemEstado,
                        sla.last_login AS UltimoLogin,

                        s.nome AS NomeSocio,
                        s.ativo AS SocioAtivo,

                        se.cidade AS EnderecoCidade,
                        se.estado AS EnderecoEstado

                    {sqlFrom}

                    ORDER BY sla.last_login DESC
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
    }
}
