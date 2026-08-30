/**
 * Admin -> AceCadastrorvo
 */

'use strict';

//#region Declare

let var_Nome = 'Cadastro',
    var_Controller = '/Cadastro',
    var_ControllerCmb = '/HelperExtensions';

let var_ImgAlt = "ACECA",
    urlImgModal = "../img/logo/logo.png",
    urlImgModalIcon = "../img/logo/logo01.png",
    urlImgModaltext = "../img/logo/logo02.png";

var msg = 'O preenchimento &eacute; obrigat&oacute;rio';

let objVariante, strMarcaAcervo;

const combosLoaded = {};
let fasesCache = null;
let xhrMarcaFase = null;

// true quando a tela abriu via "Editar" na fila de Aprovação (ver fn_CarregarModoEdicao) -
// muda o texto do botão de salvar e a action de destino (Create vs Edit).
let isModoEdicao = false;

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`LIST ${var_Controller} - Todos os recursos terminaram o carregamento!`);

        // Lida ANTES de fn_Limpar() - fn_Limpar() já limpa "modoEdicao" da URL
        // (history.replaceState), então lendo depois nunca acharia o parâmetro.
        const emModoEdicao = new URLSearchParams(window.location.search).get('modoEdicao') === '1';

        // Os dados em si vêm do sessionStorage (não da URL - ver admin-cadastro-aprovacao.js
        // :: btn-editar - registro com descrição/imagens longas passa de 2000 caracteres,
        // arriscando estourar o limite de query string do IIS em produção). Lido e removido
        // aqui, antes de fn_Limpar(), pra não sobreviver a um F5/nova visita.
        const dadosEdicaoJson = emModoEdicao ? sessionStorage.getItem('cadastroDadosEdicao') : null;
        sessionStorage.removeItem('cadastroDadosEdicao');

        fn_Limpar();

        //// Combos
        fn_PopLoadCombos();
        fn_ChangeCombos();

        //// Modo edição (chegou via "Editar" na fila de Aprovação)
        if (dadosEdicaoJson) {
            // fn_Limpar() acima já fez o próprio show/hide (síncrono, rápido demais) - o
            // carregamento de verdade aqui é assíncrono (espera os combos ficarem prontos),
            // então mantém o loading visível até fn_CarregarModoEdicao terminar de vez
            // (ele mesmo dá o hide lá no fim, depois do último campo preenchido).
            $.busyLoadFull("show");

            try {
                fn_CarregarModoEdicao(JSON.parse(dadosEdicaoJson));
            } catch (erro) {
                console.error('Falha ao ler dados de edição do sessionStorage', erro);
                $.busyLoadFull("hide");
            }
        }

        //// TEXT INPUTS
        $('#txt_Nome').blur(function () {
            //console.log("tecla pressionada campo txt_Nome ::: ", e.key);

            fn_ProcessaNome();
        });

        $('#txt_Nome').on('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                $(this).trigger('blur');
            }
            //// Tab dispara o blur nativamente, sem necessidade de tratamento adicional aqui.
        });

        $('#txt_CodigoVariante').blur(function (e) {
            //console.log("tecla pressionada campo txt_CodigoVariante ::: ", e.key);

            fn_ProcessaCodigoVariante();
        });

        $('#txt_CodigoVariante').on('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                $(this).trigger('blur');
            }
            //// Tab dispara o blur nativamente, sem necessidade de tratamento adicional aqui.
        });

        //// FORM
        document.querySelector('#formPage').addEventListener('keydown', function (event) {
            if (event.key === 'Enter') {
                event.preventDefault();
            }
        });

        document.getElementById('btCadastrar').addEventListener('click', function (e) {
            //   this.closest('form').submit();
            fn_ModalSalvar(e);
        });

        document.getElementById('btCancelar').addEventListener('click', function (e) {
            // Mesmo motivo do sucesso do Create - reload completo arrisca derrubar o menu
            // pro sócio (ver TempData["isPerfil"]/Layout). fn_Limpar() já reseta o form.
            fn_Limpar();
        });
    })();
});

//#endregion

//#region Botoes
function fn_Limpar() {
    //console.log("fn_Limpar ::: ");

    $.busyLoadFull("show");

    $('#hdId').val('0');
    $('#txt_Nome').val('');

    $('#cmbPop_MarcaFase').prop('selectedIndex', 0).change();
    $('#txt_Codigo').val('');
    $('#txt_CodigoVariante').val('');

    $('#cmbPop_MarcaFinalidade').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaFabrica').prop('selectedIndex', 0).change();
    $('#txt_CodFabrica').val('');
    $('#cmbPop_MarcaDimensao').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaTipo').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaSubTipo').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaImpressora').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaQualidadeImagem').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaRaridade').prop('selectedIndex', 0).change();
    $('#txt_Descricao').val('');
    $('#txt_Observacao').val('');
    $('#txt_Valor').val('');
    $('#txt_Valor1PI').val('');
    $('#txt_Valor2PI').val('');

    $('#txt_ImgPrincipal').val('');
    $('#txt_ImgDetalhe').val('');
    $('#img_ImgPrincipal').attr('src', '');
    $('#img_ImgDetalhe').attr('src', '');

    $('.div_tem_pais_destino').hide();

    $('.div_acervo').hide();
    $('.div_fase').hide();
    $('.div_variante').hide();
    $('.div_variante_codigo').hide();
    $('.div_original_variante').hide();

    $('.div_codigo').hide();

    $('.div_dados').hide();
    $('.div_adicional').hide();
    $('.div_imagem').hide();
    $('.div_botoes').hide();

    // Sai do modo edição (se estava) e limpa a URL, senão um F5 reabriria a edição sozinho.
    isModoEdicao = false;
    fn_AtualizarTextoBotaoCadastrar();
    $('.card-header').first().text('Novo Item de Acervo');

    if (window.location.search.includes('modoEdicao')) {
        history.replaceState(null, '', window.location.pathname);
    }

    $.busyLoadFull("hide");
}

//#endregion

//#region MODAL



function fn_ModalCaracterInvalido(idCampo) {
    Swal.fire({
        title: 'Aten&ccedil;&atilde;o !!!',
        icon: 'warning',
        html: `Caracter Inv&aacute;lido no nome preenchido!`,
        imageUrl: `${urlImgModaltext}`,
        imageWidth: 400,
        imageAlt: `${var_ImgAlt}`,
        focusConfirm: false,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: {
            confirmButton: 'btn btn-primary waves-effect waves-light'
        },
    }).then((result) => {
        $(idCampo).val('').focus();
    })
}

function fn_ModalOpcaoInvalida() {
    Swal.fire({
        title: 'Aten&ccedil;&atilde;o !!!',
        html: `Op&ccedil;&atilde;o, Inv&aacute;lida <br><br> Selecione uma das op&ccedil;&otilde;es de filtros dispon&iacute;veis!`,
        imageUrl: `${urlImgModaltext}`,
        imageWidth: 400,
        imageAlt: `${var_ImgAlt}`,
        focusConfirm: false,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: {
            confirmButton: 'btn btn-primary waves-effect waves-light'
        },
    }).then((result) => {
        //fn_Limpar();
        //console.log("fn_ModalOpcaoInvalida result ::: ", result);
    })
}

// fnhelper_AlertErro é comum (helper-ui-common.js).

