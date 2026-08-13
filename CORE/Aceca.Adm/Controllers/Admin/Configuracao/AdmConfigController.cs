using Aceca.Adm.Controllers.Admin.Socio;
using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Aceca.Adm.Controllers.Admin.Configuracao
{
    public class AdmConfigController : Controller
    {
        #region variaveis

        private readonly ILogger<AdmConfigController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;
        //
        #endregion

        public AdmConfigController(ILogger<AdmConfigController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;
        }

        #region CRUD JS

        public ActionResult Index()
        {
            return View("~/Views/Admin/Configuracao/AdmConfig.cshtml");
        }
        /*
        public async Task<IActionResult> ListGrid()
        {
            var response = string.Empty;

            var lst = new List<AdmConfig>();

            using (var httpClient = _httpClientFactory.CreateClient())
            {
                string strControllerName = "AdmConfig";
                string strControllerMethod = "GetAllAsync";

                httpClient.BaseAddress = new Uri($"{apiBaseUrl}");
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string url = BuildApiUrl(apiUrl, strControllerName, strControllerMethod);

                response = await httpClient.GetStringAsync(url);

                if (!string.IsNullOrEmpty(response))
                {
                    var data = JsonConvert
                        .DeserializeObject<List<AdmConfig>>(response)
                         //?.Where(s => s.Ativo == true)
                          ?.OrderBy(s => s.Descricao);

                    if (data == null)
                        return BadRequest();

                    lst = data.ToList();
                }
                else
                    return BadRequest();
            }

            response = HelperApiResponse.SerializeCamelCase(lst);

            await HelperUsuarioLog.RegistrarAsync(_httpClientFactory, apiUrl, _logger, User,
                EAdmLogAcao.Listou, nameof(AdmConfigController), "Listou registros de AdmConfig");

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult> Create(AdmConfig data)
        {
            try
            {
                dynamic response = new { bResult = false, message = string.Empty };

                if (string.IsNullOrEmpty(data.Descricao))
                {
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Descrição deve ser preenchida"
                    });
                }

                try
                {
                    var result = await AsyncActionAPI(data, "Create");

                    if (result.GetType() == typeof(NotFoundObjectResult) ||
                         result.GetType() == typeof(BadRequestObjectResult))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = HelperApiResponse.ExtractMessage(result) ?? "Erro ao processar a solicitação."
                        });
                }
                catch (Exception ex)
                {
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = ex?.Message?.ToString()
                    });
                }

                await HelperUsuarioLog.RegistrarAsync(_httpClientFactory, apiUrl, _logger, User,
                    EAdmLogAcao.Incluiu, nameof(AdmConfigController),
                    $"Incluiu AdmConfig '{data.Parametro}'", data.Id > 0 ? data.Id : null);

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: "
                });
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        public async Task<ActionResult> Edit(AdmConfig data)
        {
            try
            {
                dynamic response = new { bResult = false, message = string.Empty };

                if (string.IsNullOrEmpty(data.Descricao))
                {
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Descrição deve ser preenchida"
                    });
                }

                try
                {
                    var result = await AsyncActionAPI(data, "Edit");

                    if (result.GetType() == typeof(NotFoundObjectResult) ||
                         result.GetType() == typeof(BadRequestObjectResult))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = HelperApiResponse.ExtractMessage(result) ?? "Erro ao processar a solicitação."
                        });
                }
                catch (Exception ex)
                {
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = ex?.Message?.ToString()
                    });
                }

                await HelperUsuarioLog.RegistrarAsync(_httpClientFactory, apiUrl, _logger, User,
                    EAdmLogAcao.Alterou, nameof(AdmConfigController),
                    $"Alterou AdmConfig '{data.Parametro}'", data.Id > 0 ? data.Id : null);

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: "
                });
            }
            catch
            {
                return View();
            }
        }

        [HttpDelete]
        public async Task<ActionResult> Delete(int id)
        {
            dynamic response = new { bResult = false, message = string.Empty };

            if (id < 1)
            {
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Id deve ser maior que 0"
                });
            }

            var model = new List<AdmConfig>();

            try
            {
                var result = await AsyncDeleteById(id);

                if (result.GetType() == typeof(NotFoundObjectResult) ||
                     result.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = HelperApiResponse.ExtractMessage(result) ?? "Erro ao processar a solicitação."
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = ex?.Message?.ToString()
                });
            }

            await HelperUsuarioLog.RegistrarAsync(_httpClientFactory, apiUrl, _logger, User,
                EAdmLogAcao.Excluiu, nameof(AdmConfigController), $"Excluiu AdmConfig Id {id}", id);

            return Ok(new
            {
                bResult = true,
                type = "OK",
                message = "SUCESSO ::: "
            });

            //return View();
        }
         */
        #endregion
       
    }
}
