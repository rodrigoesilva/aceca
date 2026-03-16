using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;


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
                       NomeFase = x.MarcaFase.Descricao,
                       x.CodigoAceca,
                       ImgPrincipal = $"<img name=\"myImg\" class=\"td-img cmyImg\" src=\"{strUrlPath}/{x.MarcaFaseId}/{x.ImgPrincipal}\" alt=\"{x.CodigoAceca}\">",
                       ImgDetalhe = $"<img name=\"myImg\" class=\"td-img cmyImg\" src=\"{strUrlPath}/detalhes/{x.ImgDetalhe}\" alt=\"{x.CodigoAceca}\">",
                       NomeMarca = x.Nome,
                       x.MarcaFabricaId,
                       FabricaNome = x.MarcaFabrica.Nome,
                       x.Descricao,
                       x.IncluidoPor,
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
        [AllowAnonymous]
        public async Task<IActionResult> GetFullById(int id)
        {
            if (id < 1)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "GetFullById - Id deve ser maior que 0",
                    data = id
                });

            try
            {

                var result = from m in _db.Marca
                             join mfas in _db.MarcaFase on m.MarcaFaseId equals mfas.Id
                             join msub in _db.MarcaSubTipo on m.MarcaSubTipoId equals msub.Id
                             join mtip in _db.MarcaTipo on msub.MarcaTipoId equals mtip.Id
                             join mfin in _db.MarcaFinalidade on m.MarcaFinalidadeId equals mfin.Id
                             join mr in _db.MarcaRaridade on m.MarcaRaridadeId equals mr.Id
                             //join mfab in _db.MarcaFabrica on m.MarcaFabricaId equals mfab.Id
                             join mfab_temp in _db.MarcaFabrica on m.MarcaFabricaId equals mfab_temp.Id into grouping
                             from mfab in grouping.DefaultIfEmpty()
                             join mdim in _db.MarcaDimensao on m.MarcaDimensaoId equals mdim.Id
                             join mimp in _db.MarcaImpressora on m.MarcaImpressoraId equals mimp.Id
                             join mqua in _db.MarcaQualidadeImagem on m.MarcaQualidadeImagemId equals mqua.Id
                             where m.Id == id
                             select new
                             {
                                 Marca = m,
                                 MarcaFase = mfas,
                                 MarcaSubTipo = msub,
                                 MarcaTipo = mtip,
                                 MarcaFinalidade = mfin,
                                 MarcaRaridade = mr,
                                 MarcaFabrica = mfab,
                                 MarcaDimensao = mdim,
                                 MarcaImpressora = mimp,
                                 MarcaQualidadeImagem = mqua,
                             };

                var lstModel = await result.AsNoTracking().ToListAsync();

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
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = lstModel.FirstOrDefault()
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
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Post([FromForm] MarcaForm form)
        {
            var m = new Marcas();
            /*
            var m = new Marca {
                Nome         = forx.Nome,
                CodigoAceca  = forx.CodigoAceca,
                FaseId       = forx.FaseId,
                FabricaId    = forx.FabricaId,
                TipoId       = forx.TipoId,
                Descricao    = forx.Descricao,
                IncluidoPor  = forx.IncluidoPor,
            };
            x.ImagemUrl        = await SaveFile(forx.ImagemFile)        ?? x.ImagemUrl;
            x.ImagemDetalheUrl = await SaveFile(forx.ImagemDetalheFile) ?? x.ImagemDetalheUrl;
            db.Marcas.Add(m);
            */
            await _db.SaveChangesAsync();
            return Ok(m);
        }

        [HttpPut("{id}")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Put(int id, [FromForm] MarcaForm form)
        {
            var m = await _db.Marca.FindAsync(id);
            /*
            if (m == null) return NotFound();
            x.Nome        = forx.Nome;
            x.CodigoAceca = forx.CodigoAceca;
            x.FaseId      = forx.FaseId;
            x.FabricaId   = forx.FabricaId;
            x.TipoId      = forx.TipoId;
            x.Descricao   = forx.Descricao;
            x.IncluidoPor = forx.IncluidoPor;
            //x.AtualizadoEm = DateTime.UtcNow;
            if (forx.ImagemFile?.Length > 0)
                x.ImagemUrl = await SaveFile(forx.ImagemFile) ?? x.ImagemUrl;
            if (forx.ImagemDetalheFile?.Length > 0)
                x.ImagemDetalheUrl = await SaveFile(forx.ImagemDetalheFile) ?? x.ImagemDetalheUrl;
            */
            await _db.SaveChangesAsync();
            return Ok(m);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Marca.FindAsync(id);
            if (m == null) return NotFound();
            _db.Marca.Remove(m);
            await _db.SaveChangesAsync();
            return Ok();
        }

        private async Task<string?> SaveFile(IFormFile? file)
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


        [HttpPost]
        public async Task<IActionResult> FiltrarDados([FromBody] object obj)
        {
            if (obj != null && string.IsNullOrEmpty(obj.ToString()))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "FiltrarDados - Obj em branco"
                });

            try
            {
                var jObj = JObject.Parse(obj.ToString());

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


                var lstModelQuery = query
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
                        NomeFase = x.MarcaFase.Descricao,
                        x.CodigoAceca,
                        ImgPrincipal = $"{strUrlPath}/{x.MarcaFaseId}/{x.ImgPrincipal}\"",
                        ImgDetalhe = $"{strUrlPath}/detalhes/{x.ImgDetalhe}\"",
                        // Imagem = $"<img name=\"myImg\" class=\"td-img cmyImg\" src=\"{strUrlPath}/{x.MarcaFaseId}/{(x.Imagex.Contains(".") ? x.Imagem : x.Imagem + x.ExtImagem)}\" alt=\"{x.CodigoAceca}\">",
                        // ImagemDetalhe = $"<img name=\"myImg\" class=\"td-img cmyImg\" src=\"{strUrlPath}/detalhes/{(x.ImagemDetalhe.Contains(".") ? x.ImagemDetalhe : x.ImagemDetalhe + x.ExtImagemDetalhe)}\" alt=\"{x.CodigoAceca}\">",
                        NomeMarca = x.Nome,
                        x.MarcaFabricaId,
                        FabricaNome = x.MarcaFabrica.Nome,
                        x.Descricao,
                        x.IncluidoPor,
                    })
                    .AsQueryable();

                var lstModel = await lstModelQuery
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
    }

    public class MarcaForm
    {
        public string Nome { get; set; } = "";
        public string? CodigoAceca { get; set; }
        public int? FaseId { get; set; }
        public int? FabricaId { get; set; }
        public int? TipoId { get; set; }
        public string? Descricao { get; set; }
        public string? IncluidoPor { get; set; }
        public IFormFile? ImagemFile { get; set; }
        public IFormFile? ImagemDetalheFile { get; set; }
    }
}