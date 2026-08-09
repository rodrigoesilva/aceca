/**
 * Pages -> Download
 */

'use strict';

//#region Declare

let isPerfil = document.getElementById('hdIsPerfil').value;

let var_Nome = 'Downloads',
    var_Controller = '/Download',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data,
    varResultFull,
    formValid, popAddNewItemEl;

let var_Filtrado = false,
    var_ImgAlt = "ACECA",
    urlImgModal = "../img/logo/logo.png",
    urlImgModalIcon = "../img/logo/logo01.png",
    urlImgModaltext = "../img/logo/logo02.png";

var msg = 'O preenchimento &eacute; obrigat&oacute;rio';

let modalMarca = document.getElementById('ModalMarca');

let idMarcaMes,idMarcaFase;

let param_Data, param_DataIni, param_DataIniStrSel, param_DataIniSel, param_DataIniMes, param_DataIniAno;

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`LIST ${var_Controller} - Todos os recursos terminaram o carregamento!`);

        // Form validation
        const formAddNewItem = document.getElementById('form-pop-add-new-item');
        formValid = fn_PopValidator(formAddNewItem);

        // Carrega grid somente após sessão garantida
        fn_AuthSession(() => fn_GridList(formValid));

    })();
});

//#endregion

//#region GRID
function fn_GridList(formValid) {
    //console.log("fn_GridList :::", formValid);

    var varLang_UrlTranslate = '/vendor/libs/datatables-bs5/i18n/pt-BR.json',

        varAjax_UrlController = `${var_Controller}/FiltrarDados`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeContent = 'application/json; charset=utf-8',

        varCol_Exportar = [2, 3, 4, 5],
        varCol_Ordenacao = [[2, 'asc']],

        varItems_QtdPorPage = 10,
        varItems_DivPage = [5, 10, 25, 50, 75, 100],
        varItems_Row = null,
        varItems_Id = 0;

    // List Table
    // --------------------------------------------------------------------

    if (varTbl_Obj.length) {

        $.busyLoadFull("show");

        $('.datatables-basic').DataTable().clear().destroy();

        varTbl_Data = varTbl_Obj.DataTable({
            serverSide: true,
            paging: true,
            scrollCollapse: true,
            ordering: true,
            destroy: true,

            ajax: {
                url: varAjax_UrlController,
                type: varAjax_TypeAction,
                contentType: varAjax_TypeContent,

                data: function (d) {
                    return JSON.stringify({
                        draw: d.draw,
                        start: d.start,
                        length: d.length,

                        search: d.search, // 🔥 OBRIGATÓRIO para server-side

                        somenteAtivos: document.getElementById('chkFilterAtivo')?.checked === true
                    });
                },

                beforeSend: function () {
                    $.busyLoadFull("show");
                },
                complete: function () {
                    $.busyLoadFull("hide");
                },

                dataSrc: function (result) {
                    //console.log("data result :: ", result)
                    varResultFull = result;

                    return result.data;
                }
            },

            columns: [
                // COLUNA - control (sempre visível — prioridade máxima)
                { data: null, defaultContent: '', className: 'control', orderable: false, width: '30px', responsivePriority: 1 },
                // COLUNA - imagem (some primeiro no mobile)
                {
                    data: 'imagem', className: 'text-center', responsivePriority: 10004,
                    render: function (data, type, full, row) {
                        //console.log("imagem data ::: ", data);
                        //console.log("extensao type ::: ", type);
                        //console.log("imagem varResultFull ::: ", varResultFull);
                        let imgDefault = "download.png";// varResultFull.imgDefault
                        let imgData = (data === null ? `${varResultFull.arqUrlBase}/img/${imgDefault}` : `${varResultFull.arqUrlBase}/img/${data}`);
                        //console.log("imagem imgData ::: ", imgData);

                        // sem loading="lazy": grid paginado (10/25 linhas), a imagem já está
                        // visível na página atual — lazy só atrasava o disparo do request
                        // até depois do grid inteiro estar montado. fetchpriority="low" evita
                        // competir com o XHR do DataTables/demais recursos da página.
                        return `<img name="myImg" decoding="async" fetchpriority="low" class="td-img cmyImg" alt="${full.titulo}" src="${imgData}">`;
                    }
                },
                // COLUNA - Tipo (2ª a aparecer no mobile)
                { data: 'downloadTipo', className: 'text-center', width: '90px', responsivePriority: 2 },
                // COLUNA - Titulo (3ª a aparecer no mobile)
                {
                    data: 'titulo', className: 'text-center text-nowrap', width: '120px', responsivePriority: 3,
                    render: function (data, type, full) {
                        if (!data || full.Id === 0 || type !== 'display') return '';

                        const nomeTitulo = data?.trim()?.split('-').join("<br>");

                        return nomeTitulo;
                    }
                },
                // COLUNA - descricao
                { data: 'descricao', className: 'text-start', responsivePriority: 10005 },
                // COLUNA - Formato
                {
                    data: 'extensao', className: 'text-center', responsivePriority: 10006,
                    render: function (data, type, full, row) {

                        //console.log("extensao data ::: ", data);
                        //console.log("extensao type ::: ", type);
                        //console.log("extensao full ::: ", full);

                        if (type === 'display') {

                            let iconClass,
                                iconData;

                            if (data !== undefined && data !== null) {

                                switch (data) {
                                    case "pdf":
                                        iconClass = "ri-file-pdf-2-line text-danger";
                                        break;
                                    case "xls":
                                    case "xlsx":
                                        iconClass = "ri-file-excel-2-line text-success";
                                        break;
                                    case "zip":
                                        iconClass = "ri-folder-zip-line text-warning";
                                        break;
                                    default:
                                        iconClass = "ri-file-2-fill";
                                        break;
                                }

                                //<span class="ms-4"><i class="tf-icons ri ri-folder-open-line ri-16px text-warning"></i></span>
                                iconData = '<span name="spExtensao" data-icon="' + data + '" class="tf-icons ri ' + iconClass + ' ri-22px"></span> ';

                                return iconData;
                            }
                        }

                        return data;
                    }
                },
                // COLUNA - incluidoPor (avatar)
                {
                    data: 'incluidoPor', className: 'text-center', responsivePriority: 10011,
                    render: function (data, type, full) {

                        if (!data || full.Id === 0 || type !== 'display') return '';
                        var ul = `<ul class="m-0 avatar-group d-flex align-items-center justify-content-center" style="list-style:none;">`;
                        var items = data.split('/').map(function (nome, i) {
                            //console.log("IncluidoPor nome ::: ", nome);
                            let pathAvatar = nome == "Aceca" ? `../img/avatars/socio/imgAvatarAceca` : `../img/avatars/${i}`;

                            return `<li class="avatar avatar-lg pull-up" data-bs-toggle="tooltip" data-bs-placement="top"
                                title="${nome}" style="z-index:1;">
                                <img src="${pathAvatar}.png" alt="Avatar" class="rounded-circle">
                            </li>`;
                        }).join('');
                        return ul + items + '</ul>';
                    }
                },
                // COLUNA - incluidoPor hidden (filtro)
                { targets: -2, data: 'incluidoPor', visible: false, responsivePriority: 99 },
                // COLUNA - Ações (sempre visível junto com control)
                {
                    data: 'id', targets: -1, searchable: false, orderable: false, responsivePriority: 4,

                    render: function (data, type, full) {

                        //console.log("Ações data ::: ", data);
                        //console.log("Ações type ::: ", type);
                        //console.log("Ações full ::: ", full);
                        //console.log("Ações varResultFull ::: ", varResultFull);

                        if (type !== 'display') return '';                        

                        let urlData = (data === null ? "#" : `${varResultFull.arqUrlBase}/${full?.downloadTipo?.toLowerCase()}/${full?.nome}.${full?.extensao}`);
                        //console.log("Ações urlData ::: ", urlData);
                        
                        //console.log("Ações nome ::: ", full?.nome);
                        //console.log("Ações extensao ::: ", full?.nome?.extensao);

                        return `<div class="d-inline-block text-nowrap">
                            <a href="${urlData}" target="_blank"
                                class="btn btn-sm btn-icon btn-text-secondary waves-effect rounded-pill text-body me-1">
                                <i class="ri-download-cloud-line ri-22px"></i>
                            </a>
                        </div>`;
                    }
                }
            ],

            order: [[1, 'asc']], // garante base na coluna CodigoAceca

            autoWidth: false,

            language: {
                url: varLang_UrlTranslate,
                paginate: {
                    next: '<i class="ri-arrow-right-s-line"></i>',
                    previous: '<i class="ri-arrow-left-s-line"></i>'
                }
            },
            buttons: [
                {
                    extend: 'collection',
                    className: 'btn btn-label-primary dropdown-toggle me-4 waves-effect waves-light border-none',
                    text: '<i class="ri-external-link-line me-sm-1"></i> <span class="d-none d-sm-inline-block">Exportar</span>',
                    buttons: [
                        // BOTAO CABECALHO - EXPORTAR - IMPRIMIR
                        {
                            extend: 'print',
                            text: '<i class="ri-printer-line me-1" ></i>Imprimir',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: varCol_Exportar,
                            }
                        },
                        // BOTAO CABECALHO - EXPORTAR - CSV
                        {
                            extend: 'csv',
                            text: '<i class="ri-file-text-line me-1" ></i>Csv',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: varCol_Exportar,
                            }
                        },
                        // BOTAO CABECALHO - EXPORTAR - EXCEL
                        {
                            // extend: 'excel',
                            extend: 'excelHtml5',
                            text: '<i class="ri-file-excel-line me-1"></i>Excel',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: varCol_Exportar,
                            }
                        },
                        // BOTAO CABECALHO - EXPORTAR - PDF
                        {
                            //extend: 'pdf',
                            extend: "pdfHtml5",
                            text: '<i class="ri-file-pdf-line me-1"></i>PDF',
                            className: 'dropdown-item',
                            orientation: 'landscape',
                            exportOptions: {
                                columns: varCol_Exportar,
                            },
                        },
                        // BOTAO CABECALHO - EXPORTAR - COPIAR
                        {
                            //extend: 'copy',
                            extend: 'copyHtml5',
                            text: '<i class="ri-file-copy-line me-1" ></i>Copiar',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: varCol_Exportar
                            }
                        }
                    ]
                },

                {
                    text: '<i class="ri-add-line"></i> <span class="d-none d-sm-inline-block">Adicionar Novo</span>',
                    className: 'btnAddNew create-new btn btn-primary waves-effect waves-light',
                    action: function (e, dt, node, config) {
                        //console.log("BTN NEW ::: ", dt);
                        window.location.href = '/Marca/Cadastro';
                    }
                }
            ],
            responsive: {
                details: {
                    type: 'column',
                    target: 0,
                    //target: 'tr',
                    renderer: function (api, rowIdx, columns) {
                        var row = api.row(rowIdx).data();

                        // ✅ Função de zoom — injeta modal no body na primeira vez
                        if (!document.getElementById('imgZoomModal')) {
                            $('body').append(`<div id="imgZoomModal" style="
                                                        display:none;position:fixed;inset:0;z-index:99999;
                                                        background:rgba(0,0,0,0.85);
                                                        align-items:center;justify-content:center;cursor:pointer;"
                                                        onclick="document.getElementById('imgZoomModal').style.display='none';">
                                                        <div style="position:relative;max-width:92vw;max-height:92vh;">
                                                            <img id="imgZoomTarget" src="" style="
                                                                max-width:92vw;max-height:92vh;
                                                                object-fit:contain;border-radius:8px;
                                                                box-shadow:0 8px 40px rgba(0,0,0,0.6);">
                                                            <button onclick="document.getElementById('imgZoomModal').style.display='none';" style="
                                                                position:absolute;top:-14px;right:-14px;
                                                                width:30px;height:30px;border-radius:50%;
                                                                background:#fff;border:none;cursor:pointer;
                                                                font-size:16px;line-height:1;color:#333;
                                                                display:flex;align-items:center;justify-content:center;
                                                                box-shadow:0 2px 8px rgba(0,0,0,0.3);">&#x2715;</button>
                                                        </div>
                                                    </div>
                                                `);

                            // Fecha com ESC
                            $(document).on('keydown.imgZoom', function (e) {
                                if (e.key === 'Escape') {
                                    $('#imgZoomModal').css('display', 'none');
                                }
                            });
                        }

                        // ✅ Função global de abertura do zoom
                        window.fn_ZoomImg = function (src) {
                            $('#imgZoomTarget').attr('src', src);
                            $('#imgZoomModal').css('display', 'flex');
                        };

                        // ✅ Imagens clicáveis com cursor pointer e chamada ao zoom
                        var imgPrincipal = row.ImgPrincipalFull
                            ? `<img name="myImg" class="td-img cmyImg" alt="${row.CodigoAceca}" src="${row.ImgPrincipalFull}"
                                        onclick="fn_ZoomImg('${row.ImgPrincipalFull}')" style="width:64px;height:64px;object-fit:cover;border-radius:8px; 
                                        border:0.5px solid #ddd;cursor:pointer;transition:opacity .2s;"
                                        onmouseover="this.style.opacity='.75'" onmouseout="this.style.opacity='1'">`
                            : `<div style="width:64px;height:64px;background:#f4f4f4;border-radius:8px; 
                                        display:flex;align-items:center;justify-content:center;
                                        font-size:11px;color:#aaa;">sem img</div>`;

                        var imgDetalhe = row.ImgDetalheFull
                            ? `<img name="myImg" class="td-img cmyImg" alt="${row.CodigoAceca}" src="${row.ImgDetalheFull}"
                                        onclick="fn_ZoomImg('${row.ImgDetalheFull}')" style="width:64px;height:64px;object-fit:cover;border-radius:8px;
                                        border:0.5px solid #ddd;cursor:pointer;transition:opacity .2s;"
                                        onmouseover="this.style.opacity='.75'" onmouseout="this.style.opacity='1'">`
                            : `<div style="width:64px;height:64px;background:#f4f4f4;border-radius:8px;
                                        display:flex;align-items:center;justify-content:center;
                                        font-size:11px;color:#aaa;">sem detalhe</div>`;

                        // Fábrica
                        var fabrica = (row.TxtFabrica === "" || row.TxtFabrica === null)
                            ? row.NomeFabrica
                            : row.TxtFabrica;

                        // Avatares incluídoPor
                        var avatarHtml = '';
                        if (row.IncluidoPor) {
                            var pessoas = row.IncluidoPor.split("/");
                            avatarHtml = pessoas.map(function (p, i) {
                                return `<img src="../img/avatars/${i}.png" alt="${p}" title="${p}"
                                            style="width:28px;height:28px;border-radius:50%;border:1.5px solid #fff;margin-right:2px;">`;
                            }).join('');
                        }

                        // ✅ incluidoPor como TEXTO PURO no card mobile
                        var incluidoPorTexto = '';

                        if (row.IncluidoPor) {
                            // Substitui "/" por separador legível
                            incluidoPorTexto = row.IncluidoPor.split('/').join(', ');
                        }

                        // Botão editar
                        var itemObjJson = encodeURIComponent(JSON.stringify(row));
                        var btnEditar = `<a href="javascript:fn_Modal(${itemObjJson},'Edit');"
                                                class="btn btn-sm btn-icon btn-text-secondary waves-effect rounded-pill text-body">
                                                <i class="ri-edit-box-line ri-22px"></i>
                                            </a>`;

                        var card = `<div style="background:#fff;border:0.5px solid #e0e0e0;border-radius:12px;padding:1rem;margin:6px 0;">

                                <!-- ✅ MUDANÇA 1 — Primeira linha: SOMENTE as duas imagens, centralizadas -->
                                <div style="display:flex;justify-content:center;gap:100px;padding-bottom:12px;border-bottom:0.5px solid #eee;margin-bottom:12px;">
                                    <div>
                                        <div style="font-size:13px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Imagem</div>
                                        <div style="font-size:11px;color:#aaa;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">${row.CodigoAceca || ''}</div>
                                        <div>${imgPrincipal || ''}</div>
                                    </div>
                                    <div>
                                        <div style="font-size:13px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:200;">Detalhe</div>
                                        <div style="font-size:11px;color:#aaa;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">${row.CodigoAceca || ''}</div>
                                        <div>${imgDetalhe || ''}</div>
                                    </div>
                                </div>

                    <!-- Grid de campos -->
                    <!-- ✅label em negrito (font-weight:600), valor em normal (font-weight:400) -->
                    <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;">
                        <div>
                            <div style="font-size:13px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Fase</div>
                            <div style="font-size:11px;color:#aaa;font-weight:400;">${row.NomeFase || ''}</div>
                        </div>
                        <div>
                            <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Código ACECA</div>
                            <div style="font-size:13px;color:#aaa;font-weight:400;">${row.CodigoAceca || ''}</div>
                        </div>
                        <div>
                            <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Finalidade</div>
                            <div style="font-size:13px;color:#aaa;font-weight:400;">${row.NomeFinalidade || ''}</div>
                        </div>                                            
                        <div>
                            <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">SubTipo</div>
                            <div style="font-size:13px;color:#aaa;font-weight:400;">${row.SubTipo || ''}</div>
                        </div>
                        <!--
                        <div>
                            <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Marca</div>
                            <div style="font-size:13px;color:#aaa;font-weight:400;">${row.NomeMarca || ''}</div>
                        </div>
                        -->
                        <div>
                            <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Fábrica</div>
                            <div style="font-size:13px;color:#aaa;font-weight:400;">${fabrica || ''}</div>
                        </div>
                        <div>
                            <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Incluído por</div>
                            <div style="font-size:13px;color:#aaa;font-weight:400;">${incluidoPorTexto || ''}</div>
                        </div>
                    </div>

                    <!-- Footer: incluidoPor em texto + botão editar -->
                    <div style="display:flex;align-items:center;justify-content:space-between;margin-top:10px;padding-top:10px;border-top:0.5px solid #eee;">
                        <div>
                            <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;margin-bottom:4px;">Marca</div>
                            <div style="font-size:13px;color:#aaa;">${row.NomeMarca}</div>
                        </div>
                    </div>

                    <!-- Descrição -->
                    <div style="margin-top:10px;padding-top:10px;border-top:0.5px solid #eee;">
                        <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;margin-bottom:4px;">Descrição</div>
                        <div style="font-size:12px;color:#aaa;line-height:1.5;font-weight:400;">${row.Descricao || ''}</div>
                    </div>

                
                </div>`;

                        return $(card);
                    }
                },
                breakpoints: [
                    { name: 'desktop', width: Infinity },
                    { name: 'tablet', width: 1024 },
                    { name: 'mobile', width: 768 }  // era 480 — aumentar dá mais espaço
                ]
            },
            initComplete: function (settings, json) {
                // console.log("settings ::: ", settings);
                //console.log("json ::: ", json);

                fn_GridComplete(this);
            }
        });

    }

    // VALIDA SUBMIT POP
    formValid.on('core.form.valid', function (e) {
        //console.log("e ::: ", e);

        var action = document.querySelector('.data-submit').textContent;
        //console.log("action ::: ", action);

        if (action === 'Alterar') {
            var objFormData = fn_PopGetObj();
            //console.log("objFormData ::: ", objFormData);

            fnItem_Edit(objFormData)
        } else {
            fnItem_Add(varTbl_Obj)
        }
    });
}

