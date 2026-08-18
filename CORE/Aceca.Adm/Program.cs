using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// EPPlus (leitura de planilhas .xlsx na importação de coleção) exige a licença
// configurada globalmente antes do primeiro uso de ExcelPackage.
OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization(builder.Configuration["EPPlus:ExcelPackage:License"]!);

// Connect to the database
var conn = builder.Configuration.GetConnectionString("MySqlConnection")
                ?? throw new InvalidOperationException("Connection string não localizada");

// Configure DB Context with MySql
// Versão fixa (em vez de ServerVersion.AutoDetect): AutoDetect abre uma conexão síncrona
// extra durante o startup só para descobrir a versão — se houver qualquer instabilidade
// de rede até o servidor MySQL nesse instante, a aplicação inteira falha ao subir.
var mySqlServerVersion = new MySqlServerVersion(new Version(8, 0, 36));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(conn, mySqlServerVersion,
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

// Compressão HTTP (gzip/brotli) — reduz ~60-80% do payload de HTML/CSS/JS/JSON.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.AddScoped<HelperExtensionsController>();

// Registro central de erros (tabela log_erros + e-mail de alerta para ti@aceca.com.br
// quando é exceção de verdade — ver Services/ErrorLogService.cs).
builder.Services.AddScoped<Aceca.Adm.Services.ErrorLogService>();

// Automação: verifica semanalmente vencimento/pendência financeira dos sócios (socio_financeiro)
builder.Services.AddHostedService<Aceca.Adm.Services.SocioFinanceiroCheckService>();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Grava todo BadRequest devolvido por qualquer action em log_erros (auditoria).
    options.Filters.Add<Aceca.Adm.Filters.BadRequestLogFilter>();
});

// Chamadas AJAX enviam o token via header (JSON body, não form-encoded) -
// ver @Html.AntiForgeryToken() em _CommonMasterLayout.cshtml + injeção do
// header em helper-ui-common.js.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

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
                        return;
                    }

                    // Sessão única: um login novo (outro device/navegador) sobrescreve
                    // SocioSeguranca.SessionStamp - qualquer sessão com o carimbo antigo
                    // (claim "sess_stamp") é encerrada aqui, na próxima requisição dela.
                    // Sem isso, o mesmo sócio ficava logado ao mesmo tempo no celular e no
                    // computador, abrindo margem pra compartilhar acesso com outra pessoa.
                    // Claim ausente (sessão criada antes deste recurso existir) não é
                    // tratado como violação - evita derrubar em massa sessões já abertas
                    // no dia do deploy; elas seguem só até o teto de 24h (sess_abs_exp).
                    var sessStampClaim = ctx.Principal?.FindFirst("sess_stamp")?.Value;

                    if (socioId != 39 && !string.IsNullOrEmpty(sessStampClaim))
                    {
                        var stampAtual = await db.SocioSeguranca.AsNoTracking()
                            .Where(s => s.SocioId == socioId)
                            .Select(s => s.SessionStamp)
                            .FirstOrDefaultAsync();

                        if (!string.Equals(stampAtual, sessStampClaim, StringComparison.Ordinal))
                        {
                            ctx.RejectPrincipal();
                            await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
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

// Registrado logo após o UseDeveloperExceptionPage/UseExceptionHandler acima (portanto mais
// "interno" no pipeline): qualquer exceção não tratada vinda de baixo (MVC, autenticação,
// static files) passa primeiro por aqui — grava em log_erros e avisa ti@aceca.com.br por
// e-mail — e só depois é relançada, para que o UseDeveloperExceptionPage/UseExceptionHandler
// registrado acima continue tratando a resposta ao usuário exatamente como antes.
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var errorLog = ctx.RequestServices.GetRequiredService<Aceca.Adm.Services.ErrorLogService>();
        await errorLog.RegistrarExcecaoAsync(ctx, ex);
        throw;
    }
});

app.UseHttpsRedirection();
// Desativada: causava ERR_CONTENT_DECODING_FAILED no navegador (gzip/brotli corrompendo
// a resposta em certas páginas/ambientes) - confirmado que desativar resolve. Causa raiz
// exata não identificada (suspeita de antivírus/proxy interferindo na descompressão
// Brotli em HTTPS local) - se quiser reavaliar depois, dá pra tentar só com Gzip.
//app.UseResponseCompression();
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

