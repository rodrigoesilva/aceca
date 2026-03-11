using Aceca.Site.Data;
using Aceca.Site.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Aceca.Site.Controllers
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
        
        [HttpGet]
        //public IActionResult Login() => View();
        public IActionResult Login()
        {
            return RedirectToAction("Index", "Auth");
            //return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginIn dto)
        {
            //TODO ********* para testes passa direto
            //return RedirectToAction("Inicio", "Home");

            var user = await _db.Usuarios
                .FirstOrDefaultAsync(s => s.Email == dto.Email.ToLower());

            if (!LoginValidacao(dto.Senha, user))
                return Unauthorized(new { msg = "Credenciais inválidas." });


            var socio = await _db.Socios
                .Include(f => f.SocioPerfil)
                .FirstOrDefaultAsync(s => s.Id == user.socioId);
            
            string strToken = LoginToken(user, socio);

            if (string.IsNullOrEmpty(strToken))
                return BadRequest(new { msg = "Token inválido." });

            return Ok(new
            {
                token = strToken,
                nome = socio.Nome,
                email = user.Email,
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
        private string LoginToken(Usuario user, Socio socio)
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