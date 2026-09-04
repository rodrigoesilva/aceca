using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers.Acervo
{
    [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
    public class AcervoController : Controller
    {
        #region variaveis

        private readonly ILogger<AcervoController> _logger;
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

        // O socioId autenticado é usado só para marcar quais itens já estão na
        // coleção do usuário logado (flags Possui/Interesse) - nunca vem do cliente.
        private int GetSocioIdAutenticado()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(claim, out var socioId) ? socioId : 0;
        }

        public AcervoController(ILogger<AcervoController> logger,
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

        public async Task<ActionResult> Index(int id)
        {
            var modelMarcas = new Marcas { MarcaAcervoId = id };

            return View("~/Views/Admin/Acervo/Listagem.cshtml", modelMarcas);
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

                var socioIdAutenticado = GetSocioIdAutenticado();

                var imgBase = _urlBaseImg;
                var imgDefault = $"{_urlBaseSite}/assets/img/img_inexistente.jpg";

                var sqlFrom = new StringBuilder(@"
                FROM marcas m
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
                LEFT JOIN socio_colecao sc ON sc.marcaId = m.id AND sc.socioId = @SocioIdAutenticado
                WHERE 1=1
                AND m.ativo = 1
                ");

                var parameters = new DynamicParameters();

                parameters.Add("@SocioIdAutenticado", socioIdAutenticado);

                // =========================
                // FILTROS
                // =========================

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
                // ORDENAÇÃO
                // =========================

                // Mapeia o índice de coluna enviado pelo DataTables (client) para a
                // coluna SQL correspondente. Mantém alinhado com admin-acervo-listagem.js::columns.
                var colunasOrdenaveis = new Dictionary<int, string>
                {
                    { 1, "m.CodigoAceca" },
                    { 2, "m.Nome" },
                    { 5, "m.Descricao" },
                    { 6, "COALESCE(mfa.Nome, m.fabrica_txt)" },
                    { 7, "mst.Descricao" },
                    { 8, "mfi.Descricao" },
                    { 9, "mf.Descricao" },
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
                        m.id AS Id,
                        ma.id AS IdMarcaAcervo,
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

                        COALESCE(sc.possui, 0) AS Possui,
                        COALESCE(sc.interesse, 0) AS Interesse

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

        #region CRUD JS

        // Restaurado (removido por engano na migração do cadastro pra CadastroController/
        // marcas_cadastro): edita um item JÁ aprovado/publicado direto em `marcas` - não
        // confundir com CadastroController.Edit, que opera em marcas_cadastro (submissão
        // ainda pendente de aprovação). Usado pelo botão "Editar" da grid de Acervo
        // (admin-acervo-listagem.js).
        [HttpPost]
        [Authorize(Roles = "Administracao")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Models.Marcas model)
        {
            try
            {
                if (ModelState.IsValid)
                {
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

                    model?.MarcaAcervoId = (model?.MarcaAcervoId < 0 || model?.MarcaAcervoId == null) ? 0 : model?.MarcaAcervoId;
                    model?.MarcaDimensaoId = (model?.MarcaDimensaoId < 0 || model?.MarcaDimensaoId == null) ? 0 : model?.MarcaDimensaoId;
                    model?.MarcaFabricaId = (model?.MarcaFabricaId < 0 || model?.MarcaFabricaId == null) ? 0 : model?.MarcaFabricaId;
                    model?.MarcaFaseId = (model?.MarcaFaseId < 0 || model?.MarcaFaseId == null) ? 0 : model?.MarcaFaseId;
                    model?.MarcaFinalidadeId = (model?.MarcaFinalidadeId < 0 || model?.MarcaFinalidadeId == null) ? 0 : model?.MarcaFinalidadeId;
                    model?.MarcaImpressoraId = (model?.MarcaImpressoraId < 0 || model?.MarcaImpressoraId == null) ? 0 : model?.MarcaImpressoraId;
                    model?.MarcaQualidadeImagemId = (model?.MarcaQualidadeImagemId < 0 || model?.MarcaQualidadeImagemId == null) ? 0 : model?.MarcaQualidadeImagemId;
                    model?.MarcaRaridadeId = (model?.MarcaRaridadeId < 0 || model?.MarcaRaridadeId == null) ? 0 : model?.MarcaRaridadeId;
                    model?.MarcaSubTipoId = (model?.MarcaSubTipoId < 0 || model?.MarcaSubTipoId == null) ? 5 : model?.MarcaSubTipoId;

                    model?.ImgPrincipal = Path.GetFileName(model?.ImgPrincipal);
                    model?.ImgDetalhe = Path.GetFileName(model?.ImgDetalhe);

                    _db.Entry(model).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

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

        #endregion

    }
}