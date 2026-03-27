using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;
        private EPerfil _socioPerfil;
        public AuthController(ILogger<AuthController> logger, AppDbContext db, IConfiguration cfg)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg;
        }

        public record LoginIn(string Email, string Senha);

        public ActionResult Index()
        {
            return View("~/Views/Auth/Login.cshtml");
        }

        public ActionResult AccessDenied()
        {
            return View("~/Views/Pages/MiscNotAuthorized.cshtml");
        }

        public async Task<IActionResult> Access()
        {
            try
            {
                if(!User.Identity.IsAuthenticated)
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

                return ViewBag.PerfilAdm ? RedirectToAction("Inicio", "Home") : RedirectToAction("Index", "Marca");
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
        public async Task<IActionResult> Logout()
        {
            if (HttpContext.Request.Cookies.Count > 0)
            {
                var siteCookies = HttpContext.Request.Cookies
                    .Where(c => c.Key.Contains(_cfg["Cookie:Key"]?.ToString())
                        || c.Key.Contains($"{_cfg["Cookie:Key"]?.ToString()}.ExpireDateTime")
                        || c.Key.Contains(".AspNetCore.")
                        || c.Key.Contains("Microsoft.Authentication"));
                foreach (var cookie in siteCookies)
                {
                    Response.Cookies.Delete(cookie.Key);
                }
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            //HttpContext.Session.Clear();

            return Ok(new
            {
                bResult = true,
                type = "OK",
                message = "SUCESSO ::: ",
            });
        }

        #region Login

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginIn dto)
        {
            try
            {
                var user = await _db.Usuario.FirstOrDefaultAsync(s => s.Email == dto.Email.ToLower());

                if (user == null)
                    return BadRequest(new { msg = "User inválido." });

                if (!LoginValidacao(dto.Senha, user))
                {
                    ViewBag.Error = "Nome de usuário ou senha inválidos";

                    return Unauthorized(new { msg = "Credenciais inválidas." });
                }

                var socio = await _db.Socio
                    .Include(f => f.SocioPerfil)
                    .FirstOrDefaultAsync(s => s.Id == user.socioId);

                if (socio == null)
                    return BadRequest(new { msg = "Sócio inválido." });

                string strToken = LoginTokenJwt(user, socio);

                if (string.IsNullOrEmpty(strToken))
                    return BadRequest(new { msg = "Token inválido." });


                if (!await LoginSetClaimsAsync(user, socio))
                    BadRequest(new { msg = "SetClaims inválido." });

                return Ok(new
                {
                    token = strToken,
                    nameIdentifier = socio.Id.ToString(),
                    nome = socio.Nome,
                    //email = user.Email,
                    cargo = socio?.SocioPerfil?.Descricao
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

        #region Login - Validacao
        private bool LoginValidacao(string passSource, Models.Usuario user)
        {
            try
            {
                using (MD5 md5Hash = MD5.Create())
                {
                    string hash = GetMd5Hash(md5Hash, passSource);
                    //Console.WriteLine("The MD5 hash of " + source + " is: " + hash + ".");

                    if (user is null || !VerifyMd5HashWithMySecurityAlgo(md5Hash, passSource, user.Senha))
                        return false;
                }

                //if (socio is null || !BCrypt.Net.BCrypt.Verify(dto.Senha, socio.Senha))
                //    return Unauthorized(new { msg = "Credenciais inválidas." });

                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }

        #endregion

        #region Login - Token
        private string LoginTokenJwt(Models.Usuario user, Socio socio)
        {
            string strTok = string.Empty;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));

                var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var tok = new JwtSecurityToken(
                    expires: DateTime.UtcNow.AddDays(7),
                    signingCredentials: cred,
                    claims: [
                        new(ClaimTypes.NameIdentifier, socio.Id.ToString()),
                        new(ClaimTypes.Email, user.Email),
                        new(ClaimTypes.Name,  socio.Nome),
                        new(ClaimTypes.Role,  socio?.SocioPerfil?.Descricao),
                    ]);

                strTok = new JwtSecurityTokenHandler().WriteToken(tok);

                user.Token = strTok;

                // remove password before returning
                user.Senha = null;

                //strTok = new JwtSecurityTokenHandler().WriteToken(tok);
            }
            catch (Exception)
            {
                throw;
            }

            return strTok;
        }

        #endregion

        #region Login - Claims
        private async Task<bool> LoginSetClaimsAsync(Models.Usuario user, Socio socio)
        {
            string strTok = string.Empty;

            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, socio.Id.ToString()),
                    new Claim(ClaimTypes.Email, user?.Email),
                    new Claim(ClaimTypes.Name, socio.Nome),
                    new Claim(ClaimTypes.Role, socio?.SocioPerfil?.Descricao),

                    new Claim(ClaimTypes.Expiration, DateTime.Now.AddMinutes(60).ToString()),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            }
            catch (Exception)
            {
                return false;
                throw;
            }

            return true;
        }
        #endregion

        #region Login - Cookie
        private async Task<bool> LoginSetCookieAsync(string strUserEmail)
        {
            try
            {
                var options = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMinutes(60),
                    HttpOnly = true,
                    IsEssential = true
                };

                /*
                // Set the main cookie
                HttpContext.Response.Cookies.Append(
                    _cfg["Cookie:Key"]?.ToString()
                    , GenerateGuidFromString(strUserEmail).ToString()
                    , options);
                */

                // Set a separate cookie to store the expiration date
                HttpContext.Response.Cookies.Append(
                     $"{_cfg["Cookie:Key"]?.ToString()}.ExpireDateTime"
                    , options.Expires.ToString()
                    , new CookieOptions { Expires = options.Expires }
                    );

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
                    {
                        Console.WriteLine($"Authentication cookie expires at: {expiresUtc.Value}");
                    }
                }

                string expirationDateString = HttpContext.Request.Cookies[$"{_cfg["Cookie:Key"]?.ToString()}.ExpireDateTime"];

                if (expirationDateString != null && DateTimeOffset.TryParse(expirationDateString, out DateTimeOffset expirationDate))
                {
                    // Use the expirationDate (e.g., display it in the view)
                    ViewBag.CookieExpiration = expirationDate.LocalDateTime;
                }
                else
                {
                    ViewBag.CookieExpiration = "Expiration date not found or invalid.";
                }

                return Ok(new
                {
                    cookieExpiration = ViewBag.CookieExpiration
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

        #region Login Perfil
        public async Task<IActionResult> LoginPerfilAdm()
        {
            try
            {
                // Check if the user is authenticated
                if (User.Identity.IsAuthenticated)
                {
                    // Get all claims
                    var allClaims = User.Claims.ToList();

                    // Get a specific claim value using FindFirstValue or FindFirst
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var userName = User.Identity.Name;
                    var email = User.FindFirstValue(ClaimTypes.Email);
                    var role = User.FindFirstValue(ClaimTypes.Role);

                    if (string.IsNullOrEmpty(role))
                        return BadRequest(new { msg = "Role inválido." });

                    _socioPerfil = Enum.TryParse<EPerfil>(role, out _socioPerfil) ? _socioPerfil : EPerfil.Nenhum;

                    var isPerfilAdm = _socioPerfil.Equals(EPerfil.Administracao) ? true : false;

                    return Ok(new
                    {
                        isPerfilAdm = isPerfilAdm,
                        userEmail = email,
                    });
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

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = mensagemErro
                });
            }
        }

        #endregion

        #region MD5
        static string GetMd5Hash(MD5 md5Hash, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
        private bool VerifyMd5HashWithMySecurityAlgo(MD5 md5Hash, string input, string hash)
        {
            // Hash the input.  
            string hashOfInput = GetMd5Hash(md5Hash, input);
            // Create a StringComparer an compare the hashes.  
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static Guid GenerateGuidFromString(string input)
        {
            using (MD5 md5 = MD5.Create()) // MD5 produces a 16-byte hash
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return new Guid(hashBytes);
            }
        }
        #endregion

        #endregion
    }
}