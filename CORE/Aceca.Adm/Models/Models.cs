
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aceca.Adm.Models;

[Table("marcas")]
public class Marca
{
    [Key] public int Id { get; set; }
    public int? BExibir { get; set; }
    [Column("em_quarentena")] public int EmQuarentena { get; set; }
    [Column("faseId")] public int? MarcaFaseId { get; set; } 
    [Column("subtipoId")] public int? SubTipoId { get; set; }
    [Column("_tipo")] public string? Tipo { get; set; }
    [MaxLength(50)][Column("cod_aceca")] public string? CodigoAceca { get; set; }
    [MaxLength(50)][Column("cod_old")] public string? CodigoOld { get; set; }
    [MaxLength(50)][Column("cod_sc")] public string? CodigoSC { get; set; }
    [Column("_lista")] public string? Lista { get; set; }
    public int? FinalidadeId { get; set; }
    [MaxLength(255)][Column("marca")] public string NomeMarca { get; set; } = string.Empty;
    public string? Descricao { get; set; } = string.Empty;
    public int? RaridadeId { get; set; }
    public int? FabricaId { get; set; }
    [Column("fabrica_fase")] public int? FabricaFase { get; set; }
    [MaxLength(255)][Column("fabrica_txt")] public string? FabricaDesc { get; set; } = string.Empty;
    public int? DimensaoId { get; set; }
    [MaxLength(50)][Column("_dimensao")] public string? Dimensao { get; set; } = string.Empty;
    public string? Impressora { get; set; } = string.Empty;
    public int? ImpressoraId { get; set; }
    public string? Imagem { get; set; } = string.Empty;
    [MaxLength(255)][Column("nome_imagem")] public string? NomeImagem { get; set; } = string.Empty;
    [MaxLength(255)][Column("ext_imagem")] public string? ExtImagem { get; set; } = string.Empty;
    public int? QualidadeImagemId { get; set; }
    [MaxLength(255)][Column("detalhe")] public string? ImagemDetalhe { get; set; } = string.Empty;
    [MaxLength(255)][Column("nome_detalhe")] public string? NomeImagemDetalhe { get; set; } = string.Empty;
    [MaxLength(255)][Column("ext_detalhe")] public string? ExtImagemDetalhe { get; set; } = string.Empty;
    public string? Lancamento { get; set; } = string.Empty;
    public string? Final { get; set; } = string.Empty;
    public string? Versao { get; set; } = string.Empty;
    [MaxLength(50)][Column("pi-1")] public string? PI_1 { get; set; } = string.Empty;
    [MaxLength(50)][Column("pi-2")] public string? PI_2 { get; set; } = string.Empty;
    public string? Valor { get; set; } = string.Empty;
    public string? Destino { get; set; } = string.Empty;
    [MaxLength(50)][Column("incluido_por")] public string? IncluidoPor { get; set; }
    [Column("carimbo_de_hora")] public DateTime? CriadoEm { get; set; } = DateTime.UtcNow;


    public MarcaDimensao? MarcaDimensao { get; set; }
    public MarcaFabrica? MarcaFabrica { get; set; }
    public MarcaFase? MarcaFase { get; set; }
    public MarcaFinalidade? MarcaFinalidade { get; set; }
    public MarcaImpressora? MarcaImpressora { get; set; }
    public MarcaQualidadeImagem? MarcaQualidadeImagem { get; set; }
    public MarcaRaridade? MarcaRaridade { get; set; }
    public MarcaSubTipo? MarcaSubTipo { get; set; }
    public MarcaTipo? MarcaTipo { get; set; }
}

