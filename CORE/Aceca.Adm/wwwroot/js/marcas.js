/**
 * App -> Agencia
 */

'use strict';

//#region Declare

let var_Nome = 'Marcas',
    var_Controller = '/Marcas',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data;

$.busyLoadSetup({
    animation: "slide",
    background: "rgba(133, 189, 61, 0.86)"
});

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`LIST ${var_Controller}- Todos os recursos terminaram o carregamento!`);

        fn_loadPage('home', document.querySelector('.nav-item[data-page="home"]'));

        fn_Auth();

        fn_Zoom();

    // Carrega Dados Grid
        fn_FiltrarDados();
    })();
});

//#endregion

/* ──────────────────────────────────────────────
   AUTH GUARD
────────────────────────────────────────────── */
function fn_Auth() {

    const sess = sessionStorage.getItem('aceca_sessao');

    console.log("fn_Auth sess ::: ", sess);

    if (!sess) {
        window.location.href = 'login.html'; return;
    }

    const u = JSON.parse(sess);

    document.getElementById('tbNome').textContent = u.nome || 'Usuário';
    document.getElementById('tbCargo').textContent = u.cargo || '—';
    document.getElementById('tbAvatar').textContent = (u.nome || 'U')[0].toUpperCase();

   //console.log("AUTH GUARD - sess :::", sess);
};

function logout() {
    console.log("logout sessionStorage ::: ", sessionStorage);

  sessionStorage.removeItem('aceca_sessao');
  window.location.href = 'login.html';
}

/* ──────────────────────────────────────────────
  GRID
────────────────────────────────────────────── */
//#region GRID

async function fn_FiltrarDados() {

    console.log("fn_FiltrarDados ::: ");

    $.busyLoadFull("show");

    varTbl_Obj.DataTable().clear().destroy();

    try {

        const response = (await fn_Api('GET', 'marcas'));//.slice(0, 5);

        //console.log("fn_FiltrarDados response :::", response);
        //console.log("fn_FiltrarDados response bResult :::", response.bResult);

        if (response.bResul === false) {

            console.log("fn_FiltrarDados IF :::", response);

            $.busyLoadFull("hide");

            Swal.fire({
                title: 'OPS!!',
                icon: 'error',
                html: `<b> Erro ocorrido <br><br>` + data.message + `</b>`,
                focusConfirm: false,
                confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                customClass: {
                    confirmButton: 'btn btn-label-danger waves-effect'
                }
            });
        } else {
            
            //console.log("fn_FiltrarDados ELSE :::", response);
            $.busyLoadFull("hide");

            //const data = await response.data.json();

            //console.log("fn_FiltrarDados data :::", response.data);

            fn_GridListFilter(response.data);

        }
    } catch (error) {

        $.busyLoadFull("hide");

        Swal.fire({
            title: 'OPS!!',
            icon: 'error',
            html: `<b> Erro ocorrido ao carregar marcas <br><br>` + error + `</b>`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-danger waves-effect'
            }

        });

        return null; // Return a fallback value
    }
}