function fn_GridComplete(grid) {

    //console.log("fn_GridComplete ::: ", grid);

    var thisApi = grid.api();

    var countRows = grid.api().rows().count();
    //console.log("countRows ::: ", countRows);

    $('.card-header').after('<hr class="my-0">');

    //Titulo Tabela
    $('div.head-label').html(`<h5 class="card-title mb-0">${var_Nome}</h5>`);

    if (countRows > 0) {
        $.busyLoadFull("hide");

        Swal.fire({
            icon: 'success',
            title: 'Carregado!',
            html: `Dados carregados com sucesso.`,
            focusConfirm: true,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-success waves-effect'
            }
        }).then((result) => {

        });

    } else {
        Swal.fire({
            title: 'SEM DADOS!!',
            icon: 'info',
            html: `N&atilde;o h&aacute; dados para serem carregados, para o filtro selecionado!!`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-secondary waves-effect'
            },
        }).then((result) => {
            $('.card-header').after('<hr class="my-0">');

            //Titulo Tabela
            $('div.head-label').html(`<h5 class="card-title mb-0">${var_Nome}</h5>`);

            $(".card-datatable").show();
        });
    }
}

//#endregion

//#region MODAL

function fn_ModalErro(xhr, textStatus, errorThrown) {
    console.log("Server Response:", xhr.responseText);
    console.log("XMLHttpRequest  :: ", xhr);
    console.log("textStatus  :: ", textStatus);
    console.log("errorThrown  :: ", errorThrown);
    console.log("result  :: Error while posting SendResult");

    // Sempre esconde o loading e exibe o Swal, mesmo que a resposta de erro
    // nao seja um JSON valido (ex.: pagina de erro HTML, timeout, requisicao
    // abortada) - sem isso o JSON.parse podia estourar exception e travar o
    // busyLoadFull aberto para sempre, sem nenhuma mensagem para o usuario.
    $.busyLoadFull("hide");

    let mensagemErro = 'Ocorreu um erro inesperado, tente novamente.';

    try {
        const objError = JSON.parse(xhr.responseText);

        if (objError?.message)
            mensagemErro = objError.message;
    } catch (e) {
        console.log("Falha ao interpretar resposta de erro do servidor:", e);
    }

    Swal.fire({
        title: 'OPS!!',
        icon: 'error',
        html: `<b> Erro ocorrido <br><br>${mensagemErro}</b>`,
        focusConfirm: false,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: {
            confirmButton: 'btn btn-label-danger waves-effect'
        }
    });
}

