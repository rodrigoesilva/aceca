using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Data;


namespace Aceca.Adm.Controllers
{
    public class MarcasController : Controller
    {
        #region variaveis

        private readonly ILogger<MarcasController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _imgBaseUrl = string.Empty;
        private readonly string _appBaseUrl = string.Empty;
        //

        #endregion

        public MarcasController(ILogger<MarcasController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;

            _imgBaseUrl = _appConfiguration["Url:Img"]!;
            _appBaseUrl = _appConfiguration["App:Url"]!;
        }


        //[AllowAnonymous]
        public ActionResult Index()
        {
            // return Redirect("https://www.google.com");
            return View("~/Views/Marca/MarcaList.cshtml");
        }

        //[HttpGet("GetMaterials")]
        //public async Task<IActionResult> GetProductsAsync()
        [Authorize(Policy = "RequireAdministratorRole1")]
        public ActionResult GetMaterials()
        {
            return Redirect("https://www.uol.com.br");
        }

       
        //[HttpPost("CreateMaterial")]
        //public async Task<IActionResult> CreateProductAsync()
        public ActionResult CreateMaterial()
        {
            return Redirect("https://www.globo.com");
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                /*var list = await db.Marcas.Take(10).ToListAsync();*/

                string strUrlPath = "https://www.aceca.com.br/midia/geral";

                string strUrlImg = "https://www.aceca.com.br/midia/geral/10/pre0752_1.jpg";
                string strUrlImgDetalhe = "https://www.aceca.com.br/midia/geral/detalhes/dpre0752.jpg";

                //$varUrlImg = "".$urlBase.'/midia/geral/'.$linha['faseId'].'/'.$imagem."";
                //$varUrlImgDetalhe = "".$urlBase.'/midia/geral/detalhes/'.$detalhe."";

                var lstModel = await _db.Marcas
                       .Include(m => m.MarcaFase)
                       //.Include(f => f.Pais).Select(f => new { f.Id, f.Nome, f.Cidade, f.PaisId, PaisNome = f.Pais != null ? f.Pais.Nome : null }
                       .Include(m => m.MarcaFabrica)
                       .Include(m => m.MarcaSubTipo)
                       .Include(m => m.MarcaTipo)
                   .OrderBy(m => m.MarcaFaseId)
                       .ThenBy(m => m.NomeMarca)
                       .ThenBy(m => m.FabricaId)
                       .ThenBy(m => m.FabricaDesc)
                       .ThenBy(m => m.Descricao)
                   .Select(m => new
                   {
                       m.Id,
                       NomeFase = m.MarcaFase.Abre,
                       m.CodigoAceca,
                       Imagem = $"{strUrlPath}/{m.MarcaFaseId}/{(m.Imagem.Contains(".") ? m.Imagem : m.Imagem + m.ExtImagem)}",
                       ImagemDetalhe = $"{strUrlPath}/detalhes/{(m.ImagemDetalhe.Contains(".") ? m.ImagemDetalhe : m.ImagemDetalhe + m.ExtImagemDetalhe)}",
                       m.NomeMarca,
                       m.FabricaId,
                       FabricaNome = m.FabricaDesc != null ? m.FabricaDesc : null,
                       m.Descricao,
                       m.IncluidoPor,

                       /*
                       m.ImagemDetalheUrl,
                       m.FaseId,    FaseNome    = m.Fase    != null ? m.Fase.Nome    : null,

                       m.TipoId,    TipoNome    = m.Tipo    != null ? m.Tipo.Nome    : null,
                       m.CriadoEm, m.AtualizadoEm
                       */
                   })
                   .Take(100)
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
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = lstModel,
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = ex?.Message
                });
            }

        }

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {
                //_logger.LogInformation($"Recebida temperatura para conversao: {temperatura}");

                string strUrlPath = "https://www.aceca.com.br/midia/geral";

                var lstModel = await _db.Marcas
                       .Include(m => m.MarcaFase)
                       .Include(m => m.MarcaFabrica)
                       .Include(m => m.MarcaSubTipo)
                       .Include(m => m.MarcaTipo)
                   .OrderBy(m => m.MarcaFaseId)
                       .ThenBy(m => m.NomeMarca)
                       .ThenBy(m => m.FabricaId)
                       .ThenBy(m => m.FabricaDesc)
                       .ThenBy(m => m.Descricao)
                   .Select(m => new
                   {
                       m.Id,
                       NomeFase = m.MarcaFase.Abre,
                       m.CodigoAceca,
                       Imagem = $"<img name=\"myImg\" class=\"td-img cmyImg\" src=\"{strUrlPath}/{m.MarcaFaseId}/{(m.Imagem.Contains(".") ? m.Imagem : m.Imagem + m.ExtImagem)}\" alt=\"{m.CodigoAceca}\">",
                       ImagemDetalhe = $"<img name=\"myImg\" class=\"td-img cmyImg\" src=\"{strUrlPath}/detalhes/{(m.ImagemDetalhe.Contains(".") ? m.ImagemDetalhe : m.ImagemDetalhe + m.ExtImagemDetalhe)}\" alt=\"{m.CodigoAceca}\">",
                       m.NomeMarca,
                       m.FabricaId,
                       FabricaNome = m.FabricaDesc != null ? m.FabricaDesc : null,
                       m.Descricao,
                       m.IncluidoPor,
                   })
                   .Take(110)
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

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var m = new Marca();

            //var m = await _db.Marcas.Include(x=>x.Fase).Include(x=>x.Fabrica).Include(x=>x.Tipo).FirstOrDefaultAsync(x=>x.Id==id);

            return m == null ? NotFound() : Ok(m);
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Post([FromForm] MarcaForm form)
        {
            var m = new Marca();
            /*
            var m = new Marca {
                Nome         = form.Nome,
                CodigoAceca  = form.CodigoAceca,
                FaseId       = form.FaseId,
                FabricaId    = form.FabricaId,
                TipoId       = form.TipoId,
                Descricao    = form.Descricao,
                IncluidoPor  = form.IncluidoPor,
            };
            m.ImagemUrl        = await SaveFile(form.ImagemFile)        ?? m.ImagemUrl;
            m.ImagemDetalheUrl = await SaveFile(form.ImagemDetalheFile) ?? m.ImagemDetalheUrl;
            db.Marcas.Add(m);
            */
            await _db.SaveChangesAsync();
            return Ok(m);
        }

        [HttpPut("{id}")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Put(int id, [FromForm] MarcaForm form)
        {
            var m = await _db.Marcas.FindAsync(id);
            /*
            if (m == null) return NotFound();
            m.Nome        = form.Nome;
            m.CodigoAceca = form.CodigoAceca;
            m.FaseId      = form.FaseId;
            m.FabricaId   = form.FabricaId;
            m.TipoId      = form.TipoId;
            m.Descricao   = form.Descricao;
            m.IncluidoPor = form.IncluidoPor;
            //m.AtualizadoEm = DateTime.UtcNow;
            if (form.ImagemFile?.Length > 0)
                m.ImagemUrl = await SaveFile(form.ImagemFile) ?? m.ImagemUrl;
            if (form.ImagemDetalheFile?.Length > 0)
                m.ImagemDetalheUrl = await SaveFile(form.ImagemDetalheFile) ?? m.ImagemDetalheUrl;
            */
            await _db.SaveChangesAsync();
            return Ok(m);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Marcas.FindAsync(id);
            if (m == null) return NotFound();
            _db.Marcas.Remove(m);
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

                IQueryable<Marca> query = _db.Marcas;

                if (paramDynObj?.param_MarcaFaseId >= 0)
                    query = query.Where(p => p.MarcaFaseId.Equals(paramDynObj.param_MarcaFaseId));

                if (paramDynObj?.param_MarcaFabricaId >= 0)
                    query = query.Where(p => p.FabricaDesc.Equals(paramDynObj.param_MarcaFabricaNome));
                // query = query.Where(p => p.FabricaId.Equals(paramDynObj.param_MarcaFabricaId));
                /*
                if (paramDynObj?.param_MarcaTipoId >= 0)
                    query = query.Where(p => p.SubTipoId.Equals(paramDynObj.param_MarcaTipoId));
                */
                if (paramDynObj?.param_MarcaSubTipoId >= 0)
                    query = query.Where(p => p.SubTipoId.Equals(paramDynObj.param_MarcaSubTipoId));

                if (!string.IsNullOrWhiteSpace(paramDynObj?.param_IncluidoPor))
                    query = query.Where(p => p.IncluidoPor.Contains(paramDynObj.param_IncluidoPor));

                if (!string.IsNullOrWhiteSpace(paramDynObj?.param_CodigoAceca))
                    query = query.Where(p => p.CodigoAceca.Contains(paramDynObj.param_CodigoAceca));

                if (!string.IsNullOrWhiteSpace(paramDynObj?.param_NomeMarca))
                        query = paramDynObj.param_PesquisarDescricao 
                        ? query.Where(p => p.NomeMarca.Contains(paramDynObj.param_NomeMarca) || p.Descricao.Contains(paramDynObj.param_NomeMarca)) 
                        : query.Where(p => p.NomeMarca.Contains(paramDynObj.param_NomeMarca));


                var lstModelQuery = query
                    .Include(m => m.MarcaFase)
                    .Include(m => m.MarcaFabrica)
                    .Include(m => m.MarcaSubTipo)
                    .Include(m => m.MarcaTipo)
                    .OrderBy(m => m.MarcaFaseId)
                       .ThenBy(m => m.NomeMarca)
                       .ThenBy(m => m.FabricaId)
                       .ThenBy(m => m.FabricaDesc)
                       .ThenBy(m => m.Descricao)
                    .Select(m => new
                    {
                        m.Id,
                        NomeFase = m.MarcaFase.Abre,
                        m.CodigoAceca,
                        Imagem = $"{strUrlPath}/{m.MarcaFaseId}/{(m.Imagem.Contains(".") ? m.Imagem : m.Imagem + m.ExtImagem)}\"",
                        ImagemDetalhe = $"{strUrlPath}/detalhes/{(m.ImagemDetalhe.Contains(".") ? m.ImagemDetalhe : m.ImagemDetalhe + m.ExtImagemDetalhe)}\"",
                        // Imagem = $"<img name=\"myImg\" class=\"td-img cmyImg\" src=\"{strUrlPath}/{m.MarcaFaseId}/{(m.Imagem.Contains(".") ? m.Imagem : m.Imagem + m.ExtImagem)}\" alt=\"{m.CodigoAceca}\">",
                        // ImagemDetalhe = $"<img name=\"myImg\" class=\"td-img cmyImg\" src=\"{strUrlPath}/detalhes/{(m.ImagemDetalhe.Contains(".") ? m.ImagemDetalhe : m.ImagemDetalhe + m.ExtImagemDetalhe)}\" alt=\"{m.CodigoAceca}\">",
                        m.NomeMarca,
                        m.FabricaId,
                        FabricaNome = m.FabricaDesc != null ? m.FabricaDesc : null,
                        m.Descricao,
                        m.IncluidoPor,
                    })
                    .AsQueryable();

                var lstModel = await lstModelQuery
                    .Take(110)
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