//#region GRID
function fn_GridListFilter(lstData) {

    //console.log("fn_GridListFilter lstData ::: ", lstData);

    var varLang_UrlTranslate = 'https://cdn.datatables.net/plug-ins/1.12.1/i18n/pt-BR.json',

        varCol_Exportar = [2,3, 4, 5, 6, 7, 8, 9,],

        varCol_Ordenacao = [[1, 'asc']],

        varItems_QtdPorPage = 25,
        varItems_DivPage = [5, 10, 25, 50, 75, 100],
        varItems_Row = null,
        varItems_Id = 0;

    // List Table
    // --------------------------------------------------------------------

    var tblData = jQuery.map(lstData, function (el, i) {
        // console.log("data map el :: ", el);

        var data = [[

            el.id, // 0
            el.id, // 1
            el.nomeFase, // 2
            el.codigoAceca, // 3
            el.imagem, //4
            el.imagemDetalhe, //5
            el.nomeMarca, //6
            el.fabricaNome, //7
            el.descricao, //8
            el.incluidoPor, //9
            el.fabricaId, //10
        ]];
        // console.log("data map :: ", data);

        return data;
    });

    if (varTbl_Obj.length) {

       // console.log("fn_GridListFilter tblData ::: ", tblData);
       // console.log("fn_GridListFilter varTbl_Obj ::: ", varTbl_Obj);

        varTbl_Data = varTbl_Obj.DataTable({
            //responsive: true,
            //destroy: true,
            aaData: tblData,
            aoColumns: [
                // COLUNA - Responsive
                {
                    aTargets: [0],
                    className: 'control',
                    visible: false,
                    render: function (data, type, full, meta) {
                        //console.log("Responsive type ::: ", type);
                        return '';
                    }
                },
                // COLUNA - Botoes Acoes  // 'propostaCampanhaItemId',
                {
                    aTargets: [0],
                    orderable: false,
                    searchable: false,
                    visible: false,
                    render: function (data, type, full, meta) {

                        let btns = '';

                        //console.log("Acao data ::: ", data);
                        //console.log("Acao lstData ::: ", lstData);
                        //console.log("Acao full ::: ", full);
                        //console.log("Acao meta ::: ", meta);
                        if (type === 'display') {
                            let itemId = data;
                            let itemObjJson = encodeURIComponent(JSON.stringify(full));

                            btns =
                                '<div class="d-inline-block" data-pi="' + full[0] + '">' +
                                '<a href="javascript:;" class="btn btn-sm btn-text-secondary rounded-pill btn-icon dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ri-more-2-line ri-22px"></i></a>' +
                                '<ul class="dropdown-menu dropdown-menu-end m-0">' +
                                '<li><a href="javascript:fnItem_Pop(' + itemObjJson + ',' + "'Edit'" + ');" class="dropdown-item edit-record" data-pi="' + full[0] + '">Editar</a></li>' +
                                '<div class="dropdown-divider"></div>' +
                                '<li><a href="javascript:fnItem_PopInfoPi(' + itemObjJson + ',' + "'View'" + ');" class="dropdown-item edit-record">Visualizar PI</a></li>' +
                                '</ul>' +
                                '</div>'
                        }

                        return (btns);
                    }
                },
                // COLUNA - nomeFase
                {
                    aTargets: [2],
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        let id = full[0];

                        data = full[2];

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - codigoAceca
                {
                    aTargets: [3],
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        let id = full[0];

                        data = full[3];

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - imagem
                {
                    aTargets: [4],
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        let id = full[0];

                        data = full[4];

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {

                                let varImagem = full[4],
                                    varCodigoAceca = full[3];

                                return `<img name="myImg" class="td-img cmyImg" src="${varImagem}" alt="${varCodigoAceca}">`;
                            } else {
                                return '<div class="td-img-placeholder">📷</div>';
                            }
                        } else {
                            return '<div class="td-img-placeholder">📷</div>';
                        }
                    }
                },
                // COLUNA - imagemDetalhe
                {
                    aTargets: [5],
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        let id = full[0];

                        data = full[5];

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {

                                let varImagemmDetalhe = full[5],
                                    varCodigoAceca = full[3];

                                return `<img name="myImg" class="td-img cmyImg" src="${varImagemmDetalhe}" alt="${varCodigoAceca}">`;
                            } else {
                                return '<div class="td-img-placeholder">📷</div>';
                            }
                        } else {
                            return '<div class="td-img-placeholder">📷</div>';
                        }
                    }
                },
                // COLUNA - nomeMarca
                {
                    aTargets: [6],
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        let id = full[0];

                        data = full[6];

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - fabricaNome
                {
                    aTargets: [7],
                    className: "text-start",
                    ///sWidth: '25%'
                    render: function (data, type, full, meta) {
                        let id = full[0];

                        data = full[7];

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - descricao
                {
                    aTargets: [8],
                    className: "text-start",
                    //sWidth: '25%'
                    render: function (data, type, full, meta) {
                        let id = full[0];

                        data = full[8];

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - incluidoPor
                {
                    aTargets: [9],
                    className: "text-start",
                    //sWidth: '25%'
                    render: function (data, type, full, meta) {
                        let id = full[0];

                        data = full[9];

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
            ],
            order: varCol_Ordenacao,
            dom: '<"card-header flex-column flex-md-row"<"head-label text-center"><"dt-action-buttons text-end pt-3 pt-md-0"B>><"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6 d-flex justify-content-center justify-content-md-end"f>>t<"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
            displayLength: varItems_QtdPorPage,
            lengthMenu: varItems_DivPage,
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
            ],
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'Detalhes de ' + data['full_name'];
                        }
                    }),
                    type: 'column',
                    renderer: function (api, rowIdx, columns) {
                        var data = $.map(columns, function (col, i) {
                            return col.title !== '' // ? Do not show row in modal popup if title is blank (for check box)
                                ? '<tr data-dt-row="' +
                                col.rowIndex +
                                '" data-dt-column="' +
                                col.columnIndex +
                                '">' +
                                '<td>' +
                                col.title +
                                ':' +
                                '</td> ' +
                                '<td>' +
                                col.data +
                                '</td>' +
                                '</tr>'
                                : '';
                        }).join('');

                        return data ? $('<table class="table"/><tbody />').append(data) : false;
                    }
                }
            },
            error: function (obj, textstatus) {
                $.busyLoadFull("hide");
                Swal.fire({
                    title: 'OPS!!',
                    icon: 'error',
                    html: `<b>` + obj.msg + `</b>`,
                    focusConfirm: false,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-danger waves-effect'
                    }
                });
                //alert(obj.msg);
            },
            initComplete: function (settings, json) {
                //console.log("initComplete settings ::: ", settings);
                //console.log( "initComplete json ::: ", json);

                $.busyLoadFull("hide");

                fn_GridComplete(this);
            }
        });
    }
}

