using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

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

        private readonly string _imgBaseUrl = string.Empty;
        private readonly string _appBaseUrl = string.Empty;

        private string _strControllerName = string.Empty;
        private string _strActionName = string.Empty;
        //

        #endregion

        public MarcaController(ILogger<MarcaController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;

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
        public async Task<IActionResult> FiltrarDados([FromBody] object obj)
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
                    sb.Append(" AND m.param_MarcaFabricaId = " + dynObj?.param_MarcaFabricaId); //.Where(p => p.MarcaFabrica.Nome.Equals(paramDynObj.param_MarcaFabricaNome)).ToList();

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
                        sb.Append(" AND m.Nome like '%" + dynObj?.param_NomeMarca.Trim() + "%' OR m.Descricao like '%" + dynObj?.param_NomeMarca.Trim() + "%'");
                    else
                        sb.Append(" AND m.Nome like '%" + dynObj?.param_NomeMarca.Trim() + "%'");
                }

                sb.Append(" ORDER BY");
                sb.Append(" m.marcaFaseId, m.nome, m.descricao, mst.marcaTipoId, m.marcaSubTipoId, m.codigoAceca ;");

                string query = sb.ToString();

                //var queryResult = await _db.Set<VMMarcaList>().FromSqlRaw($"{query}").ToListAsync();


                //var result = await _db.Marca.FromSqlRaw(query).ToListAsync();

                //var result = await _db.Database.ExecuteSqlRawAsync(query);
                //var result = await _db.Database.SqlQuery<Marcas>(FormattableStringFactory.Create(query)).Take(10).ToListAsync();

                var lstModel = await _db.Database.SqlQuery<VMMarcaList>(FormattableStringFactory.Create(query))
                    //.Take(10)
                    .ToListAsync();
               
                //var lstModel = JsonConvert.DeserializeObject<List<VMMarca>>(JsonConvert.SerializeObject(result));

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

                /*
               if (paramDynObj?.param_MarcaTipoId >= 0)
                   lstModel = lstModel.Where(p => p.IdMarcaTipo == 1);
               */
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

                    CodigoAceca = !string.IsNullOrEmpty(vmModel?.CodigoAceca) ? vmModel?.CodigoAceca : string.Empty,
                    CodigoSC = !string.IsNullOrEmpty(vmModel?.CodigoSC) ? vmModel?.CodigoSC : null,
                    ImgPrincipal = !string.IsNullOrEmpty(vmModel?.ImgPrincipal) ? Path.GetFileName(vmModel?.ImgPrincipal) : string.Empty,
                    ImgDetalhe = !string.IsNullOrEmpty(vmModel?.ImgDetalhe) ? Path.GetFileName(vmModel?.ImgDetalhe) : string.Empty,
                    Nome = !string.IsNullOrEmpty(vmModel?.Nome) ? vmModel?.Nome : string.Empty,
                    Descricao = !string.IsNullOrEmpty(vmModel?.Descricao) ? vmModel?.Descricao : string.Empty,
                    Valor1PI = !string.IsNullOrEmpty(vmModel?.Valor1PI) ? vmModel?.Valor1PI : null,
                    Valor2PI = !string.IsNullOrEmpty(vmModel?.Valor2PI) ? vmModel?.Valor2PI : null,
                    Valor = !string.IsNullOrEmpty(vmModel?.Valor) ? vmModel?.Valor : null,
                    IncluidoPor = !string.IsNullOrEmpty(vmModel?.IncluidoPor) ? vmModel?.IncluidoPor : string.Empty,
                    EmQuarentena = !string.IsNullOrEmpty(vmModel?.EmQuarentena?.ToString()) ? vmModel?.EmQuarentena : 0,
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
                var msgErroData = $"idMarcaFase :: {idFase} , strTermoBusca :: {strTermoBusca}";

                var strCodigoAceca = string.Empty;

                var strLetraInicial = strTermoBusca?.Trim()[0].ToString();

                var query = _db.Marca
                    .Include(x => x.MarcaSubTipo.MarcaTipo)
                    .Where(x => x.MarcaFaseId.Equals(idFase));


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
                }

                var lstmodel = await query
                            .AsNoTracking()
                            .AsQueryable()
                            .FirstOrDefaultAsync();

                if (bvariante && lstmodel == null)
                {
                    return Ok(new
                    {
                        bResult = false,
                        type = "ERRO - Variante Pai Inexistente - lstModel",
                        message = "listagem Nula",
                        data = strTermoBusca
                    });
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