[Table("admin_usuario")]
public class AdminUsuario
{
    [Key] public int Id { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = string.Empty;
    public string? Usuario { get; set; }
    public string? Senha { get; set; }
    [Column("senha_aberta")] public string? SenhaAberta { get; set; }
}

[Table("agenda")]
public class Agenda
{
    [Key] public int Id { get; set; }
    [MaxLength(255)] public string Titulo { get; set; } = string.Empty;
    [MaxLength(255)] public string SubTitulo { get; set; } = string.Empty;
    public string? Data { get; set; }
    public string? BreveDesc { get; set; }
    public string? Descricao { get; set; }
    [MaxLength(255)] public string Video { get; set; } = string.Empty;
    [MaxLength(255)] public string Imagem { get; set; } = string.Empty;
    [MaxLength(255)] public string Slug { get; set; } = string.Empty;
}

[Table("agenda_img")]
public class AgendaImg
{
    [Key] public int Id { get; set; }
    public int agendaId { get; set; }
    [MaxLength(255)] public string Imagem { get; set; } = string.Empty;
}

[Table("configuracoes")]
public class Configuracao
{
    [Key] public int Id { get; set; }
    [MaxLength(6)] public string CorHeader { get; set; } = string.Empty;
    [MaxLength(6)] public string CorFundo { get; set; } = string.Empty;
    [MaxLength(255)] public string TipoFundo { get; set; } = string.Empty;
    [MaxLength(6)] public string CorRodape { get; set; } = string.Empty;
    [MaxLength(255)] public string TipoRodape { get; set; } = string.Empty;
    [MaxLength(6)] public string CorCopyright { get; set; } = string.Empty;
    public int Paginacao { get; set; }
    [MaxLength(6)] public string CorBase1 { get; set; } = string.Empty;
    [MaxLength(6)] public string CorBase2 { get; set; } = string.Empty;
    [MaxLength(6)] public string CorBase3 { get; set; } = string.Empty;
    [MaxLength(6)] public string CorBase4 { get; set; } = string.Empty;
    [MaxLength(6)] public string CorBase5 { get; set; } = string.Empty;
    [MaxLength(255)] public string NomeDoSite { get; set; } = string.Empty;
    [MaxLength(255)] public string DataRodape { get; set; } = string.Empty;
    [MaxLength(255)] public string SiteEmail { get; set; } = string.Empty;
    [MaxLength(255)] public string SiteEmailSenha { get; set; } = string.Empty;
    [MaxLength(255)] public string PublicKey { get; set; } = string.Empty;
    [MaxLength(255)] public string PrivatecKey { get; set; } = string.Empty;
    [MaxLength(255)] public string YoutubeApiKey { get; set; } = string.Empty;
}

[Table("fabricas")]
public class Fabrica
{
    [Key] public int Id { get; set; }
    [MaxLength(255)] public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    [Column("ano_inicio")] public DateTime AnoInicio { get; set; } = DateTime.UtcNow;
    [Column("ano_fim")] public DateTime AnoFim { get; set; } = DateTime.UtcNow;
}

[Table("fabricas_fase")]
public class FabricaFase
{
    [Key] public int Id { get; set; }
    public int fabrica { get; set; }
    [MaxLength(255)] public string Nome { get; set; } = string.Empty;
}

[Table("marcas_filtro_dimensao")]
public class MarcaDimensao
{
    [Key] public int Id { get; set; }
    [MaxLength(50)] public string tipo { get; set; } = string.Empty;
    [MaxLength(50)] public string descricao { get; set; } = string.Empty;
}

[Table("marcas_filtro_fabricas")]
public class MarcaFabrica
{
    [Key] public int Id { get; set; }
    [MaxLength(255)] public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    [Column("ano_inicio")] public DateTime AnoInicio { get; set; } = DateTime.UtcNow;
    [Column("ano_fim")] public DateTime AnoFim { get; set; } = DateTime.UtcNow;
}

[Table("marcas_filtro_fases")]
public class MarcaFase
{
    [Key] public int Id { get; set; }

