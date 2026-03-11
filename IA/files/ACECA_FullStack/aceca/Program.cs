using Aceca.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// MySQL
var conn = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(conn, ServerVersion.AutoDetect(conn)));

// JWT
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
          ?? "ACECA_JWT_SECRET_MUDE_EM_PRODUCAO_2025");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new() {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer   = false,
        ValidateAudience = false,
        ClockSkew        = TimeSpan.Zero
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// Auto-migrate
using (var s = app.Services.CreateScope())
    s.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();