function fn_ModalGetObj() {
    //console.log("fn_ModalGetObj ::: ");

    const objFormData = {
        Id: $('#hdId').val(),
        IdMarcaFase: $('#hdMarcaFaseId').val(),
        IdMarcaFinalidade: $('#hdMarcaFinalidadeId').val(),
        IdMarcaFabrica: $('#hdMarcaFabricaId').val(),
        IdMarcaDimensao: $('#hdMarcaDimensaoId').val(),
        IdMarcaTipo: $('#hdMarcaTipoId').val(),
        IdMarcaSubTipo: $('#hdMarcaSubTipoId').val(),
        IdMarcaImpressora: $('#hdMarcaImpressoraId').val(),
        IdMarcaQualidadeImagem: $('#hdMarcaQualidadeImagemId').val(),
        IdMarcaRaridade: $('#hdMarcaRaridadeId').val(),

        MarcaAcervoId: $('#cmbPop_MarcaAcervo').find('option:selected').val(),
        MarcaFaseId: $('#cmbPop_MarcaFase').find('option:selected').val(),
        CodigoVariante: $('#txt_CodigoVariante').val().length > 0 ? $('#txt_CodigoVariante').val() : null,
        CodigoAceca: document.getElementById("div_Codigo").textContent,
        CodigoAcecaNew: document.getElementById("div_NovoCodigo").textContent,
        Nome: $('#txt_Nome').val().length > 0 ? $('#txt_Nome').val() : null,
        IncluidoPor: $('#cmbPop_IncluidoPor').find('option:selected').val() > 0 ? $('#cmbPop_IncluidoSocio').find('option:selected').text() : ($('#txt_IncluidoPor').val().length > 0 ? $('#txt_IncluidoPor').val() : null),
        IncluidoPorSocioId: $('#cmbPop_IncluidoSocio').find('option:selected').val(),
        MarcaFinalidadeId: $('#cmbPop_MarcaFinalidade').find('option:selected').val(),
        MarcaFabricaId: $('#cmbPop_MarcaFabrica').find('option:selected').val(),
        CodigoFabrica: $('#txt_CodFabrica').val().length > 0 ? $('#txt_CodFabrica').val() : null,
        MarcaDimensaoId: $('#cmbPop_MarcaDimensao').find('option:selected').val(),
        MarcaTipoId: $('#cmbPop_MarcaTipo').find('option:selected').val(),
        MarcaSubTipoId: $('#cmbPop_MarcaSubTipo').find('option:selected').val(),
        MarcaImpressoraId: $('#cmbPop_MarcaImpressora').find('option:selected').val(),
        MarcaQualidadeImagemId: $('#cmbPop_MarcaQualidadeImagem').find('option:selected').val(),
        MarcaRaridadeId: $('#cmbPop_MarcaRaridade').find('option:selected').val(),
        Descricao: $('#txt_Descricao').val().length > 0 ? $('#txt_Descricao').val() : null,
        Observacao: $('#txt_Observacao').val().length > 0 ? $('#txt_Observacao').val() : null,

        Valor: $('#txt_Valor').val().length > 0 ? $('#txt_Valor').val() : null,
        Valor1PI: $('#txt_Valor1PI').val().length > 0 ? $('#txt_Valor1PI').val() : null,
        Valor2PI: $('#txt_Valor2PI').val().length > 0 ? $('#txt_Valor2PI').val() : null,

        ImgPrincipal: $('#txt_ImgPrincipal').val().length > 0 ? $('#txt_ImgPrincipal').val() : null,
        ImgDetalhe: $('#txt_ImgDetalhe').val().length > 0 ? $('#txt_ImgDetalhe').val() : null,
    };

    //console.log("fn_ModalGetObj !", objFormData);

    return objFormData;
}

//#endregion

//#region COMBO

function loadCombo(id, url, includeTodas = true) {

    if (combosLoaded[id]) return;

    const $cmb = $(id);

    if ($cmb.find('option').length > 1) return;

    combosLoaded[id] = true;

    $cmb.prop('disabled', true);

    $.ajax({
        url: url,
        type: 'GET',
        cache: true,
        success: function (data) {

            let options = '<option value="-1">-- Selecionar --</option>';

            if (includeTodas)
                options += '<option value="0">Todas</option>';

            data.forEach(item => {
                options += `<option value="${item.value}">${item.text}</option>`;
            });

            $cmb.html(options).prop('disabled', false);

            $cmb.trigger('change.select2');
        },
        error: function () {
            combosLoaded[id] = false;
        }
    });
}

/*
loadCombo('#cmb_MarcaFase', `${var_ControllerCmb}/AsyncCmb_MarcaFase`);
loadCombo('#cmb_MarcaTipo', `${var_ControllerCmb}/AsyncCmb_MarcaTipo`);
loadCombo('#cmb_MarcaSubTipo', `${var_ControllerCmb}/AsyncCmb_MarcaSubTipo`);
*/

function fn_ChangeCombos() {

    $('#cmbPop_MarcaAcervo').on('change', function () {

        let idMarcaAcervo = Number($(this).find('option:selected').val());

        //console.log("cmbPop_MarcaAcervo change idMarcaAcervo ::: ", idMarcaAcervo);

        fn_CamposHide("cmbPop_MarcaAcervo");

        if (idMarcaAcervo > 0) {

            fn_MenuAcervo();

            fn_LoadCmb_MarcaFase();

            $('.div_fase').show()
        } else {
            $('.div_fase').hide();
        }
    });

    $('#cmbPop_MarcaFase').on('change', function () {

        let strNovoNomeParaCadastro = document.querySelector('#txt_Nome');
        let idMarcaAcervo = $('#cmbPop_MarcaAcervo').find('option:selected').val();
        let idMarcaFase = Number($(this).find('option:selected').val());
        let idMarcaVariante = $(this).find('option:selected').val();

        //console.log("cmb_MarcaFase change idMarcaFase ::: ", idMarcaFase);
        //console.log("cmb_MarcaFase change strNovoNomeParaCadastro ::: ", strNovoNomeParaCadastro);

        fn_CamposHide("cmbPop_MarcaFase");

        if (idMarcaFase > 0) {

            //console.log("cmb_MarcaFase change idMarcaVariante ::: ", idMarcaVariante);

            let txtFaseSel = $(this).find('option:selected').text();
            document.getElementById("spNovoCodigo").textContent = `Novo Código ACECA - ${txtFaseSel}`;
            document.getElementById("h6Titulo").textContent = `Dados do Item de Cadastro - ${txtFaseSel}`;

            if (idMarcaVariante > 0) {
                $('#cmbPop_MarcaVariante').prop('selectedIndex', 0).change();
            }

            $('.div_variante_codigo').hide();

            if (idMarcaFase === 29) { //Exportacao

                //console.log("cmb_MarcaFase change Exportacao ::: ", idMarcaFase);

                $('#cmbPop_MarcaExTemPaisDestino').prop('selectedIndex', 0).change();

                $('.div_variante').attr('style', 'display:none !important');
                
                $('.div_tem_pais_destino').attr('style', 'display:block !important');
            } else {

                //console.log("cmb_MarcaFase change ELSE ::: ", idMarcaFase);
                $('.div_variante').attr('style', 'display:block !important');
                $('.div_tem_pais_destino').attr('style', 'display:none !important');
            }
        }
    });

    $('#cmbPop_MarcaVariante').on('change', function () {

        let idMarcaVariante = $(this).find('option:selected').val();
        let idMarcaFase = $('#cmbPop_MarcaFase').find('option:selected').val();

        //console.log("cmbPop_MarcaVariante change idMarcaFase ::: ", idMarcaFase);

        fn_CamposHide("cmbPop_MarcaVariante");

        if (idMarcaFase > 0) {
            if (idMarcaVariante > 0) {
                //console.log("cmbPop_MarcaVariante change idMarcaVariante ::: ", idMarcaVariante);

                if (idMarcaFase == 29) {
                    $('.div_tem_pais_destino').attr('style', 'display:block !important');
                } else {
                    $('.div_variante_codigo').show();
                }
            }
            else {
                //console.log("cmbPop_MarcaVariante change idMarcaVariante ::: ", idMarcaVariante);
                if (idMarcaVariante == 0) {
                    $('.div_variante_codigo').hide();

                    fn_GetCodigoAceca();
                }
            }
        }
    });

    $('#cmbPop_MarcaExTemPaisDestino').on('change', function () {

        let idExPaisDestino = $(this).find('option:selected').val();
        //console.log("cmbPop_MarcaExTemPaisDestino change idExPaisDestino ::: ", idExPaisDestino);

        fn_CamposHide("cmbPop_MarcaExTemPaisDestino");

        if (idExPaisDestino >= 0) {
            $('#cmbPop_MarcaVariante').prop('selectedIndex', 0).change();
            $('.div_variante').attr('style', 'display:block !important');
        }else {
            $('.div_variante').attr('style', 'display:none !important');
        }
       
    });

    $('#cmbPop_IncluidoPor').on('change', function () {

        let idIncluidoPor = $(this).find('option:selected').val();

        //console.log("cmbPop_IncluidoPor change idIncluidoPor ::: ", idIncluidoPor);

        //fn_CamposHide("cmbPop_IncluidoPor");

        if (idIncluidoPor > 0) {
            $('.div_IncluidoSocio').show();
            $('.div_IncluidoNaoSocio').hide();
            $('#txt_IncluidoPor').hide();
            $('#txt_IncluidoPor').val('');
        }
        else {
            $('.div_IncluidoSocio').hide();
            $('.div_IncluidoNaoSocio').show();
            $('#txt_IncluidoPor').show();
            $('#txt_IncluidoPor').val('');
        }
    });

    $('#cmbPop_MarcaFabrica').on('change', function () {

        let idMarcaFabrica = $(this).find('option:selected').val();
        //console.log("cmb_MarcaFabrica change idMarcaFabrica ::: ", idMarcaFabrica);

        if (idMarcaFabrica > 0) {
        }
    });

    $('#cmbPop_MarcaTipo').on('change', function () {

        let idMarcaTipo = $(this).find('option:selected').val();
        //console.log("cmb_MarcaTipo change idMarcaTipo ::: ", idMarcaTipo);

        if ($('#cmbPop_MarcaSubTipo option').length <= 1 && idMarcaTipo > 0) {

            //Limpar Combo MarcaSubTipo
            document.querySelectorAll('#cmbPop_MarcaSubTipo option').forEach(option => option.remove());
            $("#cmbPop_MarcaSubTipo").append($("<option></option>").val(0).html("-- Selecionar --"));
            
            fn_LoadCmb_MarcaSubTipo(idMarcaTipo);
        }

        if (idMarcaTipo > 0) {

            //console.log("cmb_MarcaTipo change objVariante ::: ", objVariante);

            if (objVariante !== undefined && objVariante !== null) {
                let marcaSubTipoId = ((objVariante.marcaSubTipoId === undefined || objVariante.marcaSubTipoId === null || objVariante.marcaSubTipoId <= 0) ? '-1' : objVariante.marcaSubTipoId);
                let marcaSubTipoTxt = ((objVariante.marcaSubTipoId === undefined || objVariante.marcaSubTipoId === null || objVariante.marcaSubTipoId <= 0) ? '-1' : objVariante.marcaSubTipo.descricao);

                /*
                console.log("cmb_MarcaTipo change marcaSubTipoId ::: ", marcaSubTipoId);
                console.log("cmb_MarcaTipo change marcaSubTipoId ::: ", marcaSubTipoTxt);
                */

                $("#cmbPop_MarcaSubTipo").val(Number(marcaSubTipoId));
            }
        }
    });
}

