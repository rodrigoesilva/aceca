using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aceca.Adm.VMModels
{
    public class VMBaseModel
    {
        public bool? Ativo { get; set; }
        public DateTime? DataCriacao { get; set; } = DateTime.Now;
        public DateTime? DataAtualizacao { get; init; } = DateTime.Now;
    }


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
    public class Agenda : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        public string? Data { get; set; }
        [MaxLength(255)] public string? Titulo { get; set; } = string.Empty;
        [MaxLength(255)] public string? SubTitulo { get; set; } = string.Empty;

        public string? BreveDesc { get; set; }
        public string? Descricao { get; set; }
        [MaxLength(255)] public string? Imagem { get; set; } = string.Empty;
        [MaxLength(255)] public string? Video { get; set; } = string.Empty;
    }

    [Table("agenda_img")]
    public class AgendaImg
    {
        [Key] public int? Id { get; set; }
        public int? AgendaId { get; set; }
        [MaxLength(255)] public string? Imagem { get; set; } = string.Empty;

        public Agenda? Agenda { get; set; }
    }

    #endregion

    #region Fabrica

    [Table("fabricas")]
    public class Fabrica : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int? FabricaFaseId { get; set; }

        public FabricaFase? FabricaFase { get; set; }
    }

    [Table("fabricas_fase")]
    public class FabricaFase : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        public int? Codigo { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    #endregion 

    #region Marca

    [Table("marcas")]
    public class VMMarca : VMBaseModel
    {
        public int? Id { get; set; }
        public int? MarcaDimensaoId { get; set; }
        public int? MarcaFabricaId { get; set; }
        public int? MarcaFaseId { get; set; }
        public int? MarcaFinalidadeId { get; set; }
        public int? MarcaImpressoraId { get; set; }
        public int? MarcaQualidadeImagemId { get; set; }
        public int? MarcaRaridadeId { get; set; }
        public int? MarcaSubTipoId { get; set; }

        public string? CodigoAceca { get; set; }
        public string? CodigoVariante { get; set; }
        public string? CodigoSC { get; set; }
        public string? ImgPrincipal { get; set; }
        public string? ImgDetalhe { get; set; }
        public IFormFile? FileImgPrincipal { get; set; }
        public IFormFile? FileImgDetalhe { get; set; }
        public string? Nome { get; set; }

        //[Required(ErrorMessage = "Descrição deve ser preenchida")]
        public string? Descricao { get; set; }
        public string? Valor1PI { get; set; }
        public string? Valor2PI { get; set; }
        public string? Valor { get; set; }
        public string? IncluidoPor { get; set; }
        public int? EmQuarentena { get; set; }
    }

    [Table("marcas_filtro_dimensao")]
    public class VMMarcaDimensao : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Descricao { get; set; } = string.Empty;
    }

    [Table("marcas_filtro_fabricas")]
    public class VMMarcaFabrica : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Nome { get; set; } = string.Empty;
        [MaxLength(255)] public string? Descricao { get; set; }
    }

    [Table("marcas_filtro_fases")]
    public class VMMarcaFase : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
        public int? Ordem { get; set; }
        [Column("menu_exibir")] public int? MenuExibir { get; set; }
        [MaxLength(255)] public string? Imagem { get; set; } = string.Empty;
    }

    [Table("marcas_filtro_finalidade")]
    public class VMMarcaFinalidade : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Descricao { get; set; } = string.Empty;
    }

    [Table("marcas_filtro_impressora")]
    public class VMMarcaImpressora : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Descricao { get; set; } = string.Empty;
    }

    [Table("marcas_filtro_qualidade_imagem")]
    public class VMMarcaQualidadeImagem : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Sigla { get; set; } = string.Empty;
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    [Table("marcas_filtro_raridade")]
    public class VMMarcaRaridade : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(50)] public string? Sigla { get; set; } = string.Empty;
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    [Table("marcas_filtro_subtipos")]
    public class VMMarcaSubTipo : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        public int? MarcaTipoId { get; set; }
        [MaxLength(10)] public string? Sigla { get; set; } = string.Empty;
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
        public VMMarcaTipo? MarcaTipo { get; set; }
    }

    [Table("marcas_filtro_tipos")]
    public class VMMarcaTipo : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    #endregion

    #region Pais
    [Table("paises")]
    public class Pais : VMBaseModel
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
    public class PaisCategoria : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        public int? CodigoId { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    #endregion

    #region Socio

    [Table("socios")]
    public class VMSocio : VMBaseModel
    {
        public int? Id { get; set; }
        public int? SocioContatoId { get; set; }
        public int? SocioEnderecoId { get; set; }
        public int? SocioAniversarioId { get; set; }
        public int? SocioPerfilId { get; set; }

        [MaxLength(255)] public string? Nome { get; set; } = string.Empty;
        [MaxLength(255)] public string? Email { get; set; } = string.Empty;
        public int? DDI { get; set; }
        public int? DDD { get; set; }
        public string? Telefone { get; set; } = string.Empty;
        [MaxLength(255)] public string? Endereco { get; set; } = string.Empty;
        [MaxLength(50)] public string? Numero { get; set; } = string.Empty;
        [MaxLength(50)] public string? Complemento { get; set; } = string.Empty;
        [MaxLength(255)] public string? Bairro { get; set; } = string.Empty;
        [MaxLength(255)] public string? Cidade { get; set; } = string.Empty;
        [MaxLength(255)] public string? Estado { get; set; } = string.Empty;
        [MaxLength(255)] public string? CEP { get; set; } = string.Empty;
        public string? DataAniversario { get; set; } = string.Empty;
        public int? Dia { get; set; }
        public int? Mes { get; set; }
        public bool? MostrarSite { get; set; }

        public SocioPerfil? SocioPerfil { get; set; }
    }

    [Table("socio_perfil")]
    public class SocioPerfil : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    #endregion

    #region tipos

    [Table("tipo_pagamento")]
    public class TipoPagamento : VMBaseModel
    {
        [Key] public int? Id { get; set; }
        [MaxLength(255)] public string? Descricao { get; set; } = string.Empty;
    }

    #endregion

    #region Usuario

    [Table("usuarios")]
    public class Usuario : VMBaseModel
    {
        [Key] public int? Id { get; set; }

        public int? socioId { get; set; }
        [MaxLength(180)] public string? Email { get; set; } = string.Empty;
        [MaxLength(255)] public string? Senha { get; set; } = string.Empty;
        [MaxLength(255)][Column("senha_aberta")] public string? SenhaAberta { get; set; }
        [MaxLength(255)][Column("_senha")] public string? Senha1 { get; set; }
        [MaxLength(255)][Column("_usuario")] public string? NomeUsuario { get; set; }
        [Column("last_login")] public DateTime? UltimoLogin { get; set; } = DateTime.UtcNow;

        public VMSocio? Socio { get; set; }

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

//###############################
/*
[Table("agendas")]
public class AgendaItem
{
    [Key] public int? Id { get; set; }
    [MaxLength(200)] public string? Titulo { get; set; } = "";
    public string? Descricao { get; set; }
    public DateOnly Data { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFim { get; set; }
    [MaxLength(255)] public string? Local { get; set; }
    [MaxLength(30)] public string? Tipo { get; set; } = "proximo";
    public string? CoverUrl { get; set; }
    public DateTime? CriadoEm { get; set; } = DateTime.UtcNow;
}

[Table("dimensoes")]
public class Dimensao
{
    [Key] public int? Id { get; set; }
    [MaxLength(80)] public string? Nome { get; set; } = "";
    public decimal? Largura { get; set; }
    public decimal? Altura { get; set; }
}

[Table("fases")]
public class Fase
{
    [Key] public int? Id { get; set; }
    [MaxLength(80)] public string? Nome { get; set; } = "";
    public string? Descricao { get; set; }
    public ICollection<Marca> Marcas { get; set; } = [];
}

[Table("impressoras")]
public class Impressora
{
    [Key] public int? Id { get; set; }
    [MaxLength(80)] public string? Nome { get; set; } = "";
    [MaxLength(80)] public string? Modelo { get; set; }
}

[Table("tipos")]
public class Tipo
{
    [Key] public int? Id { get; set; }
    [MaxLength(80)] public string? Nome { get; set; } = "";
    public string? Descricao { get; set; }
    [Column("ano_inicio")] public DateTime? AnoInicio { get; set; } = DateTime.UtcNow;
    [Column("ano_fim")] public DateTime? AnoFim { get; set; } = DateTime.UtcNow;
}

[Table("subtipos")]
public class SubTipo
{
    [Key] public int? Id { get; set; }
    [MaxLength(80)] public string? Nome { get; set; } = "";
    public int? TipoId { get; set; }
    public Tipo? Tipo { get; set; }
}
[Table("colecoes")]
public class Colecao
{
    [Key] public int? Id { get; set; }
    public int? SocioId { get; set; }
    public Socio? Socio { get; set; }
    [MaxLength(120)] public string? Nome { get; set; } = "";
    [MaxLength(80)] public string? Tipo { get; set; }
    public string? Descricao { get; set; }
    public int? Itens { get; set; }
}

[Table("contatos")]
public class Contato
{
    [Key] public int? Id { get; set; }
    [MaxLength(120)] public string? Nome { get; set; } = "";
    [MaxLength(180)] public string? Email { get; set; } = "";
    [MaxLength(30)] public string? Telefone { get; set; }
    [MaxLength(40)] public string? Assunto { get; set; } = "";
    public string? Mensagem { get; set; } = "";
    public string? Anexos { get; set; }
    [MaxLength(20)] public string? Status { get; set; } = "novo";
    public DateTime? CriadoEm { get; set; } = DateTime.UtcNow;
}

[Table("socio_perfil")]
public class SocioPerfil
{
    [Key] public int? Id { get; set; }
    public int? perfilId { get; set; }
    [MaxLength(120)] public string? Nome { get; set; } = "";
    [MaxLength(180)] public string? Email { get; set; } = "";
    [MaxLength(255)] public string? SenhaHash { get; set; } = "";
    [MaxLength(40)] public string? Cargo { get; set; } = "socio";
    public DateOnly? Associado { get; set; }
    public string? Foto { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime? CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<Colecao> Colecoes { get; set; } = [];
}

[Table("agendas")]
public class AgendaItem
{
    [Key] public int?    Id        { get; set; }
    [MaxLength(200)] public string? Titulo     { get; set; } = "";
    public string?               Descricao  { get; set; }
    public DateOnly              Data       { get; set; }
    public TimeOnly?             HoraInicio { get; set; }
    public TimeOnly?             HoraFim    { get; set; }
    [MaxLength(255)] public string? Local    { get; set; }
    [MaxLength(30)]  public string?  Tipo     { get; set; } = "proximo";
    public string?               CoverUrl   { get; set; }
    public DateTime?              CriadoEm   { get; set; } = DateTime.UtcNow;
}

[Table("paises")]
public class Pais
{
    [Key] public int?    Id    { get; set; }
    [MaxLength(80)] public string? Nome  { get; set; } = "";
    [MaxLength(5)]  public string? Sigla { get; set; } = "";
    public ICollection<Fabrica> Fabricas { get; set; } = [];
}

[Table("fabricas")]
public class Fabrica
{
    [Key] public int?    Id       { get; set; }
    [MaxLength(120)] public string? Nome    { get; set; } = "";
    [MaxLength(80)]  public string? Cidade { get; set; }
    public int?                  PaisId   { get; set; }
    public Pais?                 Pais     { get; set; }
    public ICollection<Marca>    Marcas   { get; set; } = [];
}

[Table("dimensoes")]
public class Dimensao
{
    [Key] public int?    Id      { get; set; }
    [MaxLength(80)] public string? Nome    { get; set; } = "";
    public decimal?              Largura { get; set; }
    public decimal?              Altura  { get; set; }
}

[Table("fases")]
public class Fase
{
    [Key] public int?    Id        { get; set; }
    [MaxLength(80)] public string? Nome      { get; set; } = "";
    public string?               Descricao { get; set; }
    public ICollection<Marca>    Marcas    { get; set; } = [];
}

[Table("impressoras")]
public class Impressora
{
    [Key] public int?    Id     { get; set; }
    [MaxLength(80)] public string? Nome   { get; set; } = "";
    [MaxLength(80)] public string? Modelo{ get; set; }
}

[Table("tipos")]
public class Tipo
{
    [Key] public int?    Id        { get; set; }
    [MaxLength(80)] public string? Nome      { get; set; } = "";
    public string?               Descricao { get; set; }
    public ICollection<SubTipo>  SubTipos  { get; set; } = [];
    public ICollection<Marca>    Marcas    { get; set; } = [];
}

[Table("subtipos")]
public class SubTipo
{
    [Key] public int?    Id     { get; set; }
    [MaxLength(80)] public string? Nome   { get; set; } = "";
    public int?                  TipoId { get; set; }
    public Tipo?                 Tipo   { get; set; }
}

[Table("0_new_geral")]
public class Marca
{
    [Key] public int?    Id                { get; set; }
    [MaxLength(120)]
    [Column("marca")] 
    public string? Nome   { get; set; } = "";
    [MaxLength(30)]
    [Column("cod_aceca")] 
    public string? CodigoAceca { get; set; }
    public int?                    FaseId { get; set; }
    public Fase?                   Fase   { get; set; }
    public int?                    FabricaId { get; set; }
    public Fabrica?                Fabrica   { get; set; }
    [Column("subtipoId")] public int? TipoId    { get; set; }
    public Tipo?                   Tipo      { get; set; }
    public string?                 Descricao { get; set; }
    [MaxLength(512)][Column("imagem")] public string? ImagemUrl        { get; set; }
    [MaxLength(512)][Column("detalhe")] public string? ImagemDetalheUrl { get; set; }
    [MaxLength(120)][Column("incluido_por")] public string? IncluidoPor      { get; set; }
    [Column("carimbo_de_hora")] public DateTime?                CriadoEm          { get; set; } = DateTime.UtcNow;
    //[Column("carimbo_de_hora")] public DateTime?                AtualizadoEm      { get; set; } = DateTime.UtcNow;
}

[Table("colecoes")]
public class Colecao
{
    [Key] public int?    Id       { get; set; }
    public int?          SocioId  { get; set; }
    public Socio?       Socio    { get; set; }
    [MaxLength(120)] public string? Nome     { get; set; } = "";
    [MaxLength(80)]  public string? Tipo    { get; set; }
    public string?               Descricao { get; set; }
    public int?                   Itens     { get; set; }
}

[Table("contatos")]
public class Contato
{
    [Key] public int?    Id       { get; set; }
    [MaxLength(120)] public string? Nome     { get; set; } = "";
    [MaxLength(180)] public string? Email    { get; set; } = "";
    [MaxLength(30)]  public string? Telefone{ get; set; }
    [MaxLength(40)]  public string? Assunto  { get; set; } = "";
    public string?                 Mensagem  { get; set; } = "";
    public string?                Anexos    { get; set; }
    [MaxLength(20)]  public string? Status   { get; set; } = "novo";
    public DateTime?               CriadoEm  { get; set; } = DateTime.UtcNow;
}

[Table("usuarios")]
public class Usuario
{
    [Key] public int? Id { get; set; }
    public int? socioId { get; set; }
    [MaxLength(180)] public string? Email { get; set; } = "";
    [MaxLength(255)] public string? Senha { get; set; } = "";
    public string? Senha_Aberta { get; set; }
    public string? _Usuario { get; set; }
    public DateTime? Last_Login { get; set; } = DateTime.UtcNow;
}
*/

