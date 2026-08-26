using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OfficeOpenXml;
using System.Data;
using System.Reflection;
using System.Security.Claims;
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

        // O socioId enviado pelo cliente (hidden field / body do POST) não é confiável -
        // sempre derivar o dono real da coleção a partir da claim de autenticação.
        private int GetSocioIdAutenticado()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(claim, out var socioId) ? socioId : 0;
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

                // Esta tela é "Minha Coleção" - o socioId do request nunca é confiável
                // (hidden field editável no client); sempre usa o sócio autenticado.
                var socioIdAutenticado = GetSocioIdAutenticado();

                if (socioIdAutenticado <= 0)
                    return BadRequest("Sessão inválida");

                sqlFrom.Append(" AND sc.socioId = @SocioId");
                parameters.Add("@SocioId", socioIdAutenticado);

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
                // ORDENAÇÃO
                // =========================

                // Mapeia o índice de coluna enviado pelo DataTables (client) para a
                // coluna SQL correspondente. Mantém alinhado com admin-socio-colecao.js::columns.
                var colunasOrdenaveis = new Dictionary<int, string>
                {
                    { 2, "m.CodigoAceca" },
                    { 3, "m.Nome" },
                    { 6, "m.Descricao" },
                    { 7, "COALESCE(mfa.Nome, m.fabrica_txt)" },
                    { 8, "mst.Descricao" },
                    { 9, "mf.Descricao" },
                    { 10, "sc.observacao" },
                };

                var orderByPartes = new List<string>();

                if (request.Order != null)
                {
                    foreach (var ordem in request.Order)
                    {
                        if (colunasOrdenaveis.TryGetValue(ordem.Column, out var coluna))
                        {
                            var direcao = string.Equals(ordem.Dir, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                            orderByPartes.Add($"{coluna} {direcao}");
                        }
                    }
                }

                if (orderByPartes.Count == 0)
                {
                    orderByPartes.Add("m.Nome ASC");
                }

                var orderBySql = string.Join(", ", orderByPartes);

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
                        sc.socioId AS IdSocio,
                        ma.id AS IdMarcaAcervo,
                        mf.id AS IdMarcaFase,

                        sc.possui, 
                        sc.interesse,  
                        sc.disponivel_negocio,  
                        s.nome AS NomeSocio,

                         -- m.codigoAcecaNew,
                        CASE
                            WHEN m.codigoAcecaNew IS NOT NULL
                            THEN CONCAT(m.codigoAcecaNew, '/', m.CodigoAceca)
                            ELSE m.CodigoAceca
                        END AS CodigoAceca,

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
                            CONCAT(@ImgBase,'/',m.MarcaFaseId,'/detalhes/',m.ImgDetalhe),
                            @ImgDefault) AS ImgDetalheFull,

                        sc.observacao

                    {sqlFrom}

                    ORDER BY {orderBySql}
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActionColecao(int itemColecaoId, int marcaId, string actionId, int socioId, bool isPerfil, string itemColecaoObs, bool disponivelNegocio = false)
        {
            try
            {
                if (!Enum.TryParse<EColecaoAcao>(actionId, ignoreCase: true, out var acao))
                    return BadRequest("ActionId inválido");

                // socioId do parâmetro vem do cliente e não é confiável (IDOR) - o dono da
                // ação é sempre o sócio autenticado, nunca um valor informado pelo request.
                var socioIdAutenticado = GetSocioIdAutenticado();

                if (socioIdAutenticado <= 0)
                    return BadRequest("Sessão inválida");

                IActionResult response;

                switch (acao)
                {
                    case EColecaoAcao.ColecaoDelete:
                        response = await RemoverItemAsync(itemColecaoId, socioIdAutenticado);
                        break;
                    case EColecaoAcao.ColecaoIncluir:
                    case EColecaoAcao.ColecaoInteresse:
                    case EColecaoAcao.ColecaoNegociar:
                    case EColecaoAcao.ColecaoObs:
                        response = await AdicionarOuAtualizarItemAsync(marcaId, socioIdAutenticado, acao, disponivelNegocio, itemColecaoObs);
                        break;
                    default:
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "ActionId inválido"
                        });
                }

                // Propaga o resultado real (sucesso ou erro) das rotinas internas -
                // antes o retorno era sempre bResult:true, mesmo quando a operação
                // falhava (exceção) ou era bloqueada (ex.: IDOR no delete).
                return response;
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

        /// <summary>
        /// Cada ação altera exclusivamente o seu próprio flag (Possui / Interesse / DisponivelNegocio).
        /// ColecaoObs apenas atualiza Observação/DisponivelNegocio (edição do modal), sem alternar
        /// nenhum outro estado da coleção.
        /// </summary>
        private async Task<IActionResult> AdicionarOuAtualizarItemAsync(int marcaId, int socioId, EColecaoAcao acao, bool disponivelNegocio, string itemColecaoObs)
        {
            try
            {
                var model = await _db.SocioColecao
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
                        Possui = acao == EColecaoAcao.ColecaoIncluir,
                        Interesse = acao == EColecaoAcao.ColecaoInteresse,
                        DisponivelNegocio = acao == EColecaoAcao.ColecaoNegociar
                            || (acao == EColecaoAcao.ColecaoObs && disponivelNegocio),
                        Observacao = !string.IsNullOrWhiteSpace(itemColecaoObs) ? itemColecaoObs.Trim() : null
                    };

                    _db.SocioColecao.Add(model);
                }
                else
                {
                    switch (acao)
                    {
                        case EColecaoAcao.ColecaoIncluir:
                            model.Possui = !model.Possui;

                            // Ao incluir na coleção, o item deixa de estar como "Interesse".
                            if (model.Possui)
                                model.Interesse = false;
                            break;
                        case EColecaoAcao.ColecaoInteresse:
                            // Item já incluído na coleção - não faz sentido marcar Interesse
                            // nele. Bloqueia antes de tocar no registro.
                            if (model.Possui)
                            {
                                return BadRequest(new
                                {
                                    bResult = false,
                                    type = "JA_POSSUI",
                                    message = "Item já incluído na coleção."
                                });
                            }

                            model.Interesse = !model.Interesse;
                            break;
                        case EColecaoAcao.ColecaoNegociar:
                            model.DisponivelNegocio = !model.DisponivelNegocio;
                            break;
                        case EColecaoAcao.ColecaoObs:
                            model.DisponivelNegocio = disponivelNegocio;
                            break;
                    }

                    if (!string.IsNullOrWhiteSpace(itemColecaoObs))
                        model.Observacao = itemColecaoObs.Trim();

                    _db.SocioColecao.Update(model);
                }

                await _db.SaveChangesAsync();

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
        private async Task<IActionResult> RemoverItemAsync(int id, int socioIdAutenticado)
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

                if (model.SocioId != socioIdAutenticado)
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Item não pertence ao sócio autenticado"
                    });

                _db.SocioColecao.Remove(model);
                await _db.SaveChangesAsync();

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

        /// <summary>
        /// Valida a planilha Excel (.xls/.xlsx) usada na importação em massa da Coleção.
        /// Só confere a estrutura do cabeçalho (A1/B1/C1) - a extensão do próprio arquivo
        /// só é confiável quando validada aqui no servidor (a checagem no front é só uma
        /// conveniência de UX, nunca a garantia de segurança).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administracao")]
        public async Task<IActionResult> ValidarPlanilhaColecao(IFormFile arquivo)
        {
            try
            {
                var (planilha, aba, mensagemErro) = await AbrirEValidarPlanilhaColecao(arquivo);

                using (planilha)
                {
                    if (mensagemErro != null)
                        return BadRequest(new { bResult = false, type = "ERRO", message = mensagemErro });

                    var totalLinhas = Math.Max(0, (aba!.Dimension?.End.Row ?? 1) - 1);

                    return Ok(new { bResult = true, type = "OK", message = "Arquivo validado com sucesso", data = new { totalLinhas } });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(ValidarPlanilhaColecao));

                return BadRequest(new { bResult = false, type = "ERRO", message = "Não foi possível ler o arquivo enviado <br><br> Verifique se é uma planilha Excel válida" });
            }
        }

        /// <summary>
        /// Abre o arquivo enviado e confere formato (.xls/.xlsx) + estrutura do cabeçalho
        /// (A1/B1/C1 = TenhoNaColecao/TenhoInteresse/CodigoACECA) - usado tanto por
        /// ValidarPlanilhaColecao (só valida) quanto por CarregarPlanilhaColecao (valida de novo
        /// antes de processar, nunca confiando apenas na validação já feita no front).
        /// Retorna MensagemErro nulo quando tudo está OK; Planilha vem null nesse caso e deve ser
        /// descartado com "using" mesmo assim (using aceita null sem erro).
        /// </summary>
        private async Task<(ExcelPackage? Planilha, ExcelWorksheet? Aba, string? MensagemErro)> AbrirEValidarPlanilhaColecao(IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                return (null, null, "Nenhum arquivo enviado");

            var extensao = Path.GetExtension(arquivo.FileName)?.ToLowerInvariant();
            var extensoesValidas = new[] { ".xls", ".xlsx" };

            if (string.IsNullOrEmpty(extensao) || !extensoesValidas.Contains(extensao))
                return (null, null, "O arquivo deve estar no formato Excel (.xls ou .xlsx)");

            var stream = new MemoryStream();
            await arquivo.CopyToAsync(stream);
            stream.Position = 0;

            var planilha = new ExcelPackage(stream);
            var aba = planilha.Workbook.Worksheets.FirstOrDefault();

            if (aba == null)
            {
                planilha.Dispose();
                return (null, null, "A planilha está vazia");
            }

            var colunaA = (aba.Cells[1, 1].Text ?? string.Empty).Trim();
            var colunaB = (aba.Cells[1, 2].Text ?? string.Empty).Trim();
            var colunaC = (aba.Cells[1, 3].Text ?? string.Empty).Trim();

            var cabecalhoValido =
                string.Equals(colunaA, "TenhoNaColecao", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(colunaB, "TenhoInteresse", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(colunaC, "CodigoACECA", StringComparison.OrdinalIgnoreCase);

            if (!cabecalhoValido)
            {
                planilha.Dispose();
                return (null, null, "O cabeçalho da planilha é inválido. <br><br> As células A1, B1 e C1 devem conter," +
                                     "respectivamente: <br><br> \"TenhoNaColecao\",<br> \"TenhoInteresse\"<br> \"CodigoACECA\".");
            }

            return (planilha, aba, null);
        }

        /// <summary>
        /// Processa de fato a planilha validada por ValidarPlanilhaColecao/AbrirEValidarPlanilhaColecao:
        /// coluna A (TenhoNaColecao) marcada com "S"/"Sim" inclui o item na coleção (Possui=true);
        /// coluna B (TenhoInteresse) marca como interesse (Interesse=true). A coluna C (CodigoACECA)
        /// é comparada contra Marcas.CodigoAceca/CodigoAcecaNew para achar o MarcaId correspondente -
        /// códigos não encontrados são apenas contados e reportados, sem interromper o restante do
        /// processamento.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administracao")]
        public async Task<IActionResult> CarregarPlanilhaColecao(IFormFile arquivo)
        {
            try
            {
                var socioIdAutenticado = GetSocioIdAutenticado();

                if (socioIdAutenticado <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var (planilha, aba, mensagemErro) = await AbrirEValidarPlanilhaColecao(arquivo);

                using (planilha)
                {
                    if (mensagemErro != null)
                        return BadRequest(new { bResult = false, type = "ERRO", message = mensagemErro });

                    var ultimaLinha = aba!.Dimension?.End.Row ?? 1;

                    if (ultimaLinha < 2)
                        return BadRequest(new { bResult = false, type = "ERRO", message = "A planilha não possui nenhuma linha de dados" });

                    // Marcas.CodigoAceca/CodigoAcecaNew/Descricao -> Id, carregado uma única vez
                    // (evita 1 query por linha da planilha).
                    var marcas = await _db.Marca
                        .Where(m => m.CodigoAceca != null || m.CodigoAcecaNew != null)
                        .Select(m => new { m.Id, m.CodigoAceca, m.CodigoAcecaNew, m.Descricao })
                        .ToListAsync();

                    var marcaIdPorCodigo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    foreach (var m in marcas)
                    {
                        if (!string.IsNullOrWhiteSpace(m.CodigoAceca))
                            marcaIdPorCodigo.TryAdd(m.CodigoAceca.Trim(), m.Id!.Value);

                        if (!string.IsNullOrWhiteSpace(m.CodigoAcecaNew))
                            marcaIdPorCodigo.TryAdd(m.CodigoAcecaNew.Trim(), m.Id!.Value);
                    }

                    // Código da planilha não encontrado em CodigoAceca/CodigoAcecaNew - último
                    // recurso antes de desistir da linha: o código pode ter mudado e continuar
                    // citado dentro da Descricao da marca (texto livre, por isso é feito em
                    // memória em vez de um LIKE '%...%' por linha no banco).
                    int? BuscarMarcaIdPorDescricao(string codigo)
                    {
                        foreach (var m in marcas)
                        {
                            if (!string.IsNullOrWhiteSpace(m.Descricao) &&
                                m.Descricao.Contains(codigo, StringComparison.OrdinalIgnoreCase))
                                return m.Id;
                        }

                        return null;
                    }

                    // Coleção já existente do sócio, carregada uma única vez (evita 1 query por linha
                    // da planilha) - reimportar a mesma planilha NUNCA sobrescreve um item já
                    // existente, só inclui o que ainda não está na coleção.
                    var colecaoExistente = await _db.SocioColecao
                        .Where(sc => sc.SocioId == socioIdAutenticado)
                        .ToDictionaryAsync(sc => sc.MarcaId!.Value);

                    var qtdPossui = 0;
                    var qtdInteresse = 0;
                    var qtdJaExistente = 0;
                    var qtdNaoEncontrado = 0;
                    var qtdCodigoMudou = 0;

                    bool AplicarItem(int marcaId, bool possui, bool interesse)
                    {
                        if (colecaoExistente.ContainsKey(marcaId))
                            return false;

                        var novo = new SocioColecao
                        {
                            SocioId = socioIdAutenticado,
                            MarcaId = marcaId,
                            Possui = possui,
                            Interesse = interesse,
                            DisponivelNegocio = false,
                            Observacao = null,
                            Ativo = true
                        };

                        _db.SocioColecao.Add(novo);
                        colecaoExistente[marcaId] = novo;

                        return true;
                    }

                    static bool CelulaMarcada(string valor) =>
                        string.Equals(valor, "S", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(valor, "Sim", StringComparison.OrdinalIgnoreCase);

                    for (var linha = 2; linha <= ultimaLinha; linha++)
                    {
                        var colA = (aba.Cells[linha, 1].Text ?? string.Empty).Trim();
                        var colB = (aba.Cells[linha, 2].Text ?? string.Empty).Trim();
                        var colC = (aba.Cells[linha, 3].Text ?? string.Empty).Trim();

                        if (string.IsNullOrEmpty(colC))
                            continue;

                        var tenhoNaColecao = CelulaMarcada(colA);
                        var tenhoInteresse = CelulaMarcada(colB);

                        if (!tenhoNaColecao && !tenhoInteresse)
                            continue;

                        if (!marcaIdPorCodigo.TryGetValue(colC, out var marcaId))
                        {
                            var marcaIdPorDescricao = BuscarMarcaIdPorDescricao(colC);

                            if (marcaIdPorDescricao == null)
                            {
                                qtdNaoEncontrado++;
                                continue;
                            }

                            marcaId = marcaIdPorDescricao.Value;
                            qtdCodigoMudou++;
                        }

                        // Uma linha marcada nas duas colunas conta só como "TenhoNaColecao" - é o
                        // estado mais forte, e evita que a segunda checagem veja o item recém
                        // incluído pela primeira e o conte erroneamente como "já existente".
                        if (tenhoNaColecao)
                        {
                            if (AplicarItem(marcaId, possui: true, interesse: false))
                                qtdPossui++;
                            else
                                qtdJaExistente++;
                        }
                        else if (tenhoInteresse)
                        {
                            if (AplicarItem(marcaId, possui: false, interesse: true))
                                qtdInteresse++;
                            else
                                qtdJaExistente++;
                        }
                    }

                    var strategy = _db.Database.CreateExecutionStrategy();

                    Func<Task<IActionResult>> operation = async () =>
                    {
                        using var transaction = await _db.Database.BeginTransactionAsync();

                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Ok(new
                        {
                            bResult = true,
                            type = "OK",
                            message = "Planilha de coleção carregada com sucesso",
                            data = new
                            {
                                totalLinhas = ultimaLinha - 1,
                                qtdPossui,
                                qtdInteresse,
                                qtdJaExistente,
                                qtdNaoEncontrado,
                                qtdCodigoMudou
                            }
                        });
                    };

                    return await strategy.ExecuteAsync(operation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(CarregarPlanilhaColecao));

                return BadRequest(new { bResult = false, type = "ERRO", message = "Não foi possível processar o arquivo enviado" });
            }
        }

        /// <summary>
        /// Ação em massa (checkboxes marcados na grid): marca os itens selecionados como
        /// "possuo" (Possui=true, Interesse=false, Ativo=true). Só afeta linhas que já
        /// pertencem ao sócio autenticado - os ids vêm do cliente, então nunca são confiáveis
        /// sozinhos (IDOR).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ColecaoIncluirTodos([FromBody] ColecaoSelecionadosRequest request)
        {
            try
            {
                var socioIdAutenticado = GetSocioIdAutenticado();

                if (socioIdAutenticado <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var ids = request?.Ids ?? new List<int>();

                if (ids.Count == 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Nenhum item selecionado" });

                var itens = await _db.SocioColecao
                    .Where(sc => sc.SocioId == socioIdAutenticado && ids.Contains(sc.Id!.Value))
                    .ToListAsync();

                foreach (var item in itens)
                {
                    item.Possui = true;
                    item.Interesse = false;
                    item.Ativo = true;
                }

                var strategy = _db.Database.CreateExecutionStrategy();

                Func<Task<IActionResult>> operation = async () =>
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        bResult = true,
                        type = "OK",
                        message = "Itens incluídos na coleção com sucesso",
                        data = new { qtdAtualizado = itens.Count }
                    });
                };

                return await strategy.ExecuteAsync(operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(ColecaoIncluirTodos));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Ação em massa (checkboxes marcados na grid): remove definitivamente os itens
        /// selecionados de socio_colecao. Só afeta linhas que já pertencem ao sócio
        /// autenticado (mesma proteção de ColecaoIncluirTodos). A confirmação ("essa ação não
        /// pode ser desfeita") é feita no front antes de chamar este endpoint.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ColecaoRemoverTodos([FromBody] ColecaoSelecionadosRequest request)
        {
            try
            {
                var socioIdAutenticado = GetSocioIdAutenticado();

                if (socioIdAutenticado <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var ids = request?.Ids ?? new List<int>();

                if (ids.Count == 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Nenhum item selecionado" });

                var itens = await _db.SocioColecao
                    .Where(sc => sc.SocioId == socioIdAutenticado && ids.Contains(sc.Id!.Value))
                    .ToListAsync();

                _db.SocioColecao.RemoveRange(itens);

                var strategy = _db.Database.CreateExecutionStrategy();

                Func<Task<IActionResult>> operation = async () =>
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        bResult = true,
                        type = "OK",
                        message = "Itens removidos da coleção com sucesso",
                        data = new { qtdRemovido = itens.Count }
                    });
                };

                return await strategy.ExecuteAsync(operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(ColecaoRemoverTodos));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }
    }
}