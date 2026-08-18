using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aceca.Adm.Models
{

    // ======================
    // MODELS
    // ======================
    #region Model Geral
    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class ForgotModel
    {
        public string Email { get; set; }
    }
    public class ResetModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
    public class BaseModel
    {
        public bool Ativo { get; set; }
        public DateTime? DataCriacao { get; set; } = DateTime.Now;
        public DateTime? DataAtualizacao { get; set; } = DateTime.Now;
    }

    #endregion

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

    #region Acervo

    [Table("marcas_acervo")]
    public class MarcaAcervo : BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }
        public string? Descricao { get; set; } = null;
    }

    #endregion

    #region admin

    [Table("adm_config")]
    public class AdmConfig : BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }
        public string? Parametro { get; set; }
        public string? Descricao { get; set; }
        public string? Valor { get; set; }
    }

    [Table("admin_usuario")]
    public class AdminUsuario
    {
        [Key] public int? Id { get; set; }
        [MaxLength(80)] public string? Nome { get; set; } = null;
        public string? Usuario { get; set; }
        public string? Senha { get; set; }
        [Column("senha_aberta")] public string? SenhaAberta { get; set; }
    }

    [Table("configuracoes")]
    public class Configuracao
    {
        [Key] public int? Id { get; set; }
        [MaxLength(6)] public string? CorHeader { get; set; } = null;
        [MaxLength(6)] public string? CorFundo { get; set; } = null;
        public string? TipoFundo { get; set; } = null;
        [MaxLength(6)] public string? CorRodape { get; set; } = null;
        public string? TipoRodape { get; set; } = null;
        [MaxLength(6)] public string? CorCopyright { get; set; } = null;
        public int? Paginacao { get; set; }
        [MaxLength(6)] public string? CorBase1 { get; set; } = null;
        [MaxLength(6)] public string? CorBase2 { get; set; } = null;
        [MaxLength(6)] public string? CorBase3 { get; set; } = null;
        [MaxLength(6)] public string? CorBase4 { get; set; } = null;
        [MaxLength(6)] public string? CorBase5 { get; set; } = null;
        public string? NomeDoSite { get; set; } = null;
        public string? DataRodape { get; set; } = null;
        public string? SiteEmail { get; set; } = null;
        public string? SiteEmailSenha { get; set; } = null;
        public string? PublicKey { get; set; } = null;
        public string? PrivatecKey { get; set; } = null;
        public string? YoutubeApiKey { get; set; } = null;
    }

    #endregion

    #region agenda

    [Table("agenda")]
    public class Agenda : BaseModel
    {
        [Key] public int? Id { get; set; }

        public int? AgendaImagemId { get; set; }
        public string? Data { get; set; }
        public string? Titulo { get; set; } = null;
        public string? SubTitulo { get; set; } = null;
        public string? BreveDesc { get; set; }
        public string? Descricao { get; set; }
        public string? Video { get; set; } = null;

        public AgendaImagem? AgendaImagem { get; set; }
    }

    [Table("agenda_img")]
    public class AgendaImagem : BaseModel
    {
        [Key] public int? Id { get; set; }        
        public string? Imagem { get; set; } = null;
        public string? Descricao { get; set; }
    }

    #endregion

    #region download

    [Table("download")]
    public class Download : BaseModel
    {
        [Key] public int? Id { get; set; }

        public int? DownloadTipoId { get; set; }
        public string? Titulo { get; set; } = null;
        public string? Nome { get; set; } = null;
        public string? Extensao { get; set; }
        public string? Imagem { get; set; }
        public string? Diretorio { get; set; } = null;
        public string? Descricao { get; set; } = null;

        public int? SocioId { get; set; }
        public DownloadTipo? DownloadTipo { get; set; }
        public Socio? Socio { get; set; }
    }

    [Table("download_tipo")]
    public class DownloadTipo : BaseModel
    {
        [Key] public int? Id { get; set; }
        public string? Descricao { get; set; }
    }

    #endregion

    #region Fabrica

    [Table("fabricas")]
    public class Fabrica : BaseModel
    {
        [Key] public int? Id { get; set; }
        public string? Nome { get; set; } = null;
        public string? Descricao { get; set; }
        public int? FabricaFaseId { get; set; }

        public FabricaFase? FabricaFase { get; set; }
    }

    [Table("fabricas_fase")]
    public class FabricaFase : BaseModel
    {
        [Key] public int? Id { get; set; }
        public int? Codigo { get; set; }
        public string? Descricao { get; set; } = null;
    }

    #endregion 

    #region Marca

    [Table("marcas")]
    public class Marcas : BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        public int? MarcaAcervoId { get; set; }
        public int? MarcaDimensaoId { get; set; }
        public int? MarcaFabricaId { get; set; }
        [Column("fabrica_txt")] public string? TxtFabrica { get; set; } = null;
        public int? MarcaFaseId { get; set; }
        public int? MarcaFaseAcervoId { get; set; }
        
        public int? MarcaFinalidadeId { get; set; }
        public int? MarcaImpressoraId { get; set; }
        [Column("impressora")] public string? TxtImpressora { get; set; } = null;
        public int? MarcaQualidadeImagemId { get; set; }
        public int? MarcaRaridadeId { get; set; }
        public int? MarcaSubTipoId { get; set; }
        public string? CodigoAceca { get; set; } = null;
        public string? CodigoAcecaNew { get; set; } = null;
        [Column("codigoSC")] public string? CodigoFabrica { get; set; } = null;
        public string? ImgPrincipal { get; set; } = null;
        public string? ImgDetalhe { get; set; } = null;
        public string? Nome { get; set; } = null;
        public string? Descricao { get; set; } = null;
        public string? Valor1PI { get; set; } = null;
        public string? Valor2PI { get; set; } = null;
        public string? Valor { get; set; } = null;
        public string? IncluidoPor { get; set; } = null;
        public string? IncluidoPorSocioId { get; set; } = null;
        public int? EmQuarentena { get; set; }
        public bool? ExibirGeral { get; set; }
        public MarcaAcervo? MarcaAcervo { get; set; }
        public MarcaDimensao? MarcaDimensao { get; set; }
        public MarcaFabrica? MarcaFabrica { get; set; }
        public MarcaFase? MarcaFase { get; set; }
        public MarcaFinalidade? MarcaFinalidade { get; set; }
        public MarcaImpressora? MarcaImpressora { get; set; }
        public MarcaQualidadeImagem? MarcaQualidadeImagem { get; set; }
        public MarcaRaridade? MarcaRaridade { get; set; }
        public MarcaSubTipo? MarcaSubTipo { get; set; }
        //[ValidateNever] public ICollection<SocioColecao>? SociosColecao { get; set; }
    }

    [Table("marcas_dimensao")]
    public class MarcaDimensao : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Descricao { get; set; } = null;
    }

    [Table("marcas_fabricas")]
    public class MarcaFabrica : BaseModel
    {
        [Key] public int? Id { get; set; }
        public string? Nome { get; set; } = null;
        public string? Descricao { get; set; }
    }

    [Table("marcas_fases")]
    public class MarcaFase : BaseModel
    {
        [Key] public int? Id { get; set; }
        public string? Descricao { get; set; } = null;
        public int? Ordem { get; set; }
        [Column("menu_exibir")] public int? MenuExibir { get; set; }
    }

    [Table("marcas_finalidade")]
    public class MarcaFinalidade : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Descricao { get; set; } = null;
    }

    [Table("marcas_impressora")]
    public class MarcaImpressora : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Descricao { get; set; } = null;
    }

    [Table("marcas_qualidade_imagem")]
    public class MarcaQualidadeImagem : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Sigla { get; set; } = null;
        public string? Descricao { get; set; } = null;
    }

    [Table("marcas_raridade")]
    public class MarcaRaridade : BaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Sigla { get; set; } = null;
        public string? Descricao { get; set; } = null;
    }

    [Table("marcas_subtipos")]
    public class MarcaSubTipo : BaseModel
    {
        [Key] public int? Id { get; set; }
        public int MarcaTipoId { get; set; }
        [MaxLength(10)] public string? Sigla { get; set; } = null;
        public string? Descricao { get; set; } = null;
        public MarcaTipo? MarcaTipo { get; set; }
    }

    [Table("marcas_tipos")]
    public class MarcaTipo : BaseModel
    {
        [Key] public int? Id { get; set; }
        public string? Descricao { get; set; } = null;
    }

    #endregion

    #region Pais
    [Table("paises")]
    public class Pais : BaseModel
    {
        [Key] public int? Id { get; set; }
        public string? Nome { get; set; } = null;
        [MaxLength(50)] public string? Descricao { get; set; } = null;
        public int? PaisCategoriaId { get; set; }
        [MaxLength(50)] public string? Imagem1 { get; set; } = null;
        [Column("ext_imagem1")] public string? ExtImagem1 { get; set; } = null;
        [MaxLength(50)] public string? Imagem2 { get; set; } = null;
        [Column("ext_imagem2")] public string? ExtImagem2 { get; set; } = null;
        [MaxLength(50)] public string? Imagem3 { get; set; } = null;
        [Column("ext_imagem3")] public string? ExtImagem3 { get; set; } = null;

        public PaisCategoria? PaisCategoria { get; set; }
    }

    [Table("paises_categorias")]
    public class PaisCategoria : BaseModel
    {
        [Key] public int? Id { get; set; }
        public int? CodigoId { get; set; }
        public string? Descricao { get; set; } = null;        
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
        public string? Nome { get; set; } = null;
        public string? ImgAvatar { get; set; } = null;
        public bool? MostrarSite { get; set; }

        public SocioPerfil? SocioPerfil { get; set; }

        //[ValidateNever] public ICollection<SocioColecao>? ColecaoSocios { get; set; }
    }    

    [Table("socio_aniversario")]
    public class SocioAniversario
    {
        [Key] public int? Id { get; set; }
        public int? SocioId { get; set; }
        public int? Dia { get; set; }
        public int? Mes { get; set; }
        public int? Ano { get; set; }

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
        public string? Email { get; set; } = null;

        public Socio? Socio { get; set; }
    }

    [Table("socio_endereco")]
    public class SocioEndereco
    {
        [Key] public int? Id { get; set; }
        public int? SocioId { get; set; }
        public string? Endereco { get; set; } = null;
        [MaxLength(50)] public string? Numero { get; set; } = null;
        [MaxLength(50)] public string? Complemento { get; set; } = null;
        public string? Bairro { get; set; } = null;
        public string? Cidade { get; set; } = null;
        public string? Estado { get; set; } = null;
        public string? CEP { get; set; } = null;
        public Socio? Socio { get; set; }
    }

    [Table("socio_financeiro")]
    public class SocioFinanceiro
    {
        [Key] public int? Id { get; set; }
        public int? SocioId { get; set; }
        public int? TipoPagamentoId { get; set; }
        public int? PagamentoEmDia { get; set; }
        [Column("dtUltimoPagamento")] public DateTime? DataUltimoPagamento { get; set; }

        // Controle de envio dos avisos de vencimento (automação SocioFinanceiroCheckService).
        // Guardam a data de vencimento (- 7 / - 2 dias) para a qual o aviso já foi disparado:
        // se DataUltimoPagamento mudar (renovação), o vencimento recalculado não bate mais
        // com o valor guardado, e o aviso volta a ser enviado no novo ciclo automaticamente.
        [Column("data_aviso_vencimento_7dias")] public DateTime? DataAvisoVencimento7Dias { get; set; }
        [Column("data_aviso_vencimento_2dias")] public DateTime? DataAvisoVencimento2Dias { get; set; }

        public Socio? Socio { get; set; }
        public TipoPagamento? TipoPagamento { get; set; }
    }

    [Table("socio_log_acesso")]
    public class SocioLogAcesso
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }
        public int? SocioId { get; set; }
        public string? IP { get; set; }
        public string? OS { get; set; }
        public string? Browser { get; set; }
        public string? Device { get; set; }
        public string? Operadora { get; set; }
        public string? Estado { get; set; }
        public string? Cidade { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        [Column("last_login")] public DateTime? UltimoLogin { get; set; }

        public Socio? Socio { get; set; }
    }

    [Table("socio_perfil")]
    public class SocioPerfil : BaseModel
    {
        [Key] public int? Id { get; set; }
        public string? Descricao { get; set; } = null;
    }
    [Table("socio_seguranca")]
    public class SocioSeguranca
    {
        [Key] public int? Id { get; set; }

        public int SocioId { get; set; }
        [Column("nome_usuario")] public string? NomeUsuario { get; set; } = null;
        public string? Email { get; set; } = null;
        public string? Senha { get; set; } = null;
        [Column("senha_aberta")] public string? SenhaAberta { get; set; }
        public bool SenhaAtualizada { get; set; }        
        [Column("last_login")] public DateTime? UltimoLogin { get; set; }

        public Socio? Socio { get; set; }

        [NotMapped] public string? Token { get; set; }

        public string? ResetPasswordToken { get; set; } = null;
        public DateTime? ResetPasswordTokenExpiry { get; set; } = null;

        // Bloqueio temporário de login após detectar tentativa de captura de tela
        // (ver AuthController.ReportImageAccess / Login).
        [Column("bloqueado_ate")] public DateTime? BloqueadoAte { get; set; }

        // Sessão única: carimbo (GUID) gerado a cada login bem-sucedido e validado em
        // todo request via OnValidatePrincipal (Program.cs) - um novo login sobrescreve
        // esse valor, o que derruba qualquer sessão anterior (outro device/navegador)
        // na próxima requisição dela.
        [Column("session_stamp")] public string? SessionStamp { get; set; }
    }

    #endregion

    #region Socio Colecao

    [Table("socio_colecao")]
    public class SocioColecao : BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }
        public int? SocioId { get; set; }
        public int? MarcaId { get; set; }
        public bool Possui { get; set; }
        public bool Interesse { get; set; }
        [Column("disponivel_negocio")] public bool DisponivelNegocio { get; set; }
        public string? Observacao { get; set; } = null;

        public Socio? Socio { get; set; }
        public Marcas? Marca { get; set; }
    }

    [Table("socio_colecao_info")]
    public class SocioColecaoInfo
    {
        [Key] public int? Id { get; set; }
        public int? SocioId { get; set; }
        public string? TipoColecao { get; set; } = null;
        public string? ItensColecao { get; set; } = null;
        public string? Advertencia { get; set; } = null;
        public string? NegociacaoColecao { get; set; } = null;
        public string? QtdEmbalagem { get; set; } = null;
        public string? QtdEmbalagemNacional { get; set; } = null;
        public int? TempoColecao { get; set; }

        public Socio? Socio { get; set; }
    }

    #endregion

    #region tipos

    [Table("tipo_pagamento")]
    public class TipoPagamento : BaseModel
    {
        [Key] public int? Id { get; set; }
        public string? Descricao { get; set; } = null;
    }

    #endregion

    #region Geo

    public class GeoModel
    {
        public string ip { get; set; }
        public string type { get; set; }
        public string continent_code { get; set; }
        public string continent_name { get; set; }
        public string country_code { get; set; }
        public string country_name { get; set; }
        public string region_code { get; set; }
        public string region_name { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    #endregion
    
}
