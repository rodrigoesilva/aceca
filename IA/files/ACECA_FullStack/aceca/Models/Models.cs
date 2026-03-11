using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aceca.Api.Models;

[Table("socios")]
public class Socio
{
    [Key] public int    Id        { get; set; }
    [MaxLength(120)] public string  Nome      { get; set; } = "";
    [MaxLength(180)] public string  Email     { get; set; } = "";
    [MaxLength(255)] public string  SenhaHash { get; set; } = "";
    [MaxLength(40)]  public string  Cargo     { get; set; } = "socio";
    public DateOnly?               Associado { get; set; }
    public string?                 Foto      { get; set; }
    public bool                    Ativo     { get; set; } = true;
    public DateTime                CriadoEm  { get; set; } = DateTime.UtcNow;
    public ICollection<Colecao>    Colecoes  { get; set; } = [];
    public ICollection<CargoDiretoria> Cargos { get; set; } = [];
}

[Table("eventos")]
public class Evento
{
    [Key] public int    Id        { get; set; }
    [MaxLength(200)] public string  Titulo    { get; set; } = "";
    public string?                 Descricao { get; set; }
    public DateOnly                Data      { get; set; }
    public TimeOnly?               HoraInicio{ get; set; }
    public TimeOnly?               HoraFim   { get; set; }
    [MaxLength(255)] public string? Local    { get; set; }
    [MaxLength(20)]  public string  Tipo     { get; set; } = "proximo";
    public string?                 CoverUrl  { get; set; }
    public DateTime                CriadoEm  { get; set; } = DateTime.UtcNow;
    public ICollection<FotoEvento> Fotos     { get; set; } = [];
}

[Table("fotos_evento")]
public class FotoEvento
{
    [Key] public int   Id       { get; set; }
    public int         EventoId { get; set; }
    public Evento?     Evento   { get; set; }
    [MaxLength(512)] public string Url     { get; set; } = "";
    public string?               Legenda  { get; set; }
    public DateTime              CriadoEm { get; set; } = DateTime.UtcNow;
}

[Table("colecoes")]
public class Colecao
{
    [Key] public int    Id       { get; set; }
    public int          SocioId  { get; set; }
    public Socio?       Socio    { get; set; }
    [MaxLength(120)] public string  Nome     { get; set; } = "";
    [MaxLength(80)]  public string? Tipo     { get; set; }
    public string?                 Descricao{ get; set; }
    public int                     Itens    { get; set; }
}

[Table("cargos_diretoria")]
public class CargoDiretoria
{
    [Key] public int   Id       { get; set; }
    public int         SocioId  { get; set; }
    public Socio?      Socio    { get; set; }
    [MaxLength(80)] public string  Cargo    { get; set; } = "";
    public DateOnly?              Inicio   { get; set; }
    public DateOnly?              Fim      { get; set; }
    public bool                   Ativo    { get; set; } = true;
}

[Table("contatos")]
public class Contato
{
    [Key] public int    Id       { get; set; }
    [MaxLength(120)] public string  Nome     { get; set; } = "";
    [MaxLength(180)] public string  Email    { get; set; } = "";
    [MaxLength(30)]  public string? Telefone { get; set; }
    [MaxLength(40)]  public string  Assunto  { get; set; } = "";
    public string                  Mensagem { get; set; } = "";
    public string?                 Anexos   { get; set; }
    [MaxLength(20)] public string   Status   { get; set; } = "novo";
    public DateTime                CriadoEm { get; set; } = DateTime.UtcNow;
}