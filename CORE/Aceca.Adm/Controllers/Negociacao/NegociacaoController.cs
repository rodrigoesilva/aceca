using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers.Admin.Socio
{
    public class NegociacaoController : Controller
    {
        #region variaveis

        private readonly ILogger<NegociacaoController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;
        private readonly HelperExtensionsController _helperController;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;
        //

        #endregion

        public NegociacaoController(ILogger<NegociacaoController> logger, AppDbContext db
            , IWebHostEnvironment env, IConfiguration cfg
            , HelperExtensionsController helperController)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;
            _helperController = helperController;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;
        }

        #region MeusNegocios

        public ActionResult Index()
        {
            return View("~/Views/Admin/Negociacao/NegociacaoAcervo.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ListGrid()
        {
            try
            {
                // 1. Executa os JOINs trazendo apenas as colunas necessárias do banco de forma assíncrona
                var dadosBrutos = await _db.Socio
                    .AsNoTracking()
                    .Join(_db.SocioColecao,
                          socio => socio.Id,
                          colecao => colecao.SocioId,
                          (socio, colecao) => new { socio.Id, socio.Nome, ColecaoPossui = colecao.Possui })
                    .Join(_db.SocioContato,
                          combinado => combinado.Id,
                          contato => contato.SocioId,
                          (combinado, contato) => new { combinado.Id, combinado.Nome, combinado.ColecaoPossui, contato.DDI, contato.DDD, contato.Telefone, contato.Email })
                    .ToListAsync();

                // 2. Agrupa pelo Nome, calcula a quantidade de itens possuídos e ordena
                var lstModel = dadosBrutos
                    .GroupBy(x => x.Nome)
                    .Select(grupo => new
                    {
                        SocioNome = grupo.Key, // Como agrupamos por Nome, usamos o grupo.Key
                        SocioId = grupo.FirstOrDefault()?.Id,// Pegamos o Id do primeiro registro do grupo (já que possuem o mesmo Nome)
                        SocioDDI = grupo.FirstOrDefault()?.DDI,
                        SocioDDD = grupo.FirstOrDefault()?.DDD,
                        SocioTelefone = grupo.FirstOrDefault()?.Telefone,
                        SocioEmail = grupo.FirstOrDefault()?.Email,
                        QuantidadePossui = grupo.Count(x => x.ColecaoPossui)
                    })
                    .OrderBy(r => r.SocioNome)
                    .ToList();

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = lstModel
                });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";

                _logger.LogError(mensagemErro);

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = mensagemErro
                });
            }
        }

        #endregion

        #region PorSocio

        public ActionResult PorSocio()
        {
            return View("~/Views/Admin/Negociacao/NegociacaoSocio.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ListGrid_PorSocio()
        {
            try
            {
                // 1. Executa os JOINs trazendo apenas as colunas necessárias do banco de forma assíncrona
                var dadosBrutos = await _db.Socio
                    .AsNoTracking()
                    .Join(_db.SocioColecao,
                          socio => socio.Id,
                          colecao => colecao.SocioId,
                          (socio, colecao) => new { socio.Id, socio.Nome, ColecaoPossui = colecao.Possui })
                    .Join(_db.SocioContato,
                          combinado => combinado.Id,
                          contato => contato.SocioId,
                          (combinado, contato) => new { combinado.Id, combinado.Nome, combinado.ColecaoPossui, contato.DDI, contato.DDD, contato.Telefone, contato.Email })
                    .ToListAsync();

                // 2. Agrupa pelo Nome, calcula a quantidade de itens possuídos e ordena
                var lstModel = dadosBrutos
                    .GroupBy(x => x.Nome)
                    .Select(grupo => new
                    {
                        SocioNome = grupo.Key, // Como agrupamos por Nome, usamos o grupo.Key
                        SocioId = grupo.FirstOrDefault()?.Id,// Pegamos o Id do primeiro registro do grupo (já que possuem o mesmo Nome)
                        SocioDDI = grupo.FirstOrDefault()?.DDI,
                        SocioDDD = grupo.FirstOrDefault()?.DDD,
                        SocioTelefone = grupo.FirstOrDefault()?.Telefone,
                        SocioEmail = grupo.FirstOrDefault()?.Email,
                        QuantidadePossui = grupo.Count(x => x.ColecaoPossui)
                    })
                    .OrderBy(r => r.SocioNome)
                    .ToList();

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = lstModel
                });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";

                _logger.LogError(mensagemErro);

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = mensagemErro
                });
            }
        }

        #endregion

        #region PorAcervo

        public ActionResult PorAcervo()
        {
            return View("~/Views/Admin/Negociacao/NegociacaoAcervo.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ListGrid_PorAcervo([FromBody] FilterDataMarca request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request inválido");

                var filtro = request.Filtros ?? new FiltroRequestMarca();

                var imgBase = _urlBaseImg;
                var imgDefault = $"{_urlBaseSite}/assets/img/img_inexistente.jpg";

                var sqlFrom = new StringBuilder(@"
                FROM marcas m
                LEFT JOIN marcas_acervo ma ON m.marcaAcervoId = ma.id
                LEFT JOIN marcas_fases mf ON m.marcaFaseId = mf.id
                LEFT JOIN marcas_finalidade mfi ON m.marcaFinalidadeId = mfi.id
                LEFT JOIN marcas_fabricas mfa ON m.marcaFabricaId = mfa.id
                LEFT JOIN marcas_dimensao md ON m.marcaDimensaoId = md.id
                LEFT JOIN marcas_impressora mi ON m.marcaImpressoraId = mi.id
                LEFT JOIN marcas_raridade mr ON m.marcaRaridadeId = mr.id
                LEFT JOIN marcas_raridade mq ON m.marcaQualidadeImagemId = mq.id
                LEFT JOIN marcas_subtipos mst ON m.marcaSubTipoId = mst.id
                LEFT JOIN marcas_tipos mt ON mst.marcaTipoId = mt.id
                WHERE 1=1
                ");

                var parameters = new DynamicParameters();

                // =========================
                // FILTROS
                // =========================

                if (filtro.MarcaAcervoId > 0)
                {
                    if (filtro.MarcaAcervoId != 1 || !filtro.ExibirGeral)
                    {
                        sqlFrom.Append(" AND m.marcaAcervoId = @MarcaAcervoId");
                        parameters.Add("@MarcaAcervoId", filtro.MarcaAcervoId);
                    }
                }

                if (filtro.MarcaFaseId > 0)
                {
                    if (filtro.MarcaAcervoId != 1)
                    {
                        sqlFrom.Append(" AND m.marcafaseAcervoId = @MarcaFaseAcervoId");
                        parameters.Add("@MarcaFaseAcervoId", filtro.MarcaFaseId);
                    }
                    else
                    {
                        sqlFrom.Append(" AND m.marcaFaseId = @MarcaFaseId");
                        parameters.Add("@MarcaFaseId", filtro.MarcaFaseId);
                    }
                }

                if (filtro.MarcaTipoId > 0)
                {
                    sqlFrom.Append(" AND mst.marcaTipoId = @MarcaTipoId");
                    parameters.Add("@MarcaTipoId", filtro.MarcaTipoId);
                }

                if (filtro.MarcaSubTipoId > 0)
                {
                    sqlFrom.Append(" AND m.marcaSubTipoId = @MarcaSubTipoId");
                    parameters.Add("@MarcaSubTipoId", filtro.MarcaSubTipoId);
                }

                // =========================
                // SEARCH
                // =========================
                if (!string.IsNullOrWhiteSpace(request.Search?.Value))
                {
                    var rawSearch = request.Search.Value.Trim();
                    var normalized = Regex.Replace(rawSearch, @"[^\w\s]", " ");

                    var fullTextSearch = string.Join(" ",
                        normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => $"+{s}*")
                    );

                    bool incluirDescricao = filtro.PesquisarDescricao;
                    bool termoCurto = rawSearch.Length < 3; // ← detecta termos que o FULLTEXT ignora

                    sqlFrom.Append(" AND (");

                    if (!termoCurto)
                    {
                        // FULLTEXT só para termos com 3+ caracteres
                        sqlFrom.Append(@"
                            MATCH(m.Nome, m.Descricao, m.CodigoAceca)
                            AGAINST(@Search IN BOOLEAN MODE)
                            OR ");
                    }

                    // LIKE sempre cobre CodigoAceca e Nome
                    sqlFrom.Append(@"
                        m.CodigoAceca LIKE @SearchLike
                        OR m.Nome LIKE @SearchLike
                        ");

                    // Descrição só se checkbox ativo
                    if (incluirDescricao)
                    {
                        sqlFrom.Append(@" OR m.Descricao LIKE @SearchLike ");
                    }

                    sqlFrom.Append(")");

                    parameters.Add("@Search", fullTextSearch);
                    parameters.Add("@SearchLike", $"%{rawSearch}%");
                }

                // fallback
                else if (!string.IsNullOrWhiteSpace(filtro.NomeMarca))
                {
                    sqlFrom.Append(" AND m.Nome LIKE @Nome");
                    parameters.Add("@Nome", $"%{filtro.NomeMarca}%");
                }

                if (filtro.PesquisarSemVariante)
                {
                    sqlFrom.Append(" AND m.codigoAceca REGEXP '[0-9]$'");
                }

                // =========================
                // COUNT
                // =========================

                var totalSql = "SELECT COUNT(1) FROM marcas";
                var filteredSql = "SELECT COUNT(1) " + sqlFrom;

                // =========================
                // DATA
                // =========================

                var dataSql = $@"
                    SELECT
                        m.id AS Id,
                        ma.id AS IdMarcaAcervo,
                        mf.id AS IdMarcaFase,
                        mfi.id AS IdMarcaFinalidade,
                        mfa.id AS IdMarcaFabrica,
                        md.id AS IdMarcaDimensao,
                        mt.id AS IdMarcaTipo,
                        mst.id AS IdMarcaSubTipo,
                        mi.id AS IdMarcaImpressora,
                        mr.id AS IdMarcaRaridade,
                        mq.id AS IdQualidadeImagem,

                        m.CodigoAceca,
                        m.Nome AS NomeMarca,
                        
                        ma.Descricao AS NomeAcervo,
                        mf.Descricao AS NomeFase,
                        mfa.Nome AS NomeFabrica,
                        md.Descricao AS NomeDimensao,
                        mfi.Descricao AS NomeFinalidade,
                        mi.Descricao AS NomeImpressora,
                        mr.Descricao AS NomeRaridade,
                        mq.Descricao AS NomeQualidade,
                        mst.Descricao AS SubTipo,
                        mt.Descricao AS Tipo,
                        m.fabrica_txt AS TxtFabrica,
                        m.impressora AS TxtImpressora,

                        m.Descricao,
                        m.IncluidoPor,

                        m.Valor,
                        m.Valor1PI,
                        m.Valor2PI,

                        m.ImgPrincipal,
                        IF(m.ImgPrincipal IS NOT NULL,
                            CONCAT(@ImgBase,'/',m.MarcaFaseId,'/',m.ImgPrincipal),
                            @ImgDefault) AS ImgPrincipalFull,

                        m.ImgDetalhe,
                        IF(m.ImgDetalhe IS NOT NULL,
                            CONCAT(@ImgBase,'/detalhes/',m.ImgDetalhe),
                            @ImgDefault) AS ImgDetalheFull

                    {sqlFrom}

                    ORDER BY mf.id, m.nome, m.CodigoAceca
                    LIMIT @Limit OFFSET @Offset
                    ";

                parameters.Add("@ImgBase", imgBase);
                parameters.Add("@ImgDefault", imgDefault);
                parameters.Add("@Limit", request.Length);
                parameters.Add("@Offset", request.Start);

                using var conn = _db.Database.GetDbConnection();

                var total = await conn.ExecuteScalarAsync<int>(totalSql);
                var filtered = await conn.ExecuteScalarAsync<int>(filteredSql, parameters);
                var data = await conn.QueryAsync(dataSql, parameters);

                return Ok(new
                {
                    draw = request.Draw,
                    recordsTotal = total,
                    recordsFiltered = filtered,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro FiltrarDados");

                return BadRequest(new { error = true, message = ex.Message });
            }
        }

        #endregion

        #region Socio Derivacao
        public async Task<IActionResult> Create_SocioSeguranca(VMSocio model)
        {
            try
            {
                var newModel = new SocioSeguranca();

                string strTempPass = _helperController.GenerateStringPassword(8);

                    newModel = new SocioSeguranca
                    {
                        SocioId = (int)model?.Id,
                        Email = model?.Email?.Trim()?.ToLower(),
                        Senha = _helperController.GenerateHashPassword(strTempPass),
                        SenhaAberta = strTempPass,
                        SenhaAtualizada = false,
                        NomeUsuario = model?.Nome,
                        UltimoLogin = DateTime.UtcNow.AddHours(-3),
                        Token = null,
                        ResetPasswordToken = null,
                        ResetPasswordTokenExpiry = null,
                    };

                _db.SocioSeguranca.Add(newModel);
                await _db.SaveChangesAsync();

                var newSocioContatoId = newModel?.Id;

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = newModel,
                });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";

                _logger.LogError(mensagemErro);

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = mensagemErro
                });
            }
        }
        public async Task<IActionResult> Create_SocioContato(VMSocio model)
        {
            try
            {
                if (!_helperController.IsValidEmailUsingMailAddress(model?.Email?.Trim()?.ToLower()))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Formato de Email Inválido"
                    });

                var newModel = new SocioContato
                {
                    SocioId = model.Id,
                    DDI =  model.DDI > 0 ? model.DDI : 55,
                    DDD =  !string.IsNullOrEmpty(model.Telefone) ? Convert.ToInt16(model.Telefone.Split(")")[0].Replace("(", string.Empty)) : null,
                    Telefone = !string.IsNullOrEmpty(model.Telefone) ? Convert.ToInt32(model.Telefone.Split(")")[1].Replace("-", string.Empty)) : null,
                    Email = model?.Email?.Trim()?.ToLower(),
                };

                _db.SocioContato.Add(newModel);
                _db.SaveChanges();

                var newSocioContatoId = newModel?.Id;

                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = model,
                });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";

                _logger.LogError(mensagemErro);

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = mensagemErro
                });
            }
        }
        public async Task<IActionResult> Create_SocioEndereco(VMSocio model)
        {
            try
            {
                var newModel = new SocioEndereco
                {
                    SocioId = model.Id,
                    Endereco = model.Endereco,
                    Numero = model.Numero,
                    Complemento = model.Complemento,
                    Bairro = model.Bairro,
                    Cidade = model.Cidade,
                    Estado = model.Estado,
                    CEP = !string.IsNullOrEmpty(model.CEP) ? model.CEP.Replace("-", string.Empty) : string.Empty,
                };

                _db.SocioEndereco.Add(newModel);
                _db.SaveChanges();

                var newSocioEnderecoId = newModel?.Id;

                return Ok(new
                {/*
                        _logger.LogInformation(
                        $"{lstModel} graus Fahrenheit = " +
                        $"{resultado.Celsius} graus Celsius = " +
                        $"{resultado.Kelvin} graus Kelvin");
                    return resultado;
                        */
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = model,
                });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";

                _logger.LogError(mensagemErro);

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = mensagemErro
                });
            }
        }
        public async Task<IActionResult> Create_SocioAniversario(VMSocio model)
        {
            try
            {
                var newModel = new SocioAniversario
                {
                    SocioId = model.Id,
                    Dia = !string.IsNullOrEmpty(model.DataAniversario) ? Convert.ToInt32(model.DataAniversario.Split("/")[0]) : null,
                    Mes = !string.IsNullOrEmpty(model.DataAniversario) ? Convert.ToInt32(model.DataAniversario.Split("/")[1]) : null,
                };

                _db.SocioAniversario.Add(newModel);
                _db.SaveChanges();

                var newSocioAniversarioId = newModel?.Id;

                return Ok(new
                {/*
                        _logger.LogInformation(
                        $"{lstModel} graus Fahrenheit = " +
                        $"{resultado.Celsius} graus Celsius = " +
                        $"{resultado.Kelvin} graus Kelvin");
                    return resultado;
                        */
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = model,
                });
            }
            catch (Exception ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";

                _logger.LogError(mensagemErro);

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = mensagemErro
                });
            }
        }

        #endregion
    }
}
