using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Aceca.Adm.Controllers.Admin.Marca
{
    [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
    public class MarcaController : Controller
    {
        #region variaveis

        private readonly ILogger<MarcaController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        private readonly string _imgBaseUrl = string.Empty;
        private readonly string _appBaseUrl = string.Empty;

        private string _strControllerName = string.Empty;
        private string _strActionName = string.Empty;
        //

        #endregion

        public MarcaController(ILogger<MarcaController> logger, 
            AppDbContext db, 
            IWebHostEnvironment env, 
            IConfiguration cfg,
            IMemoryCache cache)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;
            _cache = cache;

            _imgBaseUrl = _appConfiguration["Url:Img"]!;
            _appBaseUrl = _appConfiguration["App:Url"]!;
        }

        #region Index
        public ActionResult Index()
        {
            // return Redirect("https://www.google.com");
            return View("~/Views/Admin/Marca/Marca.cshtml");
        }

        [Authorize(Roles = "Administracao")]
        public IActionResult AdminDashboard()
        {
            // Only users with the "Admin" role can access this action.
            return View();
        }

        [Authorize(Roles = "Administracao")]
        public ActionResult Cadastro()
        {
            return View("~/Views/Admin/Marca/MarcaCadastro.cshtml");
        }

        #endregion

        #region Filtros

        [HttpPost]
        public async Task<IActionResult> FiltrarDados1([FromBody] object obj)
        {
            if (obj != null && string.IsNullOrEmpty(obj?.ToString()))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "FiltrarDados - Obj em branco"
                });

            try
            {
                string strUrlImgPath = _imgBaseUrl;

                string strUrlImgInexistente = $"{_appBaseUrl}/assets/img/img_inexistente.jpg";

                var jObj = JObject.Parse(obj?.ToString());

                var dynObj = new
                {
                    param_MarcaFaseId = jObj["param_MarcaFaseId"]?.ToObject<int>(),
                    param_MarcaFabricaId = jObj["param_MarcaFabricaId"]?.ToObject<int>(),
                    param_MarcaFabricaNome = jObj["param_MarcaFabricaNome"]?.ToObject<string>(),
                    param_MarcaTipoId = jObj["param_MarcaTipoId"]?.ToObject<int>(),
                    param_MarcaSubTipoId = jObj["param_MarcaSubTipoId"]?.ToObject<int>(),
                    param_IncluidoPor = jObj["param_IncluidoPor"]?.ToObject<string>(),
                    param_CodigoAceca = jObj["param_CodigoAceca"]?.ToObject<string>(),
                    param_NomeMarca = jObj["param_NomeMarca"]?.ToObject<string>(),
                    param_PesquisarSemVariante = jObj["param_PesquisarSemVariante"].ToObject<bool>(),
                    param_PesquisarDescricao = jObj["param_PesquisarDescricao"].ToObject<bool>(),
                };

                StringBuilder sb = new StringBuilder();

                sb.Append("SELECT");
                sb.Append(" m.id AS Id");
                sb.Append(" ,m.marcaFaseId AS IdMarcaFase");
                sb.Append(" ,m.marcaFinalidadeId AS IdMarcaFinalidade");
                sb.Append(" ,m.marcaFabricaId AS IdMarcaFabrica");
                sb.Append(" ,m.marcaDimensaoId AS IdMarcaDimensao");
                sb.Append(" ,mst.marcaTipoId AS IdMarcaTipo");
                sb.Append(" ,m.marcaSubTipoId AS IdMarcaSubTipo");
                sb.Append(" ,m.marcaImpressoraId AS IdMarcaImpressora");
                sb.Append(" ,m.marcaRaridadeId AS IdMarcaRaridade");
                sb.Append(" ,m.marcaQualidadeImagemId AS IdQualidadeImagem");
                
                sb.Append(" ,m.CodigoAceca");
                sb.Append(" ,m.Nome AS NomeMarca");
                sb.Append(" ,mf.Descricao AS NomeFase");
                sb.Append(" ,mfa.Nome AS NomeFabrica");
                sb.Append(" ,md.Descricao AS NomeDimensao");
                sb.Append(" ,mfi.Descricao AS NomeFinalidade");
                sb.Append(" ,mi.Descricao AS NomeImpressora");
                sb.Append(" ,mr.Descricao AS NomeRaridade");
                sb.Append(" ,mst.Descricao AS SubTipo");
                sb.Append(" ,mt.Descricao AS Tipo");
                sb.Append(" ,m.fabrica_txt AS TxtFabrica");
                sb.Append(" ,m.impressora AS TxtImpressora");
                sb.Append(" ,m.IncluidoPor");
                sb.Append(" ,m.Descricao");
                sb.Append(" ,m.Valor");
                sb.Append(" ,m.Valor1PI");
                sb.Append(" ,m.Valor2PI");
                sb.Append(" ,m.ImgPrincipal");
                sb.Append($",IF(m.ImgPrincipal IS NOT NULL, CONCAT('{strUrlImgPath}','/',m.MarcaFaseId,'/',m.ImgPrincipal), '{strUrlImgInexistente}') AS ImgPrincipalFull");
                sb.Append(" ,m.ImgDetalhe");
                sb.Append($",IF(m.ImgDetalhe IS NOT NULL, CONCAT('{strUrlImgPath}','/detalhes/', m.ImgDetalhe), '{strUrlImgInexistente}') AS ImgDetalheFull");
                sb.Append(" FROM");
                sb.Append(" marcas m");
                sb.Append(" LEFT JOIN marcas_fases mf ON m.marcaFaseId = mf.id");
                sb.Append(" LEFT JOIN marcas_finalidade mfi ON m.marcaFinalidadeId = mfi.id");
                sb.Append(" LEFT JOIN marcas_fabricas mfa ON m.marcaFabricaId = mfa.id");
                sb.Append(" LEFT JOIN marcas_dimensao md ON m.marcaDimensaoId = md.id");
                sb.Append(" LEFT JOIN marcas_impressora mi ON m.marcaImpressoraId = mi.id");
                sb.Append(" LEFT JOIN marcas_raridade mr ON m.marcaRaridadeId = mr.id");
                sb.Append(" LEFT JOIN marcas_qualidade_imagem mq ON m.marcaQualidadeImagemId = mq.id");
                sb.Append(" LEFT JOIN marcas_subtipos mst ON m.marcaSubTipoId = mst.id");
                sb.Append(" LEFT JOIN marcas_tipos mt ON mst.marcaTipoId = mt.id");
                sb.Append(" WHERE");
                sb.Append(" 1 = 1");

                if (dynObj?.param_MarcaFaseId > 0)
                    sb.Append(" AND m.MarcaFaseId = " + dynObj?.param_MarcaFaseId);

                if (dynObj?.param_MarcaFabricaId >= 0)
                    sb.Append(" AND m.marcaFabricaId = " + dynObj?.param_MarcaFabricaId); //.Where(p => p.MarcaFabrica.Nome.Equals(paramDynObj.param_MarcaFabricaNome)).ToList();

                if (dynObj?.param_MarcaTipoId >= 0)
                    sb.Append(" AND mst.marcaTipoId = " + dynObj?.param_MarcaTipoId);

                if (dynObj?.param_MarcaSubTipoId > 0)
                    sb.Append(" AND m.MarcaSubTipoId = " + dynObj?.param_MarcaSubTipoId);

                if (!string.IsNullOrEmpty(dynObj?.param_IncluidoPor))
                    sb.Append(" AND m.IncluidoPor like '%" + dynObj?.param_IncluidoPor.Trim() + "%'");

                if (!string.IsNullOrEmpty(dynObj?.param_CodigoAceca))
                    sb.Append(" AND m.CodigoAceca like '%" + dynObj?.param_CodigoAceca.Trim() + "%'");

                if (dynObj.param_PesquisarSemVariante)
                    sb.Append(" AND SUBSTRING(m.codigoAceca, -1) REGEXP '[0-9]'");

                if (!string.IsNullOrEmpty(dynObj?.param_NomeMarca))
                {
                    if (dynObj.param_PesquisarDescricao)
                        sb.Append(" AND (m.Nome like '%" + dynObj?.param_NomeMarca.Trim() + "%' OR m.Descricao like '%" + dynObj?.param_NomeMarca.Trim() + "%')");
                    else
                        sb.Append(" AND m.Nome like '%" + dynObj?.param_NomeMarca.Trim() + "%'");
                }

                sb.Append(" ORDER BY");
                sb.Append(" m.marcaFaseId, m.nome, m.descricao, mst.marcaTipoId, m.marcaSubTipoId, m.codigoAceca ;");

                string query = sb.ToString();

                var lstModel = await _db.Database
                    .SqlQuery<VMMarcaList>(FormattableStringFactory.Create(query))
                    //.Take(10)
                    .ToListAsync();

                if (lstModel?.Count <= 0)
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
                {
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
        public async Task<IActionResult> FiltrarDados2([FromBody] FiltroRequest filtro)
        {
            if (filtro == null)
                return BadRequest(new { bResult = false, message = "Filtro inválido" });

            try
            {
                var strUrlImgPath = _imgBaseUrl;
                var strUrlImgInexistente = $"{_appBaseUrl}/assets/img/img_inexistente.jpg";

                var sql = new StringBuilder();

                sql.Append(@"
                            SELECT
                                m.id AS Id,
                                m.marcaFaseId AS IdMarcaFase,
                                m.marcaFinalidadeId AS IdMarcaFinalidade,
                                m.marcaFabricaId AS IdMarcaFabrica,
                                m.marcaDimensaoId AS IdMarcaDimensao,
                                mst.marcaTipoId AS IdMarcaTipo,
                                m.marcaSubTipoId AS IdMarcaSubTipo,
                                m.marcaImpressoraId AS IdMarcaImpressora,
                                m.marcaRaridadeId AS IdMarcaRaridade,
                                m.marcaQualidadeImagemId AS IdQualidadeImagem,

                                m.CodigoAceca,
                                m.Nome AS NomeMarca,
                                mf.Descricao AS NomeFase,
                                mfa.Nome AS NomeFabrica,
                                md.Descricao AS NomeDimensao,
                                mfi.Descricao AS NomeFinalidade,
                                mi.Descricao AS NomeImpressora,
                                mr.Descricao AS NomeRaridade,
                                mst.Descricao AS SubTipo,
                                mt.Descricao AS Tipo,
                                m.fabrica_txt AS TxtFabrica,
                                m.impressora AS TxtImpressora,

                                m.Descricao,
                                m.Valor,
                                m.ImgPrincipal,
                                IF(m.ImgPrincipal IS NOT NULL,
                                    CONCAT(@ImgBase,'/clou/',m.MarcaFaseId,'/',m.ImgPrincipal),
                                    @ImgDefault) AS ImgPrincipalFull

                            FROM marcas m
                            LEFT JOIN marcas_fases mf ON m.marcaFaseId = mf.id
                            LEFT JOIN marcas_finalidade mfi ON m.marcaFinalidadeId = mfi.id
                            LEFT JOIN marcas_fabricas mfa ON m.marcaFabricaId = mfa.id
                            LEFT JOIN marcas_dimensao md ON m.marcaDimensaoId = md.id
                            LEFT JOIN marcas_impressora mi ON m.marcaImpressoraId = mi.id
                            LEFT JOIN marcas_raridade mr ON m.marcaRaridadeId = mr.id
                            LEFT JOIN marcas_subtipos mst ON m.marcaSubTipoId = mst.id
                            LEFT JOIN marcas_tipos mt ON mst.marcaTipoId = mt.id

                            WHERE 1=1
                            ");

                var parameters = new DynamicParameters();

                parameters.Add("@ImgBase", strUrlImgPath);
                parameters.Add("@ImgDefault", strUrlImgInexistente);

                if (filtro.MarcaFaseId > 0)
                {
                    sql.Append(" AND m.marcaFaseId = @MarcaFaseId");
                    parameters.Add("@MarcaFaseId", filtro.MarcaFaseId);
                }

                if (filtro.MarcaFabricaId > 0)
                {
                    sql.Append(" AND m.marcaFabricaId = @MarcaFabricaId");
                    parameters.Add("@MarcaFabricaId", filtro.MarcaFabricaId);
                }

                if (filtro.MarcaTipoId > 0)
                {
                    sql.Append(" AND mst.marcaTipoId = @MarcaTipoId");
                    parameters.Add("@MarcaTipoId", filtro.MarcaTipoId);
                }

                if (filtro.MarcaSubTipoId > 0)
                {
                    sql.Append(" AND m.marcaSubTipoId = @MarcaSubTipoId");
                    parameters.Add("@MarcaSubTipoId", filtro.MarcaSubTipoId);
                }

                if (!string.IsNullOrWhiteSpace(filtro.IncluidoPor))
                {
                    sql.Append(" AND m.IncluidoPor LIKE @IncluidoPor");
                    parameters.Add("@IncluidoPor", $"%{filtro.IncluidoPor}%");
                }

                if (!string.IsNullOrWhiteSpace(filtro.CodigoAceca))
                {
                    sql.Append(" AND m.CodigoAceca LIKE @CodigoAceca");
                    parameters.Add("@CodigoAceca", $"%{filtro.CodigoAceca}%");
                }

                if (filtro.PesquisarSemVariante)
                {
                    sql.Append(" AND m.codigoAceca REGEXP '[0-9]$'");
                }

                if (!string.IsNullOrWhiteSpace(filtro.NomeMarca))
                {
                    if (filtro.PesquisarDescricao)
                    {
                        sql.Append(" AND (m.Nome LIKE @NomeMarca OR m.Descricao LIKE @NomeMarca)");
                    }
                    else
                    {
                        sql.Append(" AND m.Nome LIKE @NomeMarca");
                    }

                    parameters.Add("@NomeMarca", $"%{filtro.NomeMarca}%");
                }

                sql.Append(@"
                            ORDER BY m.marcaFaseId, m.nome
                            LIMIT @Limit OFFSET @Offset
                            ");

                parameters.Add("@Limit", filtro.PageSize);
                parameters.Add("@Offset", (filtro.Page - 1) * filtro.PageSize);

                using var connection = _db.Database.GetDbConnection();

                var result = await connection.QueryAsync<VMMarcaList>(
                    sql.ToString(),
                    parameters
                );

                return Ok(new
                {
                    bResult = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em FiltrarDados");

                return BadRequest(new
                {
                    bResult = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]

        public async Task<IActionResult> FiltrarDados_([FromBody] DataTableRequest request)
        {
            try
            {
                var filtro = request.Filtros ?? new FiltroRequest();

                var imgBase = _imgBaseUrl;
                var imgDefault = $"{_appBaseUrl}/assets/img/img_inexistente.jpg";

                string cacheKey = $"marcas:{request.Start}:{request.Length}:" +
                  $"{filtro.MarcaFaseId}:{filtro.NomeMarca}:{filtro.PesquisarDescricao}";

                if (_cache.TryGetValue(cacheKey, out object cachedResult))
                {
                    return Ok(cachedResult);
                }

                var sqlFrom = new StringBuilder(@"
            FROM marcas m
            LEFT JOIN marcas_fases mf ON m.marcaFaseId = mf.id
            LEFT JOIN marcas_finalidade mfi ON m.marcaFinalidadeId = mfi.id
            LEFT JOIN marcas_fabricas mfa ON m.marcaFabricaId = mfa.id
            LEFT JOIN marcas_dimensao md ON m.marcaDimensaoId = md.id
            LEFT JOIN marcas_impressora mi ON m.marcaImpressoraId = mi.id
            LEFT JOIN marcas_raridade mr ON m.marcaRaridadeId = mr.id
            LEFT JOIN marcas_qualidade_imagem mq ON m.marcaQualidadeImagemId = mq.id
            LEFT JOIN marcas_subtipos mst ON m.marcaSubTipoId = mst.id
            LEFT JOIN marcas_tipos mt ON mst.marcaTipoId = mt.id
            WHERE 1=1
        ");

                var parameters = new DynamicParameters();

                // 🔹 parâmetros fixos (imagens)
                parameters.Add("@ImgBase", imgBase);
                parameters.Add("@ImgDefault", imgDefault);

                // 🔍 filtros
                if (filtro.MarcaFaseId > 0)
                {
                    sqlFrom.Append(" AND m.marcaFaseId = @MarcaFaseId");
                    parameters.Add("@MarcaFaseId", filtro.MarcaFaseId);
                }

                if (filtro.MarcaFabricaId > 0)
                {
                    sqlFrom.Append(" AND m.marcaFabricaId = @MarcaFabricaId");
                    parameters.Add("@MarcaFabricaId", filtro.MarcaFabricaId);
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

                if (!string.IsNullOrWhiteSpace(filtro.IncluidoPor))
                {
                    sqlFrom.Append(" AND m.IncluidoPor LIKE @IncluidoPor");
                    parameters.Add("@IncluidoPor", $"%{filtro.IncluidoPor}%");
                }

                if (!string.IsNullOrWhiteSpace(filtro.CodigoAceca))
                {
                    sqlFrom.Append(" AND m.CodigoAceca LIKE @CodigoAceca");
                    parameters.Add("@CodigoAceca", $"%{filtro.CodigoAceca}%");
                }

                if (filtro.PesquisarSemVariante)
                {
                    sqlFrom.Append(" AND m.codigoAceca REGEXP '[0-9]$'");
                }

                if (!string.IsNullOrWhiteSpace(filtro.NomeMarca))
                {
                    if (filtro.PesquisarDescricao)
                        sqlFrom.Append(" AND (m.Nome LIKE @Nome OR m.Descricao LIKE @Nome)");
                    else
                        sqlFrom.Append(" AND m.Nome LIKE @Nome");

                    parameters.Add("@Nome", $"%{filtro.NomeMarca}%");
                }

                // 🔢 total geral
                var totalSql = "SELECT COUNT(1) FROM marcas";

                // 🔢 total filtrado
                var filteredSql = "SELECT COUNT(1) " + sqlFrom.ToString();

                // 📄 SELECT completo (todas colunas)
                var dataSql = $@"
            SELECT
                m.id AS Id,
                m.marcaFaseId AS IdMarcaFase,
                m.marcaFinalidadeId AS IdMarcaFinalidade,
                m.marcaFabricaId AS IdMarcaFabrica,
                m.marcaDimensaoId AS IdMarcaDimensao,
                mst.marcaTipoId AS IdMarcaTipo,
                m.marcaSubTipoId AS IdMarcaSubTipo,
                m.marcaImpressoraId AS IdMarcaImpressora,
                m.marcaRaridadeId AS IdMarcaRaridade,
                m.marcaQualidadeImagemId AS IdQualidadeImagem,

                m.CodigoAceca,
                m.Nome AS NomeMarca,
                mf.Descricao AS NomeFase,
                mfa.Nome AS NomeFabrica,
                md.Descricao AS NomeDimensao,
                mfi.Descricao AS NomeFinalidade,
                mi.Descricao AS NomeImpressora,
                mr.Descricao AS NomeRaridade,
                mst.Descricao AS SubTipo,
                mt.Descricao AS Tipo,

                m.fabrica_txt AS TxtFabrica,
                m.impressora AS TxtImpressora,
                m.IncluidoPor,
                m.Descricao,
                m.Valor,
                m.Valor1PI,
                m.Valor2PI,
                m.ImgPrincipal,
                IF(m.ImgPrincipal IS NOT NULL,
                    CONCAT(@ImgBase,'/',m.MarcaFaseId,'/',m.ImgPrincipal),
                    @ImgDefault) AS ImgPrincipalFull,

                m.ImgDetalhe,
                IF(m.ImgDetalhe IS NOT NULL,
                    CONCAT(@ImgBase,'/detalhes/',m.ImgDetalhe),
                    @ImgDefault) AS ImgDetalheFull

            {sqlFrom}
            ORDER BY m.marcaFaseId, m.nome
            LIMIT @Limit OFFSET @Offset
        ";

                parameters.Add("@Limit", request.Length);
                parameters.Add("@Offset", request.Start);

                using var conn = _db.Database.GetDbConnection();

                var total = await conn.ExecuteScalarAsync<int>(totalSql);
                var filtered = await conn.ExecuteScalarAsync<int>(filteredSql, parameters);
                var data = await conn.QueryAsync(dataSql, parameters);

                var response = new
                {
                    draw = request.Draw,
                    recordsTotal = total,
                    recordsFiltered = filtered,
                    data = data
                };

                _cache.Set(cacheKey, response, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
                    SlidingExpiration = TimeSpan.FromSeconds(30)
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro FiltrarDados");

                return BadRequest(new
                {
                    error = true,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> FiltrarDados2222([FromBody] DataTableRequest request)
        {
            try
            {
                _logger.LogInformation($"REQUEST: {JsonConvert.SerializeObject(request)}");

                if (request == null) return BadRequest("Request inválido");

                var filtro = request?.Filtros ?? new FiltroRequest();

                var imgBase = _imgBaseUrl;
                var imgDefault = $"{_appBaseUrl}/assets/img/img_inexistente.jpg";

                var sqlFrom = new StringBuilder(@"
                                                FROM marcas m
                                                LEFT JOIN marcas_fases mf ON m.marcaFaseId = mf.id
                                                LEFT JOIN marcas_finalidade mfi ON m.marcaFinalidadeId = mfi.id
                                                LEFT JOIN marcas_fabricas mfa ON m.marcaFabricaId = mfa.id
                                                LEFT JOIN marcas_dimensao md ON m.marcaDimensaoId = md.id
                                                LEFT JOIN marcas_impressora mi ON m.marcaImpressoraId = mi.id
                                                LEFT JOIN marcas_raridade mr ON m.marcaRaridadeId = mr.id
                                                LEFT JOIN marcas_subtipos mst ON m.marcaSubTipoId = mst.id
                                                LEFT JOIN marcas_tipos mt ON mst.marcaTipoId = mt.id
                                                WHERE 1=1
                                                ");

                var parameters = new DynamicParameters();

                // =========================
                // FILTROS COMBOBOX
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

                // =========================
                // FULLTEXT SEARCH
                // =========================

                bool incluirDescricao = filtro.PesquisarSemVariante;

                if (!string.IsNullOrWhiteSpace(request?.Search?.Value))
                {
                    var rawSearch = request?.Search?.Value.Trim();

                    // remove caracteres problemáticos
                    var normalized = Regex.Replace(rawSearch, @"[^\w\s]", " ");

                    // monta FULLTEXT corretamente
                    var fullTextSearch = string.Join(" ",
                        normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => $"+{s}*")
                    );

                    sqlFrom.Append(@"
                                    AND (
                                        MATCH(m.Nome, m.Descricao, m.CodigoAceca)
                                        AGAINST(@Search IN BOOLEAN MODE)

                                        OR m.Descricao LIKE @SearchLike
                                        OR m.CodigoAceca LIKE @SearchLike
                                    )
                                    ");

                    parameters.Add("@Search", fullTextSearch);
                    parameters.Add("@SearchLike", $"%{rawSearch}%");

                    // 🔥 fallback para DESCRICAO (quando checkbox ativo)
                    if (filtro.PesquisarSemVariante)
                    {
                        sqlFrom.Append(@"
        OR m.Descricao LIKE @SearchLike
        ");
                        parameters.Add("@SearchLike", $"%{rawSearch}%");
                    }

                    sqlFrom.Append(")");

                    parameters.Add("@Search", fullTextSearch);
                }

                // fallback opcional
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
                // COUNTs
                // =========================

                var totalSql = "SELECT COUNT(1) FROM marcas";
                var filteredSql = "SELECT COUNT(1) " + sqlFrom;

                // =========================
                // DATA QUERY COMPLETA
                // =========================

                var dataSql = $@"
SELECT
    m.id AS Id,
    m.CodigoAceca,
    m.Nome AS NomeMarca,

    mf.Descricao AS NomeFase,
    mfa.Nome AS NomeFabrica,
    md.Descricao AS NomeDimensao,
    mfi.Descricao AS NomeFinalidade,
    mi.Descricao AS NomeImpressora,
    mr.Descricao AS NomeRaridade,
    mst.Descricao AS SubTipo,
    mt.Descricao AS Tipo,

    m.Descricao,
    m.IncluidoPor,

    IF(m.ImgPrincipal IS NOT NULL,
        CONCAT(@ImgBase,'/',m.MarcaFaseId,'/',m.ImgPrincipal),
        @ImgDefault) AS ImgPrincipalFull,

    IF(m.ImgDetalhe IS NOT NULL,
        CONCAT(@ImgBase,'/detalhes/',m.ImgDetalhe),
        @ImgDefault) AS ImgDetalheFull

{sqlFrom}

ORDER BY m.nome
LIMIT @Limit OFFSET @Offset
";

                parameters.Add("@ImgBase", imgBase);
                parameters.Add("@ImgDefault", imgDefault);
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

                //return Ok(new { draw = request.Draw, recordsTotal = 0, recordsFiltered = 0, data = new List<object>() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro FiltrarDados");

                return BadRequest(new { error = true, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> FiltrarDados([FromBody] DataTableRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request inválido");

                var filtro = request.Filtros ?? new FiltroRequest();

                var imgBase = _imgBaseUrl;
                var imgDefault = $"{_appBaseUrl}/assets/img/img_inexistente.jpg";

                var sqlFrom = new StringBuilder(@"
FROM marcas m
LEFT JOIN marcas_fases mf ON m.marcaFaseId = mf.id
LEFT JOIN marcas_finalidade mfi ON m.marcaFinalidadeId = mfi.id
LEFT JOIN marcas_fabricas mfa ON m.marcaFabricaId = mfa.id
LEFT JOIN marcas_dimensao md ON m.marcaDimensaoId = md.id
LEFT JOIN marcas_impressora mi ON m.marcaImpressoraId = mi.id
LEFT JOIN marcas_raridade mr ON m.marcaRaridadeId = mr.id
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

                // =========================
                // SEARCH
                // =========================

                if (!string.IsNullOrWhiteSpace(request.Search?.Value))
                {
                    var rawSearch = request.Search.Value.Trim();

                    var normalized = Regex.Replace(rawSearch, @"[^\w\s]", " ");

                    var fullTextSearch = string.Join(" ",
                        normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => $"+{s}*")
                    );

                    bool incluirDescricao = filtro.PesquisarDescricao;

                    // 🔥 INÍCIO DO BLOCO
                    sqlFrom.Append(" AND (");

                    // FULLTEXT sempre igual (usa índice)
                    sqlFrom.Append(@"
MATCH(m.Nome, m.Descricao, m.CodigoAceca)
AGAINST(@Search IN BOOLEAN MODE)
");

                    // LIKE para códigos sempre (resolve PN-181)
                    sqlFrom.Append(@"
OR m.CodigoAceca LIKE @SearchLike
");

                    // 🔥 descrição SOMENTE se checkbox TRUE
                    if (incluirDescricao)
                    {
                        sqlFrom.Append(@"
OR m.Descricao LIKE @SearchLike
");
                    }

                    // 🔥 fechamento correto
                    sqlFrom.Append(")");

                    parameters.Add("@Search", fullTextSearch);
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
    m.CodigoAceca,
    m.Nome AS NomeMarca,

    mf.Descricao AS NomeFase,
    mfa.Nome AS NomeFabrica,
    md.Descricao AS NomeDimensao,
    mfi.Descricao AS NomeFinalidade,
    mi.Descricao AS NomeImpressora,
    mr.Descricao AS NomeRaridade,
    mst.Descricao AS SubTipo,
    mt.Descricao AS Tipo,
    m.fabrica_txt AS TxtFabrica,
    m.impressora AS TxtImpressora,

    m.Descricao,
    m.IncluidoPor,

    IF(m.ImgPrincipal IS NOT NULL,
        CONCAT(@ImgBase,'/',m.MarcaFaseId,'/',m.ImgPrincipal),
        @ImgDefault) AS ImgPrincipalFull,

    IF(m.ImgDetalhe IS NOT NULL,
        CONCAT(@ImgBase,'/detalhes/',m.ImgDetalhe),
        @ImgDefault) AS ImgDetalheFull

{sqlFrom}

ORDER BY m.nome
LIMIT @Limit OFFSET @Offset
";

                parameters.Add("@ImgBase", imgBase);
                parameters.Add("@ImgDefault", imgDefault);
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
        public async Task<IActionResult> GetFullByIdFase(int id, string nome, bool bvariante)
        {
            string strNovoCodigoAceca = string.Empty;

            if (id < 1 || string.IsNullOrEmpty(nome))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "GetFullByIdFase - Id deve ser maior que 0",
                    data = id
                });

            try
            {
                var msgErroData = $"idMarcaFase :: {id} , NomeMarca :: {nome}";

                var strLetraInicial = nome.Trim()[0].ToString();

                var query = _db.Marca
                         .Where(x => x.MarcaFaseId.Equals(id));

                if (id.Equals(14) || (id >= 27 && id <= 29) || (id >= 32 && id <= 34) || id.Equals(36) || (id >= 39 && id <= 41))
                    query = query.Where(x => x.CodigoAceca != null && x.Nome.Contains(nome.Trim().ToString()))
                   // query = query.Where(x => x.CodigoAceca != null && x.CodigoAceca.StartsWith(nome.Trim()[0].ToString()))

                   .OrderByDescending(x => x.CodigoAceca);

                var lstModel = await query
                    .AsNoTracking()
                    .AsQueryable()
                    //.ToListAsync()
                    .FirstOrDefaultAsync();

                if (lstModel == null)
                {
                    return BadRequest(new
                    {
                        bResult = true,
                        type = "ERRO - GetFullByIdFase - lstModel",
                        message = "listagem Nula",
                        data = msgErroData
                    });
                }

                //var strCodigoAceca = lstmodel?.OrderByDescending(c => c.CodigoAceca)?.FirstOrDefault()?.CodigoAceca?.ToString();
                var strCodigoAceca = lstModel?.CodigoAceca?.ToString();

                string strNumCodigoAceca = new string(strCodigoAceca?.Where(char.IsDigit).ToArray());

                if (string.IsNullOrEmpty(strNumCodigoAceca))
                {
                    return BadRequest(new
                    {
                        bResult = true,
                        type = "ERRO - GetFullByIdFase - lstModel",
                        message = "strNumCodigoAceca Nula",
                        data = msgErroData
                    });
                }

                if (int.TryParse(strNumCodigoAceca, out int intNumCodigoAceca))
                    if (!bvariante)
                    {
                        strNovoCodigoAceca = strCodigoAceca?.Replace(intNumCodigoAceca.ToString(), (intNumCodigoAceca + 1).ToString());
                    }
                    else
                    {
                        var strUltimaLetraCodigoAceca = strCodigoAceca[^1];

                        char charProximaLetraCodigoAceca = (char)(strUltimaLetraCodigoAceca + 1);

                        strNovoCodigoAceca = ReplaceInPosition(strCodigoAceca.ToString(), strCodigoAceca.Length - 1, charProximaLetraCodigoAceca);
                    }

                if (string.IsNullOrEmpty(strNovoCodigoAceca))
                {
                    return BadRequest(new
                    {
                        bResult = true,
                        type = "ERRO - GetFullByIdFase - lstModel",
                        message = "strNovoCodigoAceca Nula",
                        data = msgErroData
                    });
                }

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = strNovoCodigoAceca?.ToUpper()
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
        public async Task<IActionResult> GetTipoByIdFase(int id)
        {
            var msgErroData = $"idMarcaFase :: {id}";

            if (id < 1)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "GetTipoByIdFase - Id deve ser maior que 0",
                    data = id
                });

            try
            {
                var lstModel = await _db.Marca
                    .DistinctBy(x => x.MarcaSubTipo.MarcaTipoId)
                    .Where(x => x.MarcaFaseId.Equals(id))
                    .Include(x => x.MarcaSubTipo)
                    .Include(x => x.MarcaSubTipo.MarcaTipo)
                    .OrderBy(x => x.MarcaSubTipo.MarcaTipoId)
                    .AsNoTracking()
                    .ToListAsync();

                if (lstModel == null)
                {
                    return BadRequest(new
                    {
                        bResult = true,
                        type = "ERRO - GetFullByIdFase - lstModel",
                        message = "listagem Nula",
                        data = msgErroData
                    });
                }

                return Ok(new
                {
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
        [Authorize(Roles = "Administracao")]
        public async Task<IActionResult> Create(string strObjModel, IFormFile iFileImgPrincipal, IFormFile iFileImgDetalhe)
        {
            try
            {
                if (string.IsNullOrEmpty(strObjModel))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Model Inválida",
                        data = strObjModel,
                    });

                #region Marca

                var vmModel = JsonConvert.DeserializeObject<VMMarca>(strObjModel);

                if (string.IsNullOrEmpty(vmModel?.Nome))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Nome deve ser preenchido"
                    });

                #region Upload Imagem

                //Verifica se existe ImgPrincipal para upload
                if (iFileImgPrincipal == null)
                    vmModel.ImgPrincipal = null;
                else
                {
                    if (!vmModel.ImgPrincipal.Equals("C:\\fakepath\\."))
                    {
                        var result = await UploadImg(vmModel, iFileImgPrincipal, true);

                        if (result.GetType() == typeof(NotFoundObjectResult) ||
                             result.GetType() == typeof(BadRequestObjectResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = result?.ToString()
                            });
                    }
                    else
                    {
                        vmModel?.ImgPrincipal = string.Empty;
                    }
                }

                //Verifica se existe ImgDetalhe para upload
                if (iFileImgDetalhe == null)
                    vmModel.ImgDetalhe = null;
                else
                {
                    if(!vmModel.ImgDetalhe.Equals("C:\\fakepath\\.")){
                        var result = await UploadImg(vmModel, iFileImgDetalhe, false);

                        if (result.GetType() == typeof(NotFoundObjectResult) ||
                             result.GetType() == typeof(BadRequestObjectResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = result?.ToString()
                            });
                    }
                    else
                    {
                        vmModel?.ImgDetalhe = string.Empty;
                    }
                }

                #endregion

                #region obj Marca

                // 1. Convert to Title Case
                TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;

                var model = new Marcas
                {
                    Ativo = true,

                    MarcaDimensaoId = (vmModel?.MarcaDimensaoId < 0 || vmModel?.MarcaDimensaoId == null) ? 0 : vmModel?.MarcaDimensaoId,
                    MarcaFabricaId = (vmModel?.MarcaFabricaId < 0 || vmModel?.MarcaFabricaId == null) ? 0 : vmModel?.MarcaFabricaId,
                    MarcaFaseId = (vmModel?.MarcaFaseId < 0 || vmModel?.MarcaFaseId == null) ? 0 : vmModel?.MarcaFaseId,
                    MarcaFinalidadeId = (vmModel?.MarcaFinalidadeId < 0 || vmModel?.MarcaFinalidadeId == null) ? 0 : vmModel?.MarcaFinalidadeId,
                    MarcaImpressoraId = (vmModel?.MarcaImpressoraId < 0 || vmModel?.MarcaImpressoraId == null) ? 0 : vmModel?.MarcaImpressoraId,
                    MarcaQualidadeImagemId = (vmModel?.MarcaQualidadeImagemId < 0 || vmModel?.MarcaQualidadeImagemId == null) ? 0 : vmModel?.MarcaQualidadeImagemId,
                    MarcaRaridadeId = (vmModel?.MarcaRaridadeId < 0 || vmModel?.MarcaRaridadeId == null) ? 0 : vmModel?.MarcaRaridadeId,
                    MarcaSubTipoId = (vmModel?.MarcaSubTipoId < 0 || vmModel?.MarcaSubTipoId == null) ? 5 : vmModel?.MarcaSubTipoId,

                    CodigoAceca = !string.IsNullOrEmpty(vmModel?.CodigoAceca) ? vmModel?.CodigoAceca?.Trim() : null,
                    CodigoFabrica = !string.IsNullOrEmpty(vmModel?.CodigoFabrica) ? vmModel?.CodigoFabrica?.Trim() : null,
                    ImgPrincipal = !string.IsNullOrEmpty(vmModel?.ImgPrincipal) ? Path.GetFileName(vmModel?.ImgPrincipal) : null,
                    ImgDetalhe = !string.IsNullOrEmpty(vmModel?.ImgDetalhe) ? Path.GetFileName(vmModel?.ImgDetalhe) : null,
                    Nome = !string.IsNullOrEmpty(vmModel?.Nome) ? vmModel?.Nome?.Trim() : null,
                    Descricao = !string.IsNullOrEmpty(vmModel?.Descricao) ? vmModel?.Descricao?.Trim() : null,
                    Valor1PI = !string.IsNullOrEmpty(vmModel?.Valor1PI) ? vmModel?.Valor1PI?.Trim() : null,
                    Valor2PI = !string.IsNullOrEmpty(vmModel?.Valor2PI) ? vmModel?.Valor2PI?.Trim() : null,
                    Valor = !string.IsNullOrEmpty(vmModel?.Valor) ? vmModel?.Valor?.Trim() : null,
                    IncluidoPor = !string.IsNullOrEmpty(vmModel?.IncluidoPor) ? textInfo.ToTitleCase(vmModel?.IncluidoPor?.Trim()?.ToLower()) : null,
                    EmQuarentena = !string.IsNullOrEmpty(vmModel?.EmQuarentena?.ToString()) ? vmModel?.EmQuarentena : 0,
                    
                    //
                    TxtFabrica = !string.IsNullOrEmpty(vmModel?.MarcaFabrica?.Nome) ? vmModel?.MarcaFabrica?.Nome?.Trim() : null,
                    TxtImpressora = !string.IsNullOrEmpty(vmModel?.MarcaImpressora?.Descricao) ? vmModel?.MarcaImpressora?.Descricao?.Trim() : null,
                };

                #endregion

                _db.Marca.Add(model);
                _db.SaveChanges();

                if (model?.Id <= 0)
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Falha ao Cadastrar Marca"
                    });

                vmModel?.Id = model?.Id;

                #endregion

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = vmModel,
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
        public async Task<IActionResult> Edit(Models.Marcas model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    #region Marca

                    if (model.Id < 1)
                    {
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Id deve ser maior que 0"
                        });
                    }

                    if (string.IsNullOrEmpty(model.Nome))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Nome deve ser preenchido"
                        });

                    #region IDS

                    model?.MarcaDimensaoId = (model?.MarcaDimensaoId < 0 || model?.MarcaDimensaoId == null) ? 0 : model?.MarcaDimensaoId;
                    model?.MarcaFabricaId = (model?.MarcaFabricaId < 0 || model?.MarcaFabricaId == null) ? 0 : model?.MarcaFabricaId;
                    model?.MarcaFaseId = (model?.MarcaFaseId < 0 || model?.MarcaFaseId == null) ? 0 : model?.MarcaFaseId;
                    model?.MarcaFinalidadeId = (model?.MarcaFinalidadeId < 0 || model?.MarcaFinalidadeId == null) ? 0 : model?.MarcaFinalidadeId;
                    model?.MarcaImpressoraId = (model?.MarcaImpressoraId < 0 || model?.MarcaImpressoraId == null) ? 0 : model?.MarcaImpressoraId;
                    model?.MarcaQualidadeImagemId = (model?.MarcaQualidadeImagemId < 0 || model?.MarcaQualidadeImagemId == null) ? 0 : model?.MarcaQualidadeImagemId;
                    model?.MarcaRaridadeId = (model?.MarcaRaridadeId < 0 || model?.MarcaRaridadeId == null) ? 0 : model?.MarcaRaridadeId;
                    model?.MarcaSubTipoId = (model?.MarcaSubTipoId < 0 || model?.MarcaSubTipoId == null) ? 5 : model?.MarcaSubTipoId;

                    #endregion

                    #region Upload Imagem

                    model?.ImgPrincipal = Path.GetFileName(model?.ImgPrincipal);
                    model?.ImgDetalhe = Path.GetFileName(model?.ImgDetalhe);
                    /*
                    //Verifica se existe ImgPrincipal para upload
                    if (iFileImgPrincipal == null)
                        vmModel.ImgPrincipal = null;
                    else
                    {
                        var result = await UploadImg(vmModel, iFileImgPrincipal, true);

                        if (result.GetType() == typeof(NotFoundObjectResult) ||
                             result.GetType() == typeof(BadRequestObjectResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = result?.ToString()
                            });
                    }

                    //Verifica se existe ImgDetalhe para upload
                    if (iFileImgDetalhe == null)
                        vmModel.ImgDetalhe = null;
                    else
                    {
                        var result = await UploadImg(vmModel, iFileImgDetalhe, false);

                        if (result.GetType() == typeof(NotFoundObjectResult) ||
                             result.GetType() == typeof(BadRequestObjectResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = result?.ToString()
                            });
                    }
                    */
                    #endregion

                    _db.Entry(model).State = EntityState.Modified;
                    _db.SaveChanges();

                    if (model?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar"
                        });

                    #endregion

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

                var model = await _db.Marca.FindAsync(id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                _db.Marca.Remove(model);
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

        #region Funcoes

        [HttpPost]
        public async Task<IActionResult> GetNovoCodigoAceca(int idFase, string strTermoBusca, bool bvariante)
        {
            string strNovoCodigoAceca = string.Empty;

            if (idFase < 1 || string.IsNullOrEmpty(strTermoBusca))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "GetCodigoAceca - Id deve ser maior que 0",
                    data = idFase
                });

            try
            {
                var queryExistsTermo = false;

                var msgErroData = $"idMarcaFase :: {idFase} , strTermoBusca :: {strTermoBusca}";

                var strCodigoAceca = string.Empty;

                var strLetraInicial = strTermoBusca?.Trim()[0].ToString();

                var query = _db.Marca
                    .Include(x => x.MarcaSubTipo.MarcaTipo)
                    .Include(x => x.MarcaFabrica)
                    .Include(x => x.MarcaImpressora)
                    .Where(x => x.MarcaFaseId.Equals(idFase));

                var queryExists = query.Any();

                //
                ///Fases que as marcas iniciam com letras
                ///
                if (idFase.Equals(14) // SA
                        || (idFase >= 27 && idFase <= 29) //27-Palheiros , 28 Fumos, 29 Exportacao
                        || (idFase >= 32 && idFase <= 34) //32-Cortadas, 33-Outros, 34-Quarentena
                        || idFase.Equals(36) // Comemorativas
                        || (idFase >= 39 && idFase <= 41) //39-Clandestinas, 40-Exterior, 41-M&C
                    )
                {

                    query = query.Where(x => x.CodigoAceca != null
                                            && (bvariante
                                                ? x.CodigoAceca.StartsWith(strTermoBusca.Trim().ToString())
                                                : (x.CodigoAceca.StartsWith(strLetraInicial) && x.MarcaFaseId.Equals(idFase))
                                                )
                                            )
                        .OrderByDescending(x => x.CodigoAceca);

                    queryExistsTermo = query.Any();
                }
                else
                {
                    query = query.Where(x => x.CodigoAceca != null
                                            && (bvariante
                                                ? x.CodigoAceca.StartsWith(strTermoBusca.Trim().ToString())
                                                : (x.MarcaFaseId.Equals(idFase))
                                                )
                                            )
                        .OrderByDescending(x => x.CodigoAceca)
                        .Take(5);

                    queryExistsTermo = query.Any();
                }

                var lstmodel = await query
                            .AsNoTracking()
                            .AsQueryable()
                            .FirstOrDefaultAsync();

                if (queryExists) {
                    if (queryExistsTermo && bvariante && lstmodel == null)
                    {
                        return Ok(new
                        {
                            bResult = false,
                            type = "ERRO - listagem Nula",
                            message = "Variante Pai Inexistente",
                            data = strTermoBusca
                        });
                    }

                    if (!queryExistsTermo && lstmodel == null)
                    {
                        return Ok(new
                        {
                            bResult = false,
                            type = "ERRO - listagem Nula",
                            message = "Essa fase  não inicia com essa letra",
                            data = strTermoBusca
                        });
                    }
                }

                if (lstmodel == null)
                {
                    return BadRequest(new
                    {
                        bResult = true,
                        type = "ERRO - GetCodigoAceca - lstModel",
                        message = "listagem Nula",
                        data = msgErroData
                    });
                }

                strCodigoAceca = lstmodel?.CodigoAceca?.ToString()?.Trim();

                string strNumCodigoAceca = new string(strCodigoAceca?.Where(char.IsDigit).ToArray());

                if (string.IsNullOrEmpty(strNumCodigoAceca))
                {
                    return BadRequest(new
                    {
                        bResult = true,
                        type = "ERRO - GetCodigoAceca - lstModel",
                        message = "strNumCodigoAceca Nula",
                        data = msgErroData
                    });
                }

                var strUltimaLetraCodigoAceca = 'B';

                if (int.TryParse(strNumCodigoAceca, out int intNumCodigoAceca))
                    if (!bvariante)
                    {
                        strNovoCodigoAceca = strCodigoAceca?.Replace(intNumCodigoAceca.ToString(), (intNumCodigoAceca + 1).ToString());

                        if (Char.IsLetter(strNovoCodigoAceca[^1]))
                            strNovoCodigoAceca = Char.IsLetter(strNovoCodigoAceca[^1])
                                ? strNovoCodigoAceca.Remove(strNovoCodigoAceca.Length - 1)
                                : string.Concat(strNovoCodigoAceca, strUltimaLetraCodigoAceca);
                    }
                    else
                    {
                        if (Char.IsLetter(strCodigoAceca[^1]))
                        {
                            strUltimaLetraCodigoAceca = strCodigoAceca[^1];

                            char charProximaLetraCodigoAceca = (char)(strUltimaLetraCodigoAceca + 1);

                            strNovoCodigoAceca = ReplaceInPosition(strCodigoAceca.ToString(), strCodigoAceca.Length - 1, charProximaLetraCodigoAceca);
                        }
                        else
                        {
                            strNovoCodigoAceca = string.Concat(strCodigoAceca, strUltimaLetraCodigoAceca);
                        }
                    }

                if (string.IsNullOrEmpty(strNovoCodigoAceca))
                {
                    return BadRequest(new
                    {
                        bResult = true,
                        type = "ERRO - GetFullByIdFase - lstModel",
                        message = "strNovoCodigoAceca Nula",
                        data = msgErroData
                    });
                }

                if (lstmodel?.MarcaImpressoraId == null || lstmodel?.MarcaImpressoraId <= 0)
                    if (!string.IsNullOrEmpty(lstmodel?.TxtImpressora))
                    {
                        var objImpressora = _db.MarcaImpressora
                            .Where(i => i.Descricao.Equals(lstmodel.TxtImpressora.Trim()))
                            .FirstOrDefault();

                        lstmodel.MarcaImpressora = new MarcaImpressora
                        {
                            Id = objImpressora.Id,
                            Descricao = objImpressora.Descricao
                        };

                        lstmodel.MarcaImpressoraId = objImpressora.Id;
                    }


                if (lstmodel?.MarcaFabricaId == null || lstmodel?.MarcaFabricaId <= 0)
                    if (!string.IsNullOrEmpty(lstmodel?.TxtFabrica))
                    {
                        var objFabrica = _db.MarcaFabrica
                            .Where(i => i.Descricao.Equals(lstmodel.TxtFabrica.Trim()))
                            .FirstOrDefault();

                        lstmodel.MarcaFabrica = new MarcaFabrica
                        {
                            Id = objFabrica.Id,
                            Nome = objFabrica.Nome,
                            Descricao = objFabrica.Descricao
                        };

                        lstmodel.MarcaFabricaId = objFabrica.Id;
                    }

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = lstmodel,
                    dataNovoCodigo = strNovoCodigoAceca
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

        public static string ReplaceInPosition(string input, int index, char newChar)
        {
            if (string.IsNullOrEmpty(input) || index < 0 || index >= input.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            char[] chars = input.ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }

        #endregion

        #region Upload Img

        [Authorize(Roles = "Administracao")]
        public async Task<IActionResult> UploadImg(VMMarca vmModel, IFormFile iFileImg, bool bIsImgPrincipal)
        {
            if (string.IsNullOrEmpty(iFileImg.FileName) || iFileImg?.FileName == null || iFileImg?.FileName.Length == 0)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Arquivo de Imagem Nulo ou Invalido"
                });

            string fileExtension = Path.GetExtension(iFileImg?.FileName?.ToString())?.ToLowerInvariant();

            var fileExtensionValid = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

            if (string.IsNullOrEmpty(fileExtension) || !fileExtensionValid.Contains(fileExtension))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Arquivo de Imagem com Extensão Inválida"
                });

            //Gera novo nome
            var fileSaveName = string.Concat(Guid.NewGuid(), "_", iFileImg?.FileName?.Trim()?.ToLower(), !(bool)iFileImg?.FileName.Contains(fileExtension) ? fileExtension : String.Empty);

            var fileTempPath = Path.GetTempFileName();

            // monta o caminho onde vamos salvar o arquivo :
            var strFileSaveFolderPath = bIsImgPrincipal
                ? Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", vmModel?.MarcaFaseId?.ToString())
                : Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", "detalhes");

            //Verifica diretorio existe e cria se necessario 
            if (!Directory.Exists(strFileSaveFolderPath))
                Directory.CreateDirectory(strFileSaveFolderPath);
            
            var fileDetails = new FileDetails()
            {
                FileName = Guid.NewGuid() + "_" + fileSaveName,
                FileSize = iFileImg.Length / 1000,
                FilePath = Path.Combine(strFileSaveFolderPath, fileSaveName),
                FileType = iFileImg?.ContentType,
            };

            var fileSavePath = fileDetails.FilePath;

            using (var stream = new FileStream(fileSavePath, FileMode.Create))
            {
                await iFileImg.CopyToAsync(stream);

                stream.Flush();
                stream.Close();
            }

            var fi = new FileInfo(fileTempPath);

            // Checa se arquivo existe
            if (!fi.Exists)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Arquivo Temporario ::: " + fileTempPath + " inexistente",
                    data = fileTempPath
                });

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
                data = fileSaveName,
            });
        }
        #endregion
    }
}