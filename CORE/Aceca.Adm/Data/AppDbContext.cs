using Microsoft.EntityFrameworkCore;
using Aceca.Adm.Models;

namespace Aceca.Adm.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
{
    public DbSet<Marca> Marcas { get; set; }
    public DbSet<AdminUsuario> AdminUsuarios { get; set; }
    public DbSet<Agenda> Agendas { get; set; }
    public DbSet<AgendaImg> AgendaImgs { get; set; }
    public DbSet<Configuracao> Configuracoes { get; set; }
    public DbSet<Fabrica> Fabricas { get; set; }
    public DbSet<FabricaFase> FabricaFases { get; set; }
    public DbSet<MarcaDimensao> MarcaDimensoes { get; set; }
    public DbSet<MarcaFabrica> MarcaFabricas { get; set; }
    public DbSet<MarcaFase> MarcaFases { get; set; }
    public DbSet<MarcaFinalidade> MarcaFinalidades { get; set; }
    public DbSet<MarcaImpressora> MarcaImpressoras { get; set; }
    
    public DbSet<MarcaQualidadeImagem> MarcaQualidadeImagens{ get; set; }
    public DbSet<MarcaRaridade> MarcaRaridades { get; set; }
    public DbSet<MarcaSubTipo> MarcaSubTipos { get; set; }
    public DbSet<MarcaTipo> MarcaTipos { get; set; }
    public DbSet<Pais> Paises { get; set; }
    public DbSet<PaisCategoria> PaisCategorias { get; set; }
    public DbSet<Socio> Socios { get; set; }
    public DbSet<SocioAniversario> SocioAniversarios { get; set; }
    public DbSet<SocioColecao> SocioColecoes { get; set; }
    public DbSet<SocioContato> SocioContatos { get; set; }
    public DbSet<SocioEndereco> SocioEnderecos { get; set; }
    public DbSet<SocioFinanceiro> SocioFinanceiros { get; set; }
    public DbSet<SocioPerfil> SocioPerfis { get; set; }
    public DbSet<TipoPagamento> TipoPagamentos { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<UsuarioLog> UsuarioLogs { get; set; }

    /*
    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Seed Socios
        mb.Entity<Socio>().HasData(
            S(1,"Admin ACECA","admin@aceca.com.br","Aceca@2025!","admin"),
            S(2,"Alberto Souza","alberto@aceca.com.br","Alberto@01","presidente"),
            S(3,"Carlos Lima","carlos@aceca.com.br","Carlos@02","vice_presidente"),
            S(4,"Ricardo Santos","ricardo@aceca.com.br","Ricardo@03","dir_financeiro"),
            S(5,"Daniel Oliveira","daniel@aceca.com.br","Daniel@321","socio"),
            S(6,"Fernanda Costa","fernanda@aceca.com.br","Fer@2023","socio"),
            S(7,"Julia Martins","julia@aceca.com.br","Julia@789","socio")
        );
        

    // Seed Países
    mb.Entity<Pais>().HasData(
            new Pais{Id=1,Nome="Brasil",Sigla="BR"},
            new Pais{Id=2,Nome="Estados Unidos",Sigla="USA"},
            new Pais{Id=3,Nome="Reino Unido",Sigla="UK"},
            new Pais{Id=4,Nome="Alemanha",Sigla="DE"},
            new Pais{Id=5,Nome="França",Sigla="FR"}
        );
        // Seed Fases
        mb.Entity<Fase>().HasData(
            new Fase{Id=1,Nome="Fase 1",Descricao="Embalagens até 1960"},
            new Fase{Id=2,Nome="Fase 2",Descricao="Embalagens 1960–1980"},
            new Fase{Id=3,Nome="Fase 3",Descricao="Embalagens 1980–2000"},
            new Fase{Id=4,Nome="Fase 4",Descricao="Embalagens a partir de 2000"}
        );
        // Seed Tipos
        mb.Entity<Tipo>().HasData(
            new Tipo{Id=1,Nome="Maço Rígido",Descricao="Embalagem rígida"},
            new Tipo{Id=2,Nome="Maço Mole",Descricao="Embalagem flexível"},
            new Tipo{Id=3,Nome="Lata",Descricao="Embalagem metálica"},
            new Tipo{Id=4,Nome="Caixa",Descricao="Embalagem de caixa"}
        );
        // Seed Fábricas
        mb.Entity<Fabrica>().HasData(
            new Fabrica{Id=1,Nome="Souza Cruz",Cidade="Rio de Janeiro",PaisId=1},
            new Fabrica{Id=2,Nome="Philip Morris",Cidade="Nova York",PaisId=2},
            new Fabrica{Id=3,Nome="British American Tobacco",Cidade="Londres",PaisId=3},
            new Fabrica{Id=4,Nome="Reemtsma",Cidade="Hamburgo",PaisId=4}
        );
        // Seed Impressoras
        mb.Entity<Impressora>().HasData(
            new Impressora{Id=1,Nome="Tipografia Padrão",Modelo="Offset Heidelberg"},
            new Impressora{Id=2,Nome="Digital Premium",Modelo="HP Indigo 7K"}
        );
        // Seed Dimensões
        mb.Entity<Dimensao>().HasData(
            new Dimensao{Id=1,Nome="Standard",Largura=57,Altura=90},
            new Dimensao{Id=2,Nome="King Size",Largura=60,Altura=95},
            new Dimensao{Id=3,Nome="100mm",Largura=60,Altura=100}
        );
        // Seed SubTipos
        mb.Entity<SubTipo>().HasData(
            new SubTipo{Id=1,Nome="Com filtro",TipoId=1},
            new SubTipo{Id=2,Nome="Sem filtro",TipoId=1},
            new SubTipo{Id=3,Nome="Extra Longo",TipoId=2}
        );
        // Seed Marcas
        mb.Entity<Marca>().HasData(
             new Marca{Id=1,Nome="Hollywood",CodigoAceca="ACECA-001",FaseId=1,FabricaId=1,TipoId=1,Descricao="Marca clássica brasileira",IncluidoPor="Admin ACECA",CriadoEm=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc),AtualizadoEm=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc)},
             new Marca{Id=2,Nome="Continental",CodigoAceca="ACECA-002",FaseId=2,FabricaId=2,TipoId=2,Descricao="Embalagem metálica clássica",IncluidoPor="Admin ACECA",CriadoEm=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc),AtualizadoEm=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc)},
             new Marca{Id=3,Nome="Ministers",CodigoAceca="ACECA-003",FaseId=1,FabricaId=3,TipoId=1,Descricao="Série comemorativa britânica",IncluidoPor="Daniel Oliveira",CriadoEm=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc),AtualizadoEm=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc)},
             new Marca{Id=4,Nome="Derby",CodigoAceca="ACECA-004",FaseId=3,FabricaId=1,TipoId=2,Descricao="Maço mole tradicional",IncluidoPor="Admin ACECA",CriadoEm=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc),AtualizadoEm=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc)}
            
             new Marca { Id = 1, Nome = "Hollywood", CodigoAceca = "ACECA-001", FaseId = 1, FabricaId = 1, TipoId = 1, Descricao = "Marca clássica brasileira", IncluidoPor = "Admin ACECA", CriadoEm = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)},
            new Marca { Id = 2, Nome = "Continental", CodigoAceca = "ACECA-002", FaseId = 2, FabricaId = 2, TipoId = 2, Descricao = "Embalagem metálica clássica", IncluidoPor = "Admin ACECA", CriadoEm = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)},
            new Marca { Id = 3, Nome = "Ministers", CodigoAceca = "ACECA-003", FaseId = 1, FabricaId = 3, TipoId = 1, Descricao = "Série comemorativa britânica", IncluidoPor = "Daniel Oliveira", CriadoEm = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)},
            new Marca { Id = 4, Nome = "Derby", CodigoAceca = "ACECA-004", FaseId = 3, FabricaId = 1, TipoId = 2, Descricao = "Maço mole tradicional", IncluidoPor = "Admin ACECA", CriadoEm = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)}

        );
    }

    static Socio S(int id, string nome, string email, string senha, string cargo) => new()
    {
        Id=id,Nome=nome,Email=email,
        SenhaHash=BCrypt.Net.BCrypt.HashPassword(senha),
        Cargo=cargo,Ativo=true,
        CriadoEm=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc)
    };

    */
}
