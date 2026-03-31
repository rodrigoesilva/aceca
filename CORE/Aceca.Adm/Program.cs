using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Aceca.Adm.Data;

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

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Add Distributed Memory Cache (required as a backing store for session)
builder.Services.AddDistributedMemoryCache(); //

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

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = strCookieName;
        //o.Cookie.Domain = options.CookieDomain;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        //o.TicketDataFormat = ticketFormat;
        //o.CookieManager = new CustomChunkingCookieManager();
        options.LoginPath = "/Auth/AccessDenied";  //"/Auth/Index";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });

#endregion

var app = builder.Build();
/*
// Create a service scope to get an AspnetCoreMvcFullContext instance using DI and seed the database.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    SeedData.Initialize(services);
}
*/
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    // Re-executes the pipeline for any non-success status code (like 404)
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{Id?}")
    .WithStaticAssets();

app.Run();
