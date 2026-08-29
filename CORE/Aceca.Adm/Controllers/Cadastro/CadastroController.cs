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
        public ActionResult CadastroAcervo()
        {
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
                        m.Nome AS NomeMarca,
                        ma.Descricao AS NomeAcervo,
                        mf.Descricao AS NomeFase,
                        mt.Descricao AS Tipo,
                        mst.Descricao AS SubTipo,
                        m.Descricao,
                        m.StatusCadastro,
                        m.Observacao,
                        sCriou.Nome AS CriadoPorNome,
                        sAprovou.Nome AS AprovadoPorNome,
                        m.dataCriacao AS DataCriacao,

                        m.ImgPrincipal,
                        IF(m.ImgPrincipal IS NOT NULL,
                            CONCAT(@ImgBase,'/',m.MarcaFaseId,'/',m.ImgPrincipal),
                            @ImgDefault) AS ImgPrincipalFull,

                        m.ImgDetalhe,
                        IF(m.ImgDetalhe IS NOT NULL,
                            CONCAT(@ImgBase,'/',m.MarcaFaseId,'/detalhes/',m.ImgDetalhe),
                            @ImgDefault) AS ImgDetalheFull

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

                #region Upload Imagem

                #region Upload Imagem ImgPrincipal

                string strImgPrincipalSaveName = null;

                if (iFileImgPrincipal != null)
                {
                    if (!vmModel.ImgPrincipal.Equals("C:\\fakepath\\."))
                    {
                        var result = await UploadImg(vmModel, iFileImgPrincipal, true);

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
                        var result = await UploadImg(vmModel, iFileImgDetalhe, false);

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

                #region obj MarcaCadastro

                // 1. Convert to Title Case
                TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;

                // Etapa intermediária de aprovação: o cadastro não entra mais direto em
                // `marcas` - fica em `marcas_cadastro` com StatusCadastro=Pendente até um
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

                    //
                    TxtFabrica = !string.IsNullOrEmpty(vmModel?.MarcaFabrica?.Nome) ? vmModel?.MarcaFabrica?.Nome?.Trim() : null,
                    TxtImpressora = !string.IsNullOrEmpty(vmModel?.MarcaImpressora?.Descricao) ? vmModel?.MarcaImpressora?.Descricao?.Trim() : null,
                };

                #endregion

                _db.MarcaCadastro.Add(model);
                await _db.SaveChangesAsync();

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
        [Authorize(Roles = "Administracao")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Models.MarcaCadastro model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    #region MarcaCadastro

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

                    model?.MarcaAcervoId = (model?.MarcaAcervoId < 0 || model?.MarcaAcervoId == null) ? 0 : model?.MarcaAcervoId;
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
                    await _db.SaveChangesAsync();

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
        [Authorize(Roles = "Administracao")]
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
                ".gif" => header[0] == (byte)'G' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'8',
                ".webp" => read >= 12
                    && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
                    && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P',
                _ => false
            };
        }

        [Authorize(Roles = "Administracao")]
        public async Task<IActionResult> UploadImg(VMMarca vmModel, IFormFile iFileImg, bool bIsImgPrincipal)
        {
            if (string.IsNullOrEmpty(iFileImg.FileName) || iFileImg?.FileName == null || iFileImg?.FileName.Length == 0)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Arquivo de Imagem Nulo ou Invalido",
                    data = iFileImg?.FileName
                });

            string fileExtension = Path.GetExtension(iFileImg?.FileName?.ToString())?.ToLowerInvariant();

            var fileExtensionValid = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

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

            if (_bIsLocalHost)
            {
                strPathSaveFolder = bIsImgPrincipal
                ? Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", vmModel?.MarcaFaseId?.ToString())
                : Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", vmModel?.MarcaFaseId?.ToString(),"detalhes");

                strSaveFileName = bIsImgPrincipal
                ? $"{strSaveFileName}{fileExtension}"
                : $"{strSaveFileName.Replace(strImgNomeBase, string.Concat(strImgNomeBase, strImgDetalheNomeBase))}{fileExtension}";

                strPathSaveFile = Path.Combine(strPathSaveFolder, strSaveFileName);
            }
            else
            {
                strPathSaveFolder = bIsImgPrincipal
                ? $"{_ftpBaseUrl}/midia/geral/{vmModel?.MarcaFaseId?.ToString()}"
                : $"{_ftpBaseUrl}/midia/geral/{vmModel?.MarcaFaseId?.ToString()}/detalhes";


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
                    await iFileImg.CopyToAsync(stream);

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

                using (var imgStream = iFileImg?.OpenReadStream())
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