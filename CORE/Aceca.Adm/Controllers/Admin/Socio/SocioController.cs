using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Aceca.Adm.Models;
using Aceca.Adm.VMModels;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers.Admin.Socio
{
    [Authorize(Roles = "Administracao")]
    public class SocioController : Controller
    {
        #region variaveis

        private readonly ILogger<SocioController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;
        private readonly HelperExtensionsController _helperController;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;
        //

        #endregion

        public SocioController(ILogger<SocioController> logger, AppDbContext db
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

        #region CRUD JS

        public ActionResult Index()
        {
            return View("~/Views/Admin/Socio/Socio.cshtml");
        }

        // Paginação no servidor (Dapper + LIMIT/OFFSET) — antes carregava todos os sócios
        // de uma vez, com 4 joins (ver auditoria de performance / piloto SocioLogAcesso).
        [HttpPost]
        public async Task<IActionResult> FiltrarDados([FromBody] Models.FilterDataGridSimples request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request inválido");

                // LEFT JOIN em aniversario/contato/endereco (não INNER): um sócio cujo
                // cadastro não completou todas as etapas (ex.: criado direto, ou um fluxo
                // que falhou/foi interrompido antes de gravar esses sub-registros) tem que
                // continuar aparecendo aqui pra alguém conseguir corrigir - com INNER JOIN
                // ele fica invisível na grid inteira, sem nenhum aviso, mesmo sem filtro de
                // busca. Ver SocioController.Edit, que agora sabe criar o sub-registro que
                // faltar em vez de tentar (e falhar) atualizar um que nunca existiu.
                var sqlFrom = new StringBuilder(@"
                FROM socios s
                LEFT JOIN socio_aniversario sa ON s.id = sa.SocioId
                LEFT JOIN socio_contato sc ON s.id = sc.SocioId
                LEFT JOIN socio_endereco se ON s.id = se.SocioId
                INNER JOIN socio_perfil sp ON s.socioPerfilId = sp.id
                LEFT JOIN socio_seguranca sg ON s.id = sg.SocioId
                WHERE 1=1
                ");

                var parameters = new DynamicParameters();

                if (request.SomenteAtivos)
                {
                    sqlFrom.Append(" AND s.ativo = true");
                }

                if (!string.IsNullOrWhiteSpace(request.Search?.Value))
                {
                    sqlFrom.Append(@"
                        AND (
                            s.nome LIKE @SearchLike
                            OR sc.email LIKE @SearchLike
                            OR se.cidade LIKE @SearchLike
                        )
                    ");
                    parameters.Add("@SearchLike", $"%{request.Search.Value.Trim()}%");
                }

                var totalSql = "SELECT COUNT(1) FROM socios";
                var filteredSql = "SELECT COUNT(1) " + sqlFrom;

                var dataSql = $@"
                    SELECT
                        s.id AS Id,
                        s.nome AS NomeSocio,
                        s.ativo AS SocioAtivo,
                        s.mostrarSite AS MostrarSite,
                        s.socioPerfilId AS SocioPerfilId,

                        sp.descricao AS SocioPerfilDescricao,

                        sc.id AS SocioContatoId,
                        sc.email AS Email,
                        sc.ddd AS Ddd,
                        sc.telefone AS Telefone,

                        se.id AS SocioEnderecoId,
                        se.cep AS Cep,
                        se.endereco AS Endereco,
                        se.numero AS Numero,
                        se.complemento AS Complemento,
                        se.bairro AS Bairro,
                        se.estado AS Estado,
                        se.cidade AS Cidade,

                        sa.id AS SocioAniversarioId,
                        sa.dia AS Dia,
                        sa.mes AS Mes,
                        sa.ano AS Ano,

                        sg.bloqueado AS Bloqueado,
                        sg.qtd_infracoes_print AS QtdInfracoesPrint

                    {sqlFrom}

                    ORDER BY s.nome
                    LIMIT @Limit OFFSET @Offset
                    ";

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

        [HttpPost]
        public async Task<IActionResult> GetFullById(int id)
        {
            if (id < 1)
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "GetFullById - Id deve ser maior que 0",
                    data = id
                });

            try
            {
                var result = from s in _db.Socio // Table 1
                             join sa in _db.SocioAniversario on s.Id equals sa.SocioId
                             join sc in _db.SocioContato on s.Id equals sc.SocioId
                             join se in _db.SocioEndereco on s.Id equals se.SocioId
                             join sf in _db.SocioFinanceiro on s.Id equals sf.SocioId
                             join sp in _db.SocioPerfil on s.SocioPerfilId equals sp.Id
                             where s.Id == id
                             select new
                             {
                                 Socio = s,
                                 SocioAniversario = sa,
                                 SocioContato = sc,
                                 SocioEndereco = se,
                                 SocioFinanceiro = sf,
                                 SocioPerfil = sp,
                             };


                var lstModel = await result.AsNoTracking().ToListAsync();

                if (lstModel.Count <= 0)
                {
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - VAZIO - lstResult",
                        message = "GetById - Model ID Invalido",
                        data = lstModel
                    });
                }
                return Ok(new
                {
                    bResult = true,
                    type = "OK",
                    message = "SUCESSO ::: ",
                    data = lstModel.FirstOrDefault(),
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VMSocio model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Cadastro toca 5 tabelas (Socio + Seguranca + Contato + Endereco +
                    // Aniversario) via SaveChanges separados; sem transação, uma falha no
                    // meio do caminho (ex.: telefone mal formatado) deixava um sócio com
                    // login já criado mas sem contato/endereço, e o cliente via só um erro
                    // genérico como se nada tivesse sido salvo. Dispose sem Commit reverte
                    // tudo automaticamente em qualquer "return" antecipado.
                    //
                    // A transação precisa ser aberta dentro da execution strategy (o MySql
                    // está com EnableRetryOnFailure) - abrir via BeginTransactionAsync direto
                    // dispara "does not support user-initiated transactions", pois o EF não
                    // sabe reexecutar uma transação manual em caso de retry.
                    var strategy = _db.Database.CreateExecutionStrategy();

                    // Tipo do delegate declarado explicitamente: com os vários "return
                    // BadRequest(...)/Ok(...)" abaixo (tipos concretos diferentes), o
                    // compilador não infere Task<IActionResult> sozinho e cai no overload
                    // Func<Task> (erro CS8031: lambda async não pode retornar valor).
                    Func<Task<IActionResult>> operation = async () =>
                    {
                        using var transaction = await _db.Database.BeginTransactionAsync();

                        #region Socio

                        if (string.IsNullOrEmpty(model.Nome))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = "Nome deve ser preenchido"
                            });

                        if (string.IsNullOrEmpty(model?.Email))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = "Email deve ser preenchido"

                            });

                        if (!_helperController.IsValidEmailUsingMailAddress(model?.Email?.Trim()?.ToLower()))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = "Formato de E-mail Inválido"
                            });

                        var newModel = new Models.Socio
                        {
                            SocioPerfilId = model.SocioPerfilId = model.SocioPerfilId > 0 ? model.SocioPerfilId : (int)EPerfil.Socio,
                            Nome = model.Nome,
                            ImgAvatar = !string.IsNullOrEmpty(model.ImgAvatar) ? model.ImgAvatar : null,
                            MostrarSite = model.MostrarSite != null ? model.MostrarSite : true,
                            Ativo = model.Ativo,
                        };

                        _db.Socio.Add(newModel);
                        _db.SaveChanges();

                        model.Id = newModel?.Id;

                        if (newModel?.Id <= 0)
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = "Falha ao Cadastrar Socio"
                            });

                        #endregion

                        #region SocioSeguranca

                        var resulCreateSocioSeguranca = await Create_SocioSeguranca(model);

                        if (resulCreateSocioSeguranca.GetType() == typeof(NotFoundObjectResult) ||
                                   resulCreateSocioSeguranca.GetType() == typeof(NotFoundResult) ||
                                   resulCreateSocioSeguranca.GetType() == typeof(BadRequestObjectResult) ||
                                   resulCreateSocioSeguranca.GetType() == typeof(BadRequestResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = ExtrairMensagemErro(resulCreateSocioSeguranca, "Falha ao Cadastrar Socio Seguranca"),
                                data = model
                            });

                        var objJsonResulCreateSocioSegurancaReturnApi = ((ObjectResult)resulCreateSocioSeguranca).Value;

                        var jObj = JObject.FromObject(objJsonResulCreateSocioSegurancaReturnApi);

                        var user = JsonConvert.DeserializeObject<SocioSeguranca>(jObj?.SelectToken("data")?.ToString());

                        #endregion

                        #region SocioContato

                        var resulCreateSocioContato = await Create_SocioContato(model);

                        if (resulCreateSocioContato.GetType() == typeof(NotFoundObjectResult) ||
                                   resulCreateSocioContato.GetType() == typeof(NotFoundResult) ||
                                   resulCreateSocioContato.GetType() == typeof(BadRequestObjectResult) ||
                                   resulCreateSocioContato.GetType() == typeof(BadRequestResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = ExtrairMensagemErro(resulCreateSocioContato, "Falha ao Cadastrar Socio Contato"),
                                data = model
                            });

                        var objJsonResulCreateSocioContatoReturnApi = ((ObjectResult)resulCreateSocioContato).Value;

                        #endregion

                        #region SocioEndereco

                        var resulCreateSocioEndereco = await Create_SocioEndereco(model);

                        if (resulCreateSocioEndereco.GetType() == typeof(NotFoundObjectResult) ||
                                   resulCreateSocioEndereco.GetType() == typeof(NotFoundResult) ||
                                   resulCreateSocioEndereco.GetType() == typeof(BadRequestObjectResult) ||
                                   resulCreateSocioEndereco.GetType() == typeof(BadRequestResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = ExtrairMensagemErro(resulCreateSocioEndereco, "Falha ao Cadastrar Socio Endereco"),
                                data = model
                            });

                        var objJsonResulCreateSocioEnderecoReturnApi = ((ObjectResult)resulCreateSocioEndereco).Value;

                        #endregion

                        #region SocioAniversario

                        var resulCreateSocioAniversario = await Create_SocioAniversario(model);

                        if (resulCreateSocioAniversario.GetType() == typeof(NotFoundObjectResult) ||
                                   resulCreateSocioAniversario.GetType() == typeof(NotFoundResult) ||
                                   resulCreateSocioAniversario.GetType() == typeof(BadRequestObjectResult) ||
                                   resulCreateSocioAniversario.GetType() == typeof(BadRequestResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = ExtrairMensagemErro(resulCreateSocioAniversario, "Falha ao Cadastrar Socio Aniversario"),
                                data = model
                            });

                        var objJsonResulCreateSocioAniversarioReturnApi = ((ObjectResult)resulCreateSocioAniversario).Value;

                        #endregion

                        #region  Envio de Email

                        var strToken = _helperController.GenerateSecuretToken();

                        var trackedUser = await _db.SocioSeguranca.FirstOrDefaultAsync(x => x.Id == user.Id);
                        if (trackedUser != null)
                        {
                            trackedUser.ResetPasswordToken = strToken;
                            trackedUser.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(24);
                            await _db.SaveChangesAsync();
                        }

                        // Monta link de reset
                        var resetLink = $"{_urlBaseApp}/Auth/NewRegistration?token={Uri.EscapeDataString(strToken)}&email={Uri.EscapeDataString(user.Email)}";

                        // Envia e-mail
                        var resultSendMail = await _helperController.EnviarEmailAsync(ETipoEmail.Cadastro, user.Email, model.Nome, resetLink);

                        if (resultSendMail.GetType() == typeof(NotFoundObjectResult) ||
                            resultSendMail.GetType() == typeof(BadRequestObjectResult))
                            return BadRequest(new
                            {
                                bResult = false,
                                type = "ERRO",
                                message = "Falha no envido do E-mail",
                                data = user.Email
                            });

                        #endregion

                        await transaction.CommitAsync();

                        return Ok(new
                        {
                            bResult = true,
                            type = "OK",
                            message = "E-mail enviado com sucesso",
                            data = model,
                        });
                    };

                    return await strategy.ExecuteAsync(operation);
                }

                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Model Inválida",
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VMSocio model, bool? bloqueado)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    #region Socio

                    if (string.IsNullOrEmpty(model.Nome))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Nome deve ser preenchido"
                        });

                    // Atualiza somente os campos editáveis nesta tela - marcar um objeto novo
                    // (só com os campos vindos do form) como EntityState.Modified regravava
                    // TODAS as colunas, inclusive DataCriacao (zerada para "agora" a cada edição,
                    // pois BaseModel inicializa essa propriedade com DateTime.Now por padrão).
                    var newModel = await _db.Socio.FirstOrDefaultAsync(x => x.Id == model.Id);

                    if (newModel is null)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar Socio"
                        });

                    newModel.SocioPerfilId = model.SocioPerfilId = model.SocioPerfilId > 0 ? model.SocioPerfilId : (int)EPerfil.Socio;
                    newModel.Nome = model.Nome;
                    newModel.ImgAvatar = model.ImgAvatar;
                    newModel.MostrarSite = model.MostrarSite != null ? model.MostrarSite : true;
                    newModel.Ativo = model.Ativo;

                    await _db.SaveChangesAsync();

                    model.Id = newModel?.Id;

                    #endregion

                    #region SocioSeguranca (Bloqueado)

                    // Mesmo campo/regra da tela Sócio > Segurança (SocioSegurancaController.Edit) -
                    // duplicado aqui pra não obrigar trocar de tela só pra bloquear/liberar.
                    // Ao liberar, zera o contador e o bloqueio temporário também, dando um
                    // recomeço limpo (senão a próxima tentativa já bloqueia de novo direto).
                    if (bloqueado.HasValue)
                    {
                        var seguranca = await _db.SocioSeguranca.FirstOrDefaultAsync(s => s.SocioId == model.Id);

                        if (seguranca != null)
                        {
                            seguranca.Bloqueado = bloqueado.Value;

                            if (!bloqueado.Value)
                            {
                                seguranca.QtdInfracoesPrint = 0;
                                seguranca.BloqueadoAte = null;
                            }

                            await _db.SaveChangesAsync();
                        }
                    }

                    #endregion

                    #region SocioContato

                    if (string.IsNullOrEmpty(model?.Email))
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Email deve ser preenchido"

                        });

                    // Sócio com cadastro incompleto (ver LEFT JOIN em FiltrarDados) não tem
                    // linha em socio_contato ainda - SocioContatoId chega 0/null nesse caso,
                    // e tentar "atualizar" um Id que não existe faz o EF lançar
                    // DbUpdateConcurrencyException (UPDATE afeta 0 linhas). Cria em vez de
                    // atualizar quando não há um Id de verdade.
                    var newModelSocioContato = new Models.SocioContato
                    {
                        Id = model?.SocioContatoId > 0 ? model.SocioContatoId : null,
                        SocioId = model?.Id,
                        DDI = model?.DDI != null ? model?.DDI : 55,
                        DDD = !string.IsNullOrEmpty(model?.Telefone) ? Convert.ToInt16(model?.Telefone?.Split(")")[0]?.Trim().Replace("(", string.Empty)) : null,
                        Telefone = !string.IsNullOrEmpty(model?.Telefone) ? Convert.ToInt64(model?.Telefone?.Split(")")[1]?.Trim()) : null,
                        Email = model?.Email,
                    };

                    if (model?.SocioContatoId > 0)
                        _db.Entry(newModelSocioContato).State = EntityState.Modified;
                    else
                        _db.SocioContato.Add(newModelSocioContato);

                    _db.SaveChanges();

                    model.SocioContatoId = newModelSocioContato?.Id;

                    if (newModelSocioContato?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar SocioContato"
                        });

                    #endregion

                    #region SocioEndereco

                    var newModelSocioEndereco = new Models.SocioEndereco
                    {
                        Id = model.SocioEnderecoId > 0 ? model.SocioEnderecoId : null,
                        SocioId = model.Id,
                        Endereco = model.Endereco,
                        Numero = model.Numero,
                        Complemento = model.Complemento,
                        Bairro = model.Bairro,
                        Cidade = model.Cidade,
                        Estado = model.Estado,
                        CEP = !string.IsNullOrEmpty(model.CEP) ? model.CEP.Replace("-", string.Empty) : string.Empty,
                    };

                    if (model.SocioEnderecoId > 0)
                        _db.Entry(newModelSocioEndereco).State = EntityState.Modified;
                    else
                        _db.SocioEndereco.Add(newModelSocioEndereco);

                    _db.SaveChanges();

                    model.SocioEnderecoId = newModelSocioEndereco?.Id;

                    if (newModelSocioEndereco?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar SocioEndereco"
                        });

                    #endregion

                    #region SocioAniversario

                    var dataAniversarioEdit = ParseDataAniversario(model.DataAniversario);

                    var newModelSocioAniversario = new Models.SocioAniversario
                    {
                        Id = model.SocioAniversarioId > 0 ? model.SocioAniversarioId : null,
                        SocioId = model.Id,
                        Dia = dataAniversarioEdit.Dia,
                        Mes = dataAniversarioEdit.Mes,
                        Ano = dataAniversarioEdit.Ano,
                    };

                    if (model.SocioAniversarioId > 0)
                        _db.Entry(newModelSocioAniversario).State = EntityState.Modified;
                    else
                        _db.SocioAniversario.Add(newModelSocioAniversario);

                    _db.SaveChanges();

                    model.SocioAniversarioId = newModelSocioAniversario?.Id;

                    if (newModelSocioAniversario?.Id <= 0)
                        return BadRequest(new
                        {
                            bResult = false,
                            type = "ERRO",
                            message = "Falha ao Atualizar SocioAniversario"
                        });

                    #endregion
                }

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

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id < 1)
            {
                return BadRequest(new
                {
                    bResult = false,
                    type = "ERRO",
                    message = "Id deve ser maior que 0"
                });
            }

            try
            {
                var model = await _db.Socio.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                if (model == null)
                    return Ok(new
                    {
                        bResult = true,
                        type = "ERRO - ID nao localizado",
                        message = "ID nao localizado",
                        data = id
                    });

                // Remove os registros relacionados explicitamente - sem FK cascade
                // configurada no banco, excluir só a linha de "socios" falhava por
                // violação de FK (ou deixava órfãos, se a constraint fosse permissiva).
                //
                // Transação aberta dentro da execution strategy (MySql com
                // EnableRetryOnFailure) - ver comentário equivalente em Create().
                var strategy = _db.Database.CreateExecutionStrategy();

                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();

                    _db.SocioContato.RemoveRange(_db.SocioContato.Where(x => x.SocioId == id));
                    _db.SocioEndereco.RemoveRange(_db.SocioEndereco.Where(x => x.SocioId == id));
                    _db.SocioAniversario.RemoveRange(_db.SocioAniversario.Where(x => x.SocioId == id));
                    _db.SocioFinanceiro.RemoveRange(_db.SocioFinanceiro.Where(x => x.SocioId == id));
                    _db.SocioSeguranca.RemoveRange(_db.SocioSeguranca.Where(x => x.SocioId == id));
                    _db.Socio.Remove(model);

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (IActionResult)Ok(new
                    {
                        bResult = true,
                        type = "OK",
                        message = "SUCESSO ::: ",
                        data = model,
                    });
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

        #region Socio Derivacao

        // Os métodos Create_SocioXXX abaixo devolvem BadRequest com a mensagem real do
        // erro (inclusive exceptions do banco) dentro de Value.message. Sem isso, o
        // Create() principal mostrava sempre um texto genérico tipo "Falha ao Cadastrar
        // Socio Contato", escondendo a causa real (ex.: violação de NOT NULL) do usuário.
        private static string ExtrairMensagemErro(IActionResult resultado, string mensagemPadrao)
        {
            if (resultado is ObjectResult objResult && objResult.Value != null)
            {
                var mensagem = JObject.FromObject(objResult.Value)?["message"]?.ToString();

                if (!string.IsNullOrEmpty(mensagem))
                    return mensagem;
            }

            return mensagemPadrao;
        }

        // Aceita "DD/MM/YYYY" (e tambem "DD/MM" para nao quebrar dados antigos sem ano).
        // TryParse em vez de Convert.ToInt32 direto no Split evita IndexOutOfRange/FormatException
        // quando o campo vem parcialmente preenchido.
        private static (int? Dia, int? Mes, int? Ano) ParseDataAniversario(string dataAniversario)
        {
            if (string.IsNullOrWhiteSpace(dataAniversario))
                return (null, null, null);

            var partes = dataAniversario.Split("/");

            int? dia = partes.Length > 0 && int.TryParse(partes[0].Trim(), out var d) ? d : null;
            int? mes = partes.Length > 1 && int.TryParse(partes[1].Trim(), out var m) ? m : null;
            int? ano = partes.Length > 2 && int.TryParse(partes[2].Trim(), out var a) ? a : null;

            return (dia, mes, ano);
        }

        private async Task<IActionResult> Create_SocioSeguranca(VMSocio model)
        {
            try
            {
                var newModel = new SocioSeguranca();

                string strTempPass = _helperController.GenerateStringPassword(8);

                    newModel = new SocioSeguranca
                    {
                        SocioId = (int)model.Id,
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
        private async Task<IActionResult> Create_SocioContato(VMSocio model)
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
                    // Coluna `ddd` e NOT NULL no banco (default 0) - manter null aqui
                    // quando o telefone vem vazio faz o INSERT falhar com "Column 'ddd'
                    // cannot be null".
                    DDD =  !string.IsNullOrEmpty(model.Telefone) ? Convert.ToInt16(model.Telefone.Split(")")[0].Replace("(", string.Empty)) : 0,
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
        private async Task<IActionResult> Create_SocioEndereco(VMSocio model)
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
        private async Task<IActionResult> Create_SocioAniversario(VMSocio model)
        {
            try
            {
                var dataAniversario = ParseDataAniversario(model.DataAniversario);

                var newModel = new SocioAniversario
                {
                    SocioId = model.Id,
                    Dia = dataAniversario.Dia,
                    Mes = dataAniversario.Mes,
                    Ano = dataAniversario.Ano,
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
