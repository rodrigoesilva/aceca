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
using SkiaSharp;
using System.Globalization;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers.Cadastro
{
    [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
    public class CadastroController : Controller
    {
        #region variaveis

        private readonly ILogger<CadastroController> _logger;
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

        // Mesma regra aplicada no client (admin-acervo-cadastro.js :: fn_ValidarCodigoVariante):
        // letras/números no início e fim, podendo ter espaço, traço ou underline no meio.
        private static readonly Regex RegexNomeCodigoValido = new(@"^[\p{L}\p{N}]([\p{L}\p{N} _-]*[\p{L}\p{N}])?$", RegexOptions.Compiled);

        // Usado para montar nome/caminho de arquivo — bloqueia separadores de path e "..".
        private static readonly Regex RegexCodigoArquivoValido = new(@"^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

        private const long TamanhoMaximoImagemBytes = 2 * 1024 * 1024; // 2MB

        #endregion

        // O socioId autenticado é usado só para marcar quais itens já estão na
        // coleção do usuário logado (flags Possui/Interesse) - nunca vem do cliente.
        private int GetSocioIdAutenticado()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(claim, out var socioId) ? socioId : 0;
        }

        public CadastroController(ILogger<CadastroController> logger,
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

            return View("~/Views/Admin/Cadastro/CadastroAprovacao.cshtml", modelMarcas);
        }
        // Qualquer role da classe (Administracao, Fundador, MembroHonra, Socio) pode
        // enviar cadastro pra aprovação - antes era Administracao-only.
        public async Task<ActionResult> CadastroAcervo()
        {
            ViewBag.PercentualMarcaDaguaPadrao = await GetPercentualMarcaDaguaPadraoAsync();

            return View("~/Views/Admin/Cadastro/CadastroAcervo.cshtml");
        }

        #endregion

        #region Consulta LISTAGEM


        // Fila de aprovação (tela "Cadastro > Aprovação"). Administracao vê todas as
        // submissões; qualquer outro usuário vê só as que ele mesmo enviou (CriadoPorSocioId).
        // Como Aprovar remove a linha de marcas_cadastro (ver SetStatus), essa tabela só
        // guarda Pendente/Negado - não precisa filtrar status à parte pro caso "minhas".
        [HttpPost]
        public async Task<IActionResult> FiltrarDadosAprovacao([FromBody] FilterDataMarca request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request inválido");

                var filtro = request.Filtros ?? new FiltroRequestMarca();

                var socioIdAutenticado = GetSocioIdAutenticado();
                var bVeTodas = User.IsInRole("Administracao");

                var imgBase = _urlBaseImg;
                var imgDefault = $"{_urlBaseSite}/assets/img/img_inexistente.jpg";

                var sqlFrom = new StringBuilder(@"
                FROM marcas_cadastro m
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
                LEFT JOIN socios sCriou ON sCriou.id = m.criadoPorSocioId
                LEFT JOIN socios sAprovou ON sAprovou.id = m.aprovadoPorSocioId
                WHERE 1=1
                AND m.ativo = 1
                ");

                var parameters = new DynamicParameters();

                if (!bVeTodas)
                {
                    sqlFrom.Append(" AND m.criadoPorSocioId = @SocioIdAutenticado");
                    parameters.Add("@SocioIdAutenticado", socioIdAutenticado);
                }

                if (filtro.MarcaFaseId > 0)
                {
                    sqlFrom.Append(" AND m.marcaFaseId = @MarcaFaseId");
                    parameters.Add("@MarcaFaseId", filtro.MarcaFaseId);
                }

                if (filtro.StatusCadastro > 0)
                {
                    sqlFrom.Append(" AND m.statusCadastro = @StatusCadastroFiltro");
                    parameters.Add("@StatusCadastroFiltro", filtro.StatusCadastro);
                }

                if (filtro.MarcaSubTipoId > 0)
                {
                    sqlFrom.Append(" AND m.marcaSubTipoId = @MarcaSubTipoId");
                    parameters.Add("@MarcaSubTipoId", filtro.MarcaSubTipoId);
                }

                if (!string.IsNullOrWhiteSpace(filtro.NomeMarca))
                {
                    sqlFrom.Append(" AND m.Nome LIKE @Nome");
                    parameters.Add("@Nome", $"%{filtro.NomeMarca}%");
                }

                var colunasOrdenaveis = new Dictionary<int, string>
                {
                    { 1, "sCriou.Nome" },
                    { 2, "ma.Descricao" },
                    { 3, "mf.Descricao" },
                    { 6, "m.CodigoAceca" },
                    { 7, "m.Nome" },
                    { 8, "m.Descricao" },
                    { 9, "mt.Descricao" },
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
                    orderByPartes.Add("m.dataCriacao DESC");

                var orderBySql = string.Join(", ", orderByPartes);

                var totalSql = "SELECT COUNT(1) FROM marcas_cadastro WHERE ativo = 1" + (bVeTodas ? "" : " AND criadoPorSocioId = @SocioIdAutenticado");
                var filteredSql = "SELECT COUNT(1) " + sqlFrom;

                var dataSql = $@"
                    SELECT
                        m.id AS Id,
                        m.CodigoAceca,
                        m.CodigoAcecaNew,
                        m.codigoSC AS CodigoFabrica,
                        m.Nome AS NomeMarca,
                        ma.Descricao AS NomeAcervo,
                        mf.Descricao AS NomeFase,
                        mt.Descricao AS Tipo,
                        mst.Descricao AS SubTipo,
                        m.Descricao,
                        m.StatusCadastro,
                        m.Observacao,
                        sCriou.Nome AS CriadoPorNome,
                        m.criadoPorSocioId AS CriadoPorSocioId,
                        sAprovou.Nome AS AprovadoPorNome,
                        m.dataCriacao AS DataCriacao,

                        -- Ids/valores crus, usados só pra pré-preencher o formulário de edição
                        -- (ver CadastroAcervo.cshtml / admin-acervo-cadastro.js).
                        m.marcaAcervoId AS MarcaAcervoId,
                        m.marcaFaseId AS MarcaFaseId,
                        m.marcaFinalidadeId AS MarcaFinalidadeId,
                        m.marcaFabricaId AS MarcaFabricaId,
                        m.fabrica_txt AS TxtFabrica,
                        m.marcaDimensaoId AS MarcaDimensaoId,
                        mst.marcaTipoId AS MarcaTipoId,
                        m.marcaSubTipoId AS MarcaSubTipoId,
                        m.marcaImpressoraId AS MarcaImpressoraId,
                        m.impressora AS TxtImpressora,
                        m.marcaQualidadeImagemId AS MarcaQualidadeImagemId,
                        m.marcaRaridadeId AS MarcaRaridadeId,
                        m.Valor,
                        m.Valor1PI,
                        m.Valor2PI,
                        m.IncluidoPor,
                        m.incluidoPorSocioId AS IncluidoPorSocioId,
                        m.percentualMarcaDaguaPrincipal AS PercentualMarcaDaguaPrincipal,
                        m.percentualMarcaDaguaDetalhe AS PercentualMarcaDaguaDetalhe,

                        m.ImgPrincipal,
                        IF(m.ImgPrincipal IS NOT NULL,
                            CONCAT(@ImgBase,'/_pendente/',m.MarcaFaseId,'/',m.ImgPrincipal),
                            @ImgDefault) AS ImgPrincipalFull,
                        -- Fallback pra registros enviados antes da pasta de staging existir
                        -- (imagem foi parar direto na pasta ao vivo do Acervo) - o client
                        -- tenta ImgPrincipalFull primeiro e só usa esta se aquela der 404.
                        IF(m.ImgPrincipal IS NOT NULL,
                            CONCAT(@ImgBase,'/',m.MarcaFaseId,'/',m.ImgPrincipal),
                            @ImgDefault) AS ImgPrincipalFullLive,

                        m.ImgDetalhe,
                        IF(m.ImgDetalhe IS NOT NULL,
                            CONCAT(@ImgBase,'/_pendente/',m.MarcaFaseId,'/detalhes/',m.ImgDetalhe),
                            @ImgDefault) AS ImgDetalheFull,
                        IF(m.ImgDetalhe IS NOT NULL,
                            CONCAT(@ImgBase,'/',m.MarcaFaseId,'/detalhes/',m.ImgDetalhe),
                            @ImgDefault) AS ImgDetalheFullLive

                    {sqlFrom}

                        ORDER BY {orderBySql}
                    LIMIT @Limit OFFSET @Offset
                    ";

                parameters.Add("@ImgBase", imgBase);
                parameters.Add("@ImgDefault", imgDefault);
                parameters.Add("@Limit", request.Length);
                parameters.Add("@Offset", request.Start);

                using var conn = _db.Database.GetDbConnection();

                var totalParams = new DynamicParameters();
                if (!bVeTodas)
                    totalParams.Add("@SocioIdAutenticado", socioIdAutenticado);

                var total = await conn.ExecuteScalarAsync<int>(totalSql, totalParams);
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
                _logger.LogError(ex, "Erro FiltrarDadosAprovacao");

                return BadRequest(new { error = true, message = ex.Message });
            }
        }

        #endregion

        #region Aprovação

        public class VMSetStatusCadastro
        {
            public int Id { get; set; }
            public int Status { get; set; }
            public string Observacao { get; set; }
        }

        // Admin decide o destino de uma submissão. Aprovar promove pra `marcas` (mesmo
        // mapeamento de campos que o Create original fazia direto) e remove de
        // marcas_cadastro; Negar/Pendente só atualizam a linha in-place. Quem setou o
        // status (seja qual for) fica gravado em AprovadoPorSocioId.
        [HttpPost]
        [Authorize(Roles = "Administracao")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus([FromBody] VMSetStatusCadastro request)
        {
            try
            {
                if (request == null || request.Id < 1)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Id deve ser maior que 0" });

                if (!Enum.IsDefined(typeof(EStatusCadastro), request.Status))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Status inválido" });

                var statusNovo = (EStatusCadastro)request.Status;

                if (statusNovo == EStatusCadastro.Negado && string.IsNullOrWhiteSpace(request.Observacao))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Observação é obrigatória para negar um cadastro" });

                var model = await _db.MarcaCadastro.FirstOrDefaultAsync(x => x.Id == request.Id);

                if (model == null)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Cadastro não encontrado" });

                model.AprovadoPorSocioId = GetSocioIdAutenticado();
                model.StatusCadastro = (int)statusNovo;
                model.Observacao = statusNovo == EStatusCadastro.Negado ? request.Observacao.Trim() : null;

                if (statusNovo == EStatusCadastro.Aprovado)
                {
                    var marca = new Marcas
                    {
                        Ativo = true,

                        MarcaAcervoId = model.MarcaAcervoId,
                        MarcaDimensaoId = model.MarcaDimensaoId,
                        MarcaFabricaId = model.MarcaFabricaId,
                        MarcaFaseId = model.MarcaFaseId,
                        MarcaFaseAcervoId = model.MarcaFaseAcervoId,
                        MarcaFinalidadeId = model.MarcaFinalidadeId,
                        MarcaImpressoraId = model.MarcaImpressoraId,
                        MarcaQualidadeImagemId = model.MarcaQualidadeImagemId,
                        MarcaRaridadeId = model.MarcaRaridadeId,
                        MarcaSubTipoId = model.MarcaSubTipoId,

                        CodigoAceca = model.CodigoAceca,
                        CodigoAcecaNew = model.CodigoAcecaNew,
                        CodigoFabrica = model.CodigoFabrica,
                        ImgPrincipal = model.ImgPrincipal,
                        ImgDetalhe = model.ImgDetalhe,
                        Nome = model.Nome,
                        Descricao = model.Descricao,
                        Valor1PI = model.Valor1PI,
                        Valor2PI = model.Valor2PI,
                        Valor = model.Valor,
                        IncluidoPor = model.IncluidoPor,
                        IncluidoPorSocioId = model.IncluidoPorSocioId,
                        // EmQuarentena não existe em marcas_cadastro hoje - assume o
                        // default (0) igual ao Create original quando nada é informado.
                        EmQuarentena = 0,
                        ExibirGeral = true,

                        TxtFabrica = model.TxtFabrica,
                        TxtImpressora = model.TxtImpressora,
                    };

                    // A imagem só é publicada na pasta real do Acervo na aprovação - até
                    // aqui ela ficava numa pasta "_pendente" separada (ver UploadImg/Create),
                    // então ninguém via a imagem de um item ainda não aprovado.
                    MoverImagensPendenteParaAcervo(model.MarcaFaseId, model.ImgPrincipal, model.ImgDetalhe, model.PercentualMarcaDaguaPrincipal, model.PercentualMarcaDaguaDetalhe);

                    _db.Marca.Add(marca);
                    _db.MarcaCadastro.Remove(model);
                }

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = new { model.Id, StatusCadastro = model.StatusCadastro }
                });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";

                _logger.LogError(mensagemErro);

                return BadRequest(new { bResult = false, type = "ERRO", message = mensagemErro });
            }
        }

        // Move a imagem principal/detalhe da pasta de staging ("_pendente/{fase}") pra pasta
        // real do Acervo ("{fase}") - chamado só na aprovação (ver SetStatus). Aproveita a
        // movimentação pra já gravar a versão com marca d'água (a de staging fica limpa,
        // pois pode ser negada/reeditada). Lança exceção se a movimentação falhar, pra
        // SetStatus não seguir pra SaveChangesAsync e aprovar um item cuja imagem não foi
        // de fato publicada.
        private void MoverImagensPendenteParaAcervo(int? marcaFaseId, string imgPrincipal, string imgDetalhe, double? percentualPrincipal = null, double? percentualDetalhe = null)
        {
            if (string.IsNullOrEmpty(imgPrincipal) && string.IsNullOrEmpty(imgDetalhe))
                return;

            var opacidadePrincipal = (float)(Math.Clamp(percentualPrincipal ?? OpacidadeMarcaDaguaPadrao * 100, 0, 100) / 100.0);
            var opacidadeDetalhe = (float)(Math.Clamp(percentualDetalhe ?? OpacidadeMarcaDaguaPadrao * 100, 0, 100) / 100.0);

            if (_bIsLocalHost)
            {
                if (!string.IsNullOrEmpty(imgPrincipal))
                {
                    var origem = Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", "_pendente", marcaFaseId?.ToString(), imgPrincipal);
                    var destinoPasta = Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", marcaFaseId?.ToString());
                    Directory.CreateDirectory(destinoPasta);

                    var destino = Path.Combine(destinoPasta, imgPrincipal);
                    if (System.IO.File.Exists(origem))
                    {
                        var bytesComMarca = AplicarMarcaDagua(System.IO.File.ReadAllBytes(origem), Path.GetExtension(imgPrincipal), opacidadePrincipal);
                        System.IO.File.WriteAllBytes(destino, bytesComMarca);
                        System.IO.File.Delete(origem);
                    }
                }

                if (!string.IsNullOrEmpty(imgDetalhe))
                {
                    var origem = Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", "_pendente", marcaFaseId?.ToString(), "detalhes", imgDetalhe);
                    var destinoPasta = Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", marcaFaseId?.ToString(), "detalhes");
                    Directory.CreateDirectory(destinoPasta);

                    var destino = Path.Combine(destinoPasta, imgDetalhe);
                    if (System.IO.File.Exists(origem))
                    {
                        var bytesComMarca = AplicarMarcaDagua(System.IO.File.ReadAllBytes(origem), Path.GetExtension(imgDetalhe), opacidadeDetalhe);
                        System.IO.File.WriteAllBytes(destino, bytesComMarca);
                        System.IO.File.Delete(origem);
                    }
                }

                return;
            }

            using var ftpConn = new FtpClient(_ftpHost, _ftpUser, _ftpPass);
            ftpConn.Connect();

            try
            {
                if (!string.IsNullOrEmpty(imgPrincipal))
                {
                    var origem = $"{_ftpBaseUrl}/midia/geral/_pendente/{marcaFaseId}/{imgPrincipal}";
                    var destinoPasta = $"{_ftpBaseUrl}/midia/geral/{marcaFaseId}";
                    var destino = $"{destinoPasta}/{imgPrincipal}";

                    if (ftpConn.FileExists(origem))
                    {
                        if (!ftpConn.DirectoryExists(destinoPasta))
                            ftpConn.CreateDirectory(destinoPasta, true);

                        using (var msDownload = new MemoryStream())
                        {
                            ftpConn.DownloadStream(msDownload, origem);
                            var bytesComMarca = AplicarMarcaDagua(msDownload.ToArray(), Path.GetExtension(imgPrincipal), opacidadePrincipal);

                            using var msUpload = new MemoryStream(bytesComMarca);
                            ftpConn.UploadStream(msUpload, destino, FtpRemoteExists.Overwrite);
                        }

                        ftpConn.DeleteFile(origem);
                    }
                }

                if (!string.IsNullOrEmpty(imgDetalhe))
                {
                    var origem = $"{_ftpBaseUrl}/midia/geral/_pendente/{marcaFaseId}/detalhes/{imgDetalhe}";
                    var destinoPasta = $"{_ftpBaseUrl}/midia/geral/{marcaFaseId}/detalhes";
                    var destino = $"{destinoPasta}/{imgDetalhe}";

                    if (ftpConn.FileExists(origem))
                    {
                        if (!ftpConn.DirectoryExists(destinoPasta))
                            ftpConn.CreateDirectory(destinoPasta, true);

                        using (var msDownload = new MemoryStream())
                        {
                            ftpConn.DownloadStream(msDownload, origem);
                            var bytesComMarca = AplicarMarcaDagua(msDownload.ToArray(), Path.GetExtension(imgDetalhe), opacidadeDetalhe);

                            using var msUpload = new MemoryStream(bytesComMarca);
                            ftpConn.UploadStream(msUpload, destino, FtpRemoteExists.Overwrite);
                        }

                        ftpConn.DeleteFile(origem);
                    }
                }
            }
            finally
            {
                ftpConn.Disconnect();
            }
        }

        #endregion


        #region CRUD JS

        // Qualquer role da classe pode enviar cadastro pra aprovação (antes era
        // Administracao-only) - CriadoPorSocioId sempre é o autenticado, nunca vem do
        // cliente, então não há como um sócio se passar por outro aqui.
        [HttpPost]
        [ValidateAntiForgeryToken]
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

                // Administracao não passa pela fila de aprovação - vai direto pro Acervo
                // (marcas), igual ao comportamento antigo. Só quem não é Administracao
                // entra em marcas_cadastro aguardando aprovação.
                bool ehAdministracao = User.IsInRole("Administracao");

                if (string.IsNullOrEmpty(vmModel?.Nome))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Nome deve ser preenchido"
                    });

                if (!RegexNomeCodigoValido.IsMatch(vmModel.Nome.Trim()))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Caracter inválido no nome preenchido"
                    });

                if (string.IsNullOrWhiteSpace(vmModel?.CodigoAceca))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Código Aceca deve ser gerado antes de salvar"
                    });

                if (string.IsNullOrEmpty(vmModel?.Descricao))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Descricao deve ser preenchido"
                    });

                if (string.IsNullOrEmpty(vmModel?.IncluidoPor))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "IncluidoPor deve ser preenchido"
                    });

                // Defesa em profundidade contra duplicidade de CodigoAceca: GetNovoCodigoAceca
                // já evita gerar um código já reservado por outro cadastro pendente, mas dois
                // cadastros concorrentes podem ter pego o mesmo código antes de qualquer um
                // salvar - aqui é a última checagem antes de persistir de verdade.
                var codigoAcecaTrim = vmModel.CodigoAceca.Trim();
                var codigoAcecaNewTrim = vmModel.CodigoAcecaNew?.Trim();

                var codigoJaUsado = await _db.Marca.AsNoTracking()
                        .AnyAsync(x => x.CodigoAceca == codigoAcecaTrim || (codigoAcecaNewTrim != null && x.CodigoAcecaNew == codigoAcecaNewTrim))
                    || await _db.MarcaCadastro.AsNoTracking()
                        .AnyAsync(x => x.StatusCadastro == (int)EStatusCadastro.Pendente
                                     && (x.CodigoAceca == codigoAcecaTrim || (codigoAcecaNewTrim != null && x.CodigoAcecaNew == codigoAcecaNewTrim)));

                if (codigoJaUsado)
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Código Aceca já está em uso por outro cadastro (aprovado ou aguardando aprovação) - gere o código novamente"
                    });

                // Opacidade da marca d'água escolhida na tela pra cada imagem (Principal/
                // Detalhe) - se não vier preenchida, usa o padrão configurado em adm_config
                // (ver GetPercentualMarcaDaguaPadraoAsync). Resolvido aqui (nunca null daqui
                // pra frente) pra: (a) Administracao já aplicar com o valor certo no upload
                // direto pro Acervo; (b) ficar gravado em MarcaCadastro pra reaplicar na
                // aprovação com o mesmo valor escolhido no cadastro/edição.
                var percentualPadrao = await GetPercentualMarcaDaguaPadraoAsync();
                var percentualPrincipal = vmModel?.PercentualMarcaDaguaPrincipal ?? percentualPadrao;
                var percentualDetalhe = vmModel?.PercentualMarcaDaguaDetalhe ?? percentualPadrao;

                #region Upload Imagem

                #region Upload Imagem ImgPrincipal

                string strImgPrincipalSaveName = null;

                if (iFileImgPrincipal != null)
                {
                    if (!vmModel.ImgPrincipal.Equals("C:\\fakepath\\."))
                    {
                        var result = await UploadImg(vmModel, iFileImgPrincipal, true, bStaging: !ehAdministracao, percentual: percentualPrincipal);

                        var jObjResult = JObject.FromObject(((ObjectResult)result).Value);

                        strImgPrincipalSaveName = (string)jObjResult?["data"];

                        if (result.GetType() == typeof(NotFoundObjectResult) ||
                             result.GetType() == typeof(BadRequestObjectResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = (string)jObjResult?["message"],
                                data = strImgPrincipalSaveName
                            });
                    }
                }

                vmModel?.ImgPrincipal = strImgPrincipalSaveName;

                #endregion

                #region Upload Imagem ImgDetalhe

                string strImgDetalheSaveName = null;

                if (iFileImgDetalhe != null)
                {
                    if(!vmModel.ImgDetalhe.Equals("C:\\fakepath\\.")){
                        var result = await UploadImg(vmModel, iFileImgDetalhe, false, bStaging: !ehAdministracao, percentual: percentualDetalhe);

                        var jObjResult = JObject.FromObject(((ObjectResult)result).Value);

                        strImgDetalheSaveName = (string)jObjResult?["data"];

                        if (result.GetType() == typeof(NotFoundObjectResult) ||
                             result.GetType() == typeof(BadRequestObjectResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = (string)jObjResult?["message"],
                                data = strImgDetalheSaveName
                            });
                    }
                }

                vmModel?.ImgDetalhe = strImgDetalheSaveName;

                #endregion

                #endregion

                #region obj Marca / MarcaCadastro

                // 1. Convert to Title Case
                TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;

                int? novoId;

                if (ehAdministracao)
                {
                    // Administracao não passa por aprovação - vai direto pro Acervo (marcas),
                    // igual ao comportamento anterior à etapa de aprovação.
                    var marca = new Marcas
                    {
                        Ativo = true,

                        MarcaAcervoId = (vmModel?.MarcaAcervoId < 0 || vmModel?.MarcaAcervoId == null) ? null : vmModel?.MarcaAcervoId,
                        MarcaDimensaoId = (vmModel?.MarcaDimensaoId < 0 || vmModel?.MarcaDimensaoId == null) ? null : vmModel?.MarcaDimensaoId,
                        MarcaFabricaId = (vmModel?.MarcaFabricaId < 0 || vmModel?.MarcaFabricaId == null) ? null : vmModel?.MarcaFabricaId,
                        MarcaFaseId = (vmModel?.MarcaFaseId < 0 || vmModel?.MarcaFaseId == null) ? null : vmModel?.MarcaFaseId,
                        MarcaFaseAcervoId = (vmModel?.MarcaFaseId < 0 || vmModel?.MarcaFaseId == null) ? null : vmModel?.MarcaFaseId,
                        MarcaFinalidadeId = (vmModel?.MarcaFinalidadeId < 0 || vmModel?.MarcaFinalidadeId == null) ? null : vmModel?.MarcaFinalidadeId,
                        MarcaImpressoraId = (vmModel?.MarcaImpressoraId < 0 || vmModel?.MarcaImpressoraId == null) ? null : vmModel?.MarcaImpressoraId,
                        MarcaQualidadeImagemId = (vmModel?.MarcaQualidadeImagemId < 0 || vmModel?.MarcaQualidadeImagemId == null) ? null : vmModel?.MarcaQualidadeImagemId,
                        MarcaRaridadeId = (vmModel?.MarcaRaridadeId < 0 || vmModel?.MarcaRaridadeId == null) ? null : vmModel?.MarcaRaridadeId,
                        MarcaSubTipoId = (vmModel?.MarcaSubTipoId < 0 || vmModel?.MarcaSubTipoId == null) ? 5 : vmModel?.MarcaSubTipoId,

                        CodigoAceca = !string.IsNullOrEmpty(vmModel?.CodigoAceca) ? vmModel?.CodigoAceca?.Trim() : null,
                        CodigoAcecaNew = !string.IsNullOrEmpty(vmModel?.CodigoAcecaNew) ? vmModel?.CodigoAcecaNew?.Trim() : null,
                        CodigoFabrica = !string.IsNullOrEmpty(vmModel?.CodigoFabrica) ? vmModel?.CodigoFabrica?.Trim() : null,
                        ImgPrincipal = !string.IsNullOrEmpty(vmModel?.ImgPrincipal) ? vmModel?.ImgPrincipal : null,
                        ImgDetalhe = !string.IsNullOrEmpty(vmModel?.ImgDetalhe) ? vmModel?.ImgDetalhe : null,
                        Nome = !string.IsNullOrEmpty(vmModel?.Nome) ? vmModel?.Nome?.Trim() : null,
                        Descricao = !string.IsNullOrEmpty(vmModel?.Descricao) ? vmModel?.Descricao?.Trim() : null,
                        Valor1PI = !string.IsNullOrEmpty(vmModel?.Valor1PI) ? vmModel?.Valor1PI?.Trim() : null,
                        Valor2PI = !string.IsNullOrEmpty(vmModel?.Valor2PI) ? vmModel?.Valor2PI?.Trim() : null,
                        Valor = !string.IsNullOrEmpty(vmModel?.Valor) ? vmModel?.Valor?.Trim() : null,
                        IncluidoPor = !string.IsNullOrEmpty(vmModel?.IncluidoPor) ? textInfo.ToTitleCase(vmModel?.IncluidoPor?.Trim()?.ToLower()) : null,
                        IncluidoPorSocioId = !string.IsNullOrEmpty(vmModel?.IncluidoPorSocioId) ? string.Concat(vmModel?.IncluidoPorSocioId?.Trim(), ",") : null,
                        EmQuarentena = 0,
                        ExibirGeral = true,

                        TxtFabrica = !string.IsNullOrEmpty(vmModel?.MarcaFabrica?.Nome) ? vmModel?.MarcaFabrica?.Nome?.Trim() : null,
                        TxtImpressora = !string.IsNullOrEmpty(vmModel?.MarcaImpressora?.Descricao) ? vmModel?.MarcaImpressora?.Descricao?.Trim() : null,
                    };

                    _db.Marca.Add(marca);
                    await _db.SaveChangesAsync();

                    if (marca?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Cadastrar Marca"
                        });

                    novoId = marca.Id;
                }
                else
                {
                    // Etapa intermediária de aprovação: o cadastro não entra em `marcas` -
                    // fica em `marcas_cadastro` com StatusCadastro=Pendente até um
                    // Administracao Aprovar (ver SetStatus, que promove pra `marcas`) ou Negar.
                    var model = new MarcaCadastro
                    {
                        Ativo = true,

                        MarcaAcervoId = (vmModel?.MarcaAcervoId < 0 || vmModel?.MarcaAcervoId == null) ? null : vmModel?.MarcaAcervoId,
                        MarcaDimensaoId = (vmModel?.MarcaDimensaoId < 0 || vmModel?.MarcaDimensaoId == null) ? null : vmModel?.MarcaDimensaoId,
                        MarcaFabricaId = (vmModel?.MarcaFabricaId < 0 || vmModel?.MarcaFabricaId == null) ? null : vmModel?.MarcaFabricaId,
                        MarcaFaseId = (vmModel?.MarcaFaseId < 0 || vmModel?.MarcaFaseId == null) ? null : vmModel?.MarcaFaseId,
                        MarcaFaseAcervoId = (vmModel?.MarcaFaseId < 0 || vmModel?.MarcaFaseId == null) ? null : vmModel?.MarcaFaseId,
                        MarcaFinalidadeId = (vmModel?.MarcaFinalidadeId < 0 || vmModel?.MarcaFinalidadeId == null) ? null : vmModel?.MarcaFinalidadeId,
                        MarcaImpressoraId = (vmModel?.MarcaImpressoraId < 0 || vmModel?.MarcaImpressoraId == null) ? null : vmModel?.MarcaImpressoraId,
                        MarcaQualidadeImagemId = (vmModel?.MarcaQualidadeImagemId < 0 || vmModel?.MarcaQualidadeImagemId == null) ? null : vmModel?.MarcaQualidadeImagemId,
                        MarcaRaridadeId = (vmModel?.MarcaRaridadeId < 0 || vmModel?.MarcaRaridadeId == null) ? null : vmModel?.MarcaRaridadeId,
                        MarcaSubTipoId = (vmModel?.MarcaSubTipoId < 0 || vmModel?.MarcaSubTipoId == null) ? 5 : vmModel?.MarcaSubTipoId,

                        CodigoAceca = !string.IsNullOrEmpty(vmModel?.CodigoAceca) ? vmModel?.CodigoAceca?.Trim() : null,
                        CodigoAcecaNew = !string.IsNullOrEmpty(vmModel?.CodigoAcecaNew) ? vmModel?.CodigoAcecaNew?.Trim() : null,
                        CodigoFabrica = !string.IsNullOrEmpty(vmModel?.CodigoFabrica) ? vmModel?.CodigoFabrica?.Trim() : null,
                        ImgPrincipal = !string.IsNullOrEmpty(vmModel?.ImgPrincipal) ? vmModel?.ImgPrincipal : null,
                        ImgDetalhe = !string.IsNullOrEmpty(vmModel?.ImgDetalhe) ? vmModel?.ImgDetalhe : null,
                        Nome = !string.IsNullOrEmpty(vmModel?.Nome) ? vmModel?.Nome?.Trim() : null,
                        Descricao = !string.IsNullOrEmpty(vmModel?.Descricao) ? vmModel?.Descricao?.Trim() : null,
                        Valor1PI = !string.IsNullOrEmpty(vmModel?.Valor1PI) ? vmModel?.Valor1PI?.Trim() : null,
                        Valor2PI = !string.IsNullOrEmpty(vmModel?.Valor2PI) ? vmModel?.Valor2PI?.Trim() : null,
                        Valor = !string.IsNullOrEmpty(vmModel?.Valor) ? vmModel?.Valor?.Trim() : null,
                        IncluidoPor = !string.IsNullOrEmpty(vmModel?.IncluidoPor) ? textInfo.ToTitleCase(vmModel?.IncluidoPor?.Trim()?.ToLower()) : null,
                        IncluidoPorSocioId = !string.IsNullOrEmpty(vmModel?.IncluidoPorSocioId) ? string.Concat(vmModel?.IncluidoPorSocioId?.Trim(), ",") : null,

                        // Quem enviou de fato (autenticado no servidor - nunca vem do cliente),
                        // diferente de IncluidoPorSocioId (crédito histórico de quem achou o
                        // item, escolhido livremente no combo do formulário).
                        CriadoPorSocioId = GetSocioIdAutenticado(),
                        StatusCadastro = (int)EStatusCadastro.Pendente,
                        PercentualMarcaDaguaPrincipal = percentualPrincipal,
                        PercentualMarcaDaguaDetalhe = percentualDetalhe,

                        //
                        TxtFabrica = !string.IsNullOrEmpty(vmModel?.MarcaFabrica?.Nome) ? vmModel?.MarcaFabrica?.Nome?.Trim() : null,
                        TxtImpressora = !string.IsNullOrEmpty(vmModel?.MarcaImpressora?.Descricao) ? vmModel?.MarcaImpressora?.Descricao?.Trim() : null,
                    };

                    _db.MarcaCadastro.Add(model);
                    await _db.SaveChangesAsync();

                    if (model?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Cadastrar Marca"
                        });

                    novoId = model.Id;
                }

                vmModel.Id = novoId;

                #endregion

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

        // Igual à Create, mas atualiza um cadastro já existente em marcas_cadastro (ainda não
        // aprovado). Aberto pra qualquer role da classe: o dono da submissão pode corrigir o
        // que enviou, e Administracao pode corrigir a de qualquer um (checagem de dono logo
        // abaixo, não [Authorize]). Reenvio por quem não é Administracao volta o status pra
        // Pendente - é o "Enviar para Aprovação" da tela.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string strObjModel, IFormFile iFileImgPrincipal, IFormFile iFileImgDetalhe)
        {
            try
            {
                if (string.IsNullOrEmpty(strObjModel))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Model Inválida", data = strObjModel });

                var vmModel = JsonConvert.DeserializeObject<VMMarca>(strObjModel);

                if (vmModel?.Id == null || vmModel.Id < 1)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Id deve ser maior que 0" });

                var model = await _db.MarcaCadastro.FirstOrDefaultAsync(x => x.Id == vmModel.Id);

                if (model == null)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Cadastro não encontrado" });

                bool ehAdministracao = User.IsInRole("Administracao");

                if (!ehAdministracao && model.CriadoPorSocioId != GetSocioIdAutenticado())
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Você só pode editar cadastros enviados por você" });

                if (string.IsNullOrEmpty(vmModel?.Nome))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Nome deve ser preenchido" });

                if (!RegexNomeCodigoValido.IsMatch(vmModel.Nome.Trim()))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Caracter inválido no nome preenchido" });

                if (string.IsNullOrWhiteSpace(vmModel?.CodigoAceca))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Código Aceca deve ser gerado antes de salvar" });

                if (string.IsNullOrEmpty(vmModel?.Descricao))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Descricao deve ser preenchido" });

                if (string.IsNullOrEmpty(vmModel?.IncluidoPor))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "IncluidoPor deve ser preenchido" });

                // Mesma defesa contra duplicidade da Create, excluindo a própria linha (o
                // código atual dela já era reservado por ela mesma, não é uma colisão real).
                var codigoAcecaTrim = vmModel.CodigoAceca.Trim();
                var codigoAcecaNewTrim = vmModel.CodigoAcecaNew?.Trim();

                var codigoJaUsado = await _db.Marca.AsNoTracking()
                        .AnyAsync(x => x.CodigoAceca == codigoAcecaTrim || (codigoAcecaNewTrim != null && x.CodigoAcecaNew == codigoAcecaNewTrim))
                    || await _db.MarcaCadastro.AsNoTracking()
                        .AnyAsync(x => x.Id != model.Id
                                     && x.StatusCadastro == (int)EStatusCadastro.Pendente
                                     && (x.CodigoAceca == codigoAcecaTrim || (codigoAcecaNewTrim != null && x.CodigoAcecaNew == codigoAcecaNewTrim)));

                if (codigoJaUsado)
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Código Aceca já está em uso por outro cadastro (aprovado ou aguardando aprovação) - gere o código novamente"
                    });

                // Upload de imagem - sempre em staging (o item continua em marcas_cadastro até
                // ser aprovado, então uma imagem nova aqui não pode ir pra pasta ao vivo ainda).
                // Se nenhum arquivo novo for enviado, mantém o nome de arquivo já salvo.
                if (iFileImgPrincipal != null && !vmModel.ImgPrincipal.Equals("C:\\fakepath\\."))
                {
                    var result = await UploadImg(vmModel, iFileImgPrincipal, true, bStaging: true);
                    var jObjResult = JObject.FromObject(((ObjectResult)result).Value);

                    if (result.GetType() == typeof(NotFoundObjectResult) || result.GetType() == typeof(BadRequestObjectResult))
                        return BadRequest(new { bResult = false, type = "ERRO", message = (string)jObjResult?["message"] });

                    vmModel.ImgPrincipal = (string)jObjResult?["data"];
                }
                else
                {
                    vmModel.ImgPrincipal = model.ImgPrincipal;
                }

                if (iFileImgDetalhe != null && !vmModel.ImgDetalhe.Equals("C:\\fakepath\\."))
                {
                    var result = await UploadImg(vmModel, iFileImgDetalhe, false, bStaging: true);
                    var jObjResult = JObject.FromObject(((ObjectResult)result).Value);

                    if (result.GetType() == typeof(NotFoundObjectResult) || result.GetType() == typeof(BadRequestObjectResult))
                        return BadRequest(new { bResult = false, type = "ERRO", message = (string)jObjResult?["message"] });

                    vmModel.ImgDetalhe = (string)jObjResult?["data"];
                }
                else
                {
                    vmModel.ImgDetalhe = model.ImgDetalhe;
                }

                TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;

                model.MarcaAcervoId = (vmModel?.MarcaAcervoId < 0 || vmModel?.MarcaAcervoId == null) ? null : vmModel?.MarcaAcervoId;
                model.MarcaDimensaoId = (vmModel?.MarcaDimensaoId < 0 || vmModel?.MarcaDimensaoId == null) ? null : vmModel?.MarcaDimensaoId;
                model.MarcaFabricaId = (vmModel?.MarcaFabricaId < 0 || vmModel?.MarcaFabricaId == null) ? null : vmModel?.MarcaFabricaId;
                model.MarcaFaseId = (vmModel?.MarcaFaseId < 0 || vmModel?.MarcaFaseId == null) ? null : vmModel?.MarcaFaseId;
                model.MarcaFaseAcervoId = (vmModel?.MarcaFaseId < 0 || vmModel?.MarcaFaseId == null) ? null : vmModel?.MarcaFaseId;
                model.MarcaFinalidadeId = (vmModel?.MarcaFinalidadeId < 0 || vmModel?.MarcaFinalidadeId == null) ? null : vmModel?.MarcaFinalidadeId;
                model.MarcaImpressoraId = (vmModel?.MarcaImpressoraId < 0 || vmModel?.MarcaImpressoraId == null) ? null : vmModel?.MarcaImpressoraId;
                model.MarcaQualidadeImagemId = (vmModel?.MarcaQualidadeImagemId < 0 || vmModel?.MarcaQualidadeImagemId == null) ? null : vmModel?.MarcaQualidadeImagemId;
                model.MarcaRaridadeId = (vmModel?.MarcaRaridadeId < 0 || vmModel?.MarcaRaridadeId == null) ? null : vmModel?.MarcaRaridadeId;
                model.MarcaSubTipoId = (vmModel?.MarcaSubTipoId < 0 || vmModel?.MarcaSubTipoId == null) ? 5 : vmModel?.MarcaSubTipoId;

                model.CodigoAceca = codigoAcecaTrim;
                model.CodigoAcecaNew = codigoAcecaNewTrim;
                model.CodigoFabrica = !string.IsNullOrEmpty(vmModel?.CodigoFabrica) ? vmModel?.CodigoFabrica?.Trim() : null;
                model.ImgPrincipal = !string.IsNullOrEmpty(vmModel?.ImgPrincipal) ? vmModel?.ImgPrincipal : null;
                model.ImgDetalhe = !string.IsNullOrEmpty(vmModel?.ImgDetalhe) ? vmModel?.ImgDetalhe : null;
                model.Nome = vmModel?.Nome?.Trim();
                model.Descricao = vmModel?.Descricao?.Trim();
                model.Valor1PI = !string.IsNullOrEmpty(vmModel?.Valor1PI) ? vmModel?.Valor1PI?.Trim() : null;
                model.Valor2PI = !string.IsNullOrEmpty(vmModel?.Valor2PI) ? vmModel?.Valor2PI?.Trim() : null;
                model.Valor = !string.IsNullOrEmpty(vmModel?.Valor) ? vmModel?.Valor?.Trim() : null;
                model.IncluidoPor = textInfo.ToTitleCase(vmModel?.IncluidoPor?.Trim()?.ToLower());
                model.IncluidoPorSocioId = !string.IsNullOrEmpty(vmModel?.IncluidoPorSocioId) ? string.Concat(vmModel?.IncluidoPorSocioId?.Trim(), ",") : null;
                model.Observacao = !string.IsNullOrWhiteSpace(vmModel?.Observacao) ? vmModel.Observacao.Trim() : null;

                // Reaplicado com este valor só na aprovação (ver SetStatus/MoverImagensPendenteParaAcervo) -
                // a imagem em staging continua sem marca até lá.
                var percentualPadrao = await GetPercentualMarcaDaguaPadraoAsync();
                model.PercentualMarcaDaguaPrincipal = vmModel?.PercentualMarcaDaguaPrincipal ?? percentualPadrao;
                model.PercentualMarcaDaguaDetalhe = vmModel?.PercentualMarcaDaguaDetalhe ?? percentualPadrao;

                model.TxtFabrica = !string.IsNullOrEmpty(vmModel?.MarcaFabrica?.Nome) ? vmModel?.MarcaFabrica?.Nome?.Trim() : null;
                model.TxtImpressora = !string.IsNullOrEmpty(vmModel?.MarcaImpressora?.Descricao) ? vmModel?.MarcaImpressora?.Descricao?.Trim() : null;

                // Qualquer edição (Administracao ou dono) volta o status pra Pendente - todo
                // clique em "Salvar"/"Enviar para Aprovação" é tratado como um novo ciclo de
                // aprovação, mesmo quando quem edita é quem vai aprovar depois.
                model.StatusCadastro = (int)EStatusCadastro.Pendente;

                await _db.SaveChangesAsync();

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

        // Aberto pra qualquer role da classe: o dono da submissão pode cancelar o próprio
        // envio ainda pendente ("Remover" na fila de Aprovação); Administracao pode remover
        // qualquer um. Checagem de dono abaixo, não [Authorize].
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

                var model = await _db.MarcaCadastro.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                if (!User.IsInRole("Administracao") && model.CriadoPorSocioId != GetSocioIdAutenticado())
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Você só pode remover cadastros enviados por você"
                    });

                _db.MarcaCadastro.Remove(model);
                await _db.SaveChangesAsync();

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
        public async Task<IActionResult> GetNovoCodigoAceca(int idMarcaAcervo, int idFase, string strNovoNomeParaCadastro, bool bvariante, bool bExTemPaisDestino)
        {
            /*
                10  Pré 1800 - 1943
                11  $R 1800 - 1943
                12  1PI 1942 - 1949
                13  2PI 1945 - 1965
                14  SA 1964 - 1988
                15  ams20 1988 - 1990
                16  amc20 1989 - 1993
                17  AM 1990 - 1993
                18  AMI 1992 - 1996
                19  6av 1995 - 2000
                20  5av 1999 - 2002
                21  9av 2001 - 2005
                22  10avDPF 2004 - 2008
                23  10avDS 2007 - 2009
                24  10av 2009 - 2015
                25  10av136 2013 - 2016
                26  136Frontal 2016 - 2019
                27  Palheiros - Artesanais
                28  Fumos, Cigarrilhas, RP
                29  Exportação
                32  Cortadas
                33  Outros
                34  Quarentena
                35  136Amarelo 2019 - 2025
                36  Comemorativas Aceca
                38  Vitrine
                39  Clandestinas
                40  Exterior
                41  M & C baixe as imagens no seu computador para  ler
                42  136QRCode
                */

            if (idFase < 1 || string.IsNullOrWhiteSpace(strNovoNomeParaCadastro))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "GetCodigoAceca - Id deve ser maior que 0",
                    data = idFase
                });

            if (!RegexNomeCodigoValido.IsMatch(strNovoNomeParaCadastro.Trim()))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Caracter inválido no nome preenchido",
                    data = strNovoNomeParaCadastro
                });

            try
            {
                #region variaveis

                var model = new Marcas();

                string strOldCodigoAceca = string.Empty;
                string strCodigoAceca = string.Empty;

                string strVelhoCodigoAceca = string.Empty;
                string strNovoCodigoAceca = string.Empty;
               
                string strNumOldCodigoAceca = string.Empty;
                string strNumCodigoAceca = string.Empty;

                var strUltimaLetraCodigoAceca = 'B';

                var bMarcaSemCadastro = false;
                var queryExistsTermo = false;

                var msgErroData = $"idMarcaFase :: {idFase} , strNovoNomeParaCadastro :: {strNovoNomeParaCadastro}";

                var strLetraInicial = strNovoNomeParaCadastro?.Trim()[0].ToString();

                var strCodigoPaiVariante = strNovoNomeParaCadastro.Trim();

                var query = Enumerable.Empty<Marcas>().AsQueryable();

                EFase FaseSel = (EFase)idFase;

                #endregion

                if (!idMarcaAcervo.Equals((int)EAcervo.Geral))
                {
                    query = _db.Marca
                                   .Include(x => x.MarcaSubTipo.MarcaTipo)
                                   .Include(x => x.MarcaFabrica)
                                   .Include(x => x.MarcaImpressora)
                                   .AsNoTracking()
                                   .Where(x => x.CodigoAceca != null 
                                                && (!bvariante ? x.MarcaAcervoId.Equals(idMarcaAcervo) : x.CodigoAceca.Equals(strCodigoPaiVariante))
                                          )
                                   .OrderByDescending(x => x.MarcaFaseAcervoId > 1 ? x.CodigoAcecaNew : x.CodigoAceca)
                                   .Take(1);
                }
                else
                {
                    switch (FaseSel)
                    {
                        case EFase.Exportacao:
                            {
                                // 29 Exportacao
                                //Se tem país de destino inicia com EA, Se não tem é EX (minusculos).

                                var strLetraInicialBusca = bExTemPaisDestino ? "EA" : "EX";

                                query = _db.Marca
                                    .Include(x => x.MarcaSubTipo.MarcaTipo)
                                    .Include(x => x.MarcaFabrica)
                                    .Include(x => x.MarcaImpressora)
                                    .AsNoTracking()
                                    .Where(x => x.CodigoAceca != null && x.CodigoAceca.StartsWith(strLetraInicialBusca.ToLower()) && x.MarcaAcervoId.Equals(idMarcaAcervo) && x.MarcaFaseId.Equals(idFase))
                                    .OrderByDescending(x => x.CodigoAceca)
                                    .Take(2);
                            }
                            break; 
                        case EFase.QRCode136:
                            {
                                query = _db.Marca
                                    .Include(x => x.MarcaSubTipo.MarcaTipo)
                                    .Include(x => x.MarcaFabrica)
                                    .Include(x => x.MarcaImpressora)
                                    .AsNoTracking()
                                    .Where(x => x.CodigoAceca != null && x.MarcaAcervoId.Equals(idMarcaAcervo) && x.MarcaFaseId.Equals(idFase))
                                    .OrderByDescending(x => x.CodigoAceca)
                                    .Take(2);
                            }
                            break;
                        case EFase.SA:
                            {

                                query = _db.Marca
                                   .Include(x => x.MarcaSubTipo.MarcaTipo)
                                   .Include(x => x.MarcaFabrica)
                                   .Include(x => x.MarcaImpressora)
                                   .AsNoTracking()
                                   .Where(x => x.CodigoAceca != null
                                                && (!bvariante 
                                                        ? x.MarcaAcervoId.Equals(idMarcaAcervo)
                                                            && x.MarcaFaseId.Equals(idFase) 
                                                            && (strLetraInicial.ToUpper().Equals("N") 
                                                                    ? x.CodigoAceca.StartsWith(strLetraInicial) && !x.CodigoAceca.ToUpper().StartsWith("NO") 
                                                                    : x.CodigoAceca.StartsWith(strLetraInicial)
                                                               )
                                                        : x.CodigoAceca.Equals(strCodigoPaiVariante)
                                                    )
                                          )
                                   .OrderByDescending(x => x.CodigoAceca)
                                   .Take(2);
                            }
                            break;
                        default:
                            {

                                query = _db.Marca
                                   .Include(x => x.MarcaSubTipo.MarcaTipo)
                                   .Include(x => x.MarcaFabrica)
                                   .Include(x => x.MarcaImpressora)
                                   .AsNoTracking()
                                   .Where(x => x.CodigoAceca != null
                                                && (!bvariante 
                                                        ? x.MarcaAcervoId.Equals(idMarcaAcervo) && x.MarcaFaseId.Equals(idFase) 
                                                        : x.CodigoAceca.Equals(strCodigoPaiVariante)
                                                   )
                                          )
                                   .OrderByDescending(x => x.CodigoAceca)
                                   .Take(2);
                            }
                            break;
                    }
                }

                model = await query.AsQueryable().FirstOrDefaultAsync();

                if (model == null || model?.Id == null)
                {
                    return Ok(new
                    {
                        bResult = false,
                        type = "ERRO - listagem Nula",
                        message = "Erro ao recuperar novo codigo",
                        data = strNovoNomeParaCadastro
                    });
                }
                else
                {
                    strCodigoAceca = idMarcaAcervo > 1 || (idMarcaAcervo.Equals((int)EAcervo.Geral) && idFase > 14)
                        ? model?.CodigoAcecaNew?.ToString()?.Trim() 
                        : model?.CodigoAceca?.ToString()?.Trim();

                    strOldCodigoAceca = model?.CodigoAceca?.ToString()?.Trim();

                    switch (FaseSel)
                    {
                        //////Fases que as inciiam com numero e tem letras no meio
                        case EFase.Pi1:
                        case EFase.Pi2:
                            {
                                strNumCodigoAceca = idMarcaAcervo > 1
                                    ? new string(strCodigoAceca?.Where(char.IsDigit).ToArray())
                                    : new string(strCodigoAceca?.Split("PI")[1]?.Where(char.IsDigit).ToArray());

                                strNumOldCodigoAceca = idMarcaAcervo > 1
                                    ? new string(strOldCodigoAceca?.Where(char.IsDigit).ToArray())
                                    : new string(strOldCodigoAceca?.Split("PI")[1]?.Where(char.IsDigit).ToArray());
                            }
                            break;
                        case EFase.ams20:
                        case EFase.amc20:
                        case EFase.AM:
                        case EFase.AMI:
                        case EFase.Av6:
                        case EFase.Av5:
                        case EFase.Av9:
                        case EFase.AvDPF10:
                        case EFase.AvDS10:
                        case EFase.Av10:
                        case EFase.Av136:
                        case EFase.Frontal136:
                        case EFase.Amarelo136:
                            {
                                strNumCodigoAceca = idMarcaAcervo > 1
                                    ? new string(strCodigoAceca?.Where(char.IsDigit).ToArray())
                                    : new string(strCodigoAceca?.Split("-")[1]?.Where(char.IsDigit).ToArray());

                                strNumOldCodigoAceca = new string(strOldCodigoAceca?.Where(char.IsDigit).ToArray());
                            }
                            break;
                        case EFase.QRCode136:
                            {
                                strNumCodigoAceca = idMarcaAcervo > 1
                                    ? new string(strCodigoAceca?.Where(char.IsDigit).ToArray())
                                    : new string(strCodigoAceca?.Split("-")[1]?.Where(char.IsDigit).ToArray());

                                strNumOldCodigoAceca = new string(strCodigoAceca?.Split("-")[1]?.Where(char.IsDigit).ToArray());
                            }
                            break;
                        default:
                            {
                                strNumCodigoAceca = new string(strCodigoAceca?.Where(char.IsDigit).ToArray());
                                strNumOldCodigoAceca = new string(strOldCodigoAceca?.Where(char.IsDigit).ToArray());
                            }
                            break;
                    }

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


                    #region New

                    if (int.TryParse(strNumCodigoAceca, out int intNumCodigoAceca))
                    {
                        if (!bvariante)
                        {
                            /*
                            strNovoCodigoAceca = (!FaseSel.Equals(EFase.QRCode136) || idMarcaAcervo > 1)
                                ? strCodigoAceca?.Replace(intNumCodigoAceca.ToString(), (intNumCodigoAceca + 1).ToString())
                                : string.Concat("136QR-", strNumCodigoAceca?.Replace(intNumCodigoAceca.ToString(), (intNumCodigoAceca + 1).ToString()));
                            */

                            strNovoCodigoAceca = strCodigoAceca?.Replace(intNumCodigoAceca.ToString(), (intNumCodigoAceca + 1).ToString());

                            if (Char.IsLetter(strNovoCodigoAceca[^1]))
                                strNovoCodigoAceca = strNovoCodigoAceca.Remove(strNovoCodigoAceca.Length - 1);
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
                    }

                    #endregion

                    #region Old

                    if (int.TryParse(strNumOldCodigoAceca, out int intNumOldCodigoAceca))
                    {
                        if (!bvariante)
                        {
                            strVelhoCodigoAceca = strOldCodigoAceca?.Replace(intNumOldCodigoAceca.ToString(), (intNumOldCodigoAceca + 1).ToString());

                            if (Char.IsLetter(strVelhoCodigoAceca[^1]))
                                strVelhoCodigoAceca = strVelhoCodigoAceca.Remove(strVelhoCodigoAceca.Length - 1);
                        }
                        else
                        {
                            if (Char.IsLetter(strOldCodigoAceca[^1]))
                            {
                                strUltimaLetraCodigoAceca = strOldCodigoAceca[^1];

                                char charProximaLetraCodigoAceca = (char)(strUltimaLetraCodigoAceca + 1);

                                strVelhoCodigoAceca = ReplaceInPosition(strOldCodigoAceca.ToString(), strOldCodigoAceca.Length - 1, charProximaLetraCodigoAceca);
                            }
                            else
                            {
                                strVelhoCodigoAceca = string.Concat(strOldCodigoAceca, strUltimaLetraCodigoAceca);
                            }
                        }
                    }

                    #endregion

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

                    #region Evita colisão com código já reservado (aprovado ou pendente)

                    // Achado real em produção: o cálculo acima só olha `marcas` (catálogo já
                    // aprovado) - se outro cadastro para a mesma fase ainda está em
                    // marcas_cadastro aguardando aprovação, o código dele não aparecia aqui,
                    // e dois usuários diferentes podiam receber o mesmo próximo CodigoAceca.
                    // Bumpa o código (mesma lógica de incremento já usada acima) até achar um
                    // que não esteja em uso nem por uma Marca já aprovada nem por outra
                    // submissão ainda pendente.
                    async Task<bool> CodigoReservadoAsync(string codigo)
                    {
                        if (string.IsNullOrEmpty(codigo))
                            return false;

                        if (await _db.Marca.AsNoTracking().AnyAsync(x => x.CodigoAceca == codigo || x.CodigoAcecaNew == codigo))
                            return true;

                        return await _db.MarcaCadastro.AsNoTracking().AnyAsync(x =>
                            x.StatusCadastro == (int)EStatusCadastro.Pendente
                            && (x.CodigoAceca == codigo || x.CodigoAcecaNew == codigo));
                    }

                    string BumpCodigo(string codigo)
                    {
                        var numStrBump = new string(codigo?.Where(char.IsDigit).ToArray());

                        if (!bvariante)
                        {
                            if (int.TryParse(numStrBump, out int numBump))
                            {
                                var novoBump = codigo.Replace(numBump.ToString(), (numBump + 1).ToString());

                                if (Char.IsLetter(novoBump[^1]))
                                    novoBump = novoBump.Remove(novoBump.Length - 1);

                                return novoBump;
                            }

                            return codigo;
                        }

                        if (Char.IsLetter(codigo[^1]))
                        {
                            char proximaLetraBump = (char)(codigo[^1] + 1);
                            return ReplaceInPosition(codigo, codigo.Length - 1, proximaLetraBump);
                        }

                        return string.Concat(codigo, strUltimaLetraCodigoAceca);
                    }

                    var tentativasColisao = 0;

                    while ((await CodigoReservadoAsync(strNovoCodigoAceca))
                           || (!string.IsNullOrEmpty(strVelhoCodigoAceca) && await CodigoReservadoAsync(strVelhoCodigoAceca)))
                    {
                        if (++tentativasColisao > 100)
                            break; // segurança - não deve acontecer na prática

                        strNovoCodigoAceca = BumpCodigo(strNovoCodigoAceca);

                        if (!string.IsNullOrEmpty(strVelhoCodigoAceca))
                            strVelhoCodigoAceca = BumpCodigo(strVelhoCodigoAceca);
                    }

                    #endregion

                    if (model?.MarcaImpressoraId == null || model?.MarcaImpressoraId <= 0)
                        if (!string.IsNullOrEmpty(model?.TxtImpressora))
                        {
                            var objImpressora = await _db.MarcaImpressora
                                .AsNoTracking()
                                .Where(i => i.Descricao.Equals(model.TxtImpressora.Trim()))
                                .FirstOrDefaultAsync();

                            if (objImpressora != null)
                            {
                                model?.MarcaImpressora = new MarcaImpressora
                                {
                                    Id = objImpressora?.Id,
                                    Descricao = objImpressora?.Descricao
                                };

                                model?.MarcaImpressoraId = objImpressora?.Id;
                            }
                        }

                    if (model?.MarcaFabricaId == null || model?.MarcaFabricaId <= 0)
                        if (!string.IsNullOrEmpty(model?.TxtFabrica))
                        {
                            var objFabrica = _db.MarcaFabrica
                                .AsNoTracking()
                                .Where(i => i.Nome.Equals(model.TxtFabrica.Trim()))
                                .FirstOrDefault();

                            if (objFabrica != null)
                            {
                                model?.MarcaFabrica = new MarcaFabrica
                                {
                                    Id = objFabrica?.Id,
                                    Nome = objFabrica?.Nome,
                                    Descricao = objFabrica?.Descricao
                                };

                                model?.MarcaFabricaId = objFabrica?.Id;
                            }
                        }
                }

                return Ok(new
                {
                    bResult = true,
                    bSemCadastro = bMarcaSemCadastro,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = model,
                    dataVelhoCodigo = strVelhoCodigoAceca,
                    dataNovoCodigo = strNovoCodigoAceca,
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

        // Confere a assinatura binária (magic bytes) do arquivo, já que a extensão informada
        // pelo cliente pode ser forjada (ex.: um executável renomeado para ".jpg").
        private static bool IsValidImageContent(Stream stream, string extension)
        {
            Span<byte> header = stackalloc byte[12];

            stream.Position = 0;
            int read = stream.Read(header);
            stream.Position = 0;

            if (read < 4)
                return false;

            return extension switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                _ => false
            };
        }

        // Marca d'água aplicada só na imagem que efetivamente entra pública no Acervo
        // (ver chamadas em UploadImg e MoverImagensPendenteParaAcervo) - fica em cache
        // em memória pra não decodificar o PNG a cada upload/aprovação.
        private static SKBitmap? _marcaDaguaBitmap;
        private static readonly object _marcaDaguaLock = new();

        private SKBitmap? ObterMarcaDaguaBitmap()
        {
            if (_marcaDaguaBitmap != null)
                return _marcaDaguaBitmap;

            lock (_marcaDaguaLock)
            {
                if (_marcaDaguaBitmap == null)
                {
                    var caminhoMarca = Path.Combine(_appEnvironment.WebRootPath, "img", "marca-dagua", "imagemMarcaPadrao.png");

                    if (System.IO.File.Exists(caminhoMarca))
                        _marcaDaguaBitmap = SKBitmap.Decode(caminhoMarca);
                }
            }

            return _marcaDaguaBitmap;
        }

        // Opacidade padrão (10%) usada como último recurso se o parâmetro em adm_config
        // (ver GetPercentualMarcaDaguaPadraoAsync) não existir/estiver inválido.
        private const float OpacidadeMarcaDaguaPadrao = 0.10f;

        // Valor padrão (%) sugerido nos campos de opacidade em CadastroAcervo.cshtml
        // (#txt_PercentPrincipal/#txt_PercentDetalhe) - configurável em Configurações
        // (adm_config, parâmetro "PercentualMarcaDAgua"), editável pontualmente por item
        // antes da aprovação. Cai pro padrão em código se o parâmetro não existir.
        private async Task<double> GetPercentualMarcaDaguaPadraoAsync()
        {
            var parametro = await _db.AdmConfig.AsNoTracking().FirstOrDefaultAsync(x => x.Parametro == "PercentualMarcaDAgua");

            if (parametro != null && double.TryParse(parametro.Valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor))
                return Math.Clamp(valor, 0, 100);

            return OpacidadeMarcaDaguaPadrao * 100;
        }

        // Aplica a marca d'água padrão da ACECA centralizada (horizontal e vertical) na
        // imagem, em tom de cinza (dessaturada) e com opacidade baixa - suave o suficiente
        // pra só identificar a origem, sem atrapalhar a visualização da peça.
        private byte[] AplicarMarcaDagua(byte[] imagemOriginal, string extensao, float opacidade = OpacidadeMarcaDaguaPadrao)
        {
            var marca = ObterMarcaDaguaBitmap();
            if (marca == null)
                return imagemOriginal;

            using var bitmapOriginal = SKBitmap.Decode(imagemOriginal);
            if (bitmapOriginal == null)
                return imagemOriginal;

            var samplingSuave = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

            using var surface = SKSurface.Create(new SKImageInfo(bitmapOriginal.Width, bitmapOriginal.Height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(bitmapOriginal, 0, 0, samplingSuave, null);

            // marca ocupa ~45% da largura da imagem base, mantendo a proporção original
            float escala = (bitmapOriginal.Width * 0.45f) / marca.Width;
            float larguraMarca = marca.Width * escala;
            float alturaMarca = marca.Height * escala;

            float x = (bitmapOriginal.Width - larguraMarca) / 2f;
            float y = (bitmapOriginal.Height - alturaMarca) / 2f;

            // matriz de cinza (luminosidade) preservando o canal alfa original do PNG
            float[] matrizCinza =
            {
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0,      0,      0,      1, 0
            };

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White.WithAlpha((byte)Math.Round(255 * Math.Clamp(opacidade, 0f, 1f))),
                ColorFilter = SKColorFilter.CreateColorMatrix(matrizCinza)
            };

            canvas.DrawBitmap(marca, SKRect.Create(x, y, larguraMarca, alturaMarca), samplingSuave, paint);

            using var imagemFinal = surface.Snapshot();
            var formato = extensao.Equals(".png", StringComparison.OrdinalIgnoreCase)
                ? SKEncodedImageFormat.Png
                : SKEncodedImageFormat.Jpeg;

            using var dados = imagemFinal.Encode(formato, 90);
            return dados.ToArray();
        }

        // Aplica a marca d'água na imagem enviada e devolve o resultado pra preview em
        // CadastroAcervo.cshtml (img_ImgPrincipalComMarca/img_ImgDetalheComMarca), sem
        // gravar nada em disco/FTP nem tocar no banco - deixa o usuário calibrar a
        // opacidade (%) por imagem antes de cadastrar/aprovar. Sem [Authorize] próprio -
        // herda o da classe (qualquer role que pode enviar cadastro pode usar o preview).
        [HttpPost]
        public async Task<IActionResult> TestarMarcaDagua(IFormFile iFileImg, double percentualOpacidade = 10)
        {
            if (iFileImg == null || iFileImg.Length == 0)
                return BadRequest(new { bResult = false, type = "ERRO", message = "Arquivo de Imagem Nulo ou Invalido" });

            if (iFileImg.Length > TamanhoMaximoImagemBytes)
                return BadRequest(new { bResult = false, type = "ERRO", message = "Arquivo de Imagem excede o tamanho máximo permitido (2MB)" });

            string fileExtension = Path.GetExtension(iFileImg.FileName)?.ToLowerInvariant() ?? string.Empty;

            if (fileExtension != ".png" && fileExtension != ".jpg" && fileExtension != ".jpeg")
                return BadRequest(new { bResult = false, type = "ERRO", message = "Arquivo de Imagem com Extensão Inválida" });

            // Diagnóstico explícito só nesse endpoint de teste - em produção (UploadImg/
            // MoverImagensPendenteParaAcervo) o comportamento é "falhar aberto" (devolve a
            // imagem original se a marca não for encontrada), mas aqui isso mascarava o
            // problema (preview aparecia igual, sem avisar que a marca não foi aplicada).
            if (ObterMarcaDaguaBitmap() == null)
            {
                var caminhoEsperado = Path.Combine(_appEnvironment.WebRootPath, "img", "marca-dagua", "imagemMarcaPadrao.png");
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = $"Marca d'água padrão não encontrada em: {caminhoEsperado}"
                });
            }

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await iFileImg.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            var opacidade = (float)Math.Clamp(percentualOpacidade, 0, 100) / 100f;
            var bytesComMarca = AplicarMarcaDagua(fileBytes, fileExtension, opacidade);

            if (bytesComMarca.Length == fileBytes.Length && bytesComMarca.SequenceEqual(fileBytes))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Não foi possível decodificar a imagem enviada para aplicar a marca d'água"
                });

            var contentType = fileExtension == ".png" ? "image/png" : "image/jpeg";

            return File(bytesComMarca, contentType);
        }

        // Preview "com marca" de uma imagem que já está no servidor (edição de um item ainda
        // pendente em marcas_cadastro) - diferente de TestarMarcaDagua (que recebe o arquivo
        // direto do <input type=file>), aqui a imagem só existe em disco/FTP na pasta de
        // staging. Buscar via fetch() no client não funciona (a mídia fica em outro domínio -
        // www.aceca.com.br - sem cabeçalho CORS), então lê os bytes no servidor mesmo,
        // reaproveitando a mesma lógica de caminho de UploadImg/MoverImagensPendenteParaAcervo.
        [HttpGet]
        public async Task<IActionResult> PreviewMarcaDaguaExistente(int id, bool principal, double percentualOpacidade = 10)
        {
            var model = await _db.MarcaCadastro.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
                return BadRequest(new { bResult = false, type = "ERRO", message = "Cadastro não encontrado" });

            if (!User.IsInRole("Administracao") && model.CriadoPorSocioId != GetSocioIdAutenticado())
                return BadRequest(new { bResult = false, type = "ERRO", message = "Você só pode ver cadastros enviados por você" });

            var nomeArquivo = principal ? model.ImgPrincipal : model.ImgDetalhe;

            if (string.IsNullOrEmpty(nomeArquivo))
                return BadRequest(new { bResult = false, type = "ERRO", message = "Imagem não encontrada" });

            var fileExtension = Path.GetExtension(nomeArquivo).ToLowerInvariant();
            byte[] fileBytes = LerBytesImagemStaging(model.MarcaFaseId, nomeArquivo, principal);

            if (fileBytes == null)
                return BadRequest(new { bResult = false, type = "ERRO", message = "Arquivo de imagem não encontrado no servidor" });

            var opacidade = (float)(Math.Clamp(percentualOpacidade, 0, 100) / 100.0);
            var bytesComMarca = AplicarMarcaDagua(fileBytes, fileExtension, opacidade);
            var contentType = fileExtension == ".png" ? "image/png" : "image/jpeg";

            return File(bytesComMarca, contentType);
        }

        // Lê os bytes de uma imagem ainda em staging ("_pendente/{fase}[/detalhes]") - mesma
        // construção de caminho usada em UploadImg (bStaging=true) e MoverImagensPendenteParaAcervo.
        private byte[] LerBytesImagemStaging(int? marcaFaseId, string nomeArquivo, bool principal)
        {
            if (_bIsLocalHost)
            {
                var caminho = principal
                    ? Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", "_pendente", marcaFaseId?.ToString(), nomeArquivo)
                    : Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", "_pendente", marcaFaseId?.ToString(), "detalhes", nomeArquivo);

                return System.IO.File.Exists(caminho) ? System.IO.File.ReadAllBytes(caminho) : null;
            }

            using var ftpConn = new FtpClient(_ftpHost, _ftpUser, _ftpPass);
            ftpConn.Connect();

            try
            {
                var caminho = principal
                    ? $"{_ftpBaseUrl}/midia/geral/_pendente/{marcaFaseId}/{nomeArquivo}"
                    : $"{_ftpBaseUrl}/midia/geral/_pendente/{marcaFaseId}/detalhes/{nomeArquivo}";

                if (!ftpConn.FileExists(caminho))
                    return null;

                using var ms = new MemoryStream();
                ftpConn.DownloadStream(ms, caminho);
                return ms.ToArray();
            }
            finally
            {
                ftpConn.Disconnect();
            }
        }

        [Authorize(Roles = "Administracao")]
        public async Task<IActionResult> UploadImg(VMMarca vmModel, IFormFile iFileImg, bool bIsImgPrincipal, bool bStaging = false, double percentual = OpacidadeMarcaDaguaPadrao * 100)
        {
            if (string.IsNullOrEmpty(iFileImg.FileName) || iFileImg?.FileName == null || iFileImg?.FileName.Length == 0)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Arquivo de Imagem Nulo ou Invalido",
                    data = iFileImg?.FileName
                });

            if (iFileImg!.Length > TamanhoMaximoImagemBytes)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Arquivo de Imagem excede o tamanho máximo permitido (2MB)",
                    data = iFileImg?.FileName
                });

            string fileExtension = Path.GetExtension(iFileImg?.FileName?.ToString())?.ToLowerInvariant();

            var fileExtensionValid = new[] { ".jpg", ".jpeg", ".png" };

            if (string.IsNullOrEmpty(fileExtension) || !fileExtensionValid.Contains(fileExtension))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Arquivo de Imagem com Extensão Inválida",
                    data = iFileImg?.FileName
                });

            // CodigoAceca é usado para montar nome/caminho do arquivo em disco/FTP — precisa ser
            // restrito a caracteres seguros para não permitir path traversal (ex.: "../../").
            if (string.IsNullOrWhiteSpace(vmModel?.CodigoAceca) || !RegexCodigoArquivoValido.IsMatch(vmModel.CodigoAceca.Trim()))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Código Aceca inválido para nome de arquivo",
                    data = vmModel?.CodigoAceca
                });

            using (var checkStream = iFileImg.OpenReadStream())
            {
                if (!IsValidImageContent(checkStream, fileExtension))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Conteúdo do arquivo não corresponde à extensão informada",
                        data = iFileImg?.FileName
                    });
            }

           //Recupera Nome Original da imagem
            var fileImgOriginalName = string.Concat(iFileImg?.FileName?.Trim()?.ToLower(), (!(bool)iFileImg?.FileName.Contains(fileExtension) ? fileExtension : String.Empty));

            //Gera novo nome
            string strImgNomeBase ="aceca_"; //string.Empty; //
            string strImgDetalheNomeBase = "detalhe_";

            string strSaveFileName = $"{strImgNomeBase}{vmModel?.CodigoAceca?.Trim()?.ToLower()}";

            // monta o caminho onde vamos salvar o arquivo:
            var strPathSaveFolder = string.Empty;
            var strPathSaveFile = string.Empty;

            // Enquanto aguarda aprovação (cadastro de não-Administracao), a imagem fica numa
            // pasta "_pendente" separada - só é movida pra pasta real do Acervo quando o
            // cadastro é Aprovado (ver SetStatus). Sem isso, a imagem ficava pública no
            // Acervo antes mesmo do item ser aprovado.
            var strPastaFase = bStaging
                ? $"_pendente/{vmModel?.MarcaFaseId?.ToString()}"
                : vmModel?.MarcaFaseId?.ToString();

            if (_bIsLocalHost)
            {
                strPathSaveFolder = bIsImgPrincipal
                ? Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", strPastaFase)
                : Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", strPastaFase,"detalhes");

                strSaveFileName = bIsImgPrincipal
                ? $"{strSaveFileName}{fileExtension}"
                : $"{strSaveFileName.Replace(strImgNomeBase, string.Concat(strImgNomeBase, strImgDetalheNomeBase))}{fileExtension}";

                strPathSaveFile = Path.Combine(strPathSaveFolder, strSaveFileName);
            }
            else
            {
                strPathSaveFolder = bIsImgPrincipal
                ? $"{_ftpBaseUrl}/midia/geral/{strPastaFase}"
                : $"{_ftpBaseUrl}/midia/geral/{strPastaFase}/detalhes";


                strSaveFileName = bIsImgPrincipal
                ? $"{strSaveFileName}{fileExtension}"
                : $"{strSaveFileName.Replace(strImgNomeBase, string.Concat(strImgNomeBase, strImgDetalheNomeBase))}{fileExtension}";

                strPathSaveFile = $"{strPathSaveFolder}/{strSaveFileName}";
            }

            var fileDetails = new FileDetails()
            {
                FileName = strSaveFileName,
                FileSize = iFileImg.Length / 1000,
                FilePath = strPathSaveFile,
                FileType = iFileImg?.ContentType,
            };

            // Só marca d'água na imagem que já vai direto pro Acervo público (Administracao,
            // sem passar pela fila de aprovação) - em staging a imagem fica limpa, pra não
            // marcar de novo quando for movida em MoverImagensPendenteParaAcervo.
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await iFileImg!.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            if (!bStaging)
                fileBytes = AplicarMarcaDagua(fileBytes, fileExtension, (float)(Math.Clamp(percentual, 0, 100) / 100.0));

            //local destino
            if (_bIsLocalHost)
            {
                if (!Directory.Exists(strPathSaveFolder))
                    Directory.CreateDirectory(strPathSaveFolder);

                //Verifica arquivo ja existe (mesmo comportamento do modo FTP)
                if (System.IO.File.Exists(fileDetails.FilePath))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = $"Arquivo de imagem já existente ::: <br><br> {strSaveFileName}",
                        data = strSaveFileName,
                    });

                using (var stream = new FileStream(fileDetails.FilePath, FileMode.Create))
                {
                    await stream.WriteAsync(fileBytes);

                    stream.Flush();
                    stream.Close();
                }

                // Checa se o arquivo foi realmente salvo
                if (!System.IO.File.Exists(fileDetails.FilePath))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Arquivo ::: " + fileDetails.FilePath + " não foi salvo",
                        data = fileDetails.FilePath
                    });
            }
            else
            {
                // Initialize the Remote FTP
                using var ftpConn = new FtpClient(_ftpHost, _ftpUser, _ftpPass);

                ftpConn.Connect();

                //Verifica diretorio ja existe e cria se necessario 
                if (!ftpConn.DirectoryExists(strPathSaveFolder))
                    ftpConn.CreateDirectory(strPathSaveFolder, true);

                //Verifica arquivo ja existe
                if (ftpConn.FileExists(strPathSaveFile))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = $"Arquivo de imagem já existente ::: <br><br> {strSaveFileName}",
                        data = strSaveFileName,
                    });

                using (var imgStream = new MemoryStream(fileBytes))
                {
                    var uploadStatus = ftpConn.UploadStream(imgStream, fileDetails.FilePath, FtpRemoteExists.Overwrite);

                    if (uploadStatus != FtpStatus.Success)
                    {
                        ftpConn.Disconnect();

                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Arquivo de Imagem não foi Salvo",
                            data = strSaveFileName,
                        });
                    }
                        
                }

                ftpConn.Disconnect();
            }

            return Ok(new
            {
                bResult = true,
                type = "OK",
                message = "SUCESSO ::: ",
                data = strSaveFileName
            });
        }
        #endregion
    }
}