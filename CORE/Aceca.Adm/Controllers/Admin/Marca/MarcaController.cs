using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Aceca.Adm.Controllers.Admin.Marca
{
    public class MarcaController : Controller
    {
        #region variaveis

        private readonly ILogger<MarcaController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _imgBaseUrl = string.Empty;
        private readonly string _appBaseUrl = string.Empty;
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

        [AllowAnonymous]
        public ActionResult Index()
        {
            // return Redirect("https://www.google.com");
            return View("~/Views/Admin/Marca/Marca.cshtml");
        }

        [AllowAnonymous]
        public ActionResult Cadastro()
        {
            return View("~/Views/Admin/Marca/MarcaCadastro.cshtml");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ListGrid()
        {
            try
            {
                //_logger.LogInformation($"Recebida temperatura para conversao: {temperatura}");

                string strUrlPath = "https://www.aceca.cox.br/midia/geral";

                var lstModel = await _db.Marca
                       .Include(x => x.MarcaFase)
                       .Include(x => x.MarcaFabrica)
                       .Include(x => x.MarcaSubTipo)
                       .Include(x => x.MarcaSubTipo.MarcaTipo)
                   .OrderBy(x => x.MarcaFaseId)
                       .ThenBy(x => x.Nome)
                       .ThenBy(x => x.MarcaFabricaId)
                       .ThenBy(x => x.MarcaFabrica.Nome)
                       .ThenBy(x => x.Descricao)
                   .Select(x => new
                   {
                       x.Id,
                       IdMarcaFase = x.MarcaFaseId,
                       IdMarcaFinalidade = x.MarcaFinalidadeId,
                       IdMarcaFabrica = x.MarcaFabricaId,
                       IdMarcaDimensao = x.MarcaDimensaoId,
                       IdMarcaTipo = x.MarcaSubTipo.MarcaTipoId,
                       IdMarcaSubTipo = x.MarcaSubTipoId,
                       IdMarcaImpressora = x.MarcaFabricaId,
                       IdMarcaQualidadeImagem = x.MarcaImpressoraId,
                       IdMrcaFinalidade = x.MarcaFinalidadeId,

                       x.CodigoAceca,
                       NomeMarca = x.Nome,
                       NomeFase = x.MarcaFase.Descricao,
                       NomeFabrica = x.MarcaFabrica.Nome,
                       x.IncluidoPor,
                       x.Descricao,
                       x.Valor,
                       x.Valor1PI,
                       x.Valor2PI,
                       x.ImgPrincipal,
                       ImgPrincipalFull = $"<img name=\"myImg\" class=\"td-img cmyImg\" src=\"{strUrlPath}/{x.MarcaFaseId}/{x.ImgPrincipal}\" alt=\"{x.CodigoAceca}\">",
                       x.ImgDetalhe,
                       ImgDetalheFull = $"<img name=\"myImg\" class=\"td-img cmyImg\" src=\"{strUrlPath}/detalhes/{x.ImgDetalhe}\" alt=\"{x.CodigoAceca}\">",
                   })
                   .AsNoTracking()
                   .ToListAsync();

                if (lstModel.Count <= 0)
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
                var mensagemErro = $"ListGrid : {ex?.Message}";
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
                var jObj = JObject.Parse(obj?.ToString());

                var paramDynObj = new
                {
                    param_MarcaFaseId = jObj["param_MarcaFaseId"]?.ToObject<int>(),
                    param_MarcaFabricaId = jObj["param_MarcaFabricaId"]?.ToObject<int>(),
                    param_MarcaFabricaNome = jObj["param_MarcaFabricaNome"]?.ToObject<string>(),
                    param_MarcaTipoId = jObj["param_MarcaTipoId"]?.ToObject<int>(),
                    param_MarcaSubTipoId = jObj["param_MarcaSubTipoId"]?.ToObject<int>(),
                    param_IncluidoPor = jObj["param_IncluidoPor"]?.ToObject<string>(),
                    param_CodigoAceca = jObj["param_CodigoAceca"]?.ToObject<string>(),
                    param_NomeMarca = jObj["param_NomeMarca"]?.ToObject<string>(),
                    param_PesquisarDescricao = jObj["param_PesquisarDescricao"].ToObject<bool>(),
                };

                string strUrlPath = _imgBaseUrl;

                IQueryable<Marcas> query = _db.Marca;

                if (paramDynObj?.param_MarcaFaseId >= 0)
                    query = query.Where(p => p.MarcaFaseId.Equals(paramDynObj.param_MarcaFaseId));

                if (paramDynObj?.param_MarcaFabricaId >= 0)
                    query = query.Where(p => p.MarcaFabrica.Nome.Equals(paramDynObj.param_MarcaFabricaNome));
                // query = query.Where(p => p.FabricaId.Equals(paramDynObj.param_MarcaFabricaId));
                /*
                if (paramDynObj?.param_MarcaTipoId >= 0)
                    query = query.Where(p => p.SubTipoId.Equals(paramDynObj.param_MarcaTipoId));
                */
                if (paramDynObj?.param_MarcaSubTipoId >= 0)
                    query = query.Where(p => p.MarcaSubTipoId.Equals(paramDynObj.param_MarcaSubTipoId));

                if (!string.IsNullOrWhiteSpace(paramDynObj?.param_IncluidoPor))
                    query = query.Where(p => p.IncluidoPor.Contains(paramDynObj.param_IncluidoPor));

                if (!string.IsNullOrWhiteSpace(paramDynObj?.param_CodigoAceca))
                    query = query.Where(p => p.CodigoAceca.Contains(paramDynObj.param_CodigoAceca));

                if (!string.IsNullOrWhiteSpace(paramDynObj?.param_NomeMarca))
                    query = paramDynObj.param_PesquisarDescricao
                    ? query.Where(p => p.Nome.Contains(paramDynObj.param_NomeMarca) || p.Descricao.Contains(paramDynObj.param_NomeMarca))
                    : query.Where(p => p.Nome.Contains(paramDynObj.param_NomeMarca));


                var lstModel = await query
                    .AsNoTracking()
                    .Include(x => x.MarcaFase)
                    .Include(x => x.MarcaFabrica)
                    .Include(x => x.MarcaSubTipo)
                    .Include(x => x.MarcaSubTipo.MarcaTipo)
                    .OrderBy(x => x.MarcaFaseId)
                       .ThenBy(x => x.Nome)
                       .ThenBy(x => x.MarcaFabricaId)
                       .ThenBy(x => x.MarcaFabrica.Nome)
                       .ThenBy(x => x.Descricao)
                    .Select(x => new
                    {
                        x.Id,
                        IdMarcaFase = x.MarcaFaseId,
                        IdMarcaFinalidade = x.MarcaFinalidadeId,
                        IdMarcaFabrica = x.MarcaFabricaId,
                        IdMarcaDimensao = x.MarcaDimensaoId,
                        IdMarcaTipo = x.MarcaSubTipo.MarcaTipoId,
                        IdMarcaSubTipo = x.MarcaSubTipoId,
                        IdMarcaImpressora = x.MarcaFabricaId,
                        IdMarcaQualidadeImagem = x.MarcaImpressoraId,
                        IdMrcaFinalidade = x.MarcaFinalidadeId,

                        x.CodigoAceca,
                        NomeMarca = x.Nome,
                        NomeFase = x.MarcaFase.Descricao,
                        NomeFabrica = x.MarcaFabrica.Nome,
                        x.IncluidoPor,
                        x.Descricao,
                        x.Valor,
                        x.Valor1PI,
                        x.Valor2PI,
                        x.ImgPrincipal,
                        ImgPrincipalFull = $"{strUrlPath}/{x.MarcaFaseId}/{x.ImgPrincipal}\"",
                        x.ImgDetalhe,
                        ImgDetalheFull = $"{strUrlPath}/detalhes/{x.ImgDetalhe}\"",
                        SubTipo = x.MarcaSubTipo.Descricao,
                        Tipo = x.MarcaSubTipo.MarcaTipo.Descricao
                    })
                    .AsQueryable()
                    .ToListAsync();

                if (lstModel.Count <= 0)
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
                var mensagemErro = $"ListGrid : {ex?.Message}";
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
        [AllowAnonymous]
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

                var lstmodel = await query
                    .AsNoTracking()
                    .AsQueryable()
                    //.ToListAsync()
                    .FirstOrDefaultAsync();

                if (lstmodel == null)
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
                var strCodigoAceca = lstmodel?.CodigoAceca?.ToString();

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
                var mensagemErro = $"ListGrid : {ex?.Message}";
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
        [AllowAnonymous]
        public async Task<IActionResult> GetCodigoAceca(int idFase, string nome, bool bvariante)
        {
            string strNovoCodigoAceca = string.Empty;

            if (idFase < 1 || string.IsNullOrEmpty(nome))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "GetCodigoAceca - Id deve ser maior que 0",
                    data = idFase
                });

            try
            {
                var msgErroData = $"idMarcaFase :: {idFase} , NomeMarca :: {nome}";

                var strCodigoAceca = string.Empty;

                var strLetraInicial = nome.Trim()[0].ToString();

                var query = _db.Marca.Where(x => x.MarcaFaseId.Equals(idFase));

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

                    query = query.Where(x => x.CodigoAceca != null && x.Nome.StartsWith(nome.Trim().ToString()))
                   //query = query.Where(x => x.CodigoAceca != null && x.Nome.Contains(nome.Trim().ToString()))
                   //query = query.Where(x => x.CodigoAceca != null && x.CodigoAceca.StartsWith(nome.Trim()[0].ToString()))

                   .OrderByDescending(x => x.CodigoAceca);

                    var lstmodel = await query
                        .AsNoTracking()
                        .AsQueryable()
                        //.ToListAsync()
                        .FirstOrDefaultAsync()
                        ;

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

                    //var strCodigoAceca = lstmodel?.OrderByDescending(c => c.CodigoAceca)?.FirstOrDefault()?.CodigoAceca?.ToString();
                    //strCodigoAceca = lstmodel?.FirstOrDefault()?.CodigoAceca?.ToString();

                    strCodigoAceca = lstmodel?.CodigoAceca?.ToString();
                }

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
                var mensagemErro = $"ListGrid : {ex?.Message}";
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
        public async Task<IActionResult> Create1([FromBody] object obj)
        {
            try
            {
                if (string.IsNullOrEmpty(obj?.ToString()))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Model Inválida",
                        data = obj,
                    });

                #region Marca

                var jObj = JObject.Parse(obj?.ToString());

                var newModel = new Marcas
                {
                    Ativo = true,

                    MarcaDimensaoId = jObj["cmbPop_MarcaDimensao"].ToObject<int>(),
                    MarcaFabricaId = jObj["cmbPop_MarcaFabrica"].ToObject<int>(),
                    MarcaFaseId = jObj["cmbPop_MarcaFase"].ToObject<int>(),
                    MarcaFinalidadeId = jObj["cmbPop_MarcaFinalidade"].ToObject<int>(),
                    MarcaImpressoraId = jObj["cmbPop_MarcaImpressora"].ToObject<int>(),
                    MarcaQualidadeImagemId = jObj["cmbPop_MarcaQualidadeImagem"].ToObject<int>(),
                    //MarcaRaridadeId = jObj["MarcaRaridadeId"].ToObject<int>(),
                    MarcaSubTipoId = jObj["cmbPop_MarcaSubTipo"].ToObject<int>(),
                    CodigoAceca = jObj["txt_Codigo"]?.ToObject<string>()?.Trim(),
                    //CodigoSC = jObj["CodigoSC"]?.ToObject<string>()?.Trim(),
                    ImgPrincipal = !string.IsNullOrEmpty(jObj["File"]?.ToString()) ? jObj["File"]?["name"]?.ToObject<string>()?.Trim() : null,
                    ImgDetalhe = !string.IsNullOrEmpty(jObj["File"]?.ToString()) ? jObj["File"]?["name"]?.ToObject<string>()?.Trim() : null,
                    Nome = jObj["txt_Nome"]?.ToObject<string>()?.Trim(),
                    Descricao = jObj["txt_Descricao"]?.ToObject<string>()?.Trim(),
                    Valor1PI = jObj["txt_Valor1PI"]?.ToObject<string>()?.Trim(),
                    Valor2PI = jObj["txt_Valor2PI"]?.ToObject<string>()?.Trim(),
                    Valor = jObj["txt_Valor"]?.ToObject<string>()?.Trim(),
                    IncluidoPor = jObj["txt_IncluidoPor"]?.ToObject<string>()?.Trim(),
                    //EmQuarentena = !string.IsNullOrEmpty(formCollection["EmQuarentena"]) ? jObj["EmQuarentena"]) : 0,
                };

                if (string.IsNullOrEmpty(newModel.Nome))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Nome deve ser preenchido"
                    });

                //Verifica se existe imagem para upload
                var objFile = jObj["File"]?.ToString();

                if (!string.IsNullOrEmpty(objFile?.ToString()))
                {
                    var result = await UploadImg(objFile, jObj);

                    if (result.GetType() == typeof(NotFoundObjectResult) ||
                        result.GetType() == typeof(BadRequestObjectResult))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = result?.ToString()
                        });
                }

                _db.Marca.Add(newModel);
                _db.SaveChanges();

                if (newModel?.Id <= 0)
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Falha ao Cadastrar Marca"
                    });

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
                    data = obj,
                });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ListGrid : {ex?.Message}";
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
        public ActionResult Create2(FormCollection formCollection)
        {
            try
            {
                #region Marca

                if (formCollection == null)
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "FormCollection inválido"
                    });

                if (formCollection.TryGetValue("txt_Nome", out var value) && !string.IsNullOrEmpty(value))
                {

                    var newModel = new Marcas
                    {
                        Ativo = true,

                        MarcaDimensaoId = Convert.ToInt16(formCollection["cmbPop_MarcaDimensao"]),
                        MarcaFabricaId = Convert.ToInt16(formCollection["cmbPop_MarcaFabrica"]),
                        MarcaFaseId = Convert.ToInt16(formCollection["cmbPop_MarcaFase"]),
                        MarcaFinalidadeId = Convert.ToInt16(formCollection["cmbPop_MarcaFinalidade"]),
                        MarcaImpressoraId = Convert.ToInt16(formCollection["cmbPop_MarcaImpressora"]),
                        MarcaQualidadeImagemId = Convert.ToInt16(formCollection["cmbPop_MarcaQualidadeImagem"]),
                        //MarcaRaridadeId = Convert.ToInt16(formCollection["MarcaRaridadeId"]),
                        MarcaSubTipoId = Convert.ToInt16(formCollection["cmbPop_MarcaSubTipo"]),
                        CodigoAceca = formCollection["txt_Codigo"],
                        //CodigoSC = formCollection["CodigoSC"],
                        ImgPrincipal = formCollection["txt_ImgPrincipal"],
                        ImgDetalhe = formCollection["txt_ImgDetalhe"],
                        Nome = formCollection["txt_Nome"],
                        Descricao = formCollection["txt_Descricao"],
                        Valor1PI = formCollection["txt_Valor1PI"],
                        Valor2PI = formCollection["txt_Valor2PI"],
                        Valor = formCollection["txt_Valor"],
                        IncluidoPor = formCollection["txt_IncluidoPor"],
                        //EmQuarentena = !string.IsNullOrEmpty(formCollection["EmQuarentena"]) ? Convert.ToInt16(formCollection["EmQuarentena"]) : 0,
                    };

                    _db.Marca.Add(newModel);
                    _db.SaveChanges();

                    if (newModel?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Cadastrar Marca"
                        });

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
                        data = newModel,
                    });
                }

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Nome deve ser preenchido"
                });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ListGrid : {ex?.Message}";
                _logger.LogError(mensagemErro);

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = mensagemErro
                });
            }
        }
        public async Task<string?> SaveFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var safe = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!safe.Contains(ext)) return null;
            var dir = Path.Combine(_appEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(dir);
            var name = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(dir, name);
            await using var fs = System.IO.File.Create(path);
            await file.CopyToAsync(fs);
            return $"/uploads/{name}";
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

        public async Task<IActionResult> UploadImg(object objFile, JObject jObj)
        {
            var objFileImg = new
            {
                lastModified = !string.IsNullOrEmpty(objFile?.ToString()) ? jObj["File"]?["lastModified"].ToObject<string>() : String.Empty,
                lastModifiedDate = !string.IsNullOrEmpty(objFile?.ToString()) ? jObj["File"]?["lastModifiedDate"]?.ToObject<string>() : String.Empty,
                name = !string.IsNullOrEmpty(objFile?.ToString()) ? jObj["File"]?["name"]?.ToObject<string>() : null,
                sourceName = !string.IsNullOrEmpty(objFile?.ToString()) ? jObj["Logotipo"]?.ToObject<string>() : null,
                size = !string.IsNullOrEmpty(objFile?.ToString()) ? jObj["File"]?["size"]?.ToObject<string>() : String.Empty,
                type = !string.IsNullOrEmpty(objFile?.ToString()) ? jObj["File"]?["type"]?.ToObject<string>() : String.Empty,
                Campo = !string.IsNullOrEmpty(objFile?.ToString()) ? jObj["Campo"]?.ToObject<string>() : String.Empty,
                Pasta = !string.IsNullOrEmpty(objFile?.ToString()) ? jObj["Pasta"]?.ToObject<string>() : String.Empty,
                SubPasta = !string.IsNullOrEmpty(objFile?.ToString()) ? jObj["SubPasta"]?.ToObject<string>() : String.Empty,
            };

            if (string.IsNullOrEmpty(objFileImg.name) || objFileImg.name == null || objFileImg?.name?.Length == 0)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Arquivo nulo ou invalido"
                });

            string fileExtension = Path.GetExtension(objFileImg?.name?.ToString()).ToLowerInvariant();

            if (string.IsNullOrEmpty(fileExtension))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Arquivo com extensão invalida"
                });

            //Gera novo nome
            var newFileName = string.Concat(Guid.NewGuid(), "_", objFileImg?.name, !(bool)objFileImg?.name.Contains(fileExtension) ? fileExtension : String.Empty);

            var fileSize = objFileImg?.size;
            var fileTempPath = Path.GetTempFileName();

            //< obtém o caminho físico da pasta wwwroot >
            var rootPath = _appEnvironment.ContentRootPath;

            // monta o caminho onde vamos salvar o arquivo : 
            // ~\wwwroot\Arquivos\Exibidor\Logo
            var destinationPath = Path.Combine(rootPath, "Arquivos", objFileImg?.Pasta, objFileImg?.SubPasta);

            //Verifica diretorio existe e cria se necessario 
            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            //Cria Arquivo Temp e Upload
            var filePath = Path.Combine(destinationPath, newFileName);

            var filePath1 = Path.Combine(destinationPath, objFileImg?.name);

            using (var stream = new FileStream(Path.Combine(destinationPath, fileTempPath), FileMode.Create))
            {
                FormFile fileImgUpload = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name))
                {
                    Headers = new HeaderDictionary(),
                    ContentType = objFileImg.type
                };

                stream.Position = 0;

                await fileImgUpload.CopyToAsync(stream);

                var fi = new FileInfo(fileTempPath);

                // Check if the file exists
                if (!fi.Exists)
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Arquivo Temporario ::: " + fileTempPath + " inexistente"
                    });

                stream.Flush();
                stream.Close();
            }

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
                data = newFileName,
            });
        }
    }
}