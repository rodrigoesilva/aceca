
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aceca.Adm.Models
{
    public class BaseModel
    {
        public bool? Ativo { get; set; }
        public DateTime? DataCriacao { get; set; } = DateTime.Now;
        public DateTime? DataAtualizacao { get; init; } = DateTime.Now;
    }

    #region MENU
    public class MenuItem
    {
        [Key]
        public int MenuItemId { get; set; }
        public int? MenuPaiId { get; set; }
        [Required]
        public string Nome { get; set; }
        public bool Habilitado { get; set; }

        [Required]
        public string Action { get; set; }
        [Required]
        public string Controller { get; set; }

        [ForeignKey("MenuPaiId")]
        public virtual MenuItem MenuPai { get; set; }
        public virtual ICollection<MenuItem> MenusFilhos { get; set; }
    }

    #endregion

    #region admin

    [Table("admin_usuario")]
    public class AdminUsuario
    {
        [Key] public int? Id { get; set; }
        [MaxLength(80)] public string? Nome { get; set; } = string.Empty;
        public string? Usuario { get; set; }
        public string? Senha { get; set; }
        [Column("senha_aberta")] public string? SenhaAberta { get; set; }
    }

    [Table("configuracoes")]
    public class Configuracao
    {
        [Key] public int? Id { get; set; }
        [MaxLength(6)] public string? CorHeader { get; set; } = string.Empty;
        [MaxLength(6)] public string? CorFundo { get; set; } = string.Empty;
        [MaxLength(255)] public string? TipoFundo { get; set; } = string.Empty;
        [MaxLength(6)] public string? CorRodape { get; set; } = string.Empty;
        [MaxLength(255)] public string? TipoRodape { get; set; } = string.Empty;
        [MaxLength(6)] public string? CorCopyright { get; set; } = string.Empty;
        public int? Paginacao { get; set; }
        [MaxLength(6)] public string? CorBase1 { get; set; } = string.Empty;
        [MaxLength(6)] public string? CorBase2 { get; set; } = string.Empty;
        [MaxLength(6)] public string? CorBase3 { get; set; } = string.Empty;
        [MaxLength(6)] public string? CorBase4 { get; set; } = string.Empty;
        [MaxLength(6)] public string? CorBase5 { get; set; } = string.Empty;
        [MaxLength(255)] public string? NomeDoSite { get; set; } = string.Empty;
        [MaxLength(255)] public string? DataRodape { get; set; } = string.Empty;
        [MaxLength(255)] public string? SiteEmail { get; set; } = string.Empty;
        [MaxLength(255)] public string? SiteEmailSenha { get; set; } = string.Empty;
        [MaxLength(255)] public string? PublicKey { get; set; } = string.Empty;
        [MaxLength(255)] public string? PrivatecKey { get; set; } = string.Empty;
        [MaxLength(255)] public string? YoutubeApiKey { get; set; } = string.Empty;
    }

    #endregion

    #region agenda

    [Table("agenda")]
    public class Agenda : BaseModel
    {
        [Key] public int? Id { get; set; }

        public int? AgendaImagemId { get; set; }
        public string? Data { get; set; }
        [MaxLength(255)] public string? Titulo { get; set; } = string.Empty;
        [MaxLength(255)] public string? SubTitulo { get; set; } = string.Empty;
        public string? BreveDesc { get; set; }
        public string? Descricao { get; set; }
        [MaxLength(255)] public string? Video { get; set; } = string.Empty;

        public AgendaImagem? AgendaImagem { get; set; }
    }

    [Table("agenda_img")]
    public class AgendaImagem : BaseModel
    {
        [Key] public int? Id { get; set; }        
        [MaxLength(255)] public string? Imagem { get; set; } = string.Empty;
        public string? Descricao { get; set; }
    }

    #endregion

    #region Fabrica

    [Table("fabricas")]
    public class Fabrica : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int? FabricaFaseId { get; set; }

        public FabricaFase? FabricaFase { get; set; }
    }

    [Table("fabricas_fase")]
    public class FabricaFase : BaseModel
    {
        [Key] public int? Id { get; set; }
        public int? Codigo { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    #endregion 

    #region Marca

    [Table("marcas")]
    public class Marcas : BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }
        public int? MarcaDimensaoId { get; set; }
        public int? MarcaFabricaId { get; set; }
        [Column("fabrica_txt")] public string? TxtFabrica { get; set; } = string.Empty;
        public int? MarcaFaseId { get; set; }
        public int? MarcaFinalidadeId { get; set; }
        public int? MarcaImpressoraId { get; set; }
        [Column("impressora")] public string? TxtImpressora { get; set; } = string.Empty;
        public int? MarcaQualidadeImagemId { get; set; }
        public int? MarcaRaridadeId { get; set; }
        public int? MarcaSubTipoId { get; set; }
        public string? CodigoAceca { get; set; } = string.Empty;
        public string? CodigoSC { get; set; } = string.Empty;
        public string? ImgPrincipal { get; set; } = string.Empty;
        public string? ImgDetalhe { get; set; } = string.Empty;
        public string? Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; } = string.Empty;
        public string? Valor1PI { get; set; } = string.Empty;
        public string? Valor2PI { get; set; } = string.Empty;
        public string? Valor { get; set; } = string.Empty;
        public string? IncluidoPor { get; set; } = string.Empty;
        public int? EmQuarentena { get; set; }
        public MarcaDimensao? MarcaDimensao { get; set; }
        public MarcaFabrica? MarcaFabrica { get; set; }
        public MarcaFase? MarcaFase { get; set; }
        public MarcaFinalidade? MarcaFinalidade { get; set; }
        public MarcaImpressora? MarcaImpressora { get; set; }
        public MarcaQualidadeImagem? MarcaQualidadeImagem { get; set; }
        public MarcaRaridade? MarcaRaridade { get; set; }
        public MarcaSubTipo? MarcaSubTipo { get; set; }
        //public FabricaFase? FabricaFase { get; set; }
    }

    [Table("marcas_dimensao")]
    public class MarcaDimensao : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Descricao { get; set; } = string.Empty;
    }

    [Table("marcas_fabricas")]
    public class MarcaFabrica : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Nome { get; set; } = string.Empty;
        [MaxLength(255)] public string? Descricao { get; set; }
    }

    [Table("marcas_fases")]
    public class MarcaFase : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
        public int? Ordem { get; set; }
        [Column("menu_exibir")] public int? MenuExibir { get; set; }
        [MaxLength(255)] public string? Imagem { get; set; } = string.Empty;
    }

    [Table("marcas_finalidade")]
    public class MarcaFinalidade : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Descricao { get; set; } = string.Empty;
    }

    [Table("marcas_impressora")]
    public class MarcaImpressora : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Descricao { get; set; } = string.Empty;
    }

    [Table("marcas_qualidade_imagem")]
    public class MarcaQualidadeImagem : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Sigla { get; set; } = string.Empty;
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    [Table("marcas_raridade")]
    public class MarcaRaridade : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Sigla { get; set; } = string.Empty;
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    [Table("marcas_subtipos")]
    public class MarcaSubTipo : BaseModel
    {
        [Key] public int? Id { get; set; }
        public int MarcaTipoId { get; set; }
        [MaxLength(10)] public string? Sigla { get; set; } = string.Empty;
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
        public MarcaTipo? MarcaTipo { get; set; }
    }

    [Table("marcas_tipos")]
    public class MarcaTipo : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    #endregion

    #region Pais
    [Table("paises")]
    public class Pais : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Nome { get; set; } = string.Empty;
        [MaxLength(50)] public string? Descricao { get; set; } = string.Empty;
        public int? PaisCategoriaId { get; set; }
        [MaxLength(50)] public string? Imagem1 { get; set; } = string.Empty;
        [MaxLength(255)][Column("ext_imagem1")] public string? ExtImagem1 { get; set; } = string.Empty;
        [MaxLength(50)] public string? Imagem2 { get; set; } = string.Empty;
        [MaxLength(255)][Column("ext_imagem2")] public string? ExtImagem2 { get; set; } = string.Empty;
        [MaxLength(50)] public string? Imagem3 { get; set; } = string.Empty;
        [MaxLength(255)][Column("ext_imagem3")] public string? ExtImagem3 { get; set; } = string.Empty;

        public PaisCategoria? PaisCategoria { get; set; }
    }

    [Table("paises_categorias")]
    public class PaisCategoria : BaseModel
    {
        [Key] public int? Id { get; set; }
        public int? CodigoId { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;        
    }

    #endregion

    #region Socio

    [Table("socios")]
    public class Socio : BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int? Id { get; set; }
        public int? SocioPerfilId { get; set; }
        [MaxLength(255)] public string? Nome { get; set; } = string.Empty;
        public bool? MostrarSite { get; set; }

        public SocioPerfil? SocioPerfil { get; set; }
    }

    [Table("socio_aniversario")]
    public class SocioAniversario
    {
        [Key] public int? Id { get; set; }
        public int? SocioId { get; set; }
        public int? Dia { get; set; }
        public int? Mes { get; set; }

        public Socio? Socio { get; set; }
    }

    [Table("socio_colecao_info")]
    public class SocioColecao
    {
        [Key] public int? Id { get; set; }
        public int? SocioId { get; set; }
        [MaxLength(255)] public string? TipoColecao { get; set; } = string.Empty;
        public string? ItensColecao { get; set; } = string.Empty;
        [MaxLength(255)] public string? Advertencia { get; set; } = string.Empty;
        [MaxLength(255)] public string? NegociacaoColecao { get; set; } = string.Empty;
        [MaxLength(255)] public string? QtdEmbalagem { get; set; } = string.Empty;
        [MaxLength(255)] public string? QtdEmbalagemNacional { get; set; } = string.Empty;
        public int? TempoColecao { get; set; }

        public Socio? Socio { get; set; }
    }

    [Table("socio_contato")]
    public class SocioContato
    {
        [Key] public int? Id { get; set; }
        public int? SocioId { get; set; }
        public int? DDI { get; set; }
        public int? DDD { get; set; }
        public long? Telefone { get; set; }
        [MaxLength(255)] public string? Email { get; set; } = string.Empty;

        public Socio? Socio { get; set; }
    }

    [Table("socio_endereco")]
    public class SocioEndereco
    {
        [Key] public int? Id { get; set; }
        public int? SocioId { get; set; }
        [MaxLength(255)] public string? Endereco { get; set; } = string.Empty;
        [MaxLength(50)] public string? Numero { get; set; } = string.Empty;
        [MaxLength(50)] public string? Complemento { get; set; } = string.Empty;
        [MaxLength(255)] public string? Bairro { get; set; } = string.Empty;
        [MaxLength(255)] public string? Cidade { get; set; } = string.Empty;
        [MaxLength(255)] public string? Estado { get; set; } = string.Empty;
        [MaxLength(255)] public string? CEP { get; set; } = string.Empty;
        public Socio? Socio { get; set; }
    }

    [Table("socio_financeiro")]
    public class SocioFinanceiro
    {
        [Key] public int? Id { get; set; }
        public int? SocioId { get; set; }
        public int? TipoPagamentoId { get; set; }
        public int? PagamentoEmDia { get; set; }
        [Column("dtUltimoPagamento")] public DateTime? DataUltimoPagamento { get; set; } = DateTime.UtcNow;
        public Socio? Socio { get; set; }
        public TipoPagamento? TipoPagamento { get; set; }
    }

    [Table("socio_perfil")]
    public class SocioPerfil : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    #endregion

    #region tipos

    [Table("tipo_pagamento")]
    public class TipoPagamento : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    #endregion

    #region Usuario

    [Table("usuarios")]
    public class Usuario : BaseModel
    {
        [Key] public int? Id { get; set; }

        public int? SocioId { get; set; }
        [MaxLength(180)] public string? Email { get; set; } = string.Empty;
        [MaxLength(255)] public string? Senha { get; set; } = string.Empty;
        [MaxLength(255)][Column("senha_aberta")] public string? SenhaAberta { get; set; }
        public bool SenhaAtualizada { get; set; }
        [MaxLength(255)][Column("_senha")] public string? Senha1 { get; set; }
        [MaxLength(255)][Column("_usuario")] public string? NomeUsuario { get; set; }
        [Column("last_login")] public DateTime? UltimoLogin { get; set; } = DateTime.UtcNow;

        public Socio? Socio { get; set; }

        [NotMapped] public string? Token { get; set; }
    }

    [Table("usuarios_log")]
    public class UsuarioLog
    {
        [Key] public int? Id { get; set; }

        public int? UsuarioId { get; set; }
        [MaxLength(255)] public string? IP { get; set; } = string.Empty;
        [MaxLength(255)] public string? OS { get; set; } = string.Empty;
        [MaxLength(255)] public string? Browser { get; set; }
        [MaxLength(255)] public string? Device { get; set; }
        [MaxLength(255)] public string? Operadora { get; set; }
        [MaxLength(50)] public string? Estado { get; set; }
        [MaxLength(255)] public string? Cidade { get; set; }
        [MaxLength(50)] public string? Latitude { get; set; }
        [MaxLength(50)] public string? Longitude { get; set; }
        [Column("last_login")] public DateTime? UltimoLogin { get; set; } = DateTime.UtcNow;

        public Usuario? Usuario { get; set; }
    }

    #endregion
}