    public bool Publica { get; set; }
    [Column("menu_linha_exibir")] public int MenuLinhaExibir { get; set; }
    [Column("ordem_exibir")] public int OrdemExibir { get; set; }
    [MaxLength(255)] public string Titulo { get; set; } = string.Empty;
    [MaxLength(255)] public string Abre { get; set; } = string.Empty;
    [MaxLength(255)] public string SubTitulo { get; set; } = string.Empty;
    [Column("brevedesc")] public string? BreveDescricao { get; set; }
    [MaxLength(255)] public string Imagem { get; set; } = string.Empty;
}

[Table("marcas_filtro_finalidade")]
public class MarcaFinalidade
{
    [Key] public int Id { get; set; }
    [MaxLength(50)] public string Descricao { get; set; } = string.Empty;
}

[Table("marcas_filtro_impressora")]
public class MarcaImpressora
{
    [Key] public int Id { get; set; }
    [MaxLength(50)] public string Descricao { get; set; } = string.Empty;
}

[Table("marcas_filtro_qualidade_imagem")]
public class MarcaQualidadeImagem
{
    [Key] public int Id { get; set; }
    [MaxLength(50)] public string Sigla { get; set; } = string.Empty;
    [MaxLength(255)] public string Descricao { get; set; } = string.Empty;
}

[Table("marcas_filtro_raridade")]
public class MarcaRaridade
{
    [Key] public int Id { get; set; }
    [MaxLength(50)] public string Sigla { get; set; } = string.Empty;
    [MaxLength(255)] public string Descricao { get; set; } = string.Empty;
}

[Table("marcas_filtro_subtipos")]
public class MarcaSubTipo
{
    [Key] public int Id { get; set; }
    public int TipoId { get; set; }
    [MaxLength(50)] public string Sigla { get; set; } = string.Empty;
    [MaxLength(255)] public string Descricao { get; set; } = string.Empty;
}

[Table("marcas_filtro_tipos")]
public class MarcaTipo
{
    [Key] public int Id { get; set; }
    [MaxLength(255)] public string Descricao { get; set; } = string.Empty;
}

[Table("paises")]
public class Pais
{
    [Key] public int Id { get; set; }
    [MaxLength(50)] public string Controle { get; set; } = string.Empty;
    [MaxLength(50)] public string Tipo { get; set; } = string.Empty;
    [MaxLength(255)][Column("pais")] public string PaisNome { get; set; } = string.Empty;
    [MaxLength(255)] public string Nome { get; set; } = string.Empty;
    [MaxLength(255)] public string Comentario { get; set; } = string.Empty;
    [MaxLength(50)] public string Imagem1 { get; set; } = string.Empty;
    [MaxLength(255)][Column("nome_arquivo1")] public string NomeArquvo1 { get; set; } = string.Empty;
    [MaxLength(255)][Column("ext_arquivo1")] public string ExtArquvo1 { get; set; } = string.Empty;
    [MaxLength(50)] public string Imagem2 { get; set; } = string.Empty;
    [MaxLength(255)][Column("nome_arquivo2")] public string NomeArquvo2 { get; set; } = string.Empty;
    [MaxLength(255)][Column("ext_arquivo2")] public string ExtArquvo2 { get; set; } = string.Empty;
    [MaxLength(50)] public string Imagem3 { get; set; } = string.Empty;
    [MaxLength(255)][Column("nome_arquivo3")] public string NomeArquvo3 { get; set; } = string.Empty;
    [MaxLength(255)][Column("ext_arquivo3")] public string ExtArquvo3 { get; set; } = string.Empty;
    [MaxLength(50)][Column("colecao_de")] public string ColecaoDe { get; set; } = string.Empty;
}

[Table("paises_categorias")]
public class PaisCategoria
{
    [Key] public int Id { get; set; }
    public int PaisId { get; set; }
    [MaxLength(255)] public string Titulo { get; set; } = string.Empty;
    [MaxLength(255)] public string SubTitulo { get; set; } = string.Empty;
    [Column("brevedesc")] public string? BreveDescricao { get; set; }
    [MaxLength(255)] public string Imagem { get; set; } = string.Empty;
}

[Table("socios")]
public class Socio
{
    [Key] public int Id { get; set; }
    public int socioPerfilId { get; set; }
    [MaxLength(255)] public string Nome { get; set; } = string.Empty;
    public int MostrarSite { get; set; }
    public SocioPerfil? SocioPerfil { get; set; }
}

[Table("socio_aniversario")]
public class SocioAniversario
{
    [Key] public int Id { get; set; }
    public int SocioId { get; set; }
    public int Dia { get; set; }
    public int Mes { get; set; }
}

[Table("socio_colecao_info")]
public class SocioColecao
{
    [Key] public int Id { get; set; }
    public int SocioId { get; set; }
    [MaxLength(255)] public string TipoColecao { get; set; } = string.Empty;
    public string ItensColecao { get; set; } = string.Empty;
    [MaxLength(255)] public string Advertencia { get; set; } = string.Empty;
    [MaxLength(255)] public string NegociacaoColecao { get; set; } = string.Empty;
    [MaxLength(255)] public string QtdEmbalagem { get; set; } = string.Empty;
    [MaxLength(255)] public string QtdEmbalagemNacional { get; set; } = string.Empty;
    public int TempoColecao { get; set; }
}

[Table("socio_contato")]
public class SocioContato
{
    [Key] public int Id { get; set; }
    public int SocioId { get; set; }
    public int DDI { get; set; }
    public int DDD { get; set; }
    public int Numero { get; set; }
    [MaxLength(255)] public string Email { get; set; } = string.Empty;
}

[Table("socio_endereco")]
public class SocioEndereco
{
    [Key] public int Id { get; set; }
    public int SocioId { get; set; }
    [MaxLength(255)] public string Endereco { get; set; } = string.Empty;
    [MaxLength(50)] public string Numero { get; set; } = string.Empty;
    [MaxLength(50)] public string Complemento { get; set; } = string.Empty;
    [MaxLength(255)] public string Bairro { get; set; } = string.Empty;
    [MaxLength(255)] public string Cidade { get; set; } = string.Empty;
    [MaxLength(255)] public string Estado { get; set; } = string.Empty;
    [MaxLength(255)] public string CEP { get; set; } = string.Empty;
}

[Table("socio_financeiro")]
public class SocioFinanceiro
{
    [Key] public int Id { get; set; }
    public int SocioId { get; set; }
    public int TipoPagamentoId { get; set; }
    public int PagamentoEmDia { get; set; }
    [Column("dtUltimoPagamento")] public DateTime DataUltimoPagamento { get; set; } = DateTime.UtcNow;
}

[Table("socio_perfil")]
public class SocioPerfil
{
    [Key] public int Id { get; set; }
    [MaxLength(255)] public string Descricao { get; set; } = string.Empty;
}

[Table("tipo_pagamento")]
public class TipoPagamento
{
    [Key] public int Id { get; set; }
    [MaxLength(255)] public string Descricao { get; set; } = string.Empty;
}

[Table("usuarios")]
public class Usuario
{
    [Key] public int Id { get; set; }

