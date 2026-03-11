/*
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using Preshow.Admin.Domain.Entities;

namespace Preshow.Admin.Web.Controllers.App
{
    public class AgenciaController : Controller
    {
        public AgenciaController(ILogger<AgenciaController> logger, IConfiguration iConfig, IWebHostEnvironment env)
        {
            _logger = logger;
            _appConfiguration = iConfig;
            _appEnvironment = env;

            apiUrl = _appConfiguration.GetValue<string>("PreshowConfig:apiUrl");
            apiBaseUrl = _appConfiguration.GetSection("PreshowConfig").GetSection("apiBaseUrl").Value;
        }

        #region variaveis

        private readonly ILogger<AgenciaController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;

        // Consume API
        private string? controllerName = string.Empty;
        private readonly string apiUrl = string.Empty;
        private readonly string apiBaseUrl = string.Empty;

        private const string apiMediaTypeJson = "application/json";
        //

        #endregion

        #region CRUD JS

        public ActionResult Index()
        {
            return View("~/Views/App/Agencia.cshtml");
        }

        public IActionResult ListGrid()
        {
            var response = string.Empty;

            try
            {
                var lst = new List<Agencia>();

                using (var httpClient = new HttpClient())
                {
                    var strControllerName = "Agencia";
                    var strControllerMethod = "GetAllDetailsAsync";

                    httpClient.BaseAddress = new Uri($"{apiBaseUrl}");
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept
                        .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var url = $"{apiUrl}/" +
                              $"{strControllerName}" +
                              $"{string.Concat("/", string.Join("", strControllerMethod.Where(a => !string.IsNullOrEmpty(a.ToString()))))}";

                    response = Task.FromResult(httpClient.GetStringAsync(url).Result).Result;

                    if (!string.IsNullOrEmpty(response))
                    {
                        var data = JsonConvert
                            .DeserializeObject<List<Agencia>>(response)
                             //?.Where(s => s.Ativo == true)
                              ?.OrderBy(s => s.NomeFantasia.Equals("Direto") ? 0 : 1)
                              .ThenBy(s => s.NomeFantasia);

                        if (data == null)
                            return BadRequest();

                        lst = data.ToList();
                    }
                    else
                    {
                        return BadRequest();
                    }
                }

                response = JsonConvert.SerializeObject(lst);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = ex?.Message
                });
            }

            return Ok(response);
        }

        [HttpPost]
        public IActionResult Create(Agencia data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.NomeFantasia))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Nome Fantasia deve ser preenchida"
                    });

                var result = Task.Run(() => AsyncActionAPI(data, "Create"));

                //var result = AsyncActionAPI(data, "Create");

                if (result.GetType() == typeof(NotFoundObjectResult) ||
                    result.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = result?.ToString()
                    });

                var jObj = JObject.Parse(result.ToJson());

                var jsonResult = JsonConvert.DeserializeObject<Agencia>(jObj.SelectToken("Result.Value.data").ToString());

                return Ok(new
                {
                    bResult = true,
                    data = jsonResult,
                    type = "OK",
                    message = "SUCESSO ::: "
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

        [HttpPost]
        public ActionResult Edit(Agencia data)
        {
            try
            {
                dynamic response = new { bResult = false, message = string.Empty };

                if (string.IsNullOrEmpty(data.NomeFantasia))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Nome Fantasia deve ser preenchida"
                    });

                try
                {
                    var result = AsyncActionAPI(data, "Edit");

                    if (result.GetType() == typeof(NotFoundObjectResult) ||
                        result.GetType() == typeof(BadRequestObjectResult))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = result?.ToString()
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
        public ActionResult Delete(int id)
        {
            dynamic response = new { bResult = false, message = string.Empty };

            if (id < 1)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Id deve ser maior que 0"
                });

            var model = new List<Agencia>();

            try
            {
                var result = AsyncDeleteById(id);

                if (result.GetType() == typeof(NotFoundObjectResult) ||
                    result.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = result?.ToString()
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

            return Ok(new
            {
                bResult = true,
                type = "OK",
                message = "SUCESSO ::: "
            });

            //return View();
        }

        #endregion

        #region Aux
        public async Task<IActionResult> AsyncDeleteById(int? id)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var strControllerName = "Agencia";
                    var strControllerMethod = string.Empty;

                    httpClient.BaseAddress = new Uri($"{apiBaseUrl}");
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept
                        .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var url = $"{apiUrl}/" +
                              $"{strControllerName}" +
                              $"{string.Concat("/", string.Join("", strControllerMethod.Where(a => !string.IsNullOrEmpty(a.ToString()))))}";

                    var response = await httpClient
                        .DeleteAsync($"{url}{id}");

                    if (!response.IsSuccessStatusCode)
                        return BadRequest(response.Content);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(true);
        }

        public async Task<IActionResult> AsyncActionAPI(Agencia model, string strAction)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var strControllerName = "Agencia";
                    var strControllerMethod = string.Empty;

                    httpClient.BaseAddress = new Uri($"{apiBaseUrl}");
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept
                        .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var url = $"{apiUrl}/" +
                              $"{strControllerName}" +
                              $"{string.Concat("/", string.Join("", strControllerMethod.Where(a => !string.IsNullOrEmpty(a.ToString()))))}";

                    var response = new HttpResponseMessage();

                    if (strAction.Equals("Create"))
                    {
                        response = await httpClient.PostAsync(url,
                            new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, apiMediaTypeJson));
                    }
                    else
                    {
                        url = $"{url}{model?.Id}";

                        response = await httpClient.PutAsync(url,
                            new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, apiMediaTypeJson));
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var apiResponse = await response.Content.ReadAsStringAsync();

                        var jsonModel = new Agencia();


                        if (!apiResponse.Contains("message"))
                        {
                            jsonModel = JsonConvert.DeserializeObject<Agencia>(apiResponse);
                        }

                        else
                        {
                            dynamic data = JObject.Parse(apiResponse);

                            jsonModel = JsonConvert.DeserializeObject<Agencia>(data.model.ToString());
                        }

                        if (jsonModel == null) new Agencia();

                        model = jsonModel;

                        return Ok(new
                        {
                            bResult = true,
                            message = response,
                            data = jsonModel
                        });
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            bResult = false,
                            message = response
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion
    }
}
*/