namespace Aceca.Adm.Models
{
    public class FiltroRequestMarca
    {
        public int? MarcaAcervoId { get; set; }
        public int? MarcaFaseId { get; set; }
        public int? MarcaFabricaId { get; set; }
        public int? MarcaTipoId { get; set; }
        public int? MarcaSubTipoId { get; set; }

        public int? MarcaMesId { get; set; }
        public int? MarcaAnoId { get; set; }

        public string IncluidoPor { get; set; }
        public string CodigoAceca { get; set; }
        public string NomeMarca { get; set; }
        public bool PesquisarSemVariante { get; set; }
        public bool PesquisarDescricao { get; set; }

        public bool ExibirGeral { get; set; } = true;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class FilterDataSearchMarca
    {
        public string Value { get; set; }
        public bool Regex { get; set; }
    }

    public class FilterDataMarca
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }

        public FilterDataSearchMarca Search { get; set; }

        public FiltroRequestMarca Filtros { get; set; }
    }
}