//#endregion

//#region POP

function fn_Pop(obj, action) {
    //console.log("fn_Pop varItems_Row !", obj);
    //console.log("fn_Pop action !", action);

    const popAddNewItem = document.querySelector('#pop-add-new-item');

    popAddNewItemEl = new bootstrap.Offcanvas(popAddNewItem);

    // Pop ID
    (popAddNewItem.querySelector('#hdId').value = (obj === null ? 0 : obj.id)),
        (popAddNewItem.querySelector('#hdPaisCategoriaId').value = (obj === null ? 0 : obj.paisCategoriaId)),

        // Pop Dados
        (popAddNewItem.querySelector('.dt-line-01').value = (obj === null ? '' : obj.nome)),
        (popAddNewItem.querySelector('.dt-line-02').value = (obj === null ? '' : obj.descricao)),
        (popAddNewItem.querySelector('.dt-line-04').value = (obj === null ? '-1' : ((obj.paisCategoriaId === null || obj.paisCategoriaId === 0) ? '-1' : obj.paisCategoriaId)));
    (popAddNewItem.querySelector('.dt-line-05').checked = (obj === null ? false : obj.ativo));

    // Pop Action
    (popAddNewItem.querySelector('.offcanvas-title').textContent = (action === 'Edit') ? 'Alterar Registro' : 'Novo Registro');
    (popAddNewItem.querySelector('.data-submit').textContent = (action === 'Edit') ? 'Alterar' : 'Adicionar');

    if (obj !== null) {

        (obj.paisCategoriaId === null || obj.paisCategoriaId === 0) ? $("#cmb_PaisCategoria").val('-1').change() : $("#cmb_PaisCategoria").val(obj.paisCategoriaId).change();

        //console.log("fn_Pop ex val ::: ", $("#cmb_PaisCategoria").val());
    }

    // Open Pop
    popAddNewItemEl.show();
}