function fn_PopLoadCombos() {

    //console.log("fn_PopLoadCombos  ::: ");
    fn_LoadCmb_MarcaAcervo();
    fn_LoadCmb_MarcaExPaisDestino();
    fn_LoadCmb_MarcaVariante();
    fn_LoadCmb_IncluidoSocio();
    fn_LoadCmb_MarcaFinalidade();
    fn_LoadCmb_MarcaFabrica();
    fn_LoadCmb_MarcaDimensao();
    fn_LoadCmb_MarcaTipo();
    fn_LoadCmb_MarcaSubTipo(0);
    fn_LoadCmb_MarcaImpressora();
    fn_LoadCmb_MarcaQualidadeImagem();
    fn_LoadCmb_MarcaRaridade();
}

function fn_LoadCmb_MarcaAcervo() {
    // console.log("fn_LoadCmb_MarcaAcervo ::: ");

    if ($('#cmbPop_MarcaAcervo option').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaAcervo`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaAcervo  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaAcervo  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaAcervo  result ::: ", result);
                        $("#cmbPop_MarcaAcervo").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaExPaisDestino() {
    //console.log("fn_LoadCmb_MarcaExPaisDestino ::: ");

    if ($('#cmbPop_MarcaExTemPaisDestino option').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_Variante`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaExPaisDestino  data ::: ", data);
                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaExPaisDestino  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaExPaisDestino  result ::: ", result);
                        $("#cmbPop_MarcaExTemPaisDestino").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaVariante() {
    //console.log("fn_LoadCmb_MarcaVariante ::: ");

    if ($('#cmbPop_MarcaVariante option').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_Variante`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaVariante  data ::: ", data);
                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaVariante  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaVariante  result ::: ", result);
                        $("#cmbPop_MarcaVariante").append($("<option></option>").val(result.value).html(result.text));
                        $("#cmbPop_IncluidoPor").append($("<option></option>").val(result.value).html(result.text));
                    });

                    // Quem não é Administracao é sempre o próprio incluidor - trava o
                    // toggle em "Sim" (valor 1 = ESimNao.Sim) e desabilita (ver
                    // fn_LoadCmb_IncluidoSocio, que trava o combo de sócio correspondente).
                    if (!isAdministracao) {
                        $('#cmbPop_IncluidoPor').val('1').trigger('change').prop('disabled', true);
                    }
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

function fn_LoadCmb_IncluidoSocio() {
    //console.log("fn_LoadCmb_IncluidoSocio ::: ");

    if ($('#cmbPop_IncluidoSocio option').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_Socio`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_IncluidoSocio  data ::: ", data);
                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_IncluidoSocio  result id ::: ", id);
                        //console.log("fn_LoadCmb_IncluidoSocio  result ::: ", result);
                        $("#cmbPop_IncluidoSocio").append($("<option></option>").val(result.value).html(result.text));
                    });

                    // Autocrédito obrigatório pra quem não é Administracao - não pode
                    // escolher outro sócio nem cair no texto livre (txt_IncluidoPor).
                    if (!isAdministracao) {
                        $('#cmbPop_IncluidoSocio').val(String(socioLogadoId)).trigger('change').prop('disabled', true);
                        $('#txt_IncluidoPor').val('').prop('disabled', true);
                    }
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaFase() {
    //console.log("fn_LoadCmb_MarcaFase ::: ");

    const $cmb = $('#cmbPop_MarcaFase');

    const montarOptions = function (data) {
        let options = '<option value="0">-- Selecionar --</option>';

        data.forEach(function (item) {
            options += `<option value="${item.value}">${strMarcaAcervo} - ${item.text}</option>`;
        });

        $cmb.html(options).prop('disabled', false);
    };

    // Nessa tela o combo traz TODAS as Fases (sem filtro por Acervo) - reaproveita
    // o resultado ja carregado (apenas reetiqueta com o Acervo atual), evitando nova ida ao servidor
    if (fasesCache !== null) {
        montarOptions(fasesCache);
        return;
    }

    // Cancela uma requisicao anterior ainda pendente
    if (xhrMarcaFase !== null) {
        xhrMarcaFase.abort();
    }

    $cmb.prop('disabled', true);

    xhrMarcaFase = $.ajax(
        {
            crossDomain: true,
            url: `${var_ControllerCmb}/AsyncCmb_MarcaFase`,
            type: 'GET',
            success: function (data) {
                //console.log("fn_LoadCmb_MarcaFase  data ::: ", data);

                fasesCache = data;
                montarOptions(data);
            },
            error: function (xhr, textStatus, errorThrown) {
                if (textStatus === 'abort') return;

                $cmb.prop('disabled', false);
                fnhelper_AlertErro(xhr, textStatus);
            },
        }
    );
}

function fn_LoadCmb_MarcaFinalidade() {
    //console.log("fn_LoadCmb_MarcaFinalidade ::: ");

    if ($('#cmbPop_MarcaFinalidade option').length <= 1) {

        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaFinalidade`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaFinalidade  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaFinalidade  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaFinalidade  result ::: ", result);
                        $("#cmbPop_MarcaFinalidade").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }

    //console.log("fn_LoadCmb_CinemaProgramacao ::: ");
}

function fn_LoadCmb_MarcaFabrica() {
    //console.log("fn_LoadCmb_MarcaFabrica ::: ");

    if ($('#cmbPop_MarcaFabrica option').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaFabrica`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaFabrica  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaFabrica  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaFabrica  result ::: ", result);
                        $("#cmbPop_MarcaFabrica").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaDimensao() {
    //console.log("fn_LoadCmb_MarcaDimensao ::: ");

    if ($('#cmbPop_MarcaDimensao option').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaDimensao`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaDimensao  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaDimensao  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaDimensao  result ::: ", result);
                        $("#cmbPop_MarcaDimensao").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaTipo() {
    //console.log("fn_LoadCmb_MarcaTipo ::: ");

    if ($('#cmbPop_MarcaTipo option').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaTipo`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaTipo  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaTipo  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaTipo  result ::: ", result);
                        $("#cmbPop_MarcaTipo").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaSubTipo(idMarcaTipo) {

    //console.log("fn_LoadCmb_MarcaSubTipo  idMarcaTipo ::: ", idMarcaTipo);

    let urlLoad = `${var_ControllerCmb}/AsyncCmb_MarcaSubTipo`; // idMarcaTipo > 0 ? `${var_ControllerCmb}/AsyncCmb_MarcaSubTipoByTipo` : `${var_ControllerCmb}/AsyncCmb_MarcaSubTipo`;

    if ($('#cmbPop_MarcaSubTipo option').length <= 1) {

        $.ajax(
            {
                crossDomain: true,
                url: urlLoad,
                type: 'GET',
                data: {
                    id: idMarcaTipo,
                },
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaSubTipo  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaSubTipo  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaSubTipo  result ::: ", result);
                        $("#cmbPop_MarcaSubTipo").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaImpressora() {
    //console.log("fn_LoadCmb_MarcaImpressora ::: ");

    if ($('#cmbPop_MarcaImpressora option').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaImpressora`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaImpressora  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaImpressora  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaImpressora  result ::: ", result);
                        $("#cmbPop_MarcaImpressora").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaQualidadeImagem() {
    //console.log("fn_LoadCmb_MarcaQualidadeImagem ::: ");

    if ($('#cmbPop_MarcaQualidadeImagem option').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaQualidadeImagem`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaQualidadeImagem  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaQualidadeImagem  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaQualidadeImagem  result ::: ", result);
                        $("#cmbPop_MarcaQualidadeImagem").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaRaridade() {
    //console.log("fn_LoadCmb_MarcaRaridade ::: ");

    if ($('#cmbPop_MarcaRaridade option').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaRaridade`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaRaridade  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaRaridade  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaRaridade  result ::: ", result);
                        $("#cmbPop_MarcaRaridade").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);
                },
            }
        );
    }
}

//#endregion

//#region FUNCOES

function fn_CamposHide(origem) {
    //console.log("fn_CamposHide origem ::: ", origem);

    $('.div_original_variante').hide();
    document.getElementById("p_NomePaiVariante").innerHTML = '';
    
    $('#txt_Codigo').val('');
    $('.div_codigo').hide();

    $('#txt_CodigoVariante').val('');
    $('.div_variante_codigo').hide();

    //Linha 01
    $('.div_dados').hide();

    $('.div_Incluido').hide();
    $('.div_IncluidoNaoSocio').hide();
    $('.div_IncluidoSocio').hide();
    $('#txt_IncluidoPor').val('');
    $('#txt_IncluidoPor').hide();

    //$('#cmbPop_MarcaFinalidade').prop('selectedIndex', 0).change();
    //$('#cmbPop_MarcaFabrica').prop('selectedIndex', 0).change();
    $('#txt_CodFabrica').val('');

    //Linha 02
    /*
    $('#cmbPop_MarcaDimensao').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaTipo').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaSubTipo').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaImpressora').prop('selectedIndex', 0).change();
    */

    //Linha 03
    //$('#cmbPop_MarcaQualidadeImagem').prop('selectedIndex', 0).change();
    //$('#cmbPop_MarcaRaridade').prop('selectedIndex', 0).change();

    //Linha 04
    $('#txt_Descricao').val('');

    //Linha 05 - Valores
    $('.div_adicional').hide();
    $('#txt_Valor').val('');
    $('#txt_Valor1PI').val('');
    $('#txt_Valor2PI').val('');
        
    //Linha 06 - Imagens
    $('.div_imagem').hide();
    $('.div_img_principal').hide();
    $('.div_img_detalhe').hide();

    $('#txt_ImgPrincipal').val('');
    $('#txt_ImgDetalhe').val('');

    //Linha 07 - Botoes
    $('.div_botoes').hide();

    //$('#txt_CodigoVariante').val('');
}

function fn_CamposShow(result) {
    //console.log("fn_CamposShow result ::: ", result);

    let idMarcaFase = $('#cmbPop_MarcaFase').find('option:selected').val();

    if ($('#txt_Nome').val() != '') {
        //console.log("fn_CamposShow idMarcaFase ::: ", idMarcaFase);
        if (idMarcaFase != 29) {// 29 Exportacao
            //console.log("fn_CamposShow NAO EXPORT ::: ", idMarcaFase);
            //$('.div_variante').show();
            //$('.div_tem_pais_destino').hide();
            //$('#cmbPop_MarcaExTemPaisDestino').prop('selectedIndex', 0).change();
            //$('.div_variante').attr('style', 'display:block !important');
            $('.div_tem_pais_destino').attr('style', 'display:none !important');
        } else {
            //console.log("fn_CamposShow ELSE EXPORT ::: ", idMarcaFase);
            //$('#cmbPop_MarcaVariante').prop('selectedIndex', 0).change();
            //$('.div_variante').attr('style', 'display:none !important');
            $('.div_tem_pais_destino').attr('style', 'display:block !important');
            //$('.div_variante').hide();
            //$('.div_tem_pais_destino').show();
        }
    } else {
        //console.log("fn_CamposShow idMarcaFase ::: ", idMarcaFase);
        
        if (idMarcaFase != 29) {// 29 Exportacao
            //$('.div_variante').show();
            //$('.div_tem_pais_destino').hide();
            $('.div_variante').attr('style', 'display:block !important');
            $('.div_tem_pais_destino').attr('style', 'display:none !important');
        } else {
            $('.div_variante').attr('style', 'display:none !important');
            $('.div_tem_pais_destino').attr('style', 'display:block !important');
            //$('.div_variante').hide();
            //$('.div_tem_pais_destino').show();
        }
    }

    //fn_CamposHide("fn_CamposShow");
    /*
    $('#txt_Codigo').css('color', '#FFFFFF');
    $('#txt_Codigo').css('background-color', '#47007b');
    $('#txt_Codigo').css('text-align', 'center');
    $('#txt_Codigo').css("font-weight", "500");
    $('#txt_Codigo').css("font-size", "0.9375rem");
    $('#txt_Codigo').val(result.dataNovoCodigo);
    */

    document.getElementById("div_Codigo").textContent = result.dataVelhoCodigo;
    document.getElementById("div_NovoCodigo").textContent = result.dataNovoCodigo;
    

    $('.div_codigo').show();
    $('.div_Incluido').show();
    $('.div_dados').show();
    $('.div_imagem').show();
    $('.div_botoes').show();

    // fn_CamposHide (disparado pelas trocas de Acervo/Fase/Variante até chegar aqui)
    // esconde .div_IncluidoSocio/.div_IncluidoNaoSocio incondicionalmente, e nada os
    // reexibia depois - o toggle #cmbPop_IncluidoPor ficava com o valor certo (ex.:
    // travado em "Sim" pra quem não é Administracao), mas o combo/texto correspondente
    // por baixo nunca aparecia. Reaplica a visibilidade com base no valor atual do toggle.
    if ($('#cmbPop_IncluidoPor').val() > 0) {
        $('.div_IncluidoSocio').show();
        $('.div_IncluidoNaoSocio').hide();
    } else {
        $('.div_IncluidoSocio').hide();
        $('.div_IncluidoNaoSocio').show();
    }
}

function fn_ChecaInicioNumero(strNovoNomeParaCadastro) {
    //console.log("default strNovoNomeParaCadastro match ::: ", strNovoNomeParaCadastro.value.match(/^\d/));

    fn_CamposShow();

    //verifica se iniciada com numero
    /*
    if (strNovoNomeParaCadastro.value.match(/^\d/) !== null) {
        fn_CamposShow();
    } else {        
        // console.log("Fases que as marcas iniciam com Numeros");

        const swalWithBootstrapButtons = Swal.mixin({
            customClass: {
                confirmButton: "btn btn-label-success waves-effect",
                cancelButton: "btn btn-label-danger waves-effect"
            },
            buttonsStyling: false
        });

        swalWithBootstrapButtons.fire({
            title: "Tem certeza?",
            icon: "warning",
            html: `<b> Essa fase n&atilde;o possui marcas iniciadas com letras. </b> <br><br> Deseja continuar continuar?`,
            showCancelButton: true,
            confirmButtonText: `<i class="ri-chat-delete-line"></i> &nbsp; Sim, confirmar!`,
            cancelButtonText: `<i class="ri-check-double-line"></i> &nbsp; N&atilde;o, cancelar!`,
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {

                fn_CamposShow();

            } else if (result.dismiss === Swal.DismissReason.cancel) {
                swalWithBootstrapButtons.fire({
                    title: "Cancelado",
                    icon: "info",
                    html: "Nenhuma a&ccedil;&atilde;o foi realizada !!",
                    focusConfirm: true,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-secondary waves-effect'
                    }
                }).then((result) => {
                    $('#txt_Nome').val();

                    $(this).prop('selectedIndex', 0).change();
                });
            }
        });

    
    }

    */
}

function fn_GetCodigoAceca() {
    let idMarcaFase = $('#cmbPop_MarcaFase').find('option:selected').val();
    let idMarcaAcervo = $('#cmbPop_MarcaAcervo').find('option:selected').val();

    //console.log("fn_GetCodigoAceca idMarcaFase ::: ", idMarcaFase);
    //console.log("fn_GetCodigoAceca idMarcaAcervo ::: ", idMarcaAcervo);

    let txtNome = $('#txt_Nome').val();
    let txtCodigoVariante = $('#txt_CodigoVariante').val();
    let bNovaVariante = $('#cmbPop_MarcaVariante').val() > 0 ? true : false;
    let bExTemPaisDestino = $('#cmbPop_MarcaExTemPaisDestino').val() > 0 ? true : false;
    /*
    console.log("fn_GetCodigoAceca txtNome ::: ", txtNome);
    console.log("fn_GetCodigoAceca txtCodigoVariante ::: ", txtCodigoVariante);
    console.log("fn_GetCodigoAceca bNovaVariante ::: ", bNovaVariante);
    console.log("fn_GetCodigoAceca bExTemPaisDestino ::: ", bExTemPaisDestino);
   */

    $.ajax(
        {
            crossDomain: true,
            url: `${var_Controller}/GetNovoCodigoAceca`,
            type: 'POST',
            data: {
                idMarcaAcervo: idMarcaAcervo,
                idFase: idMarcaFase,
                strNovoNomeParaCadastro: bNovaVariante ? txtCodigoVariante : txtNome,
                bvariante: bNovaVariante,
                bExTemPaisDestino: bExTemPaisDestino
            },
            success: function (result) {
                //console.log("fn_GetCodigoAceca  result ::: ", result);

                if (result.bResult) {
                    objVariante = bNovaVariante ? result.data : null;
                    //console.log("fn_GetCodigoAceca objVariante ::: ", objVariante);

                    if (objVariante?.incluidoPor !== undefined && objVariante?.incluidoPor !== null && objVariante?.incluidoPor !== '') {
                        Swal.fire({
                            title: 'ATENÇÃO !!',
                            icon: 'warning',
                            html: `N&atilde;o esqueça de selecionar quem está incluindo !!`,
                            focusConfirm: false,
                            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                            customClass: {
                                confirmButton: 'btn btn-label-success waves-effect'
                            },
                        });
                    }

                    fn_PreencheDadosExistentes(objVariante);
                    //
                    fn_CamposShow(result);
                } else {
                    //console.log("fn_GetCodigoAceca  result ::: ", result);

                    Swal.fire({
                        title: 'OPS!!',
                        icon: 'error',
                        html: `ERRO::: ${result.message} !!<br><br> <b>${result.data}</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    }).then((resultFalha) => {
                        //console.log("resultFalha  :: ", resultFalha);

                        fn_Limpar()
                    });

                    return false;
                }
            },
            error: function (xhr, textStatus, errorThrown) {
                fnhelper_AlertErro(xhr, textStatus);
            },
        }
    );
}

function fn_PreencheDadosExistentes(obj) {
    //console.log("fn_PreencheDadosExistentes obj::: ", obj);

    // Pop ID
    (document.querySelector('#hdId').value = (obj === null ? 0 : obj.id));
    (document.querySelector('#hdMarcaFaseId').value = (obj === null ? 0 : obj.marcaFaseId));
    (document.querySelector('#hdMarcaFinalidadeId').value = (obj === null ? 0 : obj.marcaFinalidadeId));
    (document.querySelector('#hdMarcaFabricaId').value = (obj === null ? 0 : obj.marcaFabricaId));
    (document.querySelector('#hdMarcaDimensaoId').value = (obj === null ? 0 : obj.marcaDimensaoId));
    (document.querySelector('#hdMarcaTipoId').value = (obj === null ? 0 : obj.marcaTipoId));
    (document.querySelector('#hdMarcaSubTipoId').value = (obj === null ? 0 : obj.marcaSubTipoId));
    (document.querySelector('#hdMarcaImpressoraId').value = (obj === null ? 0 : obj.marcaImpressoraId));
    (document.querySelector('#hdMarcaQualidadeImagemId').value = (obj === null ? 0 : obj.marcaQualidadeImagemId));

    // Pop Variante
        (document.querySelector('#txt_CodigoVariante').value = (obj === null ? '' : obj.codigoAceca));
        document.getElementById("p_NomePaiVariante").innerHTML = '';

        (document.getElementById("p_NomePaiVariante").innerHTML = (obj === null ? '' : `${obj.nome}<br><br>${obj.descricao}`));

        (obj === null ? $('.div_original_variante').hide() : $('.div_original_variante').show());

    // Pop Dados
        (document.querySelector('#txt_IncluidoPor').value = (obj === null ? '' : obj.incluidoPor));
        $("#cmbPop_MarcaFinalidade").val(obj === null ? '-1' : ((obj.marcaFinalidadeId === undefined || obj.marcaFinalidadeId === null || obj.marcaFinalidadeId <= 0) ? '-1' : obj.marcaFinalidadeId)).change();
        $("#cmbPop_MarcaFabrica").val(obj === null ? '-1' : ((obj.marcaFabricaId === undefined || obj.marcaFabricaId === null || obj.marcaFabricaId <= 0) ? '-1' : obj.marcaFabricaId)).change();
        (document.querySelector('#txt_CodFabrica').value = (obj === null ? '' : obj.codigoFabrica));
        $("#cmbPop_MarcaDimensao").val(obj === null ? '-1' : ((obj.marcaDimensaoId === undefined || obj.marcaDimensaoId === null || obj.marcaDimensaoId <= 0) ? '-1' : obj.marcaDimensaoId)).change();
        $("#cmbPop_MarcaTipo").val(obj === null ? '-1' : ((obj.marcaSubTipo.marcaTipoId === undefined || obj.marcaSubTipo.marcaTipoId === null || obj.marcaSubTipo.marcaTipoId <= 0) ? '-1' : obj.marcaSubTipo.marcaTipoId)).change();
        $("#cmbPop_MarcaSubTipo").val(obj === null ? '-1' : ((obj.marcaSubTipoId === undefined || obj.marcaSubTipoId === null || obj.marcaSubTipoId <= 0) ? '-1' : obj.marcaSubTipoId)).change();
        $("#cmbPop_MarcaImpressora").val(obj === null ? '-1' : ((obj.marcaImpressoraId === undefined || obj.marcaImpressoraId === null || obj.marcaImpressoraId <= 0) ? '-1' : obj.marcaImpressoraId)).change();
        $("#cmbPop_MarcaQualidadeImagem").val(obj === null ? '-1' : ((obj.marcaQualidadeImagemId === undefined || obj.marcaQualidadeImagemId === null || obj.marcaQualidadeImagemId <= 0) ? '-1' : obj.marcaQualidadeImagemId)).change();
        $("#cmbPop_MarcaRaridade").val(obj === null ? '-1' : ((obj.marcaRaridadeId === undefined || obj.marcaRaridadeId === null || obj.marcaRaridadeId <= 0) ? '-1' : obj.marcaRaridadeId)).change();

        (document.querySelector('#txt_Descricao').value = (obj === null ? '' : obj.descricao));

        //Pop Valores
        (obj === null ? $('.div_adicional').hide() : ((obj.valor !== null || obj.valor1PI !== null || obj.valor2PI !== null) ? $('.div_adicional').show() : $('.div_adicional').hide()));
        (document.querySelector('#txt_Valor').value = (obj === null ? '' : obj.valor));
        (document.querySelector('#txt_Valor1PI').value = (obj === null ? '' : obj.valor1PI));
        (document.querySelector('#txt_Valor2PI').value = (obj === null ? '' : obj.valor2PI));

        //Pop Arquivos
        (document.querySelector('#txt_ImgPrincipal').value = '');
        (document.querySelector('#txt_ImgDetalhe').value = '');
        //(obj === null || obj?.imgPrincipal === null) ? (document.querySelector('#txt_ImgPrincipal').value = '') : fnItem_PopImgPrincipal(obj);
        //(obj === null || obj?.imgDetalhe === null) ? (document.querySelector('#txt_ImgDetalhe').value = '') : fnItem_PopImgDetalhe(obj);
}

//#region Modo Edição (chegou via "Editar" na fila de Aprovação)

// Os combos são carregados via ajax de forma assíncrona (fn_PopLoadCombos) - só dá pra
// selecionar um valor depois que as <option> existirem de verdade no DOM. Espera cada
// seletor ter mais de 1 opção antes de rodar o callback (com um teto de ~4s de segurança).
function fn_AguardarCombosCarregados(seletores, callback, tentativas) {
    tentativas = tentativas || 0;

    const prontos = seletores.every(function (sel) { return $(sel).find('option').length > 1; });

    if (prontos || tentativas > 40) {
        callback();
        return;
    }

    setTimeout(function () { fn_AguardarCombosCarregados(seletores, callback, tentativas + 1); }, 100);
}

function fn_AtualizarTextoBotaoCadastrar() {
    let texto = 'Cadastrar';

    if (isModoEdicao) {
        texto = isAdministracao ? 'Salvar' : 'Enviar para Aprovação';
    }

    document.getElementById('btCadastrar').textContent = texto;
}

// dados vem da própria grid de Aprovação (FiltrarDadosAprovacao) - mesmas colunas
// PascalCase usadas lá (ver admin-cadastro-aprovacao.js :: btn-editar).
function fn_CarregarModoEdicao(dados) {
    if (!dados || !dados.Id) {
        $.busyLoadFull("hide");
        return;
    }

    isModoEdicao = true;

    fn_AguardarCombosCarregados([
        '#cmbPop_MarcaAcervo', '#cmbPop_MarcaFinalidade', '#cmbPop_MarcaFabrica',
        '#cmbPop_MarcaDimensao', '#cmbPop_MarcaTipo', '#cmbPop_MarcaSubTipo',
        '#cmbPop_MarcaImpressora', '#cmbPop_MarcaQualidadeImagem', '#cmbPop_MarcaRaridade'
    ], function () {
        $('#hdId').val(dados.Id);
        $('#hdMarcaFinalidadeId').val(dados.MarcaFinalidadeId || 0);
        $('#hdMarcaFabricaId').val(dados.MarcaFabricaId || 0);
        $('#hdMarcaDimensaoId').val(dados.MarcaDimensaoId || 0);
        $('#hdMarcaTipoId').val(dados.MarcaTipoId || 0);
        $('#hdMarcaSubTipoId').val(dados.MarcaSubTipoId || 0);
        $('#hdMarcaImpressoraId').val(dados.MarcaImpressoraId || 0);
        $('#hdMarcaQualidadeImagemId').val(dados.MarcaQualidadeImagemId || 0);

        $('#txt_Nome').val(dados.NomeMarca || '');

        $('#cmbPop_MarcaAcervo').val(dados.MarcaAcervoId > 0 ? dados.MarcaAcervoId : '-1').trigger('change');
        fn_MenuAcervo();

        // Fase só é populado como reação à troca de Acervo acima (fn_LoadCmb_MarcaFase) -
        // espera ficar pronto antes de selecionar o valor certo.
        fn_AguardarCombosCarregados(['#cmbPop_MarcaFase'], function () {
            $('#hdMarcaFaseId').val(dados.MarcaFaseId || 0);
            $('#cmbPop_MarcaFase').val(dados.MarcaFaseId > 0 ? dados.MarcaFaseId : '-1').trigger('change');

            document.getElementById('div_Codigo').textContent = dados.CodigoAceca || '';
            document.getElementById('div_NovoCodigo').textContent = dados.CodigoAcecaNew || '';
            $('.div_codigo').show();

            $('#cmbPop_MarcaFinalidade').val(dados.MarcaFinalidadeId > 0 ? dados.MarcaFinalidadeId : '-1').trigger('change');
            $('#cmbPop_MarcaFabrica').val(dados.MarcaFabricaId > 0 ? dados.MarcaFabricaId : '-1').trigger('change');
            $('#txt_CodFabrica').val(dados.CodigoFabrica || '');
            $('#cmbPop_MarcaDimensao').val(dados.MarcaDimensaoId > 0 ? dados.MarcaDimensaoId : '-1').trigger('change');
            $('#cmbPop_MarcaTipo').val(dados.MarcaTipoId > 0 ? dados.MarcaTipoId : '-1').trigger('change');
            $('#cmbPop_MarcaSubTipo').val(dados.MarcaSubTipoId > 0 ? dados.MarcaSubTipoId : '-1').trigger('change');
            $('#cmbPop_MarcaImpressora').val(dados.MarcaImpressoraId > 0 ? dados.MarcaImpressoraId : '-1').trigger('change');
            $('#cmbPop_MarcaQualidadeImagem').val(dados.MarcaQualidadeImagemId > 0 ? dados.MarcaQualidadeImagemId : '-1').trigger('change');
            $('#cmbPop_MarcaRaridade').val(dados.MarcaRaridadeId > 0 ? dados.MarcaRaridadeId : '-1').trigger('change');

            $('#txt_Descricao').val(dados.Descricao || '');
            $('#txt_Observacao').val(dados.Observacao || '');

            const temValoresAdicionais = dados.Valor || dados.Valor1PI || dados.Valor2PI;
            temValoresAdicionais ? $('.div_adicional').show() : $('.div_adicional').hide();
            $('#txt_Valor').val(dados.Valor || '');
            $('#txt_Valor1PI').val(dados.Valor1PI || '');
            $('#txt_Valor2PI').val(dados.Valor2PI || '');

            // Incluído por - mesma regra do combo Sim/Não usada no cadastro normal.
            const incluidoPorSocioIdLimpo = (dados.IncluidoPorSocioId || '').toString().replace(/,$/, '');

            if (incluidoPorSocioIdLimpo && parseInt(incluidoPorSocioIdLimpo) > 0) {
                $('#cmbPop_IncluidoPor').val('1').trigger('change');
                $('#cmbPop_IncluidoSocio').val(incluidoPorSocioIdLimpo).trigger('change');
            } else {
                $('#cmbPop_IncluidoPor').val('0').trigger('change');
                $('#txt_IncluidoPor').val(dados.IncluidoPor || '');
            }

            // Preview das imagens já enviadas - reenviar um arquivo novo é opcional na edição
            // (ver CadastroController.Edit, que mantém o arquivo atual se nada for anexado).
            // fn_PreviewImage (disparado ao escolher um arquivo novo) sempre dá .show() nos
            // divs - fn_Limpar() os deixa hidden por padrão, então precisa fazer o mesmo aqui.
            if (dados.ImgPrincipalFull) {
                const imgPrincipalEl = document.getElementById('img_ImgPrincipal');
                imgPrincipalEl.dataset.fallback = dados.ImgPrincipalFullLive || '';
                imgPrincipalEl.onerror = function () {
                    if (this.src !== this.dataset.fallback && this.dataset.fallback) {
                        this.src = this.dataset.fallback;
                    } else {
                        this.onerror = null;
                    }
                };
                imgPrincipalEl.src = dados.ImgPrincipalFull;
                $('.div_img_principal').show();
            }
            if (dados.ImgDetalheFull) {
                const imgDetalheEl = document.getElementById('img_ImgDetalhe');
                imgDetalheEl.dataset.fallback = dados.ImgDetalheFullLive || '';
                imgDetalheEl.onerror = function () {
                    if (this.src !== this.dataset.fallback && this.dataset.fallback) {
                        this.src = this.dataset.fallback;
                    } else {
                        this.onerror = null;
                    }
                };
                imgDetalheEl.src = dados.ImgDetalheFull;
                $('.div_img_detalhe').show();
            }

            $('.div_dados').show();
            $('.div_imagem').show();
            $('.div_botoes').show();

            $('.card-header').first().text('Editar Item de Acervo');

            fn_AtualizarTextoBotaoCadastrar();

            $.busyLoadFull("hide");

            // EStatusCadastro.Negado = 3 (Helper/HelperExtensionsController.cs) - avisa
            // pra conferir o motivo da recusa (Observação, já preenchida acima) antes de
            // corrigir e reenviar.
            if (parseInt(dados.StatusCadastro) === 3) {
                Swal.fire({
                    title: 'Cadastro negado',
                    icon: 'warning',
                    html: 'Este item foi negado. N&atilde;o se esque&ccedil;a de conferir o motivo no campo <b>Observa&ccedil;&atilde;o</b> antes de corrigir e reenviar.',
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-warning waves-effect'
                    }
                });
            }
        });
    });
}

//#endregion

function fn_MenuAcervo() {

    let idMarcaAcervo = $('#cmbPop_MarcaAcervo').find('option:selected').val();
    $('#hdMarcaAcervoId').val(idMarcaAcervo);
    //console.log("fn_MenuAcervo idMarcaAcervo :: ", idMarcaAcervo);

    switch (idMarcaAcervo) {
        case '1':
            strMarcaAcervo = `Geral`;
            break;
        case '2':
            strMarcaAcervo = `Amostras`;
            break;
        case '3':
            strMarcaAcervo = `Palheiros`;
            break;
        case '4':
            strMarcaAcervo = `Cigarrilhas`;
            break;
        case '5':
            strMarcaAcervo = `Charutos`;
            break;
        case '6':
            strMarcaAcervo = `Fumos & Diversos`;
            break;
        case '7':
            strMarcaAcervo = `Afins`;
            break;
        default:
            strMarcaAcervo = "";
    }

    $('#hdMarcaAcervoNome').val(strMarcaAcervo);

    //Titulo
    $('div.head-label-filtro').html(`<h5 class="card-title mb-0 title-filtro">${var_Nome} - ${strMarcaAcervo}</h5>`);
}

function fn_PreencheDadosDescricao() {
    console.log("fn_PreencheDadosDescricao :: ");

    let var_MarcaAcervo = $('#cmbPop_MarcaAcervo').find('option:selected').text();
    let var_MarcaFase = $('#cmbPop_MarcaFase').find('option:selected').text();
    let var_MarcaExTemPaisDestino = $('#cmbPop_MarcaExTemPaisDestino').find('option:selected').text();
    let var_MarcaVariante = $('#cmbPop_MarcaVariante').find('option:selected').text();
    let var_MarcaFinalidade = $('#cmbPop_MarcaFinalidade').find('option:selected').text();
    let var_MarcaFabrica = $('#cmbPop_MarcaFabrica').find('option:selected').text();
    let var_CodFabrica = $('#txt_CodFabrica').val();
    let var_MarcaDimensao = $('#cmbPop_MarcaDimensao').find('option:selected').text();
    let var_MarcaTipo = $('#cmbPop_MarcaTipo').find('option:selected').text();
    let var_MarcaSubTipo = $('#cmbPop_MarcaSubTipo').find('option:selected').text();
    let var_MarcaImpressora = $('#cmbPop_MarcaImpressora').find('option:selected').text();
    let var_MarcaQualidadeImagem = $('#cmbPop_MarcaQualidadeImagem').find('option:selected').text();
    let var_MarcaRaridade = $('#cmbPop_MarcaRaridade').find('option:selected').text();

    let strDescricao = `${var_MarcaFinalidade} - ${var_MarcaFabrica} - ${var_CodFabrica} - ${var_MarcaDimensao} - ${var_MarcaTipo} - ${var_MarcaSubTipo} - ${var_MarcaImpressora}`;
    console.log("fn_PreencheDadosDescricao strDescricao :: ", strDescricao);

    document.getElementById("txt_Descricao").value = strDescricao;
}

function fn_ValidarCodigoVariante(txtValue) {
    const regexCaracteresValidos = /^[\p{L}\p{N}](?:[\p{L}\p{N} _-]*[\p{L}\p{N}])?$/u;

    return regexCaracteresValidos.test(txtValue);
}

function fn_ProcessaNome() {
    let txtNome = $('#txt_Nome').val();
    //console.log("fn_ProcessaNome txtNome ::", txtNome);

    if (txtNome !== null && txtNome !== undefined && txtNome !== '') {
        if (!fn_ValidarCodigoVariante(txtNome)) {
            fn_ModalCaracterInvalido('#txt_Nome');
            return;
        }

        $('.div_codigo').hide();

        $('.div_dados').hide();
        $('.div_imagem').hide();
        $('.div_botoes').hide();
        $('.div_variante').hide();
        $('.div_tem_pais_destino').hide();
        $('.div_variante_codigo').hide();
        $('.div_original_variante').hide();

        $('#cmbPop_MarcaAcervo').prop('selectedIndex', 0).change();
        $('#cmbPop_MarcaFase').prop('selectedIndex', 0).change();

        $('.div_acervo').show();
    } else {
        fn_Limpar();
    }
}

function fn_ProcessaCodigoVariante() {
    let txtCodigoVariante = $('#txt_CodigoVariante').val();
    //console.log("fn_ProcessaCodigoVariante txtCodigoVariante ::", txtCodigoVariante);

    if (txtCodigoVariante !== null && txtCodigoVariante !== undefined && txtCodigoVariante !== '') {
        if (!fn_ValidarCodigoVariante(txtCodigoVariante)) {
            fn_ModalCaracterInvalido('#txt_CodigoVariante');
            return;
        }

        fn_GetCodigoAceca();
    } else {
        fn_Limpar();
    }
}

//#endregion

//#region CRUD

function fn_ModalSalvar(e) {
    //console.log("fn_FomSendData form ::", form);

    e.preventDefault();

    let formPage = document.forms.item(0);

    if (formPage === null || formPage === undefined) {
        $.busyLoadFull("hide");

        Swal.fire({
            title: 'OPS',
            html: 'Falha no carregamento do formul&aacute;rio da P&aacute;gina',
            icon: 'error',
            customClass: {
                confirmButton: 'btn btn-label-danger waves-effect'
            }
        });
    } else {

        let formObjData = fn_ModalGetObj();
        //console.log("fn_FomSendData formObjData ::", formObjData);

        let objFileImgPrincipal = {},
            objFileImgDetalhe = {};

        let fileImgPrincipal = $('#txt_ImgPrincipal').prop("files")[0];
        let fileImgDetalhe = $('#txt_ImgDetalhe').prop("files")[0];
        //console.log("fn_ModalGetObj fileImgPrincipal ::", fileImgPrincipal);
        //console.log("fn_ModalGetObj fileImgDetalhe ::", fileImgDetalhe);

        if (fileImgPrincipal !== undefined) {
            objFileImgPrincipal = {
                lastModified: fileImgPrincipal.lastModified,
                lastModifiedDate: fileImgPrincipal.lastModifiedDate,
                name: fileImgPrincipal.name,
                size: fileImgPrincipal.size,
                type: fileImgPrincipal.type,
                webkitRelativePath: fileImgPrincipal.webkitRelativePath,
            };
        }

        if (fileImgDetalhe !== undefined) {
            objFileImgDetalhe = {
                lastModified: fileImgDetalhe.lastModified,
                lastModifiedDate: fileImgDetalhe.lastModifiedDate,
                name: fileImgDetalhe.name,
                size: fileImgDetalhe.size,
                type: fileImgDetalhe.type,
                webkitRelativePath: fileImgDetalhe.webkitRelativePath,
            };
        }

        //console.log("fn_ModalGetObj objFileImgPrincipal ::", objFileImgPrincipal);
        //console.log("fn_ModalGetObj objFileImgDetalhe ::", objFileImgDetalhe);

        const formData = new FormData(document.forms.item(0));

        //formData.append('lstFile', fileImgPrincipal);
        //formData.append('lstFile', fileImgDetalhe);

        formData.append('strObjModel', JSON.stringify(formObjData));
        formData.append('iFileImgPrincipal', fileImgPrincipal);
        formData.append('iFileImgDetalhe', fileImgDetalhe);

        // Modo edição (hdId > 0, veio de "Editar" na fila de Aprovação) grava em cima do
        // cadastro existente; senão é um cadastro novo de verdade.
        const acaoSalvar = (parseInt($('#hdId').val()) || 0) > 0 ? 'Edit' : 'Create';

        $.ajax({
            url: `${var_Controller}/${acaoSalvar}`,
            type: 'POST',
            data: formData,
            cache: false,
            contentType: false,
            processData: false,
            success: function (result) {
                //console.log("result  :: ", result);

                $.busyLoadFull("hide");

                if (result.bResult === true && result.type === "OK") {

                    $.busyLoadFull("hide");

                    Swal.fire({
                        title: 'Dados Salvos!',
                        icon: 'success',
                        text: acaoSalvar === 'Edit' ? 'Cadastro atualizado com sucesso.' : 'Marca cadastrada com sucesso.',
                        customClass: {
                            confirmButton: 'btn btn-success waves-effect waves-light'
                        }
                    }).then((resultSucesso) => {
                        // Era window.location.reload() - um reload completo aqui reexecuta a
                        // escolha de Layout (_HorizontalLayout/_WithoutMenuLayout) baseada em
                        // TempData["isPerfil"], que só sobrevive request a request enquanto o
                        // menu (_HorizontalMenu.cshtml) é renderizado pra chamar TempData.Keep()
                        // - pra um sócio, isso podia fazer o menu sumir depois de cadastrar.
                        // fn_Limpar() já reseta o formulário pra um novo cadastro sem
                        // depender de outro request ao servidor.
                        fn_Limpar();
                        //console.log("resultSucesso  :: ", resultSucesso);
                    });

                    return true;

                } else {

                    Swal.fire({
                        title: 'OPS!!',
                        icon: 'error',
                        html: `Dados n&atilde;o podem ser Salvos !!<br><br> ERRO::: <b>` + result + `</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    }).then((resultFalha) => {
                        console.log("resultFalha  :: ", resultFalha);
                    });

                    return false;
                }
            },
            error: function (xhr, textStatus, errorThrown) {
                fnhelper_AlertErro(xhr, textStatus);
            },
        });
    }
}

//#endregion

//#region IMAGENS

function fn_PreviewImage(input) {
    // console.log("fn_PreviewImage input ::: ", input);
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function (e) {

            //console.log("fn_PreviewImage e ::: ", e);
            //console.log("fn_PreviewImage input id ::: ", input.id);
            if (input.id === 'txt_ImgPrincipal') {
                //document.getElementById('img_ImgPrincipal').src = e.target.result;
                document.getElementById('img_ImgPrincipal').src = e.target.result;
                $('.div_img_principal').show();
            } else {
                document.getElementById('img_ImgDetalhe').src = e.target.result;
                $('.div_img_detalhe').show();
            }
        };
        reader.readAsDataURL(input.files[0]); // Converts to Base64 string
    }
}

function fnItem_PopImgPrincipal(obj) {
    //console.log("fnItem_PopImgPrincipal obj !", obj);

    if (obj !== null) {
        let objFile = {},
            fileArq = obj?.imgPrincipal;

        const fileInput = document.querySelector('#txt_ImgPrincipal');

        if (fileArq !== undefined) {
            objFile = {
                NomeArquivo: fileArq.split('.')[0],
                Extensao: fileArq.split('.').pop(),
            };
        }
        // Create a new File object
        const arqFile = new File(['ARQUIVO'], `${objFile.NomeArquivo}.${objFile.Extensao}`, {
            type: `application/${objFile.Extensao}`,
            //type: 'text/plain',
            lastModified: new Date(),
        });

        // Now let's create a DataTransfer to get a FileList
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(arqFile);
        fileInput.files = dataTransfer.files;

        // Help Safari out
        if (fileInput.webkitEntries.length) {
            fileInput.dataset.file = `${dataTransfer.files[0].name}`;
        }
    }
}

function fnItem_PopImgDetalhe(obj) {
    //console.log("fnItem_PopImgDetalhe obj !", obj);

    if (obj !== null) {
        let objFile = {},
            fileArq = obj?.imgDetalhe;

        const fileInput = document.querySelector('#txt_ImgDetalhe');

        if (fileArq !== undefined) {
            objFile = {
                NomeArquivo: fileArq.split('.')[0],
                Extensao: fileArq.split('.').pop(),
            };
        }

        // Create a new File object
        const arqFile = new File(['ARQUIVO'], `${objFile.NomeArquivo}.${objFile.Extensao}`, {
            type: `application/${objFile.Extensao}`,
            //type: 'text/plain',
            lastModified: new Date(),
        });

        // Now let's create a DataTransfer to get a FileList
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(arqFile);
        fileInput.files = dataTransfer.files;

        // Help Safari out
        if (fileInput.webkitEntries.length) {
            fileInput.dataset.file = `${dataTransfer.files[0].name}`;
        }
    }
}

//#endregion
