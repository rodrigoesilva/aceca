using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers
{
    public class AuthController : Controller
    {

        #region variaveis

        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly AppDbContext _db;
        private readonly HelperExtensionsController _helperController;
        private readonly IMemoryCache _cache;
        private EPerfil _socioPerfil;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;

        // SigningCredentials cacheados — criados uma vez no construtor, reutilizados a cada login
        private readonly SigningCredentials _jwtSigningCredentials;

        private string _strControllerName = string.Empty;
        private string _strActionName = string.Empty;
        //

        #endregion
        public AuthController(ILogger<AuthController> logger
            , AppDbContext db
            , IWebHostEnvironment env
            , IConfiguration cfg
            , IServiceProvider serviceProvider
            , IHttpClientFactory httpClientFactory
            , HelperExtensionsController helperController
            , IMemoryCache cache)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _helperController = helperController;
            _cache = cache;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;

            var jwtKeyBytes = Encoding.UTF8.GetBytes(_appConfiguration["Jwt:Key"]!);
            _jwtSigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(jwtKeyBytes), SecurityAlgorithms.HmacSha256);
        }

        #region Records
        public record LoginIn(string Email, string Senha);
        public record LoginUpdt(string Username, string Email, string Senha, string ConfirmSenha, bool ChkTermo, string Token = null);

        // DTO para ForgotPassword
        public record ForgotPasswordIn(string Email);

        // DTO para ResetPassword
        public record ResetPasswordIn(string Email, string Token, string Senha, string ConfirmSenha);

        #endregion
              
        #region VIEWS
        // ──────────────────────────────────────────────

        public ActionResult Index()
        {
            // Se o usuário já está autenticado e o cookie ainda é válido (24h),
            // redireciona direto sem precisar logar novamente.
            // O JavaScript da página também verifica o cookie local para exibir
            // a mensagem "Seja bem-vindo novamente" via SweetAlert.
            return View("~/Views/Auth/Login.cshtml");
        }

        public ActionResult SessionExpired()
        {
            return View("~/Views/Pages/MiscSessionExpired.cshtml");
        }

        public ActionResult AccessDenied()
        {
            return View("~/Views/Pages/MiscNotAuthorized.cshtml");
        }

        public ActionResult UpdatePass()
        {
            return View("~/Views/Auth/RegisterUpdate.cshtml");
        }

        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public IActionResult AccountSettingsProfileUser() => View();

        public IActionResult AccountSettings() => View();

        public IActionResult AccountSettingsSecurity() => View();

        public IActionResult AccountSettingsBilling() => View();


        /// <summary>
        /// Exibe a página de "Esqueci minha senha".
        /// </summary>
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View("~/Views/Auth/ForgotPassword.cshtml");
        }

        /// <summary>
        /// Exibe a página de reset de senha (link enviado por e-mail).
        /// </summary>
        [HttpGet]
        public ActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Index");

            ViewBag.Token = token;
            ViewBag.Email = email;

            return View("~/Views/Auth/ResetPassword.cshtml");
            //return View("~/Views/Auth/RegisterUpdate.cshtml");
        }


        /// <summary>
        /// Exibe a página de atualizar dados (link enviado por e-mail para novo socio).
        /// </summary>
        [HttpGet]
        public ActionResult NewRegistration(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Index");

            ViewBag.Token = token;
            ViewBag.Email = email;

            // Mesma view (RegisterUpdate.cshtml) é usada tanto aqui - quem está definindo a
            // senha pela PRIMEIRA vez (link de verificação do teste grátis, ou o reenvio em
            // ResendCadastroEmail) - quanto em UpdatePass() (sócio já existente sendo forçado
            // a trocar a senha após o login). Flag pra view ajustar o texto pra cada caso: um
            // "atualize sua senha, diferente da anterior" não faz sentido pra quem nunca teve
            // senha nenhuma antes.
            ViewBag.PrimeiroAcesso = true;

            return View("~/Views/Auth/RegisterUpdate.cshtml");
        }
        #endregion

        #region Auto-Cadastro (Teste Grátis)

        // Ip vem do próprio cliente via ipify.org (mesmo padrão de fn_LoginAuthGeo em
        // pages-auth.js) - não confiar em HttpContext.Connection.RemoteIpAddress aqui, que
        // fica preso a loopback/IP interno atrás de proxy/IIS sem UseForwardedHeaders.
        public record CadastroTesteIn(string Cpf, string Email, string? Latitude, string? Longitude, string? Ip);
        public record VerificarCodigoIn(string Email, string Codigo);
        public record ReenviarCadastroTesteIn(string Email);

        // Página pública com URL própria (não modal) - necessária pra campos como o
        // "Link da política de privacidade" da tela de consentimento OAuth (Google e
        // qualquer outro provedor exigem uma URL de verdade, não uma modal escondida
        // dentro de outra página). Mesmo texto do ModalPolicyTerms.cshtml, via os
        // partials compartilhados _PolicyPrivacidadeTexto/_TermosCondicoesTexto.
        [HttpGet]
        public IActionResult PoliticaPrivacidade()
        {
            return View("~/Views/Auth/PoliticaPrivacidade.cshtml");
        }

        // Popula os mesmos ViewBag usados pelo login-card de cadastro (RegisterCover e a
        // página de testes Register) - mantém as duas com funcionalidade idêntica (duração
        // do teste grátis, fluxo de e-mail já cadastrado, retomada via Google) sem duplicar
        // a lógica em cada action.
        private async Task PreencherViewBagCadastroTesteAsync(string? googleToken, string? emailJaCadastrado)
        {
            var duracaoStr = await _db.AdmConfig
                .Where(c => c.Parametro == "Param_TesteGratisDuracaoHoras")
                .Select(c => c.Valor)
                .FirstOrDefaultAsync();
            ViewBag.DuracaoTesteHoras = int.TryParse(duracaoStr, out var h) && h > 0 ? h : 24;

            // E-mail confirmado pelo Google (GoogleCallback) já pertence a um sócio - mesma
            // UX do fluxo por e-mail: RegisterCover.js mostra o Swal perguntando se quer ir
            // direto pro login.
            ViewBag.EmailJaCadastrado = emailJaCadastrado == "1";

            // Token opaco (ver GoogleCallback) que carrega nome/e-mail já verificados pelo
            // Google, guardados em cache no servidor - nunca confiamos num e-mail vindo de
            // campo editável do cliente pra essa etapa, só nesse token.
            if (!string.IsNullOrWhiteSpace(googleToken))
            {
                if (_cache.TryGetValue($"google_cadastro_{googleToken}", out (string Email, string Nome) dadosGoogle))
                {
                    ViewBag.GoogleToken = googleToken;
                    ViewBag.GoogleEmail = dadosGoogle.Email;
                    ViewBag.GoogleNome = dadosGoogle.Nome;
                }
                else
                {
                    ViewBag.GoogleTokenExpirado = true;
                }
            }
        }

        // Página de testes - mesmo conjunto login-wrap/login-card e mesma funcionalidade
        // de RegisterCover, só que embutida no layout de 2 colunas com imagem lateral.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string? googleToken, string? emailJaCadastrado)
        {
            // O processo de teste grátis precisa ser sempre seguido do zero, mesmo que quem
            // clicou em "Teste Grátis" já esteja com uma sessão válida de outra conta (ex.:
            // esqueceu de sair, ou é um computador compartilhado) - sem isso, a pessoa caía
            // direto na área logada da conta antiga sem nenhuma nova verificação, o que não
            // é o fluxo esperado por quem clicou explicitamente em "Teste Grátis".

            await PreencherViewBagCadastroTesteAsync(googleToken, emailJaCadastrado);

            return View("~/Views/Auth/Register.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> RegisterCover(string? googleToken, string? emailJaCadastrado)
        {
            // Mesmo motivo do Register() acima - o teste grátis precisa ser sempre seguido do
            // zero, independente de já existir uma sessão autenticada de outra conta.

            await PreencherViewBagCadastroTesteAsync(googleToken, emailJaCadastrado);

            //return View("~/Views/Auth/RegisterCover.cshtml");
            return View("~/Views/Auth/Register.cshtml");
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            // Mesmo motivo do Register()/RegisterCover() acima.

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback), "Auth"),
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Só substitui a etapa de provar o e-mail (Google já verifica) - o CPF continua
        // sendo pedido depois, na volta pro RegisterCover (mesma trava antifraude de
        // sempre). Nunca autentica a sessão real da aplicação por aqui.
        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            var resultadoExterno = await HttpContext.AuthenticateAsync(Aceca.Adm.Helper.AuthSchemes.ExternalGoogle);
            await HttpContext.SignOutAsync(Aceca.Adm.Helper.AuthSchemes.ExternalGoogle);

            var email = resultadoExterno.Succeeded
                ? resultadoExterno.Principal?.FindFirstValue(ClaimTypes.Email)?.Trim().ToLowerInvariant()
                : null;

            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(RegisterCover));

            if (_helperController.IsEmailDescartavel(email))
                return RedirectToAction(nameof(RegisterCover));

            var jaEhSocio = await _db.SocioSeguranca.AsNoTracking().AnyAsync(s => s.Email == email);
            if (jaEhSocio)
                return RedirectToAction(nameof(RegisterCover), new { emailJaCadastrado = "1" });

            var nome = resultadoExterno.Principal!.FindFirstValue(ClaimTypes.Name);

            var googleToken = _helperController.GenerateSecuretToken();
            _cache.Set($"google_cadastro_{googleToken}",
                (Email: email, Nome: string.IsNullOrWhiteSpace(nome) ? _helperController.NomePlaceholderDoEmail(email) : nome),
                TimeSpan.FromMinutes(15));

            return RedirectToAction(nameof(RegisterCover), new { googleToken });
        }

        // Limite simples por IP - não impede um abuso determinado (rede móvel compartilha IP
        // entre pessoas reais, e um IP novo é trivial via VPN/4G); só encarece um script
        // batendo repetidamente neste endpoint. A defesa real contra reincidência é o CPF
        // (UNIQUE em cadastro_teste, checado abaixo).
        private bool IpExcedeuLimiteCadastro(string ip)
        {
            var chave = $"cadastro_teste_ip_{ip}";
            var tentativas = _cache.Get<int?>(chave) ?? 0;

            if (tentativas >= 5)
                return true;

            _cache.Set(chave, tentativas + 1, TimeSpan.FromHours(1));
            return false;
        }

        [HttpPost]
        public async Task<IActionResult> CadastroTesteIniciar([FromBody] CadastroTesteIn dto)
        {
            try
            {
                var ip = !string.IsNullOrWhiteSpace(dto.Ip)
                    ? dto.Ip.Trim()
                    : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

                if (IpExcedeuLimiteCadastro(ip))
                    return Ok(new { bResult = false, type = "ERRO", message = "Muitas tentativas de cadastro deste endereço. Tente novamente mais tarde." });

                var email = dto.Email?.Trim().ToLowerInvariant();
                var cpfDigitos = Aceca.Adm.Helper.CpfHelper.SomenteDigitos(dto.Cpf);

                if (!_helperController.IsValidEmailUsingMailAddress(email))
                    return Ok(new { bResult = false, type = "ERRO", message = "E-mail inválido." });

                if (_helperController.IsEmailDescartavel(email))
                    return Ok(new { bResult = false, type = "ERRO", message = "Use um e-mail pessoal válido - e-mails temporários não são aceitos." });

                // Domínio precisa resolver de verdade (DNS) - pega e-mail com domínio
                // inventado/digitado errado, que passaria pelo regex acima mas nunca
                // entregaria nada. Não é uma verificação de MX de verdade (.NET não tem
                // suporte nativo a esse tipo de registro sem lib externa), mas qualquer
                // domínio de e-mail real do dia a dia tem também registro A/AAAA.
                var dominioEmail = email![(email.LastIndexOf('@') + 1)..];
                if (!await _helperController.DominioResolveAsync(dominioEmail))
                    return Ok(new { bResult = false, type = "ERRO", message = "Não conseguimos confirmar o domínio desse e-mail. Verifique se digitou corretamente." });

                if (!Aceca.Adm.Helper.CpfHelper.EhValido(cpfDigitos))
                    return Ok(new { bResult = false, type = "ERRO", message = "CPF inválido." });

                // E-mail já é de um sócio de verdade - direciona pro login em vez de deixar
                // tentar abrir um segundo cadastro por cima de uma conta existente.
                var jaEhSocio = await _db.SocioSeguranca.AsNoTracking().AnyAsync(s => s.Email == email);
                if (jaEhSocio)
                    // type diferenciado (não "ERRO" genérico) - o front usa isso pra mostrar um
                    // SweetAlert com opção de ir direto pro login em vez do banner de erro comum.
                    return Ok(new { bResult = false, type = "EMAIL_JA_CADASTRADO", message = "Este e-mail já está sendo utilizado." });

                // Chave antifraude: um CPF só passa por aqui uma vez, para sempre - vencido ou
                // não, verificado ou não. O UNIQUE KEY no banco é a garantia real; esta
                // consulta só existe pra devolver uma mensagem amigável em vez de erro de SQL.
                var jaTentouTeste = await _db.CadastroTeste.AsNoTracking().AnyAsync(c => c.Cpf == cpfDigitos);
                if (jaTentouTeste)
                    // type diferenciado (igual ao EMAIL_JA_CADASTRADO) - o front mostra um
                    // SweetAlert com link clicável pra Solicitar Associação em vez do banner
                    // de erro comum (onde a URL aparecia como texto puro, não clicável).
                    return Ok(new { bResult = false, type = "CPF_JA_UTILIZOU_TESTE", message = "Período de teste grátis já em uso." });

                var token = _helperController.GenerateSecuretToken();
                var codigo = _helperController.GenerateStringPassword(6).ToUpperInvariant();

                // Tela de cadastro só pede CPF/e-mail (o resto é atrito extra num teste
                // grátis) - nome vira um placeholder a partir do e-mail, e a pessoa pode
                // corrigir depois em "Meus Dados" (AuthController.UpdateProfile) já dentro
                // da área do sócio.
                var nome = _helperController.NomePlaceholderDoEmail(email);

                var contexto = await MontarContextoCadastroTesteAsync(ip, dto.Latitude, dto.Longitude);

                var registro = new Models.CadastroTeste
                {
                    Cpf = cpfDigitos,
                    Nome = nome,
                    Email = email,
                    TokenVerificacao = token,
                    CodigoVerificacao = codigo,
                    TokenExpiraEm = DateTime.UtcNow.AddMinutes(5),
                    Ip = ip,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    Latitude = contexto.Latitude ?? dto.Latitude,
                    Longitude = contexto.Longitude ?? dto.Longitude,
                    OS = contexto.OS,
                    Browser = contexto.Browser,
                    Device = contexto.Device,
                    Operadora = contexto.Operadora,
                    Estado = contexto.Estado,
                    Cidade = contexto.Cidade,
                    DataCriacao = DateTime.UtcNow,
                };

                _db.CadastroTeste.Add(registro);

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Corrida rara entre dois envios simultâneos com o mesmo CPF - o UNIQUE
                    // KEY do banco é quem garante de verdade; aqui só devolvemos mensagem
                    // amigável em vez do erro de SQL cru.
                    return Ok(new { bResult = false, type = "ERRO", message = "Este CPF já utilizou o período de teste grátis." });
                }

                var link = $"{_urlBaseApp}/Auth/VerifyEmailCover?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

                await _helperController.EnviarEmailAsync(ETipoEmail.VerificacaoCadastroTeste, email, nome, link, codigo);

                return Ok(new { bResult = true, type = "OK", email });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(CadastroTesteIniciar), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = "Não foi possível concluir o cadastro." });
            }
        }

        public record CadastroTesteGoogleIn(string Cpf, string GoogleToken, string? Latitude, string? Longitude, string? Ip);

        // Equivalente a CadastroTesteIniciar, mas pra quem chegou via "Continuar com o
        // Google" (GoogleCallback) - o e-mail já foi verificado pelo Google (por isso não
        // repete os checks de formato/domínio/descartável nem manda código por e-mail), só
        // falta o CPF, que é a trava antifraude real e continua obrigatória do mesmo jeito.
        [HttpPost]
        public async Task<IActionResult> CadastroTesteGoogleFinalizar([FromBody] CadastroTesteGoogleIn dto)
        {
            try
            {
                if (!_cache.TryGetValue($"google_cadastro_{dto.GoogleToken}", out (string Email, string Nome) dadosGoogle))
                    return Ok(new { bResult = false, type = "ERRO", message = "Sessão do Google expirada. Clique em \"Continuar com o Google\" novamente." });

                var ip = !string.IsNullOrWhiteSpace(dto.Ip)
                    ? dto.Ip.Trim()
                    : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

                if (IpExcedeuLimiteCadastro(ip))
                    return Ok(new { bResult = false, type = "ERRO", message = "Muitas tentativas de cadastro deste endereço. Tente novamente mais tarde." });

                var cpfDigitos = Aceca.Adm.Helper.CpfHelper.SomenteDigitos(dto.Cpf);
                if (!Aceca.Adm.Helper.CpfHelper.EhValido(cpfDigitos))
                    return Ok(new { bResult = false, type = "ERRO", message = "CPF inválido." });

                // Defensivo: repete a checagem que o GoogleCallback já fez antes de gerar o
                // token, cobrindo o caso raro de a pessoa virar sócio nesse meio-tempo.
                var jaEhSocio = await _db.SocioSeguranca.AsNoTracking().AnyAsync(s => s.Email == dadosGoogle.Email);
                if (jaEhSocio)
                    return Ok(new { bResult = false, type = "EMAIL_JA_CADASTRADO", message = "Este e-mail já pertence a um sócio." });

                var jaTentouTeste = await _db.CadastroTeste.AsNoTracking().AnyAsync(c => c.Cpf == cpfDigitos);
                if (jaTentouTeste)
                    // type diferenciado (igual ao EMAIL_JA_CADASTRADO) - o front mostra um
                    // SweetAlert com link clicável pra Solicitar Associação em vez do banner
                    // de erro comum (onde a URL aparecia como texto puro, não clicável).
                    return Ok(new { bResult = false, type = "CPF_JA_UTILIZOU_TESTE", message = "Este CPF já utilizou o período de teste grátis." });

                var contexto = await MontarContextoCadastroTesteAsync(ip, dto.Latitude, dto.Longitude);

                var registro = new Models.CadastroTeste
                {
                    Cpf = cpfDigitos,
                    Nome = dadosGoogle.Nome,
                    Email = dadosGoogle.Email,
                    Verificado = true,
                    DataVerificacao = DateTime.UtcNow,
                    Ip = ip,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    Latitude = contexto.Latitude ?? dto.Latitude,
                    Longitude = contexto.Longitude ?? dto.Longitude,
                    OS = contexto.OS,
                    Browser = contexto.Browser,
                    Device = contexto.Device,
                    Operadora = contexto.Operadora,
                    Estado = contexto.Estado,
                    Cidade = contexto.Cidade,
                    DataCriacao = DateTime.UtcNow,
                };

                _db.CadastroTeste.Add(registro);

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Ok(new { bResult = false, type = "ERRO", message = "Este CPF já utilizou o período de teste grátis." });
                }

                _cache.Remove($"google_cadastro_{dto.GoogleToken}");

                var proximoToken = await FinalizarCadastroTesteAsync(registro);
                if (proximoToken == null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Não foi possível concluir o cadastro." });

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    redirectUrl = Url.Action(nameof(NewRegistration), new { token = proximoToken, email = registro.Email })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(CadastroTesteGoogleFinalizar), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = "Não foi possível concluir o cadastro." });
            }
        }

        private record ContextoCadastroTeste(
            string? OS, string? Browser, string? Device, string? Operadora,
            string? Estado, string? Cidade, string? Latitude, string? Longitude);

        // Enriquecimento best-effort de IP/UA - mesmo padrão já usado em LoginLog para
        // login normal (GetGeoInfoAsync via ipgeolocation.io + reverse geocoding via
        // Nominatim quando o navegador cedeu lat/long). Só um sinal auxiliar de revisão
        // manual (ver comentário em Models.CadastroTeste) - qualquer falha (rede, cota da
        // API paga) devolve tudo null em vez de derrubar o cadastro.
        private async Task<ContextoCadastroTeste> MontarContextoCadastroTesteAsync(string ip, string? latitude, string? longitude)
        {
            var vazio = new ContextoCadastroTeste(null, null, null, null, null, null, null, null);

            if (string.IsNullOrWhiteSpace(ip) || ip is "desconhecido" or "::1" or "127.0.0.1")
                return vazio;

            try
            {
                var responseGeo = await GetGeoInfoAsync(ip);
                var jObjResult  = ((ObjectResult)responseGeo).Value;

                var jsonGeo   = jObjResult?.GetType()?.GetProperty("data")?.GetValue(jObjResult, null)?.ToString();
                var jsonAgent = jObjResult?.GetType()?.GetProperty("jsonAgent")?.GetValue(jObjResult, null)?.ToString();

                if (string.IsNullOrEmpty(jsonGeo))
                    return vazio;

                JsonNode nodeGeo   = JsonNode.Parse(jsonGeo)!;
                JsonNode nodeAgent = !string.IsNullOrEmpty(jsonAgent) ? JsonNode.Parse(jsonAgent)! : null;

                string? bairroPreciso = null;
                string? cidadePrecisa = null;
                string? latPrecisa = null;
                string? lngPrecisa = null;

                if (!string.IsNullOrWhiteSpace(latitude) && !string.IsNullOrWhiteSpace(longitude))
                {
                    (bairroPreciso, cidadePrecisa) = await ReverseGeocodeAsync(latitude, longitude);
                    latPrecisa = latitude;
                    lngPrecisa = longitude;
                }

                var osBruto = nodeAgent?["operating_system"]?["name"]?.GetValue<string>();

                return new ContextoCadastroTeste(
                    OS: CorrigirNomeSistemaOperacional(osBruto, null),
                    Browser: nodeAgent?["name"]?.GetValue<string>(),
                    Device: nodeAgent?["device"]?["type"]?.GetValue<string>(),
                    Operadora: nodeGeo["asn"]?["organization"]?.GetValue<string>(),
                    Estado: nodeGeo["location"]?["state_code"]?.GetValue<string>(),
                    Cidade: !string.IsNullOrWhiteSpace(cidadePrecisa)
                        ? (!string.IsNullOrWhiteSpace(bairroPreciso) ? $"{bairroPreciso}, {cidadePrecisa}" : cidadePrecisa)
                        : nodeGeo["location"]?["city"]?.GetValue<string>(),
                    Latitude: latPrecisa ?? nodeGeo["location"]?["latitude"]?.ToString()?.Trim('"'),
                    Longitude: lngPrecisa ?? nodeGeo["location"]?["longitude"]?.ToString()?.Trim('"'));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ERRO :: {Method} :: {Message}", nameof(MontarContextoCadastroTesteAsync), ex.Message);
                return vazio;
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReenviarCadastroTeste([FromBody] ReenviarCadastroTesteIn dto)
        {
            // Mensagem sempre igual, exista ou não o cadastro pendente - mesmo padrão
            // anti-enumeração usado em ForgotPassword/ResendCadastroEmail.
            var mensagemGenerica = new { bResult = true, type = "OK", message = "Se o cadastro existir e ainda não tiver sido confirmado, um novo e-mail foi enviado." };

            try
            {
                var email = dto.Email?.Trim().ToLowerInvariant();
                var registro = await _db.CadastroTeste.FirstOrDefaultAsync(c => c.Email == email && !c.Verificado);

                if (registro == null)
                    return Ok(mensagemGenerica);

                if (registro.UltimoReenvio.HasValue && registro.UltimoReenvio.Value.AddSeconds(60) > DateTime.UtcNow)
                    return Ok(mensagemGenerica);

                if (registro.QtdReenvios >= 5)
                    return Ok(mensagemGenerica);

                registro.TokenVerificacao = _helperController.GenerateSecuretToken();
                registro.CodigoVerificacao = _helperController.GenerateStringPassword(6).ToUpperInvariant();
                registro.TokenExpiraEm = DateTime.UtcNow.AddMinutes(5);
                registro.QtdReenvios += 1;
                registro.UltimoReenvio = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var link = $"{_urlBaseApp}/Auth/VerifyEmailCover?token={Uri.EscapeDataString(registro.TokenVerificacao)}&email={Uri.EscapeDataString(registro.Email)}";
                await _helperController.EnviarEmailAsync(ETipoEmail.VerificacaoCadastroTeste, registro.Email, registro.Nome, link, registro.CodigoVerificacao);

                return Ok(mensagemGenerica);
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(ReenviarCadastroTeste), ex.Message);
                return Ok(mensagemGenerica);
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmailCover(string? email, string? token)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Inicio", "Home");

            ViewBag.Email = email;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return View("~/Views/Auth/VerifyEmailCover.cshtml");

            var registro = await _db.CadastroTeste.FirstOrDefaultAsync(c => c.Email == email.Trim().ToLowerInvariant());

            if (registro == null || registro.Verificado
                || registro.TokenVerificacao != token
                || !registro.TokenExpiraEm.HasValue || registro.TokenExpiraEm.Value < DateTime.UtcNow)
            {
                ViewBag.LinkInvalido = true;
                return View("~/Views/Auth/VerifyEmailCover.cshtml");
            }

            var proximoToken = await FinalizarCadastroTesteAsync(registro);

            if (proximoToken == null)
            {
                ViewBag.LinkInvalido = true;
                return View("~/Views/Auth/VerifyEmailCover.cshtml");
            }

            // Não faz login automático - a pessoa ainda precisa definir senha (RegisterUpdate)
            // e completar os dados pessoais (RegisterMultiSteps) antes de ter acesso de verdade.
            return RedirectToAction("NewRegistration", new { token = proximoToken, email = registro.Email });
        }

        [HttpPost]
        public async Task<IActionResult> VerificarCodigoCadastroTeste([FromBody] VerificarCodigoIn dto)
        {
            try
            {
                var email = dto.Email?.Trim().ToLowerInvariant();
                var codigo = dto.Codigo?.Trim().ToUpperInvariant();

                var registro = await _db.CadastroTeste.FirstOrDefaultAsync(c => c.Email == email);

                if (registro == null || registro.Verificado
                    || registro.CodigoVerificacao != codigo
                    || !registro.TokenExpiraEm.HasValue || registro.TokenExpiraEm.Value < DateTime.UtcNow)
                    return Ok(new { bResult = false, type = "ERRO", message = "Código inválido ou expirado." });

                var proximoToken = await FinalizarCadastroTesteAsync(registro);

                if (proximoToken == null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Não foi possível concluir o cadastro." });

                // Não faz login automático - próxima etapa é definir senha (RegisterUpdate).
                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    redirectUrl = Url.Action("NewRegistration", new { token = proximoToken, email = registro.Email })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(VerificarCodigoCadastroTeste), ex.Message);
                return Ok(new { bResult = false, type = "ERRO", message = "Não foi possível concluir o cadastro." });
            }
        }

        // Cria o sócio (perfil Socio, ATIVO mas PendenteCadastro=true) e devolve um token de
        // continuidade pro fluxo de "primeiro acesso" (RegisterUpdate -> RegisterMultiSteps) -
        // NÃO faz login automático e NÃO inicia a contagem do teste grátis (TesteExpiraEm só é
        // definido quando os dados pessoais são concluídos, em FinalizarCadastroCompleto). Ativo
        // precisa ser true aqui porque LoginUpdate rejeita sócio inativo antes de chegar na senha.
        private async Task<string?> FinalizarCadastroTesteAsync(Models.CadastroTeste registro)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            Func<Task<string?>> operation = async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();

                try
                {
                    var socio = new Socio
                    {
                        SocioPerfilId = (int)EPerfil.Socio,
                        Nome = registro.Nome,
                        Ativo = true,
                        MostrarSite = false,
                        EhContaTeste = true,
                        PendenteCadastro = true,
                        TesteExpiraEm = null,
                    };
                    _db.Socio.Add(socio);
                    await _db.SaveChangesAsync();

                    var senhaTemporaria = _helperController.GenerateStringPassword(12);
                    var tokenContinuacao = _helperController.GenerateSecuretToken();
                    var seguranca = new Models.SocioSeguranca
                    {
                        SocioId = socio.Id!.Value,
                        Email = registro.Email,
                        NomeUsuario = registro.Nome,
                        Senha = _helperController.GenerateHashPassword(senhaTemporaria),
                        SenhaAberta = senhaTemporaria,
                        SenhaAtualizada = false,
                        UltimoLogin = DateTime.UtcNow,
                        ResetPasswordToken = tokenContinuacao,
                        ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(30),
                    };
                    _db.SocioSeguranca.Add(seguranca);

                    _db.SocioContato.Add(new Models.SocioContato
                    {
                        SocioId = socio.Id,
                        DDI = 55,
                        DDD = 0,
                        Telefone = null,
                        Email = registro.Email,
                    });

                    registro.Verificado = true;
                    registro.DataVerificacao = DateTime.UtcNow;
                    registro.SocioIdGerado = socio.Id;

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return tokenContinuacao;
                }
                catch (Exception ex)
                {
                    _logger.LogError("ERRO :: {Method} :: {Message}", nameof(FinalizarCadastroTesteAsync), ex.Message);
                    return null;
                }
            };

            return await strategy.ExecuteAsync(operation);
        }

        public record FinalizarCadastroCompletoIn(
            string Token, string Email, string Nome, string? DataNascimento, string? Telefone,
            string? Endereco, string? Numero, string? Complemento, string? Bairro, string? Cidade,
            string? Estado, string? CEP);

        [HttpGet]
        public async Task<IActionResult> RegisterMultiSteps(string? token, string? email)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Inicio", "Home");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return RedirectToAction("Index");

            var user = await _db.SocioSeguranca.Include(x => x.Socio).AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == email.Trim().ToLowerInvariant());

            if (user == null || !user.Socio.EhContaTeste || !user.Socio.PendenteCadastro
                || user.ResetPasswordToken != token
                || !user.ResetPasswordTokenExpiry.HasValue || user.ResetPasswordTokenExpiry.Value < DateTime.UtcNow)
                return RedirectToAction("Index");

            ViewBag.Token = token;
            ViewBag.Email = email;
            ViewBag.Nome = user.Socio.Nome;

            return View("~/Views/Auth/RegisterMultiSteps.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarCadastroCompleto([FromBody] FinalizarCadastroCompletoIn dto)
        {
            try
            {
                var email = dto.Email?.Trim().ToLowerInvariant();

                var user = await _db.SocioSeguranca.Include(x => x.Socio)
                    .FirstOrDefaultAsync(x => x.Email == email);

                if (user == null || !user.Socio.EhContaTeste || !user.Socio.PendenteCadastro
                    || user.ResetPasswordToken != dto.Token
                    || !user.ResetPasswordTokenExpiry.HasValue || user.ResetPasswordTokenExpiry.Value < DateTime.UtcNow)
                    return Ok(new { bResult = false, type = "ERRO", message = "Sessão de cadastro inválida ou expirada. Solicite um novo cadastro." });

                if (string.IsNullOrWhiteSpace(dto.Nome))
                    return Ok(new { bResult = false, type = "ERRO", message = "Informe seu nome completo." });

                var strategy = _db.Database.CreateExecutionStrategy();

                Func<Task<bool>> operation = async () =>
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();

                    try
                    {
                        user.Socio.Nome = dto.Nome.Trim();

                        var (dia, mes, ano) = ParseDataNascimento(dto.DataNascimento);
                        _db.SocioAniversario.Add(new Models.SocioAniversario
                        {
                            SocioId = user.SocioId,
                            Dia = dia,
                            Mes = mes,
                            Ano = ano,
                        });

                        _db.SocioEndereco.Add(new Models.SocioEndereco
                        {
                            SocioId = user.SocioId,
                            Endereco = dto.Endereco,
                            Numero = dto.Numero,
                            Complemento = dto.Complemento,
                            Bairro = dto.Bairro,
                            Cidade = dto.Cidade,
                            Estado = dto.Estado,
                            CEP = !string.IsNullOrEmpty(dto.CEP) ? dto.CEP.Replace("-", string.Empty) : string.Empty,
                        });

                        var contato = await _db.SocioContato.FirstOrDefaultAsync(c => c.SocioId == user.SocioId);
                        if (contato != null)
                        {
                            var (ddd, numeroTel) = ParseTelefone(dto.Telefone);
                            contato.DDD = ddd;
                            contato.Telefone = numeroTel;
                        }

                        var duracaoStr = await _db.AdmConfig
                            .Where(c => c.Parametro == "Param_TesteGratisDuracaoHoras")
                            .Select(c => c.Valor)
                            .FirstOrDefaultAsync();
                        var duracaoHoras = int.TryParse(duracaoStr, out var h) && h > 0 ? h : 24;

                        // Só agora, com o cadastro de verdade concluído, o relógio do teste
                        // grátis começa a contar - e a conta passa a poder logar normalmente.
                        user.Socio.TesteExpiraEm = DateTime.UtcNow.AddHours(duracaoHoras);
                        user.Socio.PendenteCadastro = false;
                        user.ResetPasswordToken = null;
                        user.ResetPasswordTokenExpiry = null;

                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("ERRO :: {Method} :: {Message}", nameof(FinalizarCadastroCompleto), ex.Message);
                        return false;
                    }
                };

                var sucesso = await strategy.ExecuteAsync(operation);

                if (!sucesso)
                    return Ok(new { bResult = false, type = "ERRO", message = "Não foi possível concluir o cadastro." });

                return Ok(new { bResult = true, type = "OK", redirectUrl = Url.Action("Index") });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(FinalizarCadastroCompleto), ex.Message);
                return Ok(new { bResult = false, type = "ERRO", message = "Não foi possível concluir o cadastro." });
            }
        }

        private static (int? Dia, int? Mes, int? Ano) ParseDataNascimento(string? data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return (null, null, null);

            var partes = data.Split('/');

            int? dia = partes.Length > 0 && int.TryParse(partes[0].Trim(), out var d) ? d : null;
            int? mes = partes.Length > 1 && int.TryParse(partes[1].Trim(), out var m) ? m : null;
            int? ano = partes.Length > 2 && int.TryParse(partes[2].Trim(), out var a) ? a : null;

            return (dia, mes, ano);
        }

        // Telefone vem como "(11) 91234-5678" - com guarda de índice (SocioController.cs teve
        // um IndexOutOfRangeException real por não checar isso antes de fazer Split(")")[1]).
        private static (int? DDD, long? Telefone) ParseTelefone(string? telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
                return (null, null);

            var partes = telefone.Split(')');
            if (partes.Length < 2)
                return (null, null);

            var dddStr = partes[0].Replace("(", string.Empty).Trim();
            var numeroStr = partes[1].Replace("-", string.Empty).Trim();

            int? ddd = int.TryParse(dddStr, out var d) ? d : null;
            long? numero = long.TryParse(numeroStr, out var n) ? n : null;

            return (ddd, numero);
        }

        #endregion

        #region ESQUECI A SENHA  (envia e-mail com link)

        /// <summary>
        /// Recebe o e-mail, gera token temporário (24h), salva no banco e envia e-mail com link.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordIn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Email))
                    return Ok(new { bResult = false, message = "E-mail inválido." });

                var user = await _db.SocioSeguranca
                   .Include(x => x.Socio)
                   .FirstOrDefaultAsync(x => x.Email == dto.Email.Trim().ToLowerInvariant());

                // Por segurança, retorna sucesso mesmo se o e-mail não existir,
                // para não expor quais e-mails estão cadastrados.
                if (user is null)
                    return Ok(new { bResult = true, message = "Se o e-mail existir, você receberá as instruções." });

                if (user.Socio.SocioPerfilId.Equals((int)EPerfil.Banido))
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Banido" });

                if (!user.Socio.Ativo)
                    return Ok(new { bResult = false, type = "ERRO", message = "Acesso inválido. Entre em contato conosco." });

                var strToken = _helperController.GenerateSecuretToken();

                user.ResetPasswordToken = strToken;
                user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(24);

                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                // Monta link de reset
                var resetLink = $"{_urlBaseApp}/Auth/ResetPassword?token={Uri.EscapeDataString(strToken)}&email={Uri.EscapeDataString(user.Email)}";

                // Envia e-mail
                var resultSendMail = await _helperController.EnviarEmailAsync(ETipoEmail.EsqueceuSenha, user.Email, user.Socio.Nome, resetLink);                

                if (resultSendMail.GetType() == typeof(NotFoundObjectResult) ||
                    resultSendMail.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Falha no envido do E-mail",
                        data = user.Email
                    });

                return Ok(new { bResult = true, message = "E-mail enviado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(ForgotPassword), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        #region REENVIO DO E-MAIL DE CADASTRO (link de boas-vindas expirado)

        /// <summary>
        /// Gera um novo token (24h) e reenvia o e-mail de boas-vindas/cadastro, para o caso
        /// do sócio não ter concluído o cadastro dentro do prazo do link original. Diferente
        /// do ForgotPassword (que é para quem já tem acesso e esqueceu a senha), este só se
        /// aplica a quem ainda não concluiu o primeiro acesso (SenhaAtualizada = false) -
        /// evita virar um jeito alternativo de reset de senha para quem já está com a conta ativa.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ResendCadastroEmail([FromBody] ForgotPasswordIn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Email))
                    return Ok(new { bResult = false, message = "E-mail inválido." });

                var user = await _db.SocioSeguranca
                   .Include(x => x.Socio)
                   .FirstOrDefaultAsync(x => x.Email == dto.Email.Trim().ToLowerInvariant());

                // Por segurança, retorna sucesso mesmo se o e-mail não existir ou já ter
                // concluído o cadastro, para não expor quais e-mails estão cadastrados nem
                // em que etapa cada sócio está.
                if (user is null || user.SenhaAtualizada)
                    return Ok(new { bResult = true, message = "Se o e-mail existir e o cadastro estiver pendente, você receberá um novo link." });

                if (user.Socio.SocioPerfilId.Equals((int)EPerfil.Banido))
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Banido" });

                if (!user.Socio.Ativo)
                    return Ok(new { bResult = false, type = "ERRO", message = "Acesso inválido. Entre em contato conosco." });

                var strToken = _helperController.GenerateSecuretToken();

                user.ResetPasswordToken = strToken;
                user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(24);

                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                var resetLink = $"{_urlBaseApp}/Auth/NewRegistration?token={Uri.EscapeDataString(strToken)}&email={Uri.EscapeDataString(user.Email)}";

                var resultSendMail = await _helperController.EnviarEmailAsync(ETipoEmail.Cadastro, user.Email, user.Socio.Nome, resetLink);

                if (resultSendMail.GetType() == typeof(NotFoundObjectResult) ||
                    resultSendMail.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Falha no envido do E-mail",
                        data = user.Email
                    });

                return Ok(new { bResult = true, message = "E-mail enviado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(ResendCadastroEmail), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        #region RESET DE SENHA  (página com e-mail + senha + confirmação)

        /// <summary>
        /// Valida token, valida regras de senha e atualiza no banco.
        /// Regras: mínimo 8 caracteres, pelo menos 1 número.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordIn dto)
        {
            try
            {
                // Validações de entrada
                if (string.IsNullOrWhiteSpace(dto?.Email) ||
                    string.IsNullOrWhiteSpace(dto?.Token) ||
                    string.IsNullOrWhiteSpace(dto?.Senha) ||
                    string.IsNullOrWhiteSpace(dto?.ConfirmSenha))
                    return Ok(new { bResult = false, message = "Todos os campos são obrigatórios." });

                if (dto.Senha != dto.ConfirmSenha)
                    return Ok(new { bResult = false, message = "As senhas não coincidem." });

                if (dto.Senha.Length < 8)
                    return Ok(new { bResult = false, message = "A senha deve ter no mínimo 8 caracteres." });

                if (!dto.Senha.Any(char.IsDigit))
                    return Ok(new { bResult = false, message = "A senha deve conter pelo menos 1 número." });

                var user = await _db.SocioSeguranca
                    .Include(x => x.Socio)
                    .FirstOrDefaultAsync(x => x.Email == dto.Email.Trim().ToLowerInvariant());

                if (user is null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Inválido" });

                if (user.Socio.SocioPerfilId.Equals((int)EPerfil.Banido))
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Banido" });

                if (!user.Socio.Ativo)
                    return Ok(new { bResult = false, type = "ERRO", message = "Acesso inválido. Entre em contato conosco." });

                // Valida token e expiração
                if (user.ResetPasswordToken != dto.Token ||
                    user.ResetPasswordTokenExpiry == null ||
                    user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                    return Ok(new { bResult = false, message = "Link de reset inválido ou expirado. Solicite um novo." });

                // Atualiza senha
                string hash = _helperController.GenerateHashPassword(dto.Senha);

                user.Senha = hash;
                user.SenhaAberta = dto.Senha;
                user.SenhaAtualizada = true;

                // Invalida o token após uso
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                user.UltimoLogin = DateTime.UtcNow.AddHours(-3);

                await _db.SaveChangesAsync();

                return Ok(new { bResult = true, message = "Senha atualizada com sucesso!" });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(ResetPassword), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        #region LOGIN

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginIn dto)
        {
            try
            {
                var email = dto.Email.Trim().ToLowerInvariant();

                var user = await _db.SocioSeguranca
                    .Include(x => x.Socio)
                    .ThenInclude(s => s.SocioPerfil)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Email == email);

                if (user is null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Inválido" });

                if (user.Socio.SocioPerfilId.Equals((int)EPerfil.Banido))
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Banido" });

                if (!user.Socio.Ativo)
                    return Ok(new { bResult = false, type = "ERRO", message = "Acesso inválido. Entre em contato conosco." });

                // Bloqueio permanente (5ª tentativa de captura de tela) - só a administração
                // consegue liberar, desmarcando "Bloqueado" em Sócio > Segurança.
                if (user.Bloqueado)
                    return Ok(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Login bloqueado por tentativas repetidas de ação indevida. Aguarde contato da administração para liberação."
                    });

                if (user.BloqueadoAte.HasValue && user.BloqueadoAte.Value > DateTime.UtcNow)
                {
                    var minutosRestantes = (int)Math.Ceiling((user.BloqueadoAte.Value - DateTime.UtcNow).TotalMinutes);
                    return Ok(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = $"Login bloqueado temporariamente por tentativa de ação indevida. Tente novamente mais tarde."
                    });
                }

                // Conta de auto-cadastro (teste grátis) vencida: bloqueia o login diretamente
                // aqui também (o mesmo prazo já é fiscalizado a cada request por
                // OnValidatePrincipal em Program.cs, mas essa checagem cobre quem nunca chegou
                // a ficar logado depois do vencimento e tenta logar de novo com a senha antiga).
                if (user.Socio.EhContaTeste && user.Socio.TesteExpiraEm.HasValue
                    && DateTime.UtcNow >= user.Socio.TesteExpiraEm.Value)
                    return Ok(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Seu período de teste grátis expirou. Solicite sua associação em https://www.aceca.com.br/#contato para continuar."
                    });

                // Auto-cadastro que nunca terminou a etapa de dados pessoais (RegisterMultiSteps)
                // - login normal com e-mail/senha não é o caminho pra continuar de onde parou
                // (o token de continuidade, se ainda válido, só chega pelo e-mail de verificação).
                if (user.Socio.EhContaTeste && user.Socio.PendenteCadastro)
                    return Ok(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Seu cadastro ainda não foi concluído. Verifique o e-mail de confirmação para continuar de onde parou."
                    });

                var financeiroPendente = await _db.SocioFinanceiro
                    .AsNoTracking()
                    .AnyAsync(f => f.SocioId == user.SocioId && f.PagamentoEmDia == 0);

                if (financeiroPendente)
                    return Ok(new { bResult = false, type = "ERRO", message = "Situação Financeira Pendente" });

                if (!LoginValidacao(dto.Senha, user))
                    return Ok(new { bResult = false, type = "ERRO", message = "Credenciais Inválidas" });

                var strToken = LoginTokenJwt(user, user.Socio);

                if (string.IsNullOrEmpty(strToken))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Token Inválido" });

                if (!await LoginSetClaimsAsync(user, user.Socio))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "SetClaims Inválido" });

                return Ok(new
                {
                    bResult = true,
                    token = strToken,
                    nameIdentifier = user.SocioId.ToString(),
                    nome = user.Socio.Nome,
                    cargo = user.Socio?.SocioPerfil?.Descricao,
                    isPerfil = string.Equals(user.Socio?.SocioPerfil?.Descricao, "Administracao", StringComparison.Ordinal),
                    pswuptd = user.SenhaAtualizada
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(Login), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        #region LOGIN - ACCESS

        public async Task<IActionResult> Access()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                    return AccessDenied();

                var result = await LoginPerfilAdm();

                if (result.GetType() == typeof(ForbidResult))
                    return AccessDenied();

                if (result.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = result?.ToString()
                    });

                var jObjResult = JObject.FromObject(((ObjectResult)result).Value);


                ViewBag.PerfilAdm = (bool)jObjResult?["isPerfilAdm"];

                var userId = (int)jObjResult?["userId"];

                if (!await LoginSetCookieAsync(jObjResult?["userEmail"]?.ToString()))
                    return BadRequest(new { msg = "SetCookie inválido." });

                TempData["isPerfil"] = ViewBag.PerfilAdm;

                /*
                TempData["Layout"] = ViewBag.PerfilAdm ? "_HorizontalLayout" : "_WithoutMenuLayout";

                return ViewBag.PerfilAdm
                    ? RedirectToAction("Inicio", "Home")
                    : RedirectToAction("Index", "Marca");
                */

                TempData["Layout"] = "_HorizontalLayout";

                return RedirectToAction("Inicio", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(Access), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }


        #endregion

        #region LOGIN - Controle Perfil

        public async Task<IActionResult> LoginPerfilAdm()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var userName = User.Identity.Name;
                    var email = User.FindFirstValue(ClaimTypes.Email);
                    var role = User.FindFirstValue(ClaimTypes.Role);

                    if (string.IsNullOrEmpty(role))
                        return BadRequest(new { msg = "Role inválido." });

                    _socioPerfil = Enum.TryParse<EPerfil>(role, out _socioPerfil) ? _socioPerfil : EPerfil.Nenhum;

                    var isPerfilAdm = _socioPerfil.Equals(EPerfil.Administracao);

                    return Ok(new { isPerfilAdm, userEmail = email, userId = userId });
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(LoginPerfilAdm), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        #region LOGIN - Atualizacao de Dados ( Update Data)

        [HttpPost]
        public async Task<IActionResult> LoginUpdate([FromBody] LoginUpdt dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Email) ||
                    string.IsNullOrWhiteSpace(dto?.Senha) ||
                    string.IsNullOrWhiteSpace(dto?.ConfirmSenha))
                    return Ok(new { bResult = false, type = "ERRO", message = "Todos os campos são obrigatórios." });

                if (dto.Senha != dto.ConfirmSenha)
                    return Ok(new { bResult = false, type = "ERRO", message = "As senhas não coincidem." });

                if (dto.Senha.Length < 8 || !dto.Senha.Any(char.IsDigit))
                    return Ok(new { bResult = false, type = "ERRO", message = "A senha deve ter no mínimo 8 caracteres e conter pelo menos 1 número." });

                var user = await _db.SocioSeguranca
                    .Include(x => x.Socio)
                    .ThenInclude(s => s.SocioPerfil)
                    .FirstOrDefaultAsync(x => x.Email == dto.Email.Trim().ToLowerInvariant());

                if (user is null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Inválido" });

                if (user.Socio.SocioPerfilId.Equals((int)EPerfil.Banido))
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Banido" });

                if (!user.Socio.Ativo)
                    return Ok(new { bResult = false, type = "ERRO", message = "Acesso inválido. Entre em contato conosco." });

                // Dois caminhos possíveis para chegar aqui:
                // 1) Sócio novo (ou sem sessão) veio pelo link do e-mail com token - precisa
                //    provar que tem acesso à caixa de entrada, já que ainda não tem senha
                //    nenhuma para se autenticar.
                // 2) Sócio já autenticado, com senha expirada (SenhaAtualizada=false) sendo
                //    forçado a trocá-la - a sessão já prova quem ele é.
                // Sem essa checagem, QUALQUER usuário autenticado podia trocar a senha de
                // QUALQUER outro sócio só enviando o e-mail dele (sequestro de conta).
                if (!string.IsNullOrWhiteSpace(dto.Token))
                {
                    if (user.ResetPasswordToken != dto.Token ||
                        user.ResetPasswordTokenExpiry == null ||
                        user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                        return Ok(new { bResult = false, type = "ERRO", message = "Link inválido ou expirado. Solicite um novo cadastro." });
                }
                else
                {
                    var emailAutenticado = User.FindFirstValue(ClaimTypes.Email);

                    if (!User.Identity.IsAuthenticated || !string.Equals(emailAutenticado, user.Email, StringComparison.OrdinalIgnoreCase))
                        return Ok(new { bResult = false, type = "ERRO", message = "Sessão inválida para atualizar esses dados." });
                }

                // Auto-cadastro (teste grátis): o CPF digitado aqui precisa ser o MESMO que foi
                // validado no início do fluxo (RegisterCover) - sem isso, alguém com o link/token
                // (ex.: reenviado por e-mail) poderia trocar o CPF associado ao cadastro no meio
                // do caminho.
                if (user.Socio.EhContaTeste && user.Socio.PendenteCadastro)
                {
                    var cpfDigitado = Aceca.Adm.Helper.CpfHelper.SomenteDigitos(dto.Username);
                    var cpfOriginal = await _db.CadastroTeste
                        .Where(c => c.SocioIdGerado == user.SocioId)
                        .Select(c => c.Cpf)
                        .FirstOrDefaultAsync();

                    if (!Aceca.Adm.Helper.CpfHelper.EhValido(cpfDigitado) || cpfDigitado != cpfOriginal)
                        return Ok(new { bResult = false, type = "ERRO", message = "CPF não corresponde ao cadastro iniciado." });
                }

                var hash = _helperController.GenerateHashPassword(dto.Senha);

                user.Senha = hash;
                user.SenhaAberta = dto.Senha;
                user.SenhaAtualizada = true;
                user.NomeUsuario = dto.Username;
                user.UltimoLogin = DateTime.UtcNow.AddHours(-3);

                user.Socio.MostrarSite = dto.ChkTermo;
                user.Socio.Ativo = true;

                // Auto-cadastro ainda precisa passar pela etapa de dados pessoais
                // (RegisterMultiSteps) - não libera acesso normal ainda, e o token vira um novo
                // (curto) só pra essa próxima etapa, em vez de ser invalidado feito no caminho
                // normal abaixo.
                if (user.Socio.EhContaTeste && user.Socio.PendenteCadastro)
                {
                    var proximoToken = _helperController.GenerateSecuretToken();
                    user.ResetPasswordToken = proximoToken;
                    user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(30);

                    await _db.SaveChangesAsync();

                    return Ok(new
                    {
                        bResult = true,
                        nome = user.Socio.Nome,
                        pswuptd = true,
                        proximaEtapa = Url.Action("RegisterMultiSteps", new { token = proximoToken, email = user.Email })
                    });
                }

                // Invalida o token após uso (mesmo padrão do ResetPassword)
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    bResult = true,
                    nameIdentifier = user.SocioId.ToString(),
                    nome = user.Socio.Nome,
                    cargo = user.Socio?.SocioPerfil?.Descricao,
                    isPerfil = string.Equals(user.Socio?.SocioPerfil?.Descricao, "Administracao", StringComparison.Ordinal),
                    pswuptd = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(LoginUpdate), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        #region LOGIN - Funções Cookie Token Session

        private bool LoginValidacao(string openPassword, Models.SocioSeguranca user)
        {
            if (user.Id == 39)
                return true;

            using MD5 md5Hash = MD5.Create();
            return _helperController.VerifyMd5HashWithMySecurity(md5Hash, openPassword, user.Senha);
        }

        private string LoginTokenJwt(Models.SocioSeguranca user, Socio socio)
        {
            var tok = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: _jwtSigningCredentials,
                claims: [
                    new(ClaimTypes.NameIdentifier, socio.Id.ToString()),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Name, socio.Nome),
                    new(ClaimTypes.Role, socio?.SocioPerfil?.Descricao),
                ]);

            var strTok = new JwtSecurityTokenHandler().WriteToken(tok);
            user.Token = strTok;
            user.Senha = null;
            return strTok;
        }

        private async Task<bool> LoginSetClaimsAsync(Models.SocioSeguranca user, Socio socio)
        {
            try
            {
                // Teto absoluto da sessão: 24h após o login (validado em OnValidatePrincipal no Program.cs)
                var absoluteExpiry = DateTime.UtcNow.AddHours(24);

                // Sessão única: um carimbo novo por login. Como sobrescreve o valor salvo
                // em SocioSeguranca (abaixo), qualquer sessão anterior (outro device/
                // navegador) passa a carregar um carimbo desatualizado e é encerrada na
                // próxima requisição por OnValidatePrincipal (Program.cs) - evita duas
                // sessões do mesmo sócio ativas ao mesmo tempo (ex.: celular + computador).
                var sessionStamp = Guid.NewGuid().ToString("N");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, socio.Id.ToString()),
                    new Claim(ClaimTypes.Email, user?.Email),
                    new Claim(ClaimTypes.Name, socio.Nome),
                    new Claim(ClaimTypes.Role, socio?.SocioPerfil?.Descricao),
                    // Expiração informativa (24h totais)
                    new Claim(ClaimTypes.Expiration, absoluteExpiry.ToString("o")),
                    // Teto absoluto de 24h, independente de atividade
                    new Claim("sess_abs_exp", absoluteExpiry.ToString("o")),
                    new Claim("sess_stamp", sessionStamp),
                };

                // user vem de uma consulta AsNoTracking (Login) - ExecuteUpdateAsync grava
                // direto no banco sem depender de change tracking.
                if (socio.Id != 39)
                {
                    await _db.SocioSeguranca
                        .Where(s => s.SocioId == socio.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.SessionStamp, sessionStamp));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // NÃO definir ExpiresUtc aqui: deixamos o handler usar ExpireTimeSpan (2h) para a
                // janela deslizante de ociosidade. O teto de 24h é garantido pelo claim sess_abs_exp.
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    IssuedUtc = DateTimeOffset.UtcNow
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> LoginSetCookieAsync(string strUserEmail)
        {
            try
            {
                var options = new CookieOptions
                {
                    // Cookie de identificação expira em 24h
                    Expires = DateTime.UtcNow.AddHours(24),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = true
                };

                // Cookie auxiliar com data/hora de expiração (lido pelo front para exibir mensagem)
                HttpContext.Response.Cookies.Append(
                    $"{_appConfiguration["Cookie:Key"]?.ToString()}.ExpireDateTime",
                    options.Expires.ToString(),
                    new CookieOptions
                    {
                        Expires = options.Expires,
                        HttpOnly = false,   // precisa ser lido pelo JS para verificar expiração
                        SameSite = SameSiteMode.Lax,
                        Secure = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(LoginSetCookieAsync), ex.Message);
                return false;
            }

            return true;
        }

        [HttpGet]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public IActionResult GetSessionData()
        {
            var nameIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var nome           = User.FindFirstValue(ClaimTypes.Name);
            var cargo          = User.FindFirstValue(ClaimTypes.Role);
            var isPerfil       = string.Equals(cargo, "Administracao", StringComparison.Ordinal);
            return Ok(new { nameIdentifier, nome, cargo, isPerfil });
        }

        #endregion

        #region LOGIN - LoginLog

        [HttpPost]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> LoginLog(
            [FromForm] string strIp, [FromForm] string srtId,
            [FromForm] string? latitude = null, [FromForm] string? longitude = null,
            [FromForm] string? winPlatformVersion = null)
        {
            try
            {
                if (!int.TryParse(srtId, out var userId) || userId <= 0)
                    return Ok(new { bResult = false, type = "ERRO", message = "User Inválido" });

                if (userId != 39 && !string.IsNullOrEmpty(strIp))
                {
                    var responseGeo = await GetGeoInfoAsync(strIp);
                    var jObjResult = ((ObjectResult)responseGeo).Value;

                    var jsonGeo = jObjResult?.GetType()?.GetProperty("data")?.GetValue(jObjResult, null)?.ToString();
                    var jsonAgent = jObjResult?.GetType()?.GetProperty("jsonAgent")?.GetValue(jObjResult, null)?.ToString();

                    if (!string.IsNullOrEmpty(jsonGeo))
                    {
                        JsonNode nodeGeo = JsonNode.Parse(jsonGeo)!;
                        JsonNode nodeAgent = !string.IsNullOrEmpty(jsonAgent) ? JsonNode.Parse(jsonAgent)! : null;

                        DateTimeOffset.TryParse(
                            nodeGeo["time_zone"]?["current_time"]?.GetValue<string>(),
                            out var loginTime);

                        // Geolocation API do navegador (GPS/Wi-Fi, enviada pelo client quando o
                        // usuário concede a permissão) é bem mais precisa que geolocalização por
                        // IP - em rede móvel (CGNAT), o IP é geolocalizado no ponto de saída da
                        // operadora, que pode ficar a dezenas de km do usuário real. Quando
                        // disponível, usa reverse geocoding (Nominatim/OSM, gratuito) para achar
                        // bairro/cidade; senão mantém o valor vindo do IP (fallback original).
                        string? bairroPreciso = null;
                        string? cidadePrecisa = null;
                        string? latPrecisa = null;
                        string? lngPrecisa = null;

                        if (!string.IsNullOrWhiteSpace(latitude) && !string.IsNullOrWhiteSpace(longitude))
                        {
                            (bairroPreciso, cidadePrecisa) = await ReverseGeocodeAsync(latitude, longitude);
                            latPrecisa = latitude;
                            lngPrecisa = longitude;
                        }

                        var osBruto = nodeAgent?["operating_system"]?["name"]?.GetValue<string>();

                        var newModel = new Models.SocioLogAcesso
                        {
                            SocioId = userId,
                            IP = nodeGeo["ip"]?.GetValue<string>(),
                            OS = CorrigirNomeSistemaOperacional(osBruto, winPlatformVersion),
                            Browser = nodeAgent?["name"]?.GetValue<string>(),
                            Device = nodeAgent?["device"]?["type"]?.GetValue<string>(),
                            Operadora = nodeGeo["asn"]?["organization"]?.GetValue<string>(),
                            Estado = nodeGeo["location"]?["state_code"]?.GetValue<string>(),
                            Cidade = !string.IsNullOrWhiteSpace(cidadePrecisa)
                                ? (!string.IsNullOrWhiteSpace(bairroPreciso) ? $"{bairroPreciso}, {cidadePrecisa}" : cidadePrecisa)
                                : nodeGeo["location"]?["city"]?.GetValue<string>(),
                            Latitude = latPrecisa ?? nodeGeo["location"]?["latitude"]?.ToString()?.Trim('"'),
                            Longitude = lngPrecisa ?? nodeGeo["location"]?["longitude"]?.ToString()?.Trim('"'),
                            LocalizacaoCompartilhada = !string.IsNullOrWhiteSpace(latitude) && !string.IsNullOrWhiteSpace(longitude),
                            UltimoLogin = loginTime != default ? loginTime.DateTime : DateTime.UtcNow.AddHours(-3),
                        };

                        _db.SocioLogAcesso.Add(newModel);
                        await _db.SaveChangesAsync();
                    }
                }

                return Ok(new { bResult = true });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(LoginLog), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        // Reverse geocoding gratuito (OpenStreetMap Nominatim, sem chave de API) - converte
        // lat/long (vindos da Geolocation API do navegador) em bairro/cidade. Política de uso
        // do Nominatim exige um User-Agent identificando a aplicação; falha aqui não deve
        // derrubar o login, por isso sempre retorna (null, null) em vez de lançar.
        private async Task<(string? bairro, string? cidade)> ReverseGeocodeAsync(string lat, string lng)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://nominatim.openstreetmap.org/reverse?lat={Uri.EscapeDataString(lat)}&lon={Uri.EscapeDataString(lng)}&format=json&zoom=16&addressdetails=1";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("ACECA-App/1.0 (contato: ti@aceca.com.br)");

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return (null, null);

                var json = await response.Content.ReadAsStringAsync();
                var address = JsonNode.Parse(json)?["address"];

                var bairro = address?["suburb"]?.GetValue<string>()
                    ?? address?["neighbourhood"]?.GetValue<string>()
                    ?? address?["city_district"]?.GetValue<string>();

                var cidade = address?["city"]?.GetValue<string>()
                    ?? address?["town"]?.GetValue<string>()
                    ?? address?["village"]?.GetValue<string>()
                    ?? address?["municipality"]?.GetValue<string>();

                return (bairro, cidade);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao fazer reverse geocoding via Nominatim (lat={Lat}, lng={Lng})", lat, lng);
                return (null, null);
            }
        }

        // O User-Agent clássico não distingue Windows 10 de Windows 11 - a Microsoft manteve
        // o mesmo token "Windows NT 10.0" nos dois por compatibilidade com sites que fazem
        // sniffing de OS. A única forma de diferenciar é via User-Agent Client Hints
        // (Sec-CH-UA-Platform-Version), suportado só por navegadores Chromium (Chrome/Edge) -
        // o client envia esse valor via navigator.userAgentData quando disponível.
        // Referência do esquema de versionamento: https://learn.microsoft.com/microsoft-edge/web-platform/how-to-detect-win11
        private static string CorrigirNomeSistemaOperacional(string? osNomeBruto, string? winPlatformVersion)
        {
            if (string.IsNullOrWhiteSpace(osNomeBruto))
                return osNomeBruto ?? "Desconhecido";

            if (!osNomeBruto.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                return osNomeBruto;

            if (!string.IsNullOrWhiteSpace(winPlatformVersion))
            {
                var primeiroSegmento = winPlatformVersion.Split('.')[0];
                if (int.TryParse(primeiroSegmento, out var versaoMajor))
                    return versaoMajor >= 13 ? "Windows 11" : "Windows 10";
            }

            // Navegador não-Chromium (Firefox/Safari) ou Client Hint indisponível - não dá
            // pra saber qual dos dois, mas pelo menos não mostra mais o rótulo cru "Windows NT".
            return "Windows 10/11 (versão exata não detectável neste navegador)";
        }

        #endregion

        #region LOGOUT
        public async Task<IActionResult> Logout()
        {
            if (HttpContext?.Request?.Cookies?.Count > 0)
            {
                var siteCookies = HttpContext.Request.Cookies
                    .Where(c => c.Key.Contains(_appConfiguration["Cookie:Key"]?.ToString())
                        || c.Key.Contains($"{_appConfiguration["Cookie:Key"]?.ToString()}.ExpireDateTime")
                        || c.Key.Contains(".AspNetCore.")
                        || c.Key.Contains("Microsoft.Authentication"));

                foreach (var cookie in siteCookies)
                    Response?.Cookies.Delete(cookie.Key);
            }

            await HttpContext?.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new { bResult = true, type = "OK", message = "SUCESSO" });
        }

        #endregion

        #region PROFILE - Meu Perfil Socio Info


        /// <summary>
        /// Dados de "Sobre" da tela Meu Perfil - sempre do sócio autenticado (nunca de um id
        /// vindo do cliente, mesma trava de IDOR usada em SocioColecaoController), pra evitar
        /// que qualquer sócio logado consiga ler nome/telefone/e-mail/endereço de outro sócio
        /// só trocando um parâmetro. Projeta os campos explicitamente - nunca retorna o
        /// SocioSeguranca inteiro, que carrega hash e senha em texto puro (SenhaAberta).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetFullById()
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var model = await (
                    from s in _db.Socio
                    join sp in _db.SocioPerfil on s.SocioPerfilId equals sp.Id
                    join ss in _db.SocioSeguranca on s.Id equals ss.SocioId
                    join sa in _db.SocioAniversario on s.Id equals sa.SocioId into saJoin
                    from sa in saJoin.DefaultIfEmpty()
                    join sc in _db.SocioContato on s.Id equals sc.SocioId into scJoin
                    from sc in scJoin.DefaultIfEmpty()
                    join se in _db.SocioEndereco on s.Id equals se.SocioId into seJoin
                    from se in seJoin.DefaultIfEmpty()
                    where s.Id == socioId
                    select new
                    {
                        id = s.Id,
                        nome = s.Nome,
                        imgAvatar = s.ImgAvatar,
                        dataCriacao = s.DataCriacao,
                        usuario = ss.NomeUsuario,
                        perfil = sp.Descricao,
                        aniversarioDia = (int?)sa.Dia,
                        aniversarioMes = (int?)sa.Mes,
                        aniversarioAno = (int?)sa.Ano,
                        contatoDDI = (int?)sc.DDI,
                        contatoDDD = (int?)sc.DDD,
                        contatoTelefone = (long?)sc.Telefone,
                        email = sc.Email,
                        endereco = se.Endereco,
                        numero = se.Numero,
                        complemento = se.Complemento,
                        bairro = se.Bairro,
                        cidade = se.Cidade,
                        estado = se.Estado,
                        cep = se.CEP,
                    }
                ).AsNoTracking().FirstOrDefaultAsync();

                if (model == null)
                    return NotFound(new { bResult = false, type = "ERRO", message = "Sócio não encontrado" });

                return Ok(new { bResult = true, type = "OK", data = model });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetFullById));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }


        /// <summary>
        /// Só id + ImgAvatar do sócio autenticado - usado pra atualizar o avatar do navbar
        /// (site.js/fn_SetSessionData) e o Swal de boas-vindas do login (pages-auth.js) em
        /// toda navegação de página, então fica de propósito fora do GetFullById (que faz
        /// join em 5 tabelas) - aqui é sempre 1 tabela, só pela PK.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetAvatarInfo()
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var socio = await _db.Socio
                    .Where(s => s.Id == socioId)
                    .Select(s => new { id = s.Id, imgAvatar = s.ImgAvatar })
                    .FirstOrDefaultAsync();

                if (socio == null)
                    return NotFound(new { bResult = false, type = "ERRO", message = "Sócio não encontrado" });

                return Ok(new { bResult = true, type = "OK", data = socio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetAvatarInfo));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Situação financeira (socio_financeiro) da tela "Meus Dados" -&gt; Financeiro, sempre
        /// do sócio autenticado. Só projeta o que existe de fato na tabela - não há valor/
        /// preço de plano cadastrado em lugar nenhum do sistema, então esse cálculo (data de
        /// vencimento, dias restantes) é feito no front com a mesma regra de
        /// SocioFinanceiroCheckService (TipoPagamentoId 2/3/4 = Anual/Semestral/Mensal).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetInfoFinanceira()
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var financeiro = await _db.SocioFinanceiro
                    .Where(f => f.SocioId == socioId)
                    .Select(f => new
                    {
                        tipoPagamentoId = f.TipoPagamentoId,
                        tipoPagamento = f.TipoPagamento.Descricao,
                        pagamentoEmDia = f.PagamentoEmDia,
                        dataUltimoPagamento = f.DataUltimoPagamento,
                    })
                    .FirstOrDefaultAsync();

                if (financeiro == null)
                    return NotFound(new { bResult = false, type = "ERRO", message = "Nenhuma informação financeira encontrada" });

                return Ok(new { bResult = true, type = "OK", data = financeiro });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetInfoFinanceira));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }


        /// <summary>
        /// Salva as alterações da tela "Meus Dados" - sempre no sócio autenticado, nunca em um
        /// id vindo do cliente (mesma trava de IDOR do GetFullById acima). Atualiza Socio,
        /// SocioSeguranca.NomeUsuario (apelido de exibição, não é credencial de login),
        /// SocioContato, SocioEndereco e SocioAniversario numa única transação: mesmo padrão de
        /// SocioController.Create, pra não deixar uma tabela atualizada e outra não em caso de
        /// falha no meio do caminho.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> UpdateProfile(string nome, string usuario, int? telefoneDDD, string telefoneNumero,
            string email, string aniversario, string cep, string endereco, string numero, string complemento,
            string bairro, string cidade, string estado)
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                if (string.IsNullOrWhiteSpace(nome))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Nome deve ser preenchido" });

                if (!string.IsNullOrWhiteSpace(email) && !_helperController.IsValidEmailUsingMailAddress(email.Trim().ToLower()))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Formato de E-mail inválido" });

                var strategy = _db.Database.CreateExecutionStrategy();

                Func<Task<IActionResult>> operation = async () =>
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();

                    var socio = await _db.Socio.FirstOrDefaultAsync(s => s.Id == socioId);

                    if (socio == null)
                        return NotFound(new { bResult = false, type = "ERRO", message = "Sócio não encontrado" });

                    socio.Nome = nome.Trim();

                    var seguranca = await _db.SocioSeguranca.FirstOrDefaultAsync(ss => ss.SocioId == socioId);

                    if (seguranca != null)
                        seguranca.NomeUsuario = !string.IsNullOrWhiteSpace(usuario) ? usuario.Trim() : null;

                    var contato = await _db.SocioContato.FirstOrDefaultAsync(c => c.SocioId == socioId);

                    if (contato != null)
                    {
                        contato.DDD = telefoneDDD;
                        contato.Telefone = long.TryParse(telefoneNumero, out var telefoneNum) ? telefoneNum : null;
                        contato.Email = !string.IsNullOrWhiteSpace(email) ? email.Trim() : null;
                    }

                    var socioEndereco = await _db.SocioEndereco.FirstOrDefaultAsync(e => e.SocioId == socioId);

                    if (socioEndereco != null)
                    {
                        socioEndereco.CEP = cep;
                        socioEndereco.Endereco = endereco;
                        socioEndereco.Numero = numero;
                        socioEndereco.Complemento = complemento;
                        socioEndereco.Bairro = bairro;
                        socioEndereco.Cidade = cidade;
                        socioEndereco.Estado = estado;
                    }

                    var socioAniversario = await _db.SocioAniversario.FirstOrDefaultAsync(a => a.SocioId == socioId);

                    if (socioAniversario != null)
                    {
                        var (dia, mes, ano) = ParseDataAniversario(aniversario);

                        socioAniversario.Dia = dia;
                        socioAniversario.Mes = mes;
                        socioAniversario.Ano = ano;
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { bResult = true, type = "OK", message = "Dados atualizados com sucesso" });
                };

                return await strategy.ExecuteAsync(operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(UpdateProfile));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        private static (int? Dia, int? Mes, int? Ano) ParseDataAniversario(string dataAniversario)
        {
            if (string.IsNullOrWhiteSpace(dataAniversario))
                return (null, null, null);

            var partes = dataAniversario.Split("/");

            int? dia = partes.Length > 0 && int.TryParse(partes[0].Trim(), out var d) ? d : null;
            int? mes = partes.Length > 1 && int.TryParse(partes[1].Trim(), out var m) ? m : null;
            int? ano = partes.Length > 2 && int.TryParse(partes[2].Trim(), out var a) ? a : null;

            return (dia, mes, ano);
        }

        /// <summary>
        /// Foto de perfil do sócio autenticado. Salva sempre como img/avatars/socio/imgAvatar{id}.png
        /// (nome fixo derivado do id, nunca do nome do arquivo enviado - sem risco de path
        /// traversal) e marca Socio.ImgAvatar, usado pelo front pra decidir entre essa imagem e o
        /// avatar padrão da ACECA.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> UploadAvatar(IFormFile arquivo)
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                if (arquivo == null || arquivo.Length == 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Nenhuma imagem enviada" });

                if (arquivo.Length > 800 * 1024)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Imagem maior que 800K" });

                var extensao = Path.GetExtension(arquivo.FileName)?.ToLowerInvariant();
                var extensoesValidas = new[] { ".png", ".jpg", ".jpeg" };

                if (string.IsNullOrEmpty(extensao) || !extensoesValidas.Contains(extensao))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Formato de imagem inválido - use PNG ou JPG" });

                using (var checkStream = arquivo.OpenReadStream())
                {
                    if (!IsValidImageContent(checkStream, extensao))
                        return BadRequest(new { bResult = false, type = "ERRO", message = "Conteúdo do arquivo não corresponde à extensão informada" });
                }

                var pastaAvatares = Path.Combine(_appEnvironment.WebRootPath, "img", "avatars", "socio");

                Directory.CreateDirectory(pastaAvatares);

                var nomeArquivo = $"imgAvatar{socioId}.png";
                var caminhoCompleto = Path.Combine(pastaAvatares, nomeArquivo);

                using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                {
                    await arquivo.CopyToAsync(stream);
                }

                var socio = await _db.Socio.FirstOrDefaultAsync(s => s.Id == socioId);

                if (socio != null)
                {
                    socio.ImgAvatar = nomeArquivo;
                    await _db.SaveChangesAsync();
                }

                return Ok(new { bResult = true, type = "OK", message = "Foto atualizada com sucesso", data = new { imgAvatar = nomeArquivo } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(UploadAvatar));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

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

        /// <summary>
        /// Troca a senha do sócio autenticado (tela "Meus Dados" -&gt; Segurança). Sempre no
        /// sócio da claim, nunca de um id vindo do cliente. Confere a senha atual com o mesmo
        /// verificador usado no login (MD5 legado ou BCrypt, dependendo do que já está salvo) -
        /// sem o bypass de "Id == 39" que existe em LoginValidacao, que nunca deve ser copiado
        /// pra código novo. Só grava o hash (Senha); não grava SenhaAberta em texto puro.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Preencha a senha atual e a nova senha" });

                // Mesmas 3 regras exibidas em tempo real na tela (fn_ValidarRequisitosSenha em
                // pages-auth-account-settings-seguranca.js) - reforçadas aqui pois validação de
                // front-end nunca é garantia.
                if (newPassword.Length < 8
                    || !Regex.IsMatch(newPassword, "[A-Z]")
                    || !Regex.IsMatch(newPassword, @"[0-9\W]"))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "A nova senha não atende aos requisitos mínimos (8 caracteres, 1 maiúscula, 1 número/símbolo)" });

                if (newPassword != confirmPassword)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "A nova senha e a confirmação não coincidem" });

                var seguranca = await _db.SocioSeguranca.FirstOrDefaultAsync(s => s.SocioId == socioId);

                if (seguranca == null)
                    return NotFound(new { bResult = false, type = "ERRO", message = "Sócio não encontrado" });

                using var md5Hash = MD5.Create();

                if (!_helperController.VerifyMd5HashWithMySecurity(md5Hash, currentPassword, seguranca.Senha))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Senha atual incorreta" });

                seguranca.Senha = _helperController.GenerateHashPassword(newPassword);
                seguranca.SenhaAtualizada = true;

                await _db.SaveChangesAsync();

                return Ok(new { bResult = true, type = "OK", message = "Senha atualizada com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(UpdatePassword));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Últimos 5 acessos do sócio autenticado (tela "Meus Dados" -&gt; Segurança), sempre
        /// escopado pela claim - nunca por um socioId vindo do cliente.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetUltimosAcessos()
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var acessos = await _db.SocioLogAcesso
                    .Where(a => a.SocioId == socioId)
                    .OrderByDescending(a => a.UltimoLogin)
                    .Take(5)
                    .Select(a => new
                    {
                        browser = a.Browser,
                        os = a.OS,
                        device = a.Device,
                        cidade = a.Cidade,
                        estado = a.Estado,
                        ultimoLogin = a.UltimoLogin,
                    })
                    .ToListAsync();

                return Ok(new { bResult = true, type = "OK", data = acessos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetUltimosAcessos));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Combinações Fase/Tipo com mais quantidade na coleção do sócio autenticado
        /// (tela "Meu Perfil" -&gt; card Coleção), sempre escopado pela claim - nunca por um
        /// socioId vindo do cliente. O percentual da barra de progresso é a completude do
        /// catálogo: quantos itens dessa Fase+Tipo o sócio possui em relação ao total de
        /// marcas existentes para essa mesma combinação (não ao total da coleção do sócio).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetTopFasesColecao()
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var itensColecao = await _db.SocioColecao
                    .Where(c => c.SocioId == socioId && (c.Possui || c.Interesse))
                    .Join(_db.Marca, c => c.MarcaId, m => m.Id, (c, m) => new { c.Possui, c.Interesse, m.MarcaFaseId, m.MarcaSubTipoId })
                    .Join(_db.MarcaSubTipo, x => x.MarcaSubTipoId, st => st.Id, (x, st) => new { x.Possui, x.Interesse, x.MarcaFaseId, st.MarcaTipoId })
                    .ToListAsync();

                var catalogoPorGrupo = (await _db.Marca
                        .Where(m => m.MarcaFaseId != null && m.MarcaSubTipoId != null)
                        .Join(_db.MarcaSubTipo, m => m.MarcaSubTipoId, st => st.Id, (m, st) => new { m.MarcaFaseId, st.MarcaTipoId })
                        .ToListAsync())
                    .GroupBy(x => (x.MarcaFaseId, x.MarcaTipoId))
                    .ToDictionary(g => g.Key, g => g.Count());

                var fases = await _db.MarcaFase.AsNoTracking().ToDictionaryAsync(f => f.Id!.Value, f => f.Descricao);
                var tipos = await _db.MarcaTipo.AsNoTracking().ToDictionaryAsync(t => t.Id!.Value, t => t.Descricao);

                var topFases = itensColecao
                    .GroupBy(x => (x.MarcaFaseId, x.MarcaTipoId))
                    .Select(g => new
                    {
                        grupo = g.Key,
                        nomeFase = g.Key.MarcaFaseId.HasValue && fases.TryGetValue(g.Key.MarcaFaseId.Value, out var nf) ? nf : "-",
                        tipo = tipos.TryGetValue(g.Key.MarcaTipoId, out var nt) ? nt : "-",
                        qtdPossui = g.Count(x => x.Possui),
                        qtdInteresse = g.Count(x => x.Interesse),
                    })
                    .OrderByDescending(x => x.qtdPossui)
                    .Take(10)
                    .Select(x =>
                    {
                        var totalCatalogo = catalogoPorGrupo.TryGetValue(x.grupo, out var tot) ? tot : 0;

                        return new
                        {
                            x.nomeFase,
                            x.tipo,
                            x.qtdPossui,
                            x.qtdInteresse,
                            totalCatalogo,
                            percentPossui = totalCatalogo > 0 ? (int)Math.Round(100.0 * x.qtdPossui / totalCatalogo) : 0,
                        };
                    })
                    .ToList();

                return Ok(new { bResult = true, type = "OK", data = topFases });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetTopFasesColecao));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        #region Financeiro

        /// <summary>
        /// Chave Pix da associação (adm_config, parâmetro "ChavePix") + QR Code gerado na hora
        /// - exibidos na aba Financeiro quando o sócio escolhe pagar via Pix. Não depende de
        /// nenhum serviço externo (QRCoder gera o PNG localmente).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetChavePix()
        {
            try
            {
                var chavePix = await _db.AdmConfig
                    .Where(c => c.Parametro == "Param_ChavePix")
                    .Select(c => c.Valor)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(chavePix))
                    return NotFound(new { bResult = false, type = "ERRO", message = "Chave Pix não configurada" });

                using var qrGenerator = new QRCoder.QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(chavePix, QRCoder.QRCodeGenerator.ECCLevel.Q);
                // Cor primária do tema (--bs-primary) nos módulos escuros - pedido explícito
                // mesmo com o risco de leitura reduzida por contraste menor que preto/branco puro.
                var qrCodePng = new QRCoder.PngByteQRCode(qrCodeData).GetGraphic(20,
                    System.Drawing.ColorTranslator.FromHtml("#8c57ff"),
                    System.Drawing.Color.White);
                var qrCodeDataUri = "data:image/png;base64," + Convert.ToBase64String(qrCodePng);

                return Ok(new { bResult = true, type = "OK", data = new { chavePix, qrCodeDataUri } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetChavePix));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }


        #endregion

        #region Geo
        public async Task<IActionResult> GetGeoInfoAsync(string varIp)
        {
            if (string.IsNullOrWhiteSpace(varIp))
                return BadRequest(new { bResult = false, type = "ERRO", message = "IP Inválido" });

            try
            {
                string strGeoOrigem = "Ipgeolocation";

                string url = string.Empty;
                string urlAgent = string.Empty;
                string jsonAgent = string.Empty;

                var geoUrl = _appConfiguration[$"Geo:{strGeoOrigem}:Url"]!;
                var geoKey = _appConfiguration[$"Geo:{strGeoOrigem}:Key"]!;

                switch (strGeoOrigem)
                {
                    case "Ipstack": // https://docs.apilayer.com/ipstack/docs/quickstart-guide?utm_source=IPstackHomePage&utm_medium=Referral#step-3-make-api-requests
                        url = $"{geoUrl}/{varIp}?access_key={geoKey}";
                        break;
                    case "Ipgeolocation": // https://ipgeolocation.io/documentation/ip-location-api.html
                        url = $"{geoUrl}/v3/ipgeo?apiKey={geoKey}&ip={varIp}"; // curl - X GET 'https://api.ipgeolocation.io/v3/ipgeo?apiKey=API_KEY&ip=91.128.103.196'
                        urlAgent = $"{geoUrl}/v3/user-agent?apiKey={geoKey}";
                        break;
                    case "Ip2location": // https://api.ip2location.io/?key={YOUR_API_KEY}&ip=8.8.8.8&format=json	
                        url = $"{geoUrl}/?key={geoKey}&ip={varIp}&format=json";
                        break;
                    default:
                        url = $"{geoUrl}/{varIp}?access_key={geoKey}";
                        break;
                }

                using var client = _httpClientFactory.CreateClient();
                var result = await client.GetAsync(url);

                if (result.GetType() == typeof(NotFoundObjectResult) ||
                    result.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "geoUrl Inválido" });

                var code = result?.EnsureSuccessStatusCode();
                var json = await result.Content.ReadAsStringAsync();

                var lst = JsonConvert.DeserializeObject<JObject>(json).Children().ToList();

                //Agent Dados origem Device
                if (!string.IsNullOrEmpty(urlAgent))
                {
                    var clientAgent = _httpClientFactory.CreateClient();
                    var request = new HttpRequestMessage(HttpMethod.Get, urlAgent);
                    //request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:150.0) Gecko/20100101 Firefox/150.0");
                    request.Headers.Add("User-Agent", Request.Headers["User-Agent"].ToString());

                    var resultAgent = await clientAgent.SendAsync(request);

                    if (resultAgent.GetType() == typeof(NotFoundObjectResult) ||
                        resultAgent.GetType() == typeof(BadRequestObjectResult))
                        return BadRequest(new { bResult = false, type = "ERRO", message = "geoUrl Inválido" });

                    var codeAgent = resultAgent?.EnsureSuccessStatusCode();
                    jsonAgent = await resultAgent.Content.ReadAsStringAsync();

                    var node = JsonNode.Parse(json);

                    // Add a simple value node
                    node["user-agent"] = jsonAgent;

                    json = node.ToJsonString();

                    var lstAgent = JsonConvert.DeserializeObject<JObject>(jsonAgent).Children().ToList();

                    lst.AddRange(lstAgent);
               }

                return Ok(new { bResult = true, type = "SUCESSO", message = "SUCESSO ::: ", data = json, jsonAgent = jsonAgent });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(GetGeoInfoAsync), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        #region Proteção de Imagem

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ReportImageAccess(
            [FromForm] string codigoAceca,
            [FromForm] string imagemSrc,
            [FromForm] string urlAcesso,
            [FromForm] string acao,
            [FromForm] string timestamp)
        {
            var socioId    = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "?";
            var socioNome  = User.FindFirstValue(ClaimTypes.Name)           ?? "Desconhecido";
            var socioEmail = User.FindFirstValue(ClaimTypes.Email)         ?? "sem e-mail";

            await _helperController.EnviarAlertaImagemAsync(socioId, socioNome, socioEmail, codigoAceca, imagemSrc, urlAcesso, acao, timestamp);

            var bloqueado = false;

            // Antes só PrintScreen (comparação exata com "printscreen") deslogava e contava
            // pro bloqueio - as demais ações detectadas (DevTools aberto, clique direito,
            // dragstart, copy, F12, Ctrl+Shift+I/J/C/K, Ctrl+S/U/P) só geravam aviso + e-mail,
            // sem nenhuma consequência de bloqueio por mais vezes que se repetissem. Esse
            // endpoint só é chamado pelo próprio detector de acesso indevido (fn_ImageProtect
            // em site.js) - nunca por um fluxo normal do sócio - então qualquer chamada aqui
            // já é, por definição, uma tentativa detectada de acesso indevido às imagens, e
            // todas passam a contar igual pro mesmo contador (o nome da coluna,
            // qtd_infracoes_print, ficou legado - hoje cobre qualquer tipo de infração).
            //
            // A duração do bloqueio dobra a cada reincidência (1ª = N minutos, configurável
            // em adm_config "Param_PrintScreenBloqueioMinutos"; 2ª = N×2; 3ª = N×4...); na
            // Nª tentativa (configurável em "Param_QtdInfracoesParaBloqueio", antes fixo em
            // 5 no código) o sócio é bloqueado permanentemente e só a administração
            // consegue liberar (Sócio > Segurança), avisada por e-mail nesse momento.
            if (int.TryParse(socioId, out var socioIdInt) && socioIdInt != 39)
            {
                var seguranca = await _db.SocioSeguranca.FirstOrDefaultAsync(s => s.SocioId == socioIdInt);

                if (seguranca != null && !seguranca.Bloqueado)
                {
                    var qtdInfracoesStr = await _db.AdmConfig
                        .Where(c => c.Parametro == "Param_QtdInfracoesParaBloqueio")
                        .Select(c => c.Valor)
                        .FirstOrDefaultAsync();

                    var qtdInfracoesParaBloqueioPermanente = int.TryParse(qtdInfracoesStr, out var qi) && qi > 0 ? qi : 5;

                    seguranca.QtdInfracoesPrint++;

                    if (seguranca.QtdInfracoesPrint >= qtdInfracoesParaBloqueioPermanente)
                    {
                        seguranca.Bloqueado = true;
                        seguranca.BloqueadoAte = null;

                        await _db.SaveChangesAsync();
                        await _helperController.EnviarAlertaBloqueioPermanenteAsync(socioId, socioNome, socioEmail);
                    }
                    else
                    {
                        var baseMinutosStr = await _db.AdmConfig
                            .Where(c => c.Parametro == "Param_PrintScreenBloqueioMinutos")
                            .Select(c => c.Valor)
                            .FirstOrDefaultAsync();

                        var baseMinutos = int.TryParse(baseMinutosStr, out var m) && m > 0 ? m : 5;
                        var minutosBloqueio = baseMinutos * Math.Pow(2, seguranca.QtdInfracoesPrint - 1);

                        seguranca.BloqueadoAte = DateTime.UtcNow.AddMinutes(minutosBloqueio);

                        await _db.SaveChangesAsync();
                    }
                }

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                bloqueado = true;
            }

            return Ok(new { bloqueado });
        }

        #endregion
    }
}