function fn_PopGetObj() {

    const objFormData = {
        Id: $('#hdId').val(),
        Nome: $('.form-add-new-item .dt-line-01').val(),
        Descricao: $('.form-add-new-item .dt-line-02').val(),
        PaisCategoriaId: $('#cmb_PaisCategoria').val(),
        Ativo: $('.form-add-new-item .dt-line-05').is(':checked')
    };

    //console.log("fn_PopGetObj !", objFormData);

    return objFormData;
}

function fn_PopValidator(formAddNewItem) {
    var varformValid = FormValidation.formValidation(formAddNewItem, {
        fields: {
            pop_line_item_01: {
                validators: {
                    notEmpty: {
                        message: 'O preenchimento &eacute; obrigat&oacute;rio'
                    }
                }
            }
        },
        plugins: {
            trigger: new FormValidation.plugins.Trigger(),
            bootstrap5: new FormValidation.plugins.Bootstrap5({
                // Use this for enabling/changing valid/invalid class
                // eleInvalidClass: '',
                eleValidClass: '',
                rowSelector: '.col-sm-12'
            }),
            submitButton: new FormValidation.plugins.SubmitButton(),
            // defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
            autoFocus: new FormValidation.plugins.AutoFocus()
        },
        init: instance => {
            instance.on('plugins.message.placed', function (e) {
                if (e.element.parentElement.classList.contains('input-group')) {
                    e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
                }
            });
        }
    });

    return varformValid;
}

