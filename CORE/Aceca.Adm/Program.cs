using Aceca.Adm.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(conn, ServerVersion.AutoDetect(conn),
        x => x.EnableRetryOnFailure(3)));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "ACECA_JWT_SECRET_MUDE_EM_PRODUCAO_2025";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new() {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer   = false,
        ValidateAudience = false,
        ClockSkew        = TimeSpan.Zero
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try { db.Database.Migrate(); }
    catch (Exception ex) { Console.WriteLine($"[WARN] Migration skipped: {ex.Message}"); }
}

app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();
