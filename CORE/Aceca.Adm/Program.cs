using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Microsoft.EntityFrameworkCore;
using System.Text;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        var conn = builder.Configuration.GetConnectionString("Default");
        builder.Services.AddDbContext<AppDbContext>(o =>
            o.UseMySql(conn, ServerVersion.AutoDetect(conn),
                x => x.EnableRetryOnFailure(3)));

        var jwtKey = builder.Configuration["Jwt:Key"] ?? "ACECA_JWT_SECRET_MUDE_EM_PRODUCAO_2025";
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);


        /*
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
        */

        builder.Services.AddControllersWithViews();

        // 1. Add Distributed Memory Cache (required as a backing store for session)
        builder.Services.AddDistributedMemoryCache(); //

        // 2. Add Session services, optionally configuring options like timeout
        builder.Services.AddSession(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true; // Makes the session cookie essential for compliance
            options.IdleTimeout = TimeSpan.FromMinutes(20); // Default is 20 minutes
        });

        /*
         * 
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdministratorRole",
                 policy => policy.RequireRole("Administrator"));




         * builder.Services.AddAuthentication()
                .AddCookie(options =>
                {
                    options.Cookie.Name = "UserLoginCookie";
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Login/AccessDenied";
                })
                .AddJwtBearer(options =>
                {
                    options.Audience = "http://localhost:5001/";
                    options.Authority = "http://localhost:5000/";
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


        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            //app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        //app.UseIdentityServer();
        app.UseAuthorization();

        // 3. Use the Session middleware, it must be placed BEFORE UseMvc/UseEndpoints/MapControllers
        app.UseSession(); //

        //app.MapDefaultControllerRoute();
        //app.MapRazorPages();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
            name: "default",
           pattern: "{controller=Home}/{action=Inicio}/{Id?}");

            //pattern: "{controller=Auth}/{action=Index}/{Id?}");
        });

        app.Run();
    }
}