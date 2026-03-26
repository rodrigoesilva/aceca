using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Aceca.Adm.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;
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
            var result = await LoginPerfilAdm();

            if (result.GetType() == typeof(ForbidResult))
                AccessDenied();

            if (result.GetType() == typeof(BadRequestObjectResult))
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = result?.ToString()
                });


            //return RedirectToAction("Index", "Usuario");
            return (bool)((ObjectResult)result).Value ? RedirectToAction("Inicio", "Home") : RedirectToAction("Index", "Marca"); ;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginIn dto)
        {
            //TODO ********* para testes passa direto
            //return RedirectToAction("Inicio", "Home");

            var user = await _db.Usuario
                .FirstOrDefaultAsync(s => s.Email == dto.Email.ToLower());

            //if (user.Email == "rodrigoesilva@gmail.com" && user.SenhaAberta == "1")
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

            string strToken = LoginTokenJwt(user, socio);

            if (string.IsNullOrEmpty(strToken))
                return BadRequest(new { msg = "Token inválido." });

            #region Token Cookie

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, socio.Id.ToString()),
                    new Claim(ClaimTypes.Email, user?.Email),
                    new Claim(ClaimTypes.Name, socio.Nome),
                    new Claim(ClaimTypes.Role, socio?.SocioPerfil?.Descricao),
                    //new Claim(ClaimTypes.Role, "User"),
                    //new Claim(ClaimTypes.Role, "SuperAdmin")
                };

            var claimsIdentity = new ClaimsIdentity(claims, "cookie");

            await HttpContext.SignInAsync("cookie", new ClaimsPrincipal(claimsIdentity));

            #endregion

            return Ok(new
            {
                token = strToken,
                nameIdentifier = socio.Id.ToString(),
                nome = socio.Nome,
                //email = user.Email,
                cargo = socio?.SocioPerfil?.Descricao
            });
        }


        #region Login - Validacao
        private bool LoginValidacao(string passSource, Usuario user)
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
        private string LoginTokenJwt(Usuario user, Socio socio)
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

                /*
                 * 
                 *  var key = Encoding.ASCII.GetBytes(_cfg["Jwt:Key"]!);
                 *  
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, socio.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Name,  socio.Nome),
                        new Claim(ClaimTypes.Role,  socio?.SocioPerfil?.Descricao),
                    }),


                    Issuer = "......",
                    Audience ="......",
                    NotBefore = DateTime.Now,
                    Expires = DateTime.UtcNow.AddDays(7),
                    //Expires = DateTime.UtcNow.AddHours(2),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    //SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                

                var token = tokenHandler.CreateToken(tokenDescriptor);
                strTok = tokenHandler.WriteToken(token);
                */

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
                    var userName = User.Identity.Name; // Often maps to ClaimTypes.Name
                    var email = User.FindFirstValue(ClaimTypes.Email);
                    var role = User.FindFirstValue(ClaimTypes.Role);

                    if (string.IsNullOrEmpty(role))
                        return BadRequest(new { msg = "Role inválido." });

                    return Ok(new
                    {
                        isPerfilAdm = role.Equals("Administração") ? true : false
                    });
                }
                else
                {
                    return Forbid();
                }
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
        public bool VerifyMd5HashWithMySecurityAlgo(MD5 md5Hash, string input, string hash)
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

        #endregion
    }
}