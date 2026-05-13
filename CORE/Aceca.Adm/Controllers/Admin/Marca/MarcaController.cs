using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Dapper;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Reflection;
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

                        m.CodigoAceca,
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

        #region Filtros

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
                    ImgPrincipal = !string.IsNullOrEmpty(vmModel?.ImgPrincipal) ? vmModel?.ImgPrincipal : null,
                    ImgDetalhe = !string.IsNullOrEmpty(vmModel?.ImgDetalhe) ? vmModel?.ImgDetalhe : null,
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
        public async Task<IActionResult> GetNovoCodigoAceca(int idFase, string strNovoNomeParaCadastro, bool bvariante, bool bExTemPaisDestino)
        {
            string strNovoCodigoAceca = string.Empty;

            if (idFase < 1 || string.IsNullOrEmpty(strNovoNomeParaCadastro))
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

                var msgErroData = $"idMarcaFase :: {idFase} , strNovoNomeParaCadastro :: {strNovoNomeParaCadastro}";

                var strCodigoAceca = string.Empty;

                var strLetraInicial = strNovoNomeParaCadastro?.Trim()[0].ToString();

                var query = _db.Marca
                    .Include(x => x.MarcaSubTipo.MarcaTipo)
                    .Include(x => x.MarcaFabrica)
                    .Include(x => x.MarcaImpressora)
                    .Where(x => x.MarcaFaseId.Equals(idFase))
                    .OrderByDescending(x => x.CodigoAceca)
                    .Take(10);

                var queryExists = query.Any();

                //
                ///Fases que as marcas iniciam com letras
                ///
                if (idFase.Equals(14) // SA
                        || (idFase >= 27 && idFase <= 29) //27-Palheiros , 28 Fumos, 29 Exportacao
                        || (idFase >= 32 && idFase <= 34) //32-Cortadas, 33-Outros, 34-Quarentena
                        
                        || (idFase >= 39 && idFase <= 41) //39-Clandestinas, 40-Exterior, 41-M&C
                    )
                {
                    if (idFase != 29) // 29 Exportacao // 36 Comemorativas
                    {
                        query = query.Where(x => x.CodigoAceca != null
                                                && (bvariante
                                                    ? x.CodigoAceca.StartsWith(strNovoNomeParaCadastro.Trim().ToString())
                                                    : (x.CodigoAceca.StartsWith(strLetraInicial) && x.MarcaFaseId.Equals(idFase))
                                                    )
                                                )
                            .OrderByDescending(x => x.CodigoAceca);
                    }
                    else
                    {
                        // 29 Exportacao
                        //Se tem país de destino inicia com EA, Se não tem é EX (minusculos).

                        var strLetraInicialBusca = bExTemPaisDestino ? "EA" : "EX";

                        query = query.Where(x => x.CodigoAceca != null
                                                && x.CodigoAceca.StartsWith(strLetraInicialBusca.ToLower()) && x.MarcaFaseId.Equals(idFase)
                                                )
                            .OrderByDescending(x => x.CodigoAceca);
                    }

                    queryExistsTermo = query.Any();
                }
                else
                {
                    //|| idFase.Equals(36) // Comemorativas

                    query = query.Where(x => x.CodigoAceca != null
                                            && (bvariante
                                                ? x.CodigoAceca.StartsWith(strNovoNomeParaCadastro.Trim().ToString())
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
                            data = strNovoNomeParaCadastro
                        });
                    }

                    if (!queryExistsTermo && lstmodel == null)      
                    {
                        return Ok(new
                        {
                            bResult = false,
                            type = "ERRO - listagem Nula",
                            message = "Essa fase não possui esse código Pai",
                            data = strNovoNomeParaCadastro
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

                string strNumCodigoAceca = string.Empty;

                strNumCodigoAceca = idFase != 42 
                    ? new string(strCodigoAceca?.Where(char.IsDigit).ToArray())
                    :new string(strCodigoAceca?.Split("-")[1]?.Where(char.IsDigit).ToArray());

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

                        if (objImpressora != null)
                        {
                            lstmodel?.MarcaImpressora = new MarcaImpressora
                            {
                                Id = objImpressora?.Id,
                                Descricao = objImpressora?.Descricao
                            };

                            lstmodel?.MarcaImpressoraId = objImpressora?.Id;
                        }
                    }


                if (lstmodel?.MarcaFabricaId == null || lstmodel?.MarcaFabricaId <= 0)
                    if (!string.IsNullOrEmpty(lstmodel?.TxtFabrica))
                    {
                        var objFabrica = _db.MarcaFabrica
                            .Where(i => i.Nome.Equals(lstmodel.TxtFabrica.Trim()))
                            .FirstOrDefault();

                        if(objFabrica != null)
                        {
                            lstmodel?.MarcaFabrica = new MarcaFabrica
                            {
                                Id = objFabrica?.Id,
                                Nome = objFabrica?.Nome,
                                Descricao = objFabrica?.Descricao
                            };

                            lstmodel?.MarcaFabricaId = objFabrica?.Id;
                        }
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

           //Recupera Nome Original da imagem
            var fileImgOriginalName = string.Concat(iFileImg?.FileName?.Trim()?.ToLower(), (!(bool)iFileImg?.FileName.Contains(fileExtension) ? fileExtension : String.Empty));

            //Gera novo nome
            string strImgNomeBase ="aceca_"; //string.Empty; //
            string strImgDetalheNomeBase = "detalhe_";

            string strSaveFileName = $"{strImgNomeBase}{vmModel?.CodigoAceca?.Trim()?.ToLower()}";          
            
            fileExtension = (fileExtension.Equals(".jpg") ? fileExtension : ".jpg");

            // monta o caminho onde vamos salvar o arquivo:
            var strPathSaveFolder = string.Empty;
            var strPathSaveFile = string.Empty;

            if (_bIsLocalHost)
            {
                strPathSaveFolder = bIsImgPrincipal
                ? Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", vmModel?.MarcaFaseId?.ToString())
                : Path.Combine(_appEnvironment.WebRootPath, "midia", "geral", "detalhes");

                strSaveFileName = bIsImgPrincipal
                ? $"{strSaveFileName}{fileExtension}"
                : $"{strSaveFileName.Replace(strImgNomeBase, string.Concat(strImgNomeBase, strImgDetalheNomeBase))}{fileExtension}";

                strPathSaveFile = Path.Combine(strPathSaveFolder, strSaveFileName);
            }
            else
            {
                strPathSaveFolder = bIsImgPrincipal
                ? $"{_ftpBaseUrl}/midia/geral/{vmModel?.MarcaFaseId?.ToString()}"
                : $"{_ftpBaseUrl}/midia/geral/detalhes";


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

                var fileTempPath = Path.GetTempFileName();

                using (var stream = new FileStream(fileDetails.FilePath, FileMode.Create))
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