
using Microsoft.EntityFrameworkCore;
using ACECA.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8,0,32))
    ));

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
