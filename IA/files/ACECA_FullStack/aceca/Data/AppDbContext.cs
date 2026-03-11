using Microsoft.EntityFrameworkCore;
using Aceca.Api.Models;

namespace Aceca.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
{
    public DbSet<Socio>          Socios          { get; set; }
    public DbSet<Evento>         Eventos         { get; set; }
    public DbSet<FotoEvento>     FotosEvento     { get; set; }
    public DbSet<Colecao>        Colecoes        { get; set; }
    public DbSet<CargoDiretoria> CargosDiretoria { get; set; }
    public DbSet<Contato>        Contatos        { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        /* Seed — senhas em BCrypt */
        mb.Entity<Socio>().HasData(
            Seed(1, "Admin ACECA",     "admin@aceca.com.br",    "Aceca@2025!",  "admin"),
            Seed(2, "Alberto Souza",   "alberto@aceca.com.br",  "Alberto@01",   "presidente"),
            Seed(3, "Carlos Lima",     "carlos@aceca.com.br",   "Carlos@02",    "vice_presidente"),
            Seed(4, "Ricardo Santos",  "ricardo@aceca.com.br",  "Ricardo@03",   "dir_financeiro"),
            Seed(5, "Pedro Martins",   "pedro@aceca.com.br",    "Pedro@04",     "dir_extraordinario"),
            Seed(6, "Camila Alves",    "camila@aceca.com.br",   "Camila@05",    "gestao_ti"),
            Seed(7, "Daniel Oliveira", "daniel@aceca.com.br",   "Daniel@321",   "socio"),
            Seed(8, "Fernanda Costa",  "fernanda@aceca.com.br", "Fer@2023",     "socio"),
            Seed(9, "Lucas Mendes",    "lucas@aceca.com.br",    "Lucas#456",    "socio"),
            Seed(10,"Julia Martins",   "julia@aceca.com.br",    "Julia@789",    "socio"),
            Seed(11,"Gustavo Lima",    "gustavo@aceca.com.br",  "Gust@000",     "socio")
        );
    }

    static Socio Seed(int id, string nome, string email, string senha, string cargo) => new()
    {
        Id       = id,
        Nome     = nome,
        Email    = email,
        SenhaHash= BCrypt.Net.BCrypt.HashPassword(senha),
        Cargo    = cargo,
        Ativo    = true,
        CriadoEm = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}