// Tabela de log de erros (log_erros) — ver Models/LogErro.cs e Services/ErrorLogService.cs.
// CREATE TABLE IF NOT EXISTS é sintaxe padrão suportada por qualquer versão de MySQL/MariaDB
// (diferente do "IF NOT EXISTS" em ADD COLUMN/ADD INDEX tratado abaixo), então pode rodar direto.
using (var scopeLog = app.Services.CreateScope())
{
    var dbLog = scopeLog.ServiceProvider.GetRequiredService<Aceca.Adm.Data.AppDbContext>();
    var logTableLogger = scopeLog.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await dbLog.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS log_erros (
                Id INT NOT NULL AUTO_INCREMENT,
                tipo VARCHAR(50) NULL,
                url VARCHAR(1000) NULL,
                metodo_http VARCHAR(10) NULL,
                usuario VARCHAR(255) NULL,
                mensagem_humanizada VARCHAR(500) NULL,
                mensagem_original TEXT NULL,
                stack_trace TEXT NULL,
                email_enviado TINYINT(1) NOT NULL DEFAULT 0,
                data_criacao DATETIME NULL,
                PRIMARY KEY (Id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
    }
    catch (Exception ex)
    {
        logTableLogger.LogWarning(ex, "Não foi possível garantir a tabela log_erros");
    }
}

// Colunas de segurança em socio_seguranca — bloqueio temporário de login após tentativa de
// captura de tela (BloqueadoAte) e carimbo de sessão única (SessionStamp), ver
// AuthController.Login/ReportImageAccess e Program.cs::OnValidatePrincipal. Checa via
// INFORMATION_SCHEMA (DbSchemaHelper) antes do ALTER pelo mesmo motivo do bloco de índices
// abaixo — "ADD COLUMN IF NOT EXISTS" não é aceito pelo servidor em uso.
using (var scopeSeguranca = app.Services.CreateScope())
{
    var dbSeguranca = scopeSeguranca.ServiceProvider.GetRequiredService<Aceca.Adm.Data.AppDbContext>();
    var segurancaLogger = scopeSeguranca.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var errorLogSeguranca = scopeSeguranca.ServiceProvider.GetRequiredService<Aceca.Adm.Services.ErrorLogService>();

    var colunasSeguranca = new[]
    {
        ("bloqueado_ate", "DATETIME NULL"),
        ("session_stamp", "VARCHAR(64) NULL"),
    };

    foreach (var (coluna, tipoSql) in colunasSeguranca)
    {
        try
        {
            if (await Aceca.Adm.Helper.DbSchemaHelper.ColunaExisteAsync(dbSeguranca.Database, "socio_seguranca", coluna))
                continue;

            await dbSeguranca.Database.ExecuteSqlRawAsync(
                "ALTER TABLE socio_seguranca ADD COLUMN " + coluna + " " + tipoSql);
        }
        catch (Exception ex)
        {
            segurancaLogger.LogWarning(ex, "Não foi possível garantir a coluna {Coluna} em socio_seguranca", coluna);
            await errorLogSeguranca.RegistrarExcecaoAsync(null, ex);
        }
    }
}

// Índices de banco — preparação para escala em duas tabelas que ainda vão crescer bastante
// (marcas ~65 mil linhas hoje; socio_colecao ainda pequena, mas vai passar de 100 mil).
// Cobrem exatamente as colunas usadas em WHERE/JOIN nos FiltrarDados/upserts existentes
// (AcervoController, SocioColecaoController, NegociacaoController). Idempotente: seguro
// rodar em todo restart — checa via INFORMATION_SCHEMA antes de criar (DbSchemaHelper), em
// vez de "ADD INDEX IF NOT EXISTS" (só suportado a partir do MySQL 8.0.29 e rejeitado pelo
// servidor em uso, o que antes fazia essa rotina falhar com erro de sintaxe em todo restart).
using (var scope = app.Services.CreateScope())
{
    var dbIndex = scope.ServiceProvider.GetRequiredService<Aceca.Adm.Data.AppDbContext>();
    var indexLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var errorLogIndices = scope.ServiceProvider.GetRequiredService<Aceca.Adm.Services.ErrorLogService>();

    var indices = new[]
    {
        ("marcas", "idx_marcas_marcaAcervoId", "marcaAcervoId"),
        ("marcas", "idx_marcas_marcaFaseId", "marcaFaseId"),
        ("marcas", "idx_marcas_marcafaseAcervoId", "marcafaseAcervoId"),
        ("marcas", "idx_marcas_marcaSubTipoId", "marcaSubTipoId"),
        ("marcas", "idx_marcas_CodigoAceca", "CodigoAceca"),
        ("socio_colecao", "idx_socio_colecao_socio_marca", "SocioId, MarcaId"),
        ("socio_colecao", "idx_socio_colecao_marca", "MarcaId"),
    };

    foreach (var (tabela, nomeIndice, colunas) in indices)
    {
        try
        {
            if (await Aceca.Adm.Helper.DbSchemaHelper.IndiceExisteAsync(dbIndex.Database, tabela, nomeIndice))
                continue;

            // Identificadores vêm só da lista constante acima (nunca de input externo),
            // então a concatenação aqui é segura — DDL não aceita nomes de tabela/coluna
            // como parâmetro bindado de qualquer forma.
            string sqlDdl = "ALTER TABLE " + tabela + " ADD INDEX " + nomeIndice + " (" + colunas + ")";
            await dbIndex.Database.ExecuteSqlRawAsync(sqlDdl);
        }
        catch (Exception ex)
        {
            indexLogger.LogWarning(ex, "Não foi possível garantir o índice {Indice} em {Tabela}", nomeIndice, tabela);
            await errorLogIndices.RegistrarExcecaoAsync(null, ex);
        }
    }

    // Warm-up do EF Core: o ALTER TABLE acima já abre conexão com o MySQL, mas só uma
    // consulta LINQ de verdade força o EF a compilar o modelo (reflection sobre todas as
    // entidades mapeadas) - sem isso, esse custo (perceptível, ~1s+) cai na primeira
    // requisição autenticada de um usuário real (ex.: GetAvatarInfo, chamado antes do Swal
    // de "bem-vindo novamente" no login), fazendo aquele Swal demorar a aparecer logo após
    // subir a aplicação.
    try
    {
        await dbIndex.Socio.AsNoTracking().Select(s => s.Id).FirstOrDefaultAsync();
    }
    catch (Exception ex)
    {
        indexLogger.LogWarning(ex, "Não foi possível aquecer o modelo do EF Core na inicialização");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Index}/{Id?}")
    .WithStaticAssets();

app.Run();