function fnItem_Delete(varItems_Row) {

    //console.log("DELETE OBJ ::: ", varItems_Row);

    var varItems_Id = varItems_Row.id;

    var varAjax_UrlController = `${var_Controller}/Delete`, //'/TipoMidia/Delete',
        varAjax_TypeAction = 'DELETE',
        varAjax_TypeData = 'JSON';

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
        html: `<b> Essa a&ccedil;&atilde;o ir&aacute; excluir esse item </b> <br><br> voc&ecirc; n&atilde;o poder&aacute; reverter isso!`,
        showCancelButton: true,
        confirmButtonText: `<i class="ri-chat-delete-line"></i> &nbsp; Sim, confirmar!`,
        cancelButtonText: `<i class="ri-check-double-line"></i> &nbsp; N&atilde;o, cancelar!`,
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {

            $.busyLoadFull("show");

            $.ajax(
                {
                    type: varAjax_TypeAction,
                    //dataType: varAjax_TypeData,
                    url: varAjax_UrlController,
                    data: { id: varItems_Id },
                    success: function (result) {
                        //console.log("result  :: ", result);
                        //console.log("result bResult :: ", result.bResult);

                        var varTbl;

                        if ($.fn.dataTable.isDataTable('.datatables-basic')) {
                            //console.log("YES :: ");
                            varTbl = varTbl_Obj.DataTable();

                            $.busyLoadFull("hide");

                            Swal.fire({
                                title: 'Deletado!',
                                icon: 'success',
                                html: 'Item exclu&iacute;do com sucesso !!',
                                customClass: {
                                    confirmButton: 'btn btn-success waves-effect waves-light'
                                }
                            }).then((result) => {
                                varTbl.ajax.reload(null, false);
                            });
                        } else {
                            // console.log("NO :: ");
                            varTbl = $('#example').DataTable({
                                paging: false
                            });
                        }
                    },
                    error: function (XMLHttpRequest, textStatus, errorThrown) {
                        console.log("XMLHttpRequest  :: ", XMLHttpRequest);
                        console.log("textStatus  :: ", textStatus);
                        console.log("errorThrown  :: ", errorThrown);
                        console.log("result  :: Error while posting SendResult");

                        Swal.fire({
                            title: 'OPS!!',
                            icon: 'error',
                            html: `<b> Erro ocorrido <br><br>` + errorThrown.msg + `</b>`,
                            focusConfirm: false,
                            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                            customClass: {
                                confirmButton: 'btn btn-label-danger waves-effect'
                            }
                        });

                        return false;
                    }
                });

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
            });
        }
    });
}

