using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;

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

                var strLetraInicial = strTermoBusca.Trim()[0].ToString();

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
                        query = query.Where(x => x.CodigoAceca != null 
                                            && (bvariante 
                                                ? x.CodigoAceca.StartsWith(strTermoBusca.Trim().ToString()) 
                                                : x.Nome.StartsWith(strTermoBusca.Trim().ToString())
                                                )
                                            )


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

                    strCodigoAceca = lstmodel?.CodigoAceca?.ToString()?.Trim();
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

                var strUltimaLetraCodigoAceca = 'A';

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
                    data = strNovoCodigoAceca
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
                    vmModel.ImgPrincipal = string.Empty;
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
                    vmModel.ImgDetalhe = string.Empty;
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

                #endregion

                #region obj Marca

                var model = new Marcas
                {
                    Ativo = true,

                    MarcaDimensaoId = vmModel?.MarcaDimensaoId,
                    MarcaFabricaId = vmModel?.MarcaFabricaId,
                    MarcaFaseId = vmModel?.MarcaFaseId,
                    MarcaFinalidadeId = vmModel?.MarcaFinalidadeId,
                    MarcaImpressoraId = vmModel?.MarcaImpressoraId,
                    MarcaQualidadeImagemId = vmModel?.MarcaQualidadeImagemId,
                    MarcaRaridadeId = vmModel?.MarcaRaridadeId,
                    MarcaSubTipoId = vmModel?.MarcaSubTipoId,
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
            var fileSaveName = string.Concat(Guid.NewGuid(), "_", iFileImg?.FileName?.Trim()?.ToUpper(), !(bool)iFileImg?.FileName.Contains(fileExtension) ? fileExtension : String.Empty);

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
            /*
            //Cria Arquivo Temp e Upload
            var filePath = Path.Combine(destinationPath, newFileName);

            var filePath1 = Path.Combine(destinationPath, iFileImg?.FileName);

            using (var stream = new FileStream(Path.Combine(destinationPath, fileTempPath), FileMode.Create))
            {
                FormFile fileImgUpload = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name))
                {
                    Headers = new HeaderDictionary(),
                    ContentType = iFileImg?.ContentType
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
                data = fileSaveName,
            });
        }
    }
}