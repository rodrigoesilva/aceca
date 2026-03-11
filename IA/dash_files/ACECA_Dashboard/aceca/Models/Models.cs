
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aceca.Api.Models;

[Table("socios")]
public class Socio
{
    [Key] public int     Id        { get; set; }
    [MaxLength(120)] public string Nome      { get; set; } = "";
    [MaxLength(180)] public string Email     { get; set; } = "";
    [MaxLength(255)] public string SenhaHash { get; set; } = "";
    [MaxLength(40)]  public string Cargo     { get; set; } = "socio";
    public DateOnly?              Associado { get; set; }
    public string?                Foto      { get; set; }
    public bool                   Ativo     { get; set; } = true;
    public DateTime               CriadoEm  { get; set; } = DateTime.UtcNow;
    public ICollection<Colecao>   Colecoes  { get; set; } = [];
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

[Table("marcas")]
public class Marca
{
    [Key] public int    Id                { get; set; }
    [MaxLength(120)] public string Nome   { get; set; } = "";
    [MaxLength(30)]  public string? CodigoAceca { get; set; }
    public int?                    FaseId { get; set; }
    public Fase?                   Fase   { get; set; }
    public int?                    FabricaId { get; set; }
    public Fabrica?                Fabrica   { get; set; }
    public int?                    TipoId    { get; set; }
    public Tipo?                   Tipo      { get; set; }
    public string?                 Descricao { get; set; }
    [MaxLength(512)] public string? ImagemUrl        { get; set; }
    [MaxLength(512)] public string? ImagemDetalheUrl { get; set; }
    [MaxLength(120)] public string? IncluidoPor      { get; set; }
    public DateTime                CriadoEm          { get; set; } = DateTime.UtcNow;
    public DateTime                AtualizadoEm      { get; set; } = DateTime.UtcNow;
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
