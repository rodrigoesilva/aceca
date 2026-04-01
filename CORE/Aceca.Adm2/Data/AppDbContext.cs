using Aceca.Adm.Models;
using Microsoft.EntityFrameworkCore;

namespace Aceca.Adm.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        #region Admin
        public DbSet<AdminUsuario> AdminUsuarios { get; set; }
        public DbSet<Configuracao> Configuracoes { get; set; }
        #endregion

        #region Agenda
        public DbSet<Agenda> Agenda { get; set; }
        public DbSet<AgendaImg> AgendaImagem { get; set; }
        #endregion

        #region Fabrica        
        public DbSet<Fabrica> Fabrica { get; set; }
        public DbSet<FabricaFase> FabricaFase { get; set; }
        #endregion

        #region Marca
        public DbSet<Marcas> Marca { get; set; }
        public DbSet<MarcaDimensao> MarcaDimensao { get; set; }
        public DbSet<MarcaFabrica> MarcaFabrica { get; set; }
        public DbSet<MarcaFase> MarcaFase { get; set; }
        public DbSet<MarcaFinalidade> MarcaFinalidade { get; set; }
        public DbSet<MarcaImpressora> MarcaImpressora { get; set; }
        public DbSet<MarcaQualidadeImagem> MarcaQualidadeImagem { get; set; }
        public DbSet<MarcaRaridade> MarcaRaridade { get; set; }
        public DbSet<MarcaSubTipo> MarcaSubTipo { get; set; }
        public DbSet<MarcaTipo> MarcaTipo { get; set; }

        #endregion

        #region Pais
        public DbSet<Pais> Pais { get; set; }
        public DbSet<PaisCategoria> PaisCategoria { get; set; }
        #endregion

        #region Socio
        public DbSet<Socio> Socio { get; set; }
        public DbSet<SocioAniversario> SocioAniversario { get; set; }
        public DbSet<SocioColecao> SocioColecao { get; set; }
        public DbSet<SocioContato> SocioContato { get; set; }
        public DbSet<SocioEndereco> SocioEndereco { get; set; }
        public DbSet<SocioFinanceiro> SocioFinanceiro { get; set; }
        public DbSet<SocioPerfil> SocioPerfil { get; set; }
        #endregion

        #region Tipo
        public DbSet<TipoPagamento> TipoPagamento { get; set; }
        #endregion

        #region Usuario
        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<UsuarioLog> UsuarioLog { get; set; }

        #endregion

    }
}