    public int socioId { get; set; }
    [MaxLength(180)] public string Email { get; set; } = string.Empty;
    [MaxLength(255)] public string Senha { get; set; } = string.Empty;
    [MaxLength(255)][Column("senha_aberta")] public string? SenhaAberta { get; set; }
    [MaxLength(255)][Column("_senha")] public string? Senha1 { get; set; }
    [MaxLength(255)][Column("_usuario")] public string? NomeUsuario { get; set; }
    [Column("last_login")] public DateTime UltimoLogin { get; set; } = DateTime.UtcNow;
}

[Table("usuarios_log")]
public class UsuarioLog
{
    [Key] public int Id { get; set; }

    public int UsuarioId { get; set; }
    [MaxLength(255)] public string IP { get; set; } = string.Empty;
    [MaxLength(255)] public string OS { get; set; } = string.Empty;
    [MaxLength(255)] public string? Browser { get; set; }
    [MaxLength(255)] public string? Device { get; set; }
    [MaxLength(255)] public string? Operadora { get; set; }
    [MaxLength(50)] public string? Estado { get; set; }
    [MaxLength(255)] public string? Cidade { get; set; }
    [MaxLength(50)] public string? Latitude { get; set; }
    [MaxLength(50)] public string? Longitude { get; set; }
    [Column("last_login")] public DateTime UltimoLogin { get; set; } = DateTime.UtcNow;
}

//###############################
/*
[Table("agendas")]
public class AgendaItem
{
    [Key] public int Id { get; set; }
    [MaxLength(200)] public string Titulo { get; set; } = "";
    public string? Descricao { get; set; }
    public DateOnly Data { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFim { get; set; }
    [MaxLength(255)] public string? Local { get; set; }
    [MaxLength(30)] public string Tipo { get; set; } = "proximo";
    public string? CoverUrl { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

[Table("dimensoes")]
public class Dimensao
{
    [Key] public int Id { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = "";
    public decimal? Largura { get; set; }
    public decimal? Altura { get; set; }
}

[Table("fases")]
public class Fase
{
    [Key] public int Id { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = "";
    public string? Descricao { get; set; }
    public ICollection<Marca> Marcas { get; set; } = [];
}

[Table("impressoras")]
public class Impressora
{
    [Key] public int Id { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = "";
    [MaxLength(80)] public string? Modelo { get; set; }
}

[Table("tipos")]
public class Tipo
{
    [Key] public int Id { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = "";
    public string? Descricao { get; set; }
    [Column("ano_inicio")] public DateTime AnoInicio { get; set; } = DateTime.UtcNow;
    [Column("ano_fim")] public DateTime AnoFim { get; set; } = DateTime.UtcNow;
}

[Table("subtipos")]
public class SubTipo
{
    [Key] public int Id { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = "";
    public int? TipoId { get; set; }
    public Tipo? Tipo { get; set; }
}
[Table("colecoes")]
public class Colecao
{
    [Key] public int Id { get; set; }
    public int SocioId { get; set; }
    public Socio? Socio { get; set; }
    [MaxLength(120)] public string Nome { get; set; } = "";
    [MaxLength(80)] public string? Tipo { get; set; }
    public string? Descricao { get; set; }
    public int Itens { get; set; }
}

[Table("contatos")]
public class Contato
{
    [Key] public int Id { get; set; }
    [MaxLength(120)] public string Nome { get; set; } = "";
    [MaxLength(180)] public string Email { get; set; } = "";
    [MaxLength(30)] public string? Telefone { get; set; }
    [MaxLength(40)] public string Assunto { get; set; } = "";
    public string Mensagem { get; set; } = "";
    public string? Anexos { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "novo";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

[Table("socio_perfil")]
public class SocioPerfil
{
    [Key] public int Id { get; set; }
    public int perfilId { get; set; }
    [MaxLength(120)] public string Nome { get; set; } = "";
    [MaxLength(180)] public string Email { get; set; } = "";
    [MaxLength(255)] public string SenhaHash { get; set; } = "";
    [MaxLength(40)] public string Cargo { get; set; } = "socio";
    public DateOnly? Associado { get; set; }
    public string? Foto { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<Colecao> Colecoes { get; set; } = [];
}

[Table("agendas")]
public class AgendaItem
{
    [Key] public int    Id        { get; set; }
    [MaxLength(200)] public string Titulo     { get; set; } = "";
    public string?               Descricao  { get; set; }
    public DateOnly              Data       { get; set; }
    public TimeOnly?             HoraInicio { get; set; }
    public TimeOnly?             HoraFim    { get; set; }
    [MaxLength(255)] public string? Local    { get; set; }
    [MaxLength(30)]  public string  Tipo     { get; set; } = "proximo";
    public string?               CoverUrl   { get; set; }
    public DateTime              CriadoEm   { get; set; } = DateTime.UtcNow;
}

[Table("paises")]
public class Pais
{
    [Key] public int    Id    { get; set; }
    [MaxLength(80)] public string Nome  { get; set; } = "";
    [MaxLength(5)]  public string Sigla { get; set; } = "";
    public ICollection<Fabrica> Fabricas { get; set; } = [];
}

[Table("fabricas")]
public class Fabrica
{
    [Key] public int    Id       { get; set; }
    [MaxLength(120)] public string Nome    { get; set; } = "";
    [MaxLength(80)]  public string? Cidade { get; set; }
    public int?                  PaisId   { get; set; }
    public Pais?                 Pais     { get; set; }
    public ICollection<Marca>    Marcas   { get; set; } = [];
}

[Table("dimensoes")]
public class Dimensao
{
    [Key] public int    Id      { get; set; }
    [MaxLength(80)] public string Nome    { get; set; } = "";
    public decimal?              Largura { get; set; }
    public decimal?              Altura  { get; set; }
}

[Table("fases")]
public class Fase
{
    [Key] public int    Id        { get; set; }
    [MaxLength(80)] public string Nome      { get; set; } = "";
    public string?               Descricao { get; set; }
    public ICollection<Marca>    Marcas    { get; set; } = [];
}

[Table("impressoras")]
public class Impressora
{
    [Key] public int    Id     { get; set; }
    [MaxLength(80)] public string Nome   { get; set; } = "";
    [MaxLength(80)] public string? Modelo{ get; set; }
}

[Table("tipos")]
public class Tipo
{
    [Key] public int    Id        { get; set; }
    [MaxLength(80)] public string Nome      { get; set; } = "";
    public string?               Descricao { get; set; }
    public ICollection<SubTipo>  SubTipos  { get; set; } = [];
    public ICollection<Marca>    Marcas    { get; set; } = [];
}

[Table("subtipos")]
public class SubTipo
{
    [Key] public int    Id     { get; set; }
    [MaxLength(80)] public string Nome   { get; set; } = "";
    public int?                  TipoId { get; set; }
    public Tipo?                 Tipo   { get; set; }
}

[Table("0_new_geral")]
public class Marca
{
    [Key] public int    Id                { get; set; }
    [MaxLength(120)]
    [Column("marca")] 
    public string Nome   { get; set; } = "";
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
    //[Column("carimbo_de_hora")] public DateTime                AtualizadoEm      { get; set; } = DateTime.UtcNow;
}

[Table("colecoes")]
public class Colecao
{
    [Key] public int    Id       { get; set; }
    public int          SocioId  { get; set; }
    public Socio?       Socio    { get; set; }
    [MaxLength(120)] public string Nome     { get; set; } = "";
    [MaxLength(80)]  public string? Tipo    { get; set; }
    public string?               Descricao { get; set; }
    public int                   Itens     { get; set; }
}

[Table("contatos")]
public class Contato
{
    [Key] public int    Id       { get; set; }
    [MaxLength(120)] public string Nome     { get; set; } = "";
    [MaxLength(180)] public string Email    { get; set; } = "";
    [MaxLength(30)]  public string? Telefone{ get; set; }
    [MaxLength(40)]  public string Assunto  { get; set; } = "";
    public string                 Mensagem  { get; set; } = "";
    public string?                Anexos    { get; set; }
    [MaxLength(20)]  public string Status   { get; set; } = "novo";
    public DateTime               CriadoEm  { get; set; } = DateTime.UtcNow;
}

[Table("usuarios")]
public class Usuario
{
    [Key] public int Id { get; set; }
    public int socioId { get; set; }
    [MaxLength(180)] public string Email { get; set; } = "";
    [MaxLength(255)] public string Senha { get; set; } = "";
    public string? Senha_Aberta { get; set; }
    public string? _Usuario { get; set; }
    public DateTime Last_Login { get; set; } = DateTime.UtcNow;
}
*/