function fn_GridComplete(grid) {

    var thisApi = grid.api();

    var countRows = grid.api().rows().count();
    //console.log("countRows ::: ", countRows);

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
            $('.card-header').after('<hr class="my-0">');

            //Titulo Tabela
            $('div.head-label').html(`<h5 class="card-title mb-0">${var_Nome}</h5>`);

            $(".card-datatable").show();
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
/* ──────────────────────────────────────────────
   API WRAPPER
────────────────────────────────────────────── */
async function fn_Api(method, endpoint, body = null, isFormData = false) {
    //console.log("fn_Api method :::", method);
    //console.log("fn_Api endpoint :::", endpoint);

    const sess = JSON.parse(sessionStorage.getItem('aceca_sessao') || '{}');
    //console.log("fn_Api sess :::", sess);

    const headers = {};
    if (sess.token && sess.token !== 'demo')
        headers['Authorization'] = `Bearer ${sess.token}`;

    if (body && !isFormData) {
        //console.log("fn_Api body :::", body);

        headers['Content-Type'] = 'application/json';
        body = JSON.stringify(body);
    }

    const res = await fetch(`/api/${endpoint}`, {
        method, headers, body: body || undefined
    });

    //console.log("fn_Api res :::", res);

    if (!res.ok)
        throw new Error(await res.text());

    const ct = res.headers.get('Content-Type') || '';

    //console.log("fn_Api ct :::", ct);

    if (ct.includes('json'))
        return res.json();

    return {};
}

/* ──────────────────────────────────────────────
   PAGE ROUTER
────────────────────────────────────────────── */
function fn_loadPage(page, el) {
  //  console.log("loadPage - page :::", page);
  //  console.log("loadPage - el :::", el);

  document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
  if (el) el.classList.add('active');
  const c = document.getElementById('pageContent');
    c.innerHTML = '<div style="padding:40px;text-align:center;color:#7a5c9a;">⏳ Carregando…</div>';

    fn_RenderHome();
}

/* ──────────────────────────────────────────────
   HOME DASHBOARD
────────────────────────────────────────────── */
function fn_RenderHome() {
 //   console.log("renderHome :::");
  document.getElementById('pageContent').innerHTML = `
  <div class="page-header">
    <div>
      <h1><span class="ph-icon">🏠</span> Dashboard</h1>
      <p>Bem-vindo ao painel administrativo da ACECA</p>
    </div>
  </div>
  <div class="stats-grid">
    <div class="stat-card"><div class="stat-icon">👥</div><div class="stat-info"><h3 id="stSocios">—</h3><p>Sócios Ativos</p></div></div>
    <div class="stat-card"><div class="stat-icon">⭐</div><div class="stat-info"><h3 id="stMarcas">—</h3><p>Marcas Cadastradas</p></div></div>
    <div class="stat-card"><div class="stat-icon">🏭</div><div class="stat-info"><h3 id="stFabricas">—</h3><p>Fábricas</p></div></div>
    <div class="stat-card"><div class="stat-icon">🌍</div><div class="stat-info"><h3 id="stPaises">—</h3><p>Países</p></div></div>
    <div class="stat-card"><div class="stat-icon">📅</div><div class="stat-info"><h3 id="stAgenda">—</h3><p>Eventos</p></div></div>
    <div class="stat-card"><div class="stat-icon">📋</div><div class="stat-info"><h3 id="stTipos">—</h3><p>Tipos</p></div></div>
  </div>
  <div class="table-wrap" style="margin-top:8px">
    <div style="padding:16px 18px;border-bottom:1px solid rgba(163,43,255,.1)">
      <h3 style="font-size:.95rem;font-weight:700;color:#1a0030">⭐ Últimas Marcas Cadastradas</h3>
    </div>
    <div id="recentMarcas"></div>
  </div>`;
  //fetchStats();
  //fn_FetchRecentMarcas();
}

async function fetchStats() {
   // console.log("fetchStats :::");

  const endpoints = [
    ['stSocios','socios'],['stMarcas','marcas'],['stFabricas','fabricas'],
    ['stPaises','paises'],['stAgenda','agendas'],['stTipos','tipos']
  ];
  for (const [id,ep] of endpoints) {
      try {
          console.log("fetchStats ep :::", ep);
          console.log("fetchStats id :::", id);
          const r = await fn_Api('GET', ep);

          console.log("fetchStats r :::", r);
        document.getElementById(id).textContent = Array.isArray(r) ? r.length : '—';
      }
      catch {
          console.log("fetchStats catch :::", id);
          document.getElementById(id).textContent = '0';
      }
  }
}

async function fn_FetchRecentMarcas() {
    console.log("fetchRecentMarcas :::");

    const el = document.getElementById('recentMarcas');


    try
    {
        const rows = (await fn_Api('GET', 'marcas'));//.slice(0, 5);

        console.log("fetchRecentMarcas rows :::", rows);

        if (!rows.length) {
            el.innerHTML = emptyState('Nenhuma marca cadastrada ainda.');
            return;
        }

        console.log("fetchRecentMarcas rows :::", rows);

        new DataTable('#example', {
            ajax: rows,
            columns: [
                { data: 'id' },
                { data: 'nomeFase' },
                { data: 'imagem' },
                { data: 'imagemDetalhe' },
                { data: 'nomeMarca' },
                { data: 'fabricaNome' },
                { data: 'descricao' },
                { data: 'incluidoPor' },
            ]
        });
          /*
        el.innerHTML =
            `<table id="example" class="display data-table">
                <thead>
                    <tr>
                        <th>Id</th>
                        <th>Fase</th>
                        <th>Cód. ACECA</th>
                        <th>Imagem</th>
                        <th>Detalhe</th>
                        <th>Nome</th>
                        <th>Fábrica</th>
                        <th>Descrição</th>
                        <th>Incluído por</th>
                    </tr>
                </thead>                
            </table>`;
 */
      
        el.innerHTML = 
            `<table class="data-table">
                <thead>
                    <tr>
                        <th>Id</th>
                        <th>Fase</th>
                        <th>Cód. ACECA</th>
                        <th>Imagem</th>
                        <th>Detalhe</th>
                        <th>Nome</th>
                        <th>Fábrica</th>
                        <th>Descrição</th>
                        <th>Incluído por</th>
                    </tr>
                </thead>
                <tbody>
                    ${
                        rows.map(m =>
                            `<tr>
                                <td>${m.id || '—'}</td>
                                <td>${m.nomeFase || '—'}</td>
                                <td><strong>${m.codigoAceca || '—'}</strong></td>
                                <td>${m.imagem ? `<img name="myImg" class="td-img cmyImg" src="${m.imagem}" alt="${m.codigoAceca}">` : '<div class="td-img-placeholder">📷</div>'}</td>
                                <td>${m.imagemDetalhe ? `<img name="myImg" class="td-img cmyImg" src="${m.imagemDetalhe}" alt="${m.codigoAceca}">` : '<div class="td-img-placeholder">📷</div>'}</td>
                                <td>${m.nomeMarca ||'—'}</td>
                                <td>${m.fabricaNome || '—'}</td>
                                <td>${m.descricao || '—'}</td>
                                <td>${m.incluidoPor || '—'}</td>
                            </tr>`
                        ).join('')
                    }
                </tbody>
            </table>`;

           
  }
  catch {
      console.log("fetchRecentMarcas catch :::", el);
      el.innerHTML = emptyState('Erro ao carregar marcas.');
  }
}


/* ──────────────────────────────────────────────
   MARCAS PAGE (custom, with image uploads)
────────────────────────────────────────────── */
let marcasAll=[], marcasFiltered=[], marcasPage=1;

function renderMarcas() {
  document.getElementById('pageContent').innerHTML = `
  <div class="page-header">
    <div>
      <h1><span class="ph-icon">⭐</span> Marcas</h1>
      <p>Gerencie as marcas cadastradas no acervo</p>
    </div>
    <button class="btn btn-primary" onclick="openMarcaForm(null)">+ Nova Marca</button>
  </div>
  <div class="grid-toolbar">
    <div class="search-box">
      <span>🔍</span>
      <input type="text" placeholder="Buscar por nome, fábrica, fase…" oninput="filterMarcas(this.value)">
    </div>
    <span id="marcasCount" style="font-size:.78rem;color:var(--muted)"></span>
  </div>
  <div id="marcasTable" class="table-wrap"></div>`;
  loadMarcas();
}

async function loadMarcas() {
  const tw = document.getElementById('marcasTable');
  tw.innerHTML = '<div style="padding:32px;text-align:center;color:#7a5c9a">⏳ Carregando…</div>';
  try {
    marcasAll = await fn_Api('GET','marcas');
    marcasFiltered = [...marcasAll];
    renderMarcasTable();
  } catch {
    marcasAll = getMarcasDemo();
    marcasFiltered = [...marcasAll];
    renderMarcasTable();
  }
}

function filterMarcas(q) {
  const s = q.toLowerCase();
  marcasFiltered = marcasAll.filter(m =>
    [m.nome, m.fabricaNome, m.faseNome, m.codigoAceca, m.descricao, m.incluídoPor]
    .some(v => String(v||'').toLowerCase().includes(s))
  );
  marcasPage = 1;
  renderMarcasTable();
}

function renderMarcasTable() {
  const tw = document.getElementById('marcasTable');
  const rc = document.getElementById('marcasCount');
  if (rc) rc.textContent = `${marcasFiltered.length} marca(s)`;
  if (!marcasFiltered.length) { tw.innerHTML = emptyState('Nenhuma marca encontrada.'); return; }
  const start=(marcasPage-1)*perPage, slice=marcasFiltered.slice(start,start+perPage);
  const totalPages=Math.ceil(marcasFiltered.length/perPage);
  tw.innerHTML = `
  <table class="data-table">
    <thead><tr>
      <th>Imagem</th>
      <th>Detalhe</th>
      <th>Fase</th>
      <th>Cód. ACECA</th>
      <th>Nome</th>
      <th>Fábrica</th>
      <th>Descrição</th>
      <th>Incluído por</th>
      <th>Ações</th>
    </tr></thead>
    <tbody>
    ${slice.map(m => `<tr>
      <td>${m.imagemUrl ? `<img class="td-img" src="${m.imagemUrl}" alt="${m.nome}">` : '<div class="td-img-placeholder">📷</div>'}</td>
      <td>${m.imagemDetalheUrl ? `<img class="td-img" src="${m.imagemDetalheUrl}" alt="detalhe">` : '<div class="td-img-placeholder">🔍</div>'}</td>
      <td><span class="badge badge-purple">${m.faseNome||'—'}</span></td>
      <td><code style="font-size:.76rem;background:#f3eeff;padding:2px 7px;border-radius:4px">${m.codigoAceca||'—'}</code></td>
      <td><strong>${m.nome||'—'}</strong></td>
      <td>${m.fabricaNome||'—'}</td>
      <td style="max-width:180px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap" title="${m.descricao||''}">${m.descricao||'—'}</td>
      <td>${m.incluídoPor||m.incluidoPor||'—'}</td>
      <td><div class="td-actions">
        <button class="btn btn-secondary btn-sm btn-icon" onclick="openMarcaForm(${m.id})" title="Editar">✏️</button>
        <button class="btn btn-danger btn-sm btn-icon" onclick="deleteMarca(${m.id})" title="Excluir">🗑️</button>
      </div></td>
    </tr>`).join('')}
    </tbody>
  </table>
  <div class="pagination">
    <span>Mostrando ${start+1}–${Math.min(start+perPage,marcasFiltered.length)} de ${marcasFiltered.length}</span>
    <div class="pg-btns">
      <button class="pg-btn" onclick="marcasChangePage(${marcasPage-1})" ${marcasPage<=1?'disabled':''}>‹</button>
      ${Array.from({length:totalPages},(_,i)=>`<button class="pg-btn ${i+1===marcasPage?'active':''}" onclick="marcasChangePage(${i+1})">${i+1}</button>`).join('')}
      <button class="pg-btn" onclick="marcasChangePage(${marcasPage+1})" ${marcasPage>=totalPages?'disabled':''}>›</button>
    </div>
  </div>`;
}

function marcasChangePage(p) {
  const total=Math.ceil(marcasFiltered.length/perPage);
  if(p<1||p>total) return;
  marcasPage=p; renderMarcasTable();
}

async function openMarcaForm(id) {
  let marca = id ? marcasAll.find(m=>m.id===id) : null;
  let fases=[], fabricas=[], tipos=[];
  try { fases    = await fn_Api('GET','fases'); } catch { fases=[{id:1,nome:'Fase 1'},{id:2,nome:'Fase 2'},{id:3,nome:'Fase 3'}]; }
  try { fabricas = await fn_Api('GET','fabricas'); } catch { fabricas=[{id:1,nome:'Fábrica Alpha'},{id:2,nome:'Fábrica Beta'}]; }
  try { tipos    = await fn_Api('GET','tipos'); } catch { tipos=[{id:1,nome:'Tipo A'},{id:2,nome:'Tipo B'}]; }

  const faseOpts    = fases.map(f=>`<option value="${f.id}" ${marca&&marca.faseId==f.id?'selected':''}>${f.nome}</option>`).join('');
  const fabricaOpts = fabricas.map(f=>`<option value="${f.id}" ${marca&&marca.fabricaId==f.id?'selected':''}>${f.nome}</option>`).join('');
  const tipoOpts    = tipos.map(t=>`<option value="${t.id}" ${marca&&marca.tipoId==t.id?'selected':''}>${t.nome}</option>`).join('');

  const html = `
  <form id="marcaForm" onsubmit="saveMarca(event)" enctype="multipart/form-data">
    <input type="hidden" name="id" value="${marca?.id||0}">
    <div class="form-grid">
      <div class="fg">
        <label>Nome <span class="req">*</span></label>
        <input type="text" name="nome" value="${marca?.nome||''}" required placeholder="Nome da marca">
      </div>
      <div class="fg">
        <label>Código ACECA <span class="req">*</span></label>
        <input type="text" name="codigoAceca" value="${marca?.codigoAceca||''}" required placeholder="Ex: ACECA-001">
      </div>
      <div class="fg">
        <label>Fase <span class="req">*</span></label>
        <select name="faseId" required>
          <option value="" disabled ${!marca?'selected':''}>Selecione…</option>
          ${faseOpts}
        </select>
      </div>
      <div class="fg">
        <label>Fábrica <span class="req">*</span></label>
        <select name="fabricaId" required>
          <option value="" disabled ${!marca?'selected':''}>Selecione…</option>
          ${fabricaOpts}
        </select>
      </div>
      <div class="fg">
        <label>Tipo</label>
        <select name="tipoId">
          <option value="">— Selecione —</option>
          ${tipoOpts}
        </select>
      </div>
      <div class="fg">
        <label>Incluído por</label>
        <input type="text" name="incluidoPor" value="${marca?.incluidoPor||marca?.incluídoPor||''}" placeholder="Nome do responsável">
      </div>
      <div class="fg span2">
        <label>Descrição</label>
        <textarea name="descricao" rows="3" placeholder="Descrição detalhada da marca…">${marca?.descricao||''}</textarea>
      </div>

      <div class="fg">
        <label>Imagem Principal</label>
        <div class="img-upload-area" id="imgPrevWrap" onclick="document.getElementById('imgFile').click()">
          <input type="file" name="imagemFile" id="imgFile" accept="image/*" onchange="previewImg(this,'imgPrev')" style="display:none">
          ${marca?.imagemUrl
            ? `<img id="imgPrev" class="img-preview" src="${marca.imagemUrl}" alt="preview">`
            : `<span class="iu-icon">🖼️</span><span class="iu-txt">Clique ou arraste<br>PNG · JPG · WEBP</span><img id="imgPrev" class="img-preview" style="display:none" alt="preview">`}
        </div>
        ${marca?.imagemUrl?`<input type="hidden" name="imagemUrlAtual" value="${marca.imagemUrl}">`:''}
      </div>
      <div class="fg">
        <label>Imagem de Detalhe</label>
        <div class="img-upload-area" id="imgDetPrevWrap" onclick="document.getElementById('imgDetFile').click()">
          <input type="file" name="imagemDetalheFile" id="imgDetFile" accept="image/*" onchange="previewImg(this,'imgDetPrev')" style="display:none">
          ${marca?.imagemDetalheUrl
            ? `<img id="imgDetPrev" class="img-preview" src="${marca.imagemDetalheUrl}" alt="detalhe">`
            : `<span class="iu-icon">🔍</span><span class="iu-txt">Clique ou arraste<br>PNG · JPG · WEBP</span><img id="imgDetPrev" class="img-preview" style="display:none" alt="detalhe">`}
        </div>
        ${marca?.imagemDetalheUrl?`<input type="hidden" name="imagemDetalheUrlAtual" value="${marca.imagemDetalheUrl}">`:''}
      </div>
    </div>
    <div class="modal-footer" style="margin:16px -22px -22px;padding:14px 22px">
      <button type="button" class="btn btn-secondary" onclick="closeModal()">Cancelar</button>
      <button type="submit" class="btn btn-primary">💾 Salvar Marca</button>
    </div>
  </form>`;
  openModal(marca ? `Editar Marca — ${marca.nome}` : 'Nova Marca', html);
}

function previewImg(input, previewId) {
  const file = input.files[0];
  if (!file) return;
  const reader = new FileReader();
  reader.onload = e => {
    const img = document.getElementById(previewId);
    if (img) { img.src = e.target.result; img.style.display='block'; }
    // hide placeholder text
    const wrap = input.closest('.img-upload-area');
    if (wrap) { wrap.querySelectorAll('.iu-icon,.iu-txt').forEach(el=>el.style.display='none'); }
  };
  reader.readAsDataURL(file);
}

async function saveMarca(e) {
  e.preventDefault();
  const form = document.getElementById('marcaForm');
  const fd = new FormData(form);
  const isEdit = fd.get('id') && fd.get('id') !== '0';
  const id = fd.get('id');
  try {
    if (isEdit) {
      await fn_Api('PUT',`marcas/${id}`, fd, true);
      toast('✅ Marca atualizada!', 'success');
    } else {
      await fn_Api('POST','marcas', fd, true);
      toast('✅ Marca criada!', 'success');
    }
    closeModal(); loadMarcas();
  } catch(err) {
    toast('❌ Erro ao salvar marca.', 'error');
  }
}

async function deleteMarca(id) {
  if (!confirm('Excluir esta marca?')) return;
  try {
    await fn_Api('DELETE',`marcas/${id}`);
    toast('🗑️ Marca excluída.', 'success');
    loadMarcas();
  } catch { toast('❌ Erro ao excluir.','error'); }
}

/* ──────────────────────────────────────────────
   MODAL
────────────────────────────────────────────── */
function openModal(title, html) {
  document.getElementById('modalTitle').textContent = title;
  document.getElementById('modalBody').innerHTML = html;
  document.getElementById('modalBackdrop').classList.add('open');
  document.body.style.overflow = 'hidden';
}
function closeModal(e) {
  if (e && e.target !== document.getElementById('modalBackdrop')) return;
  document.getElementById('modalBackdrop').classList.remove('open');
  document.body.style.overflow = '';
}

/* ──────────────────────────────────────────────
   TOAST & HELPERS
────────────────────────────────────────────── */
function toast(msg, type='') {
  const t = document.getElementById('toast');
  t.textContent = msg;
  t.className = `toast show ${type}`;
  setTimeout(() => t.className='toast', 3500);
}

function emptyState(msg) {
  return `<div class="empty-state"><div class="es-icon">📭</div><p>${msg}</p></div>`;
}

/* ──────────────────────────────────────────────
   ZOOM
────────────────────────────────────────────── */

function fn_Zoom() {
    var modal = document.getElementById('myModal');
    //console.log("fnZoom modal ::: ", modal);

    // Get the image and insert it inside the modal - use its "alt" text as a caption
    var img = document.querySelectorAll(".cmyImg"); //document.getElementById('myImg');
    var modalImg = document.getElementById("img01");
    var captionText = document.getElementById("caption");

    //console.log("fnZoom img ::: ", img);
    //console.log("fnZoom modalImg ::: ", modalImg);
    //console.log("fnZoom captionText ::: ", captionText);


    $(".cmyImg").click(function () {
        //console.log("fnZoom cmyImg ::: ", $(this));
        //console.log("fnZoom cmyImg  modal ::: ", modal);
        //console.log("fnZoom cmyImg  modalImg ::: ", modalImg);

        modal.style.display = "block";
        modalImg.src = this.src;
        modalImg.alt = this.alt;
        captionText.innerHTML = this.alt;
    });


    // When the user clicks on <span> (x), close the modal
    $("#myModal").click(function () {
        //console.log("fnZoom myModal::: ", img);
        img01.className += " out";
        setTimeout(function () {
            modal.style.display = "none";
            img01.className = "modal-content";
        }, 400);

    });
}

document.addEventListener('hidden.bs.modal', function (event) {
    if (document.activeElement) {
        document.activeElement.blur();
    }
});
