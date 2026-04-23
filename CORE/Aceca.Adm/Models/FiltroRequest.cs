namespace Aceca.Adm.Models
{
    public class FiltroRequest
    {
        public int? MarcaFaseId { get; set; }
        public int? MarcaFabricaId { get; set; }
        public int? MarcaTipoId { get; set; }
        public int? MarcaSubTipoId { get; set; }
        public string IncluidoPor { get; set; }
        public string CodigoAceca { get; set; }
        public string NomeMarca { get; set; }
        public bool PesquisarSemVariante { get; set; }
        public bool PesquisarDescricao { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class DataTableSearch
    {
        public string Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }

        public DataTableSearch Search { get; set; }

        public FiltroRequest Filtros { get; set; }
    }
}
