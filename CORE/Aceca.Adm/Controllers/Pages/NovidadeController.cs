using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Dapper;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Aceca.Adm.Controllers.Pages.Novidade
{
    [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
    public class NovidadeController : Controller
    {
        #region variaveis

        private readonly ILogger<NovidadeController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;

        private readonly string _ftpBaseUrl = string.Empty;
        private readonly string _ftpHost = string.Empty;
        private readonly string _ftpUser = string.Empty;
        private readonly string _ftpPass = string.Empty;

        private readonly bool _bIsLocalHost = false;
        //

        #endregion

        public NovidadeController(ILogger<NovidadeController> logger,
            AppDbContext db,
            IWebHostEnvironment env,
            IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;

            _ftpHost = _appConfiguration["Ftp:Host"]!;
            _ftpUser = _appConfiguration["Ftp:User"]!;
            _ftpPass = _appConfiguration["Ftp:Pass"]!;
            _ftpBaseUrl = _appConfiguration["Ftp:Path"]!;
        }

        #region Index
        public ActionResult Index()
        {
            return View("~/Views/Pages/Novidade.cshtml");
        }

        #endregion

        #region Consulta LISTAGEM

        [HttpPost]
        public async Task<IActionResult> FiltrarDados([FromBody] FilterDataMarca request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request inválido");

                var filtro = request.Filtros ?? new FiltroRequestMarca();

                var imgBase = _urlBaseImg;
                var imgDefault = $"{_urlBaseSite}/assets/img/img_inexistente.jpg";

                var sqlFrom = new StringBuilder(@"
                FROM marcas m
                LEFT JOIN marcas_fases mf ON m.marcaFaseId = mf.id
                LEFT JOIN marcas_finalidade mfi ON m.marcaFinalidadeId = mfi.id
                LEFT JOIN marcas_fabricas mfa ON m.marcaFabricaId = mfa.id
                LEFT JOIN marcas_dimensao md ON m.marcaDimensaoId = md.id
                LEFT JOIN marcas_impressora mi ON m.marcaImpressoraId = mi.id
                LEFT JOIN marcas_raridade mr ON m.marcaRaridadeId = mr.id
                LEFT JOIN marcas_raridade mq ON m.marcaQualidadeImagemId = mq.id
                LEFT JOIN marcas_subtipos mst ON m.marcaSubTipoId = mst.id
                LEFT JOIN marcas_tipos mt ON mst.marcaTipoId = mt.id
                WHERE 1=1
                ");

                var parameters = new DynamicParameters();

                // =========================
                // FILTROS
                // =========================

                if (filtro.MarcaFaseId > 0)
                {
                    sqlFrom.Append(" AND m.marcaFaseId = @MarcaFaseId");
                    parameters.Add("@MarcaFaseId", filtro.MarcaFaseId);
                }

                if (filtro.MarcaTipoId > 0)
                {
                    sqlFrom.Append(" AND mst.marcaTipoId = @MarcaTipoId");
                    parameters.Add("@MarcaTipoId", filtro.MarcaTipoId);
                }

                if (filtro.MarcaSubTipoId > 0)
                {
                    sqlFrom.Append(" AND m.marcaSubTipoId = @MarcaSubTipoId");
                    parameters.Add("@MarcaSubTipoId", filtro.MarcaSubTipoId);
                }

                //

                if (filtro.MarcaMesId > 0)
                {
                    sqlFrom.Append(" AND MONTH(m.dataCriacao) = @MarcaMesId");
                    parameters.Add("@MarcaMesId", filtro.MarcaMesId);
                }

                if (filtro.MarcaAnoId > 0)
                {
                    sqlFrom.Append(" AND YEAR(m.dataCriacao) = @MarcaAnoId");
                    parameters.Add("@MarcaAnoId", filtro.MarcaAnoId);
                }

                // =========================
                // SEARCH
                // =========================
                if (!string.IsNullOrWhiteSpace(request.Search?.Value))
                {
                    var rawSearch = request.Search.Value.Trim();
                    bool incluirDescricao = filtro.PesquisarDescricao;
                    bool termoCurto = rawSearch.Length < 3; // ← detecta termos que o FULLTEXT ignora

                    sqlFrom.Append(" AND (");

                    // O único índice FULLTEXT da tabela (idx_fulltext_busca) cobre
                    // nome+descricao+codigoAceca juntos - não existe (nem dá pra montar em
                    // runtime) um MATCH() só com Nome/CodigoAceca, então com "Pesquisar na
                    // Descrição" desligado o FULLTEXT fica de fora inteiro (senão qualquer
                    // termo que só aparecesse na Descrição seguiria "vazando" pro resultado
                    // por trás do MATCH(), mesmo com o checkbox desmarcado e o LIKE de
                    // Descricao abaixo corretamente omitido).
                    if (incluirDescricao && !termoCurto)
                    {
                        var normalized = Regex.Replace(rawSearch, @"[^\w\s]", " ");
                        var fullTextSearch = string.Join(" ",
                            normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => $"+{s}*")
                        );

                        sqlFrom.Append(@"
                            MATCH(m.Nome, m.Descricao, m.CodigoAceca)
                            AGAINST(@Search IN BOOLEAN MODE)
                            OR ");
                        parameters.Add("@Search", fullTextSearch);
                    }

                    // LIKE sempre cobre CodigoAceca, codigoAcecaNew e Nome
                    sqlFrom.Append(@"
                        m.CodigoAceca LIKE @SearchLike
                        OR m.codigoAcecaNew LIKE @SearchLike
                        OR m.Nome LIKE @SearchLike
                        ");

                    // Descrição só se checkbox ativo
                    if (incluirDescricao)
                    {
                        sqlFrom.Append(@" OR m.Descricao LIKE @SearchLike ");
                    }

                    sqlFrom.Append(")");

                    parameters.Add("@SearchLike", $"%{rawSearch}%");
                }

                // fallback
                else if (!string.IsNullOrWhiteSpace(filtro.NomeMarca))
                {
                    sqlFrom.Append(" AND m.Nome LIKE @Nome");
                    parameters.Add("@Nome", $"%{filtro.NomeMarca}%");
                }

                if (filtro.PesquisarSemVariante)
                {
                    sqlFrom.Append(" AND m.codigoAceca REGEXP '[0-9]$'");
                }

                // =========================
                // COUNT
                // =========================

                var totalSql = "SELECT COUNT(1) FROM marcas";
                var filteredSql = "SELECT COUNT(1) " + sqlFrom;

                // =========================
                // DATA
                // =========================

                var dataSql = $@"
                    SELECT
                        m.id AS Id,

                        mf.id AS IdMarcaFase,
                        mfi.id AS IdMarcaFinalidade,
                        mfa.id AS IdMarcaFabrica,
                        md.id AS IdMarcaDimensao,
                        mt.id AS IdMarcaTipo,
                        mst.id AS IdMarcaSubTipo,
                        mi.id AS IdMarcaImpressora,
                        mr.id AS IdMarcaRaridade,
                        mq.id AS IdQualidadeImagem,

                         -- m.codigoAcecaNew,
                        CASE
                            WHEN m.codigoAcecaNew IS NOT NULL AND m.codigoAcecaNew <> m.CodigoAceca
                            THEN CONCAT(m.codigoAcecaNew, '/', m.CodigoAceca)
                            ELSE m.CodigoAceca
                        END AS CodigoAceca,

                        m.Nome AS NomeMarca,
                        mf.Descricao AS NomeFase,
                        mfa.Nome AS NomeFabrica,
                        md.Descricao AS NomeDimensao,
                        mfi.Descricao AS NomeFinalidade,
                        mi.Descricao AS NomeImpressora,
                        mr.Descricao AS NomeRaridade,
                        mq.Descricao AS NomeQualidade,
                        mst.Descricao AS SubTipo,
                        mt.Descricao AS Tipo,
                        m.fabrica_txt AS TxtFabrica,
                        m.impressora AS TxtImpressora,

                        m.Descricao,
                        m.IncluidoPor,

                        m.Valor,
                        m.Valor1PI,
                        m.Valor2PI,

                        m.ImgPrincipal,
                        IF(m.ImgPrincipal IS NOT NULL,
                            CONCAT(@ImgBase,'/',m.MarcaFaseId,'/',m.ImgPrincipal),
                            @ImgDefault) AS ImgPrincipalFull,

                        m.ImgDetalhe,
                        IF(m.ImgDetalhe IS NOT NULL,
                            CONCAT(@ImgBase,'/',m.MarcaFaseId,'/detalhes/',m.ImgDetalhe),
                            @ImgDefault) AS ImgDetalheFull

                    {sqlFrom}

                   ORDER BY m.dataCriacao DESC,
                            m.CodigoAceca, m.nome
                    LIMIT @Limit OFFSET @Offset
                    ";

                parameters.Add("@ImgBase", imgBase);
                parameters.Add("@ImgDefault", imgDefault);
                parameters.Add("@Limit", request.Length);
                parameters.Add("@Offset", request.Start);

                using var conn = _db.Database.GetDbConnection();

                var total = await conn.ExecuteScalarAsync<int>(totalSql);
                var filtered = await conn.ExecuteScalarAsync<int>(filteredSql, parameters);

                var lstData = (await conn.QueryAsync(dataSql, parameters))
                    .Cast<IDictionary<string, object>>()
                    .ToList();

                return Ok(new
                {
                    draw = request.Draw,
                    recordsTotal = total,
                    recordsFiltered = filtered,
                    data = lstData
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