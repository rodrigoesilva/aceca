using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers.Admin.Socio
{
    public class NegociacaoController : Controller
    {
        #region variaveis

        private readonly ILogger<NegociacaoController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;
        private readonly HelperExtensionsController _helperController;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;
        //

        #endregion

        public NegociacaoController(ILogger<NegociacaoController> logger, AppDbContext db
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

        #region MeusNegocios

        public ActionResult Index()
        {
            return View("~/Views/Admin/Negociacao/NegociacaoMeusNegocios.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ListGrid([FromBody] FilterDataMarca request)
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
                        sqlFrom.Append(" AND sc.disponivel_negocio = true");
                        sqlFrom.Append(" AND sc.socioId = @SocioId");
                        parameters.Add("@SocioId", filtroColecao.SocioId);
                    }
                }

                if ((int)filtroColecao.ColecaoStatus > 0)
                {
                    switch (filtroColecao.ColecaoStatus)
                    {
                        case EColecaoStatus.Possui:
                            sqlFrom.Append(" AND sc.possui = true");
                            break;
                        case EColecaoStatus.Interesse:
                            sqlFrom.Append(" AND sc.interesse = true");
                            break;
                        case EColecaoStatus.DisponivelNegocio:
                            sqlFrom.Append(" AND sc.disponivel_negocio = true");
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
                            CONCAT(@ImgBase,'/detalhes/',m.ImgDetalhe),
                            @ImgDefault) AS ImgDetalheFull

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

        #region PorSocio

        public ActionResult PorSocio()
        {
            return View("~/Views/Admin/Negociacao/NegociacaoSocio.cshtml");
        }

        // Paginação no servidor (Dapper + LIMIT/OFFSET + GROUP BY) — antes trazia TODAS as
        // linhas de socios x socio_colecao x socio_contato pro C# só para agrupar e contar
        // (um sócio com 500 itens na coleção gerava 500 linhas pra virar 1 no resultado final).
        // Também corrige o agrupamento: era feito por Nome (dois sócios com nome igual eram
        // fundidos em um só resultado); agora agrupa por s.id, que é o identificador real.
        [HttpPost]
        public async Task<IActionResult> FiltrarDados_PorSocio([FromBody] Models.FilterDataGridSimples request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request inválido");

                var sqlFrom = new StringBuilder(@"
                FROM socios s
                INNER JOIN socio_contato sc ON s.id = sc.SocioId
                INNER JOIN socio_colecao col ON s.id = col.SocioId
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
                            OR sc.email LIKE @SearchLike
                        )
                    ");
                    parameters.Add("@SearchLike", $"%{request.Search.Value.Trim()}%");
                }

                var totalSql = @"
                    SELECT COUNT(*) FROM (
                        SELECT s.id
                        FROM socios s
                        INNER JOIN socio_contato sc ON s.id = sc.SocioId
                        INNER JOIN socio_colecao col ON s.id = col.SocioId
                        GROUP BY s.id
                    ) t
                    ";

                var filteredSql = $@"
                    SELECT COUNT(*) FROM (
                        SELECT s.id
                        {sqlFrom}
                        GROUP BY s.id
                    ) t
                    ";

                var dataSql = $@"
                    SELECT
                        s.id AS socioId,
                        s.nome AS socioNome,
                        sc.ddi AS socioDDI,
                        sc.ddd AS socioDDD,
                        sc.telefone AS socioTelefone,
                        sc.email AS socioEmail,
                        COUNT(CASE WHEN col.possui = 1 THEN 1 END) AS quantidadePossui

                    {sqlFrom}

                    GROUP BY s.id, s.nome, sc.ddi, sc.ddd, sc.telefone, sc.email
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
                _logger.LogError(ex, "Erro FiltrarDados_PorSocio");

                return BadRequest(new { error = true, message = ex.Message });
            }
        }

        #endregion

        #region PorAcervo

        public ActionResult PorAcervo()
        {
            return View("~/Views/Admin/Negociacao/NegociacaoAcervo.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ListGrid_PorAcervo([FromBody] FilterDataMarca request)
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
                            CONCAT(@ImgBase,'/detalhes/',m.ImgDetalhe),
                            @ImgDefault) AS ImgDetalheFull

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

        #region Socio Derivacao
        public async Task<IActionResult> Create_SocioSeguranca(VMSocio model)
        {
            try
            {
                var newModel = new SocioSeguranca();

                string strTempPass = _helperController.GenerateStringPassword(8);

                    newModel = new SocioSeguranca
                    {
                        SocioId = (int)model?.Id,
                        Email = model?.Email?.Trim()?.ToLower(),
                        Senha = _helperController.GenerateHashPassword(strTempPass),
                        SenhaAberta = strTempPass,
                        SenhaAtualizada = false,
                        NomeUsuario = model?.Nome,
                        UltimoLogin = DateTime.UtcNow.AddHours(-3),
                        Token = null,
                        ResetPasswordToken = null,
                        ResetPasswordTokenExpiry = null,
                    };

                _db.SocioSeguranca.Add(newModel);
                await _db.SaveChangesAsync();

                var newSocioContatoId = newModel?.Id;

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = newModel,
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
        public async Task<IActionResult> Create_SocioContato(VMSocio model)
        {
            try
            {
                if (!_helperController.IsValidEmailUsingMailAddress(model?.Email?.Trim()?.ToLower()))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Formato de Email Inválido"
                    });

                var newModel = new SocioContato
                {
                    SocioId = model.Id,
                    DDI =  model.DDI > 0 ? model.DDI : 55,
                    DDD =  !string.IsNullOrEmpty(model.Telefone) ? Convert.ToInt16(model.Telefone.Split(")")[0].Replace("(", string.Empty)) : null,
                    Telefone = !string.IsNullOrEmpty(model.Telefone) ? Convert.ToInt32(model.Telefone.Split(")")[1].Replace("-", string.Empty)) : null,
                    Email = model?.Email?.Trim()?.ToLower(),
                };

                _db.SocioContato.Add(newModel);
                _db.SaveChanges();

                var newSocioContatoId = newModel?.Id;

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
        public async Task<IActionResult> Create_SocioEndereco(VMSocio model)
        {
            try
            {
                var newModel = new SocioEndereco
                {
                    SocioId = model.Id,
                    Endereco = model.Endereco,
                    Numero = model.Numero,
                    Complemento = model.Complemento,
                    Bairro = model.Bairro,
                    Cidade = model.Cidade,
                    Estado = model.Estado,
                    CEP = !string.IsNullOrEmpty(model.CEP) ? model.CEP.Replace("-", string.Empty) : string.Empty,
                };

                _db.SocioEndereco.Add(newModel);
                _db.SaveChanges();

                var newSocioEnderecoId = newModel?.Id;

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
        public async Task<IActionResult> Create_SocioAniversario(VMSocio model)
        {
            try
            {
                var newModel = new SocioAniversario
                {
                    SocioId = model.Id,
                    Dia = !string.IsNullOrEmpty(model.DataAniversario) ? Convert.ToInt32(model.DataAniversario.Split("/")[0]) : null,
                    Mes = !string.IsNullOrEmpty(model.DataAniversario) ? Convert.ToInt32(model.DataAniversario.Split("/")[1]) : null,
                };

                _db.SocioAniversario.Add(newModel);
                _db.SaveChanges();

                var newSocioAniversarioId = newModel?.Id;

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

        [HttpPost]
        public async Task<IActionResult> ActionNegociacao(int socioNegociacaoId, int quantidadePossui, int socioLogadoId, int actionId, bool isPerfil)
        {
            try
            {
                if (actionId < 0)
                    return BadRequest("ActionId inválido");

                IActionResult response = Ok();

                bool bDispoivelNegocio = false;
                bool bInteresse = false;

                switch ((ENegociacaoAcao)actionId)
                {
                    case ENegociacaoAcao.NegociacaoMeusNegocios:
                        //response = await RemoverItemAsync(itemColecaoId);
                        break;
                    case ENegociacaoAcao.NegociacaoSocio:
                        //response = await AdicionarOuAtualizarItemAsync(itemColecaoId, marcaId, socioId, bDispoivelNegocio, bInteresse, itemColecaoObs);
                        break;
                    case ENegociacaoAcao.NegociacaoAcervo:
                        {
                            bInteresse = true;
                            //response = await AdicionarOuAtualizarItemAsync(itemColecaoId, marcaId, socioId, bDispoivelNegocio, bInteresse, itemColecaoObs);
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
    }
}
