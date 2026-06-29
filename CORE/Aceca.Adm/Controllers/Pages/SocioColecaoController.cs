using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers.Pages
{
    [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
    public class SocioColecaoController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioColecaoController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;

        private readonly string _ftpBaseUrl = string.Empty;
        private readonly string _ftpHost = string.Empty;
        private readonly string _ftpUser = string.Empty;
        private readonly string _ftpPass = string.Empty;

        private readonly bool _bIsLocalHost = false;


        #endregion

        public SocioColecaoController(ILogger<SocioColecaoController> logger,
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
            return View("~/Views/Admin/Socio/SocioColecao.cshtml");
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

                var filtroColecao = request.FiltrosColecao ?? new FiltroRequestColecao();

                var imgBase = _urlBaseImg;
                var imgDefault = $"{_urlBaseSite}/assets/img/img_inexistente.jpg";

                var sqlFrom = new StringBuilder(@"
                FROM socio_colecao sc
                INNER JOIN socios s ON sc.socioId = s.id
                INNER JOIN marcas m ON sc.marcaId = m.id
                LEFT JOIN marcas_acervo ma ON m.marcaAcervoId = ma.id
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
                if (filtroColecao.SocioId > 0)
                {
                    if (filtroColecao.SocioId != 1 || !filtro.ExibirGeral)
                    {
                        sqlFrom.Append(" AND sc.socioId = @SocioId");
                        parameters.Add("@SocioId", filtroColecao.SocioId);
                    }
                }

                if ((int)filtroColecao.ColecaoStatus > 0)
                {
                    switch ((EColecaoStatus)filtroColecao.ColecaoStatus)
                    {
                        case EColecaoStatus.Possui:
                            {
                                sqlFrom.Append(" AND sc.possui = true");
                            }
                            break;
                        case EColecaoStatus.Interesse:
                            {
                                sqlFrom.Append(" AND sc.interesse  = true");
                            }
                            break;
                        case EColecaoStatus.DisponivelNegocio:
                            {
                                sqlFrom.Append(" AND sc.disponivel_negocio = true");
                            }
                            break;
                        default:
                            break;
                    }
                }

                if (filtro.MarcaAcervoId > 0)
                {
                    if (filtro.MarcaAcervoId != 1 || !filtro.ExibirGeral)
                    {
                        sqlFrom.Append(" AND m.marcaAcervoId = @MarcaAcervoId");
                        parameters.Add("@MarcaAcervoId", filtro.MarcaAcervoId);
                    }
                }

                if (filtro.MarcaFaseId > 0)
                {
                    if (filtro.MarcaAcervoId != 1)
                    {
                        sqlFrom.Append(" AND m.marcafaseAcervoId = @MarcaFaseAcervoId");
                        parameters.Add("@MarcaFaseAcervoId", filtro.MarcaFaseId);
                    }
                    else
                    {
                        sqlFrom.Append(" AND m.marcaFaseId = @MarcaFaseId");
                        parameters.Add("@MarcaFaseId", filtro.MarcaFaseId);
                    }
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
                    bool termoCurto = rawSearch.Length < 3; // ← detecta termos que o FULLTEXT ignora

                    sqlFrom.Append(" AND (");

                    if (!termoCurto)
                    {
                        // FULLTEXT só para termos com 3+ caracteres
                        sqlFrom.Append(@"
                            MATCH(m.Nome, m.Descricao, m.CodigoAceca)
                            AGAINST(@Search IN BOOLEAN MODE)
                            OR ");
                    }

                    // LIKE sempre cobre CodigoAceca e Nome
                    sqlFrom.Append(@"
                        m.CodigoAceca LIKE @SearchLike
                        OR m.Nome LIKE @SearchLike
                        ");

                    // Descrição só se checkbox ativo
                    if (incluirDescricao)
                    {
                        sqlFrom.Append(@" OR m.Descricao LIKE @SearchLike ");
                    }

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
                        sc.id AS Id,
                        m.id AS IdMarca,
                        sc.id AS IdSocio,
                        ma.id AS IdMarcaAcervo,
                        mf.id AS IdMarcaFase,

                        sc.possui, 
                        sc.interesse,  
                        sc.disponivel_negocio,  
                        s.nome AS NomeSocio,
                        m.CodigoAceca,
                        m.Nome AS NomeMarca,
                        
                        ma.Descricao AS NomeAcervo,
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
                            CONCAT(@ImgBase,'/detalhes/',m.ImgDetalhe),
                            @ImgDefault) AS ImgDetalheFull,

                        sc.observacao

                    {sqlFrom}

                    ORDER BY mf.id, m.nome, m.CodigoAceca
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

        #endregion

        [HttpPost]
        public async Task<IActionResult> ActionColecao(int itemColecaoId, int marcaId, int actionId, int socioId, bool isPerfil, string itemColecaoObs)
        {
            try
            {
                if (actionId < 0)
                    return BadRequest("ActionId inválido");

                IActionResult response = Ok();

                bool bDispoivelNegocio = false;
                bool bInteresse = false;

                switch ((EColecaoAcao)actionId)
                {
                    case EColecaoAcao.ColecaoDelete:
                        response = await RemoverItemAsync(itemColecaoId);
                        break;
                    case EColecaoAcao.ColecaoIncluir:
                        response = await AdicionarOuAtualizarItemAsync(itemColecaoId, marcaId, socioId, bDispoivelNegocio, bInteresse, itemColecaoObs);
                        break;
                    case EColecaoAcao.ColecaoInteresse:
                        {
                            bInteresse = true;
                            response = await AdicionarOuAtualizarItemAsync(itemColecaoId, marcaId, socioId, bDispoivelNegocio, bInteresse, itemColecaoObs);
                        }
                        break;
                    case EColecaoAcao.ColecaoNegociar:
                        {
                            bDispoivelNegocio = true;
                            response = await AdicionarOuAtualizarItemAsync(itemColecaoId, marcaId, socioId, bDispoivelNegocio, bInteresse, itemColecaoObs);
                        }
                        break;
                    default:
                        break;
                }                

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: "
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
        public async Task<IActionResult> AdicionarOuAtualizarItemAsync(int itemColecaoId, int marcaId, int socioId, bool disponivelNegocio, bool interesse, string itemColecaoObs)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var model = await context.SocioColecao
                         .Include(x => x.Socio)
                         .AsNoTracking()
                         .Where(x =>
                            x.SocioId == socioId &&
                            x.MarcaId == marcaId)
                         .FirstOrDefaultAsync();

                    if (model == null)
                    {
                        model = new SocioColecao
                        {
                            SocioId = socioId,
                            MarcaId = marcaId,
                            Possui = true,
                            DisponivelNegocio = disponivelNegocio,
                            Interesse = interesse
                        };

                        context.SocioColecao.Add(model);
                    }
                    else
                    {
                        if (interesse && !model.Possui) {
                            var response = await RemoverItemAsync(itemColecaoId);
                        }
                        else
                        {
                            model.DisponivelNegocio = !model.Possui ? disponivelNegocio : !model.DisponivelNegocio;
                            model.Interesse = !model.Possui ? interesse : !model.Interesse;
                            model.Observacao = !string.IsNullOrWhiteSpace(itemColecaoObs) ? itemColecaoObs?.Trim() : null;
                            model.Possui = !model.Possui;
                            context.SocioColecao.Update(model);
                        }
                    }

                    await context.SaveChangesAsync();
                }

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: "
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
        public async Task<IActionResult> RemoverItemAsync(int id)
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

                using (var context = new AppDbContext())
                {

                    var model = await context.SocioColecao.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                    if (model == null)
                        return Ok(new
                        {
                            bResult = true,
                            type = "ERRO - ID nao localizado",
                            message = "ID nao localizado",
                            data = id
                        });

                    context.SocioColecao.Remove(model);
                    await context.SaveChangesAsync();
                }

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: "
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
    }
}