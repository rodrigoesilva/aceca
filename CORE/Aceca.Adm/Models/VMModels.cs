using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aceca.Adm.VMModels
{
    public class VMBaseModel
    {
        public bool Ativo { get; set; }
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
        public int? MarcaAcervoId { get; set; }
        public int? MarcaDimensaoId { get; set; }
        public int? MarcaFabricaId { get; set; }
        public int? MarcaFaseId { get; set; }
        public int? MarcaFinalidadeId { get; set; }
        public int? MarcaImpressoraId { get; set; }
        public int? MarcaQualidadeImagemId { get; set; }
        public int? MarcaRaridadeId { get; set; }
        public int? MarcaSubTipoId { get; set; }

        public string? CodigoAceca { get; set; }
        public string? CodigoAcecaNew { get; set; }
        public string? CodigoVariante { get; set; }
        public string? CodigoFabrica { get; set; }        
        public string? ImgPrincipal { get; set; }
        public string? ImgDetalhe { get; set; }
        [NotMapped] public IFormFile? FileImgPrincipal { get; set; }
        [NotMapped] public IFormFile? FileImgDetalhe { get; set; }
        public string? Nome { get; set; }

        //[Required(ErrorMessage = "Descrição deve ser preenchida")]
        public string? Descricao { get; set; }
        public string? Valor1PI { get; set; }
        public string? Valor2PI { get; set; }
        public string? Valor { get; set; }
        public string? IncluidoPor { get; set; }
        public string? IncluidoPorSocioId { get; set; }
        public int? EmQuarentena { get; set; }

        public VMMarcaDimensao? MarcaDimensao { get; set; }
        public VMMarcaFabrica? MarcaFabrica { get; set; }
        public VMMarcaFase? MarcaFase { get; set; }
        public VMMarcaFinalidade? MarcaFinalidade { get; set; }
        public VMMarcaImpressora? MarcaImpressora { get; set; }
        public VMMarcaQualidadeImagem? MarcaQualidadeImagem { get; set; }
        public VMMarcaRaridade? MarcaRaridade { get; set; }
        public VMMarcaSubTipo? MarcaSubTipo { get; set; }
    }

    public class VMMarcaList
    {
        public int? Id { get; set; }
        public int? IdMarcaFase { get; set; }
        public int? IdMarcaAcervo { get; set; }
        public int? IdMarcaFinalidade { get; set; }
        public int? IdMarcaFabrica { get; set; }
        public int? IdMarcaDimensao { get; set; }
        public int? IdMarcaTipo { get; set; }
        public int? IdMarcaSubTipo { get; set; }
        public int? IdMarcaImpressora { get; set; }
        public int? IdMarcaRaridade { get; set; }
        public int? IdQualidadeImagem { get; set; }

        public string? CodigoAceca { get; set; }
        public string? NomeMarca { get; set; }
        public string? NomeAcervo { get; set; }        
        public string? NomeFase { get; set; }
        public string? NomeFabrica { get; set; }
        public string? NomeDimensao { get; set; }
        public string? NomeFinalidade { get; set; }
        public string? NomeImpressora { get; set; }
        public string? NomeRaridade { get; set; }
        public string? SubTipo { get; set; }
        public string? Tipo { get; set; }
        public string? TxtFabrica { get; set; }
        public string? TxtImpressora { get; set; }
        public string? IncluidoPor { get; set; }
        public string? Descricao { get; set; }
        public string? Valor { get; set; }
        public string? Valor1PI { get; set; }
        public string? Valor2PI { get; set; }
        public string? ImgPrincipal { get; set; }
        public string? ImgPrincipalFull { get; set; }
        public string? ImgDetalhe { get; set; }
        public string? ImgDetalheFull { get; set; }
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
        public string? ImgAvatar { get; set; } = string.Empty;
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
}