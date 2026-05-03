using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers
{
    public class AuthController : Controller
    {

        #region variaveis

        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;
        private EPerfil _socioPerfil;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;

        private string _strControllerName = string.Empty;
        private string _strActionName = string.Empty;
        //

        #endregion
        public AuthController(ILogger<AuthController> logger, AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;
        }

        public record LoginIn(string Email, string Senha);
        public record LoginUpdt(string Username, string Email, string Senha, string ConfirmSenha, bool ChkTermo);

        // DTO para ForgotPassword
        public record ForgotPasswordIn(string Email);

        // DTO para ResetPassword
        public record ResetPasswordIn(string Email, string Token, string Senha, string ConfirmSenha);

        // ──────────────────────────────────────────────
        // VIEWS
        // ──────────────────────────────────────────────

        public ActionResult Index()
        {
            // Se o usuário já está autenticado e o cookie ainda é válido (24h),
            // redireciona direto sem precisar logar novamente.
            // O JavaScript da página também verifica o cookie local para exibir
            // a mensagem "Seja bem-vindo novamente" via SweetAlert.
            return View("~/Views/Auth/Login.cshtml");
        }

        public ActionResult AccessDenied()
        {
            return View("~/Views/Pages/MiscNotAuthorized.cshtml");
        }

        public ActionResult UpdatePass()
        {
            return View("~/Views/Auth/RegisterUpdate.cshtml");
        }

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

        // ──────────────────────────────────────────────
        // ACCESS / LOGOUT
        // ──────────────────────────────────────────────

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

                TempData["isPerfil"] = ViewBag.PerfilAdm;
                TempData["Layout"] = ViewBag.PerfilAdm ? "_HorizontalLayout" : "_WithoutMenuLayout";

                if (!await LoginSetCookieAsync(jObjResult?["userEmail"]?.ToString()))
                    BadRequest(new { msg = "SetCookie inválido." });

                return ViewBag.PerfilAdm
                    ? RedirectToAction("Inicio", "Home")
                    : RedirectToAction("Index", "Marca");
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";
                _logger.LogError(mensagemErro);
                return BadRequest(new { bResult = false, type = "ERRO", message = mensagemErro });
            }
        }

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

        // ──────────────────────────────────────────────
        // LOGIN
        // ──────────────────────────────────────────────

        #region Login

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginIn dto)
        {
            try
            {
                var user = await _db.Usuario.FirstOrDefaultAsync(s => s.Email == dto.Email.ToLower());

                if (user == null)
                    return Ok(new { bResult = false, type = "ERRO", message = "User Inválido" });

                if (!LoginValidacao(dto.Senha, user))
                {
                    ViewBag.Error = "Nome de usuário ou senha inválidos";
                    return Ok(new { bResult = false, type = "ERRO", message = "Credenciais Inválidas" });
                }

                var socio = await _db.Socio
                    .Include(f => f.SocioPerfil)
                    .FirstOrDefaultAsync(s => s.Id == user.SocioId);

                if (socio == null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Sócio Inválido" });

                var userPass = user.Senha;

                string strToken = LoginTokenJwt(user, socio);

                if (string.IsNullOrEmpty(strToken))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Token Inválido" });

                if (!await LoginSetClaimsAsync(user, socio))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "SetClaims Inválido" });

                var rootPathImgAvatar = Path.Combine(_appEnvironment.WebRootPath, "img", "avatars", "socio", "imgAvatar", socio?.Id?.ToString(), ".jpg");

                // Atualiza UltimoLogin
                user.UltimoLogin = DateTime.UtcNow;

                user.Senha = !string.IsNullOrEmpty(user.Senha) ? user.Senha : userPass;
                user.NomeUsuario = socio.Nome;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    bResult = true,
                    token = strToken,
                    nameIdentifier = socio.Id.ToString(),
                    nome = socio.Nome,
                    avatar = !string.IsNullOrEmpty(socio.ImgAvatar) ? rootPathImgAvatar : rootPathImgAvatar,
                    cargo = socio?.SocioPerfil?.Descricao,
                    isPerfil = Convert.ToBoolean(socio?.SocioPerfil?.Descricao?.Equals("Administracao")),
                    pswuptd = user.SenhaAtualizada
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

        // ──────────────────────────────────────────────
        // ESQUECI A SENHA  (envia e-mail com link)
        // ──────────────────────────────────────────────

        #region ForgotPassword

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

                var user = await _db.Usuario
                    .FirstOrDefaultAsync(s => s.Email == dto.Email.Trim().ToLower());

                // Por segurança, retorna sucesso mesmo se o e-mail não existir,
                // para não expor quais e-mails estão cadastrados.
                if (user == null)
                    return Ok(new { bResult = true, message = "Se o e-mail existir, você receberá as instruções." });

                var socio = await _db.Socio
                    .FirstOrDefaultAsync(s => s.Id == user.SocioId);

                if (socio == null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Sócio Inválido" });

                // Gera token seguro
                var tokenBytes = RandomNumberGenerator.GetBytes(32);
                var token = Convert.ToBase64String(tokenBytes)
                                   .Replace("+", "-").Replace("/", "_").Replace("=", "");

                // Armazena no usuário (campos que precisam existir no model Usuario)
                user.ResetPasswordToken = token;
                user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(24);
                await _db.SaveChangesAsync();

                // Monta link de reset
                var resetLink = $"{_urlBaseApp}/Auth/ResetPassword?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";

                // Envia e-mail
                var result = await EnviarEmailResetSenhaAsync(user.Email, socio.Nome, resetLink);

                if (result.GetType() == typeof(NotFoundObjectResult) ||
                    result.GetType() == typeof(BadRequestObjectResult))
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
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";
                _logger.LogError(mensagemErro);
                return BadRequest(new { bResult = false, type = "ERRO", message = mensagemErro });
            }
        }

        #endregion

        // ──────────────────────────────────────────────
        // RESET DE SENHA  (página com e-mail + senha + confirmação)
        // ──────────────────────────────────────────────

        #region ResetPassword

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

                var user = await _db.Usuario
                    .FirstOrDefaultAsync(s => s.Email == dto.Email.Trim().ToLower());

                if (user == null)
                    return Ok(new { bResult = false, message = "Usuário não encontrado." });

                // Valida token e expiração
                if (user.ResetPasswordToken != dto.Token ||
                    user.ResetPasswordTokenExpiry == null ||
                    user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                    return Ok(new { bResult = false, message = "Link de reset inválido ou expirado. Solicite um novo." });

                // Atualiza senha
                using (MD5 md5Hash = MD5.Create())
                {
                    string hash = GetMd5Hash(md5Hash, dto.Senha);
                    user.Senha = hash;
                    user.SenhaAberta = dto.Senha;
                    user.SenhaAtualizada = true;
                }

                // Invalida o token após uso
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                user.UltimoLogin = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new { bResult = true, message = "Senha atualizada com sucesso!" });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";
                _logger.LogError(mensagemErro);
                return BadRequest(new { bResult = false, type = "ERRO", message = mensagemErro });
            }
        }

        #endregion

        // ──────────────────────────────────────────────
        // LOGIN PERFIL
        // ──────────────────────────────────────────────

        #region Login Perfil

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

                    return Ok(new { isPerfilAdm, userEmail = email });
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";
                _logger.LogError(mensagemErro);
                return BadRequest(new { bResult = false, type = "ERRO", message = mensagemErro });
            }
        }

        #endregion

        // ──────────────────────────────────────────────
        // UPDATE DATA
        // ──────────────────────────────────────────────

        #region Update Data

        [HttpPost]
        public async Task<IActionResult> LoginUpdate([FromBody] LoginUpdt model)
        {
            try
            {
                var newModel = new Models.Usuario();

                var user = await _db.Usuario.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Email == model.Email.ToLower());

                if (user == null)
                    return Ok(new { bResult = false, type = "ERRO", message = "User Inválido" });

                var socio = await _db.Socio
                    .Include(f => f.SocioPerfil)
                    .FirstOrDefaultAsync(s => s.Id == user.SocioId);

                if (socio == null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Sócio Inválido" });

                using (MD5 md5Hash = MD5.Create())
                {
                    string hash = GetMd5Hash(md5Hash, model.Senha);

                    newModel = new Models.Usuario
                    {
                        Id = user.Id,
                        SocioId = socio.Id,
                        Email = model.Email,
                        Senha = hash,
                        SenhaAberta = model.Senha,
                        SenhaAtualizada = true,
                        NomeUsuario = model.Username,
                        UltimoLogin = DateTime.UtcNow,
                        Ativo = true,
                    };

                    _db.Entry(newModel).State = EntityState.Modified;
                    _db.SaveChanges();
                }

                if (newModel?.Id <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Falha ao Atualizar Socio" });

                return Ok(new
                {
                    bResult = true,
                    nameIdentifier = socio.Id.ToString(),
                    nome = socio.Nome,
                    cargo = socio?.SocioPerfil?.Descricao,
                    isPerfil = Convert.ToBoolean(socio?.SocioPerfil?.Descricao?.Equals("Administracao")),
                    pswuptd = true
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

        // ──────────────────────────────────────────────
        // FUNÇÕES AUXILIARES
        // ──────────────────────────────────────────────

        #region Funções - MD5

        static string GetMd5Hash(MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));
            return sBuilder.ToString();
        }

        private bool VerifyMd5HashWithMySecurityAlgo(MD5 md5Hash, string input, string hash)
        {
            string hashOfInput = GetMd5Hash(md5Hash, input);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }

        private static Guid GenerateGuidFromString(string input)
        {
            using MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return new Guid(hashBytes);
        }

        #endregion

        #region Funções - Login

        private bool LoginValidacao(string passSource, Models.Usuario user)
        {
            using MD5 md5Hash = MD5.Create();
            if (user is null || !VerifyMd5HashWithMySecurityAlgo(md5Hash, passSource, user.Senha))
                return false;
            return true;
        }

        private string LoginTokenJwt(Models.Usuario user, Socio socio)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appConfiguration["Jwt:Key"]!));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Cookie dura 24h, token JWT alinhado
            var tok = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: cred,
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

        private async Task<bool> LoginSetClaimsAsync(Models.Usuario user, Socio socio)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, socio.Id.ToString()),
                    new Claim(ClaimTypes.Email, user?.Email),
                    new Claim(ClaimTypes.Name, socio.Nome),
                    new Claim(ClaimTypes.Role, socio?.SocioPerfil?.Descricao),
                    // Expiração: 24h totais; inatividade de 1h controlada pelo SlidingExpiration no Program.cs
                    new Claim(ClaimTypes.Expiration, DateTime.UtcNow.AddHours(24).ToString("o")),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    // Sliding: renova o cookie a cada request (inatividade de 1h)
                    IsPersistent = true,
                    IssuedUtc = DateTimeOffset.UtcNow,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
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
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";
                _logger.LogError(mensagemErro);
                return false;
            }

            return true;
        }

        public async Task<IActionResult> GetCookieExpirationAsync()
        {
            try
            {
                var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (authenticateResult.Succeeded)
                {
                    var expiresUtc = authenticateResult.Properties.ExpiresUtc;
                    if (expiresUtc.HasValue)
                        Console.WriteLine($"Authentication cookie expires at: {expiresUtc.Value}");

                    ViewBag.CookieExpiration = expiresUtc?.LocalDateTime.ToString() ?? "N/A";
                }

                string expirationDateString = HttpContext.Request.Cookies[
                    $"{_appConfiguration["Cookie:Key"]?.ToString()}.ExpireDateTime"];

                if (expirationDateString != null &&
                    DateTimeOffset.TryParse(expirationDateString, out DateTimeOffset expirationDate))
                    ViewBag.CookieExpiration = expirationDate.LocalDateTime;
                else
                    ViewBag.CookieExpiration = "Expiration date not found or invalid.";

                return Ok(new { cookieExpiration = ViewBag.CookieExpiration });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";
                _logger.LogError(mensagemErro);
                return BadRequest(new { bResult = false, type = "ERRO", message = mensagemErro });
            }
        }

        #endregion

        // ──────────────────────────────────────────────
        // ENVIO DE E-MAIL (Forgot Password)
        // ──────────────────────────────────────────────

        #region Email

        /// <summary>
        /// Envia o e-mail de reset de senha via SMTP configurado no appsettings.json.
        /// Configure as chaves: Email:Host, Email:Port, Email:EnableSsl,
        ///                      Email:From, Email:User, Email:Password, Email:DisplayName
        /// </summary>
        public async Task<IActionResult> EnviarEmailResetSenhaAsync(string toEmail, string socioNome, string resetLink)
        {
            var smtpHost = _appConfiguration["Email:Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_appConfiguration["Email:Port"] ?? "587");
            var smtpSsl = bool.Parse(_appConfiguration["Email:EnableSsl"] ?? "true");
            var smtpFrom = _appConfiguration["Email:From"] ?? "noreply@aceca.com.br";
            var smtpUser = _appConfiguration["Email:User"] ?? smtpFrom;
            var smtpPassword = _appConfiguration["Email:Password"] ?? "";
            var displayName = _appConfiguration["Email:DisplayName"] ?? "ACECA - Área do Sócio";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpFrom, displayName),
                Subject = "Redefinição de senha - ACECA Área do Sócio",
                IsBodyHtml = true,
                Body = $@"
                    <!DOCTYPE html>
                    <html lang=""pt-BR"">
                        <head><meta charset=""UTF-8""></head>
                        <body style=""font-family:Arial,sans-serif;background:#f4f4f4;padding:30px;"">
                            <div style=""max-width:520px;margin:0 auto;background:#fff;border-radius:10px;padding:36px 40px;box-shadow:0 2px 12px rgba(0,0,0,.08);"">
                                <div style=""text-align:center;"">
                                    <img src=""https://www.aceca.com.br/img/logo/logo02.png"" alt=""ACECA"" width=""250"" style=""max-width:100%;"">
                                </div>
                                <h2 style=""color:#47007b;margin-top:0;"">Redefinição de Senha</h2>
                                <p>Olá, {socioNome}</p>
                                <p>Recebemos uma solicitação para redefinir a senha da sua conta na <strong>ACECA Área do Sócio</strong>.</p>
                                <p>Clique no botão abaixo para criar uma nova senha.<br>
                                   <em>Este link é válido por <strong>24 horas</strong>.</em>
                                </p>
                                <div style=""text-align:center;margin:32px 0;"">
                                    <a href=""{resetLink}"" style=""background:#47007b;color:#fff;padding:14px 32px;border-radius:6px; text-decoration:none;font-size:16px;display:inline-block;"">
                                        Redefinir minha senha
                                    </a>
                                </div>
                                <p style=""font-size:13px;color:#888;"">Se você não solicitou a redefinição, ignore este e-mail.
                                   Sua senha permanece inalterada.</p>
                                <hr style=""border:none;border-top:1px solid #eee;margin:24px 0;"">
                                <p style=""font-size:12px;color:#aaa;text-align:center;"">
                                  © ACECA – Associação dos Corretores de Câmbio e Afins
                                </p>
                            </div>
                        </body>
                    </html>"
            };

            mailMessage.To.Add(toEmail);

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = smtpSsl
            };

            // 3. Send asynchronously
            try
            {
                await smtp.SendMailAsync(mailMessage);
                // The task completes when the message is successfully sent
            }
            catch (SmtpException ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";
                _logger.LogError(mensagemErro);
                return BadRequest(new { bResult = false, type = "ERRO", message = mensagemErro });
            }

            return Ok(true);
        }

        #endregion
    }
}