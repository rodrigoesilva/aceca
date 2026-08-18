using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aceca.Adm.Models
{
    [Table("log_erros")]
    public class LogErro
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        // "BadRequest" ou "Exception" (unificado sob "Exception" tanto para catch local
        // quanto para exceção não tratada que sobe até o middleware global).
        [Column("tipo")] public string? Tipo { get; set; }

        [Column("url")] public string? Url { get; set; }
        [Column("metodo_http")] public string? MetodoHttp { get; set; }
        [Column("usuario")] public string? Usuario { get; set; }
        [Column("mensagem_humanizada")] public string? MensagemHumanizada { get; set; }
        [Column("mensagem_original")] public string? MensagemOriginal { get; set; }
        [Column("stack_trace")] public string? StackTrace { get; set; }
        [Column("email_enviado")] public bool EmailEnviado { get; set; }
        [Column("data_criacao")] public DateTime? DataCriacao { get; set; } = DateTime.Now;
    }
}