function fnItem_Edit(varItems_Row) {
    //console.log("EDIT CLICK ::: ", varItems_Row);
    //var varPop_BtnAction = 'Edit';

    //fn_Pop(varItems_Row, varPop_BtnAction);

    var varAjax_UrlController = `${var_Controller}/Edit`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeData = 'JSON',
        varAjax_TypeContent = 'application/json; charset=utf-8';

    if (varItems_Row.Id === 0) {
        Swal.fire({
            title: 'OPS!!',
            icon: 'error',
            html: `Dados n&atilde;o identificados !!`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-danger waves-effect'
            },
        });
    } else {

        $.busyLoadFull("show");

        $.ajax(
            {
                url: varAjax_UrlController,
                type: varAjax_TypeAction,
                dataType: varAjax_TypeData,
                data: varItems_Row,
                // contentType: varAjax_TypeContent,
                success: function (result) {

                    //console.log("result  :: ", result);
                    //console.log("result bResult :: ", result.bResult);

                    if (result.bResult) {

                        $.busyLoadFull("hide");

                        var varTbl;

                        if ($.fn.dataTable.isDataTable('.datatables-basic')) {
                            //console.log("YES :: ");
                            varTbl = varTbl_Obj.DataTable();

                            // Hide offcanvas using javascript method
                            popAddNewItemEl.hide();

                            $.busyLoadFull("hide");

                            Swal.fire({
                                title: 'Dados Salvos!',
                                icon: 'success',
                                text: 'Item alterado com sucesso.',
                                customClass: {
                                    confirmButton: 'btn btn-success waves-effect waves-light'
                                }
                            }).then((result) => {
                                varTbl.ajax.reload(null, false);
                            });
                        } else {
                            // console.log("NO :: ");
                            varTbl = $('#example').DataTable({
                                paging: false
                            });
                        }
                    } else {
                        //console.log("result  :: ", result);
                        $.busyLoadFull("hide");

                        Swal.fire({
                            title: 'OPS!!',
                            icon: 'error',
                            html: `<b> Erro ocorrido <br><br>` + result + `</b>`,
                            focusConfirm: false,
                            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                            customClass: {
                                confirmButton: 'btn btn-label-danger waves-effect'
                            }
                        });
                    }
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    //console.log("XMLHttpRequest  :: ", XMLHttpRequest);
                    //console.log("textStatus  :: ", textStatus);
                    //console.log("errorThrown  :: ", errorThrown);
                    //console.log("result  :: Error while posting SendResult");

                    $.busyLoadFull("hide");

                    Swal.fire({
                        title: 'OPS!!',
                        icon: 'error',
                        html: `<b> Erro ocorrido <br><br>` + errorThrown.msg + `</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    });

                    return false;
                }
            });
    }
}

function fnItem_Add(varTbl_Obj) {
    //console.log("ADD CLICK ::: ", varTbl_Obj.row);

    var varPop_BtnAction = 'Create';

    var varAjax_UrlController = `${var_Controller}/Create`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeData = 'JSON',
        varAjax_TypeContent = 'application/json; charset=utf-8';

    const formData_newItem = fn_PopGetObj();

    if (formData_newItem != '') {

        $.busyLoadFull("show");

        $.ajax(
            {
                url: varAjax_UrlController,
                type: varAjax_TypeAction,
                dataType: varAjax_TypeData,
                data: formData_newItem,
                // contentType: varAjax_TypeContent,
                success: function (result) {

                    //console.log("result  :: ", result);
                    //console.log("result bResult :: ", result.bResult);

                    if (result.bResult) {

                        $.busyLoadFull("hide");

                        var varTbl;

                        if ($.fn.dataTable.isDataTable('.datatables-basic')) {
                            //console.log("YES :: ");
                            varTbl = varTbl_Obj.DataTable();

                            // Hide offcanvas using javascript method
                            popAddNewItemEl.hide();

                            $.busyLoadFull("hide");

                            Swal.fire({
                                title: 'Dados Salvos!',
                                icon: 'success',
                                text: 'Item adicionado com sucesso.',
                                customClass: {
                                    confirmButton: 'btn btn-success waves-effect waves-light'
                                }
                            }).then((result) => {
                                varTbl.ajax.reload(null, false);
                            });
                        } else {
                            // console.log("NO :: ");
                            varTbl = $('#example').DataTable({
                                paging: false
                            });
                        }
                    } else {
                        //console.log("result  :: ", result);
                        $.busyLoadFull("hide");

                        Swal.fire({
                            title: 'OPS!!',
                            icon: 'error',
                            html: `<b> Erro ocorrido <br><br>` + result + `</b>`,
                            focusConfirm: false,
                            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                            customClass: {
                                confirmButton: 'btn btn-label-danger waves-effect'
                            }
                        });
                    }
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    console.log("XMLHttpRequest  :: ", XMLHttpRequest);
                    console.log("textStatus  :: ", textStatus);
                    console.log("errorThrown  :: ", errorThrown);
                    console.log("result  :: Error while posting SendResult");

                    $.busyLoadFull("hide");

                    Swal.fire({
                        title: 'OPS!!',
                        icon: 'error',
                        html: `<b> Erro ocorrido <br><br>` + errorThrown.msg + `</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    });

                    return false;
                }
            });
    }
}

//#endregion