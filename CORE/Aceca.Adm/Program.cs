using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Connect to the database
var conn = builder.Configuration.GetConnectionString("MySqlConnection")
                ?? throw new InvalidOperationException("Connection string não localizada");

// Configure DB Context with MySql
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(conn, ServerVersion.AutoDetect(conn),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            );
        })
    .LogTo(Console.WriteLine, LogLevel.Error)
// Quick logging configuration
);

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddScoped<HelperExtensionsController>();

// Automação: verifica semanalmente vencimento/pendência financeira dos sócios (socio_financeiro)
builder.Services.AddHostedService<Aceca.Adm.Services.SocioFinanceiroCheckService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Add Distributed Memory Cache (required as a backing store for session)
builder.Services.AddDistributedMemoryCache(); //
builder.Services.AddMemoryCache();

//
builder.Services.AddHttpClient();

#region TODO - Configure Token authentication

/*
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ACECA_JWT_SECRET_MUDE_EM_PRODUCAO_2025";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateAudience = false,
        ValidateIssuer = false,
        ClockSkew = TimeSpan.Zero
    };
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    });

*/
#endregion

#region TODO - Configure Session authentication 

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Makes the session cookie essential for compliance
    // 2 horas sem uso (ociosidade) — alinhado com o ExpireTimeSpan do cookie de autenticação
    options.IdleTimeout = TimeSpan.FromHours(2);
});

/*
//Add Session services, optionally configuring options like timeout
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Makes the session cookie essential for compliance
    options.IdleTimeout = TimeSpan.FromMinutes(20); // Default is 20 minutes
});

*/
#endregion

#region Configure Cookie authentication

string strCookieName = builder.Configuration["Cookie:Key"];

// Identifica requisições AJAX/fetch para responder com status (401/403) em vez de
// redirecionar (302) para uma página HTML — o JavaScript trata o redirecionamento.
static bool IsAjaxRequest(HttpRequest request)
{
    return string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
        || request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = strCookieName;
        //o.Cookie.Domain = options.CookieDomain;

        // Ociosidade: 2h sem uso. Com SlidingExpiration o cookie é renovado a cada
        // atividade; após 2h sem requisições ele expira.
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);

        // Sessão expirada / não autenticada -> SessionExpired
        options.LoginPath = "/Auth/SessionExpired";
        options.LogoutPath = "/Auth/Logout";
        // Autenticado, porém sem permissão (role) -> AccessDenied
        options.AccessDeniedPath = "/Auth/AccessDenied";

        options.Events = new CookieAuthenticationEvents
        {
            // Teto absoluto de 24h: independentemente da atividade, a sessão encerra
            // 24h após o login. O prazo é gravado no claim "sess_abs_exp" no login.
            OnValidatePrincipal = async ctx =>
            {
                var absClaim = ctx.Principal?.FindFirst("sess_abs_exp")?.Value;

                if (DateTimeOffset.TryParse(absClaim, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var absExp)
                    && DateTimeOffset.UtcNow >= absExp)
                {
                    ctx.RejectPrincipal();
                    await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                // Sócio desativado (Ativo = false): encerra a sessão já autenticada
                // imediatamente na próxima requisição, mesmo sem novo login.
                var socioIdClaim = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(socioIdClaim, out var socioId))
                {
                    var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                    var ativo = await db.Socio.AsNoTracking()
                        .Where(s => s.Id == socioId)
                        .Select(s => s.Ativo)
                        .FirstOrDefaultAsync();

                    if (!ativo)
                    {
                        ctx.RejectPrincipal();
                        await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
            },

            // Desafio de autenticação (sessão expirada/ausente): AJAX recebe 401 e o
            // JS redireciona; navegação normal é redirecionada para SessionExpired.
            OnRedirectToLogin = ctx =>
            {
                if (IsAjaxRequest(ctx.Request))
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                else
                    ctx.Response.Redirect(ctx.RedirectUri);

                return Task.CompletedTask;
            },

            // Sem permissão: AJAX recebe 403; navegação normal vai para AccessDenied.
            OnRedirectToAccessDenied = ctx =>
            {
                if (IsAjaxRequest(ctx.Request))
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                else
                    ctx.Response.Redirect(ctx.RedirectUri);

                return Task.CompletedTask;
            }
        };
    });

#endregion


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else {
    app.UseExceptionHandler("/Error");
    
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    // Re-executes the pipeline for any non-success status code (like 404)
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}

app.UseHttpsRedirection();
//app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Cache-Control", "public,max-age=604800");
    }
});

app.UseRouting();

// A ordem correta é: UseAuthentication → UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Index}/{Id?}")
    .WithStaticAssets();

app.Run();
