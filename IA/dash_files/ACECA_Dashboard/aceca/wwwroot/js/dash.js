
/* ──────────────────────────────────────────────
   AUTH GUARD
────────────────────────────────────────────── */
(function() {
  const sess = sessionStorage.getItem('aceca_sessao');
  if (!sess) { window.location.href = 'login.html'; return; }
  const u = JSON.parse(sess);
  document.getElementById('tbNome').textContent  = u.nome  || 'Usuário';
  document.getElementById('tbCargo').textContent = u.cargo || '—';
  document.getElementById('tbAvatar').textContent = (u.nome||'U')[0].toUpperCase();
})();

function logout() {
  sessionStorage.removeItem('aceca_sessao');
  window.location.href = 'login.html';
}

/* ──────────────────────────────────────────────
   SIDEBAR TOGGLE
────────────────────────────────────────────── */
let sidebarOpen = window.innerWidth > 900;
function toggleSidebar() {
  const sb  = document.getElementById('sidebar');
  const ov  = document.getElementById('sidebarOverlay');
  const mn  = document.getElementById('dashMain');
  if (window.innerWidth > 900) {
    sb.classList.toggle('collapsed');
    mn.classList.toggle('expanded');
  } else {
    sb.classList.toggle('open');
    ov.classList.toggle('show');
  }
}
window.addEventListener('resize', () => {
  const sb = document.getElementById('sidebar');
  const ov = document.getElementById('sidebarOverlay');
  if (window.innerWidth > 900) {
    sb.classList.remove('open');
    ov.classList.remove('show');
  }
});

/* ──────────────────────────────────────────────
   PAGE ROUTER
────────────────────────────────────────────── */
function loadPage(page, el) {
  document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
  if (el) el.classList.add('active');
  const c = document.getElementById('pageContent');
  c.innerHTML = '<div style="padding:40px;text-align:center;color:#7a5c9a;">⏳ Carregando…</div>';
  setTimeout(() => {
    switch(page) {
      case 'home':       renderHome();       break;
      case 'agenda':     renderGeneric('agenda','📅','Agenda','agendas',agendaCols,agendaForm);  break;
      case 'socios':     renderGeneric('socios','👥','Sócios','socios',sociosCols,sociosForm);   break;
      case 'paises':     renderGeneric('paises','🌍','Países','paises',paisesCols,paisesForm);  break;
      case 'fabricas':   renderGeneric('fabricas','🏭','Fábricas','fabricas',fabricasCols,fabricasForm); break;
      case 'dimensao':   renderGeneric('dimensao','📐','Dimensão','dimensoes',dimensaoCols,dimensaoForm); break;
      case 'fase':       renderGeneric('fase','🔖','Fase','fases',faseCols,faseForm); break;
      case 'impressora': renderGeneric('impressora','🖨️','Impressora','impressoras',impressoraCols,impressoraForm); break;
      case 'subtipos':   renderGeneric('subtipos','🏷️','Sub-Tipos','subtipos',subtiposCols,subtiposForm); break;
      case 'tipos':      renderGeneric('tipos','📋','Tipos','tipos',tiposCols,tiposForm); break;
      case 'marcas':     renderMarcas(); break;
      default:           renderHome();
    }
  }, 80);
}

/* ──────────────────────────────────────────────
   HOME DASHBOARD
────────────────────────────────────────────── */
function renderHome() {
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
  fetchStats();
  fetchRecentMarcas();
}

async function fetchStats() {
  const endpoints = [
    ['stSocios','socios'],['stMarcas','marcas'],['stFabricas','fabricas'],
    ['stPaises','paises'],['stAgenda','agendas'],['stTipos','tipos']
  ];
  for (const [id,ep] of endpoints) {
    try {
      const r = await api('GET',ep);
      document.getElementById(id).textContent = Array.isArray(r) ? r.length : '—';
    } catch { document.getElementById(id).textContent = '0'; }
  }
}

async function fetchRecentMarcas() {
  const el = document.getElementById('recentMarcas');
  try {
    const rows = (await api('GET','marcas')).slice(0,5);
    if (!rows.length) { el.innerHTML = emptyState('Nenhuma marca cadastrada ainda.'); return; }
    el.innerHTML = `<table class="data-table"><thead><tr>
      <th>Imagem</th><th>Nome</th><th>Fábrica</th><th>Fase</th><th>Cód. ACECA</th>
    </tr></thead><tbody>${rows.map(m=>`<tr>
      <td>${m.imagemUrl ? `<img class="td-img" src="${m.imagemUrl}" alt="${m.nome}">` : '<div class="td-img-placeholder">📷</div>'}</td>
      <td><strong>${m.nome||'—'}</strong></td>
      <td>${m.fabricaNome||'—'}</td>
      <td>${m.faseNome||'—'}</td>
      <td>${m.codigoAceca||'—'}</td>
    </tr>`).join('')}</tbody></table>`;
  } catch { el.innerHTML = emptyState('Erro ao carregar marcas.'); }
}

/* ──────────────────────────────────────────────
   GENERIC CRUD PAGE
────────────────────────────────────────────── */
let currentEndpoint='', currentCols=[], currentFormFn=null;
let allRows=[], filteredRows=[], page=1, perPage=10;

function renderGeneric(page_, icon, title, endpoint, cols, formFn) {
  currentEndpoint = endpoint;
  currentCols     = cols;
  currentFormFn   = formFn;
  page = 1;
  document.getElementById('pageContent').innerHTML = `
  <div class="page-header">
    <div>
      <h1><span class="ph-icon">${icon}</span> ${title}</h1>
      <p>Gerencie os registros de ${title.toLowerCase()}</p>
    </div>
    <button class="btn btn-primary" onclick="openCreate()">+ Novo Registro</button>
  </div>
  <div class="grid-toolbar">
    <div class="search-box">
      <span>🔍</span>
      <input type="text" placeholder="Buscar…" id="searchInput" oninput="filterRows(this.value)">
    </div>
    <span id="rowCount" style="font-size:.78rem;color:var(--muted)"></span>
  </div>
  <div id="tableWrap" class="table-wrap"></div>`;
  loadRows();
}

async function loadRows() {
  const tw = document.getElementById('tableWrap');
  if (!tw) return;
  tw.innerHTML = '<div style="padding:32px;text-align:center;color:#7a5c9a">⏳ Carregando…</div>';
  try {
    allRows = await api('GET', currentEndpoint);
    filteredRows = [...allRows];
    renderTable();
  } catch { tw.innerHTML = emptyState('Erro ao carregar dados. Backend pode estar offline — dados de demonstração.'); }
}

function filterRows(q) {
  const s = q.toLowerCase();
  filteredRows = allRows.filter(r =>
    Object.values(r).some(v => String(v).toLowerCase().includes(s))
  );
  page = 1;
  renderTable();
}

function renderTable() {
  const tw = document.getElementById('tableWrap');
  if (!tw) return;
  const rc = document.getElementById('rowCount');
  if (rc) rc.textContent = `${filteredRows.length} registro(s)`;
  if (!filteredRows.length) { tw.innerHTML = emptyState('Nenhum registro encontrado.'); return; }
  const start = (page-1)*perPage, slice = filteredRows.slice(start, start+perPage);
  const totalPages = Math.ceil(filteredRows.length/perPage);
  tw.innerHTML = `
  <table class="data-table">
    <thead><tr>
      ${currentCols.map(c=>`<th>${c.label}</th>`).join('')}
      <th>Ações</th>
    </tr></thead>
    <tbody>
      ${slice.map(row => `<tr>
        ${currentCols.map(c => `<td>${renderCell(row,c)}</td>`).join('')}
        <td><div class="td-actions">
          <button class="btn btn-secondary btn-sm btn-icon" onclick="openEdit(${row.id})" title="Editar">✏️</button>
          <button class="btn btn-danger btn-sm btn-icon" onclick="deleteRow(${row.id})" title="Excluir">🗑️</button>
        </div></td>
      </tr>`).join('')}
    </tbody>
  </table>
  <div class="pagination">
    <span>Mostrando ${start+1}–${Math.min(start+perPage,filteredRows.length)} de ${filteredRows.length}</span>
    <div class="pg-btns">
      <button class="pg-btn" onclick="changePage(${page-1})" ${page<=1?'disabled':''}>‹</button>
      ${Array.from({length:totalPages},(_, i)=>`<button class="pg-btn ${i+1===page?'active':''}" onclick="changePage(${i+1})">${i+1}</button>`).join('')}
      <button class="pg-btn" onclick="changePage(${page+1})" ${page>=totalPages?'disabled':''}>›</button>
    </div>
  </div>`;
}

function changePage(p) {
  const total = Math.ceil(filteredRows.length/perPage);
  if (p<1||p>total) return;
  page = p; renderTable();
}

function renderCell(row, col) {
  const val = row[col.key];
  if (col.type === 'img') return val ? `<img class="td-img" src="${val}" alt="">` : '<div class="td-img-placeholder">📷</div>';
  if (col.type === 'badge') return val ? `<span class="badge badge-${col.badgeClass||'purple'}">${val}</span>` : '—';
  if (col.type === 'bool') return val ? '<span class="badge badge-green">Ativo</span>' : '<span class="badge badge-gray">Inativo</span>';
  return val ?? '—';
}

/* ──────────────────────────────────────────────
   CRUD OPERATIONS
────────────────────────────────────────────── */
function openCreate() {
  openModal('Novo Registro', currentFormFn(null));
}
function openEdit(id) {
  const row = allRows.find(r => r.id === id);
  if (!row) return;
  openModal('Editar Registro', currentFormFn(row));
}

async function saveForm() {
  const form = document.getElementById('genericForm');
  if (!form) return;
  const fd = new FormData(form);
  const isEdit = !!fd.get('id') && fd.get('id') !== '0';
  const id  = fd.get('id');
  try {
    if (isEdit) {
      await api('PUT', `${currentEndpoint}/${id}`, fd, true);
      toast('✅ Registro atualizado com sucesso!', 'success');
    } else {
      await api('POST', currentEndpoint, fd, true);
      toast('✅ Registro criado com sucesso!', 'success');
    }
    closeModal();
    loadRows();
  } catch(e) {
    toast('❌ Erro ao salvar: ' + (e.message||'Tente novamente.'), 'error');
  }
}

async function deleteRow(id) {
  if (!confirm('Confirmar exclusão deste registro?')) return;
  try {
    await api('DELETE', `${currentEndpoint}/${id}`);
    toast('🗑️ Registro excluído.', 'success');
    loadRows();
  } catch(e) {
    toast('❌ Erro ao excluir.', 'error');
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
    marcasAll = await api('GET','marcas');
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
  try { fases    = await api('GET','fases'); } catch { fases=[{id:1,nome:'Fase 1'},{id:2,nome:'Fase 2'},{id:3,nome:'Fase 3'}]; }
  try { fabricas = await api('GET','fabricas'); } catch { fabricas=[{id:1,nome:'Fábrica Alpha'},{id:2,nome:'Fábrica Beta'}]; }
  try { tipos    = await api('GET','tipos'); } catch { tipos=[{id:1,nome:'Tipo A'},{id:2,nome:'Tipo B'}]; }

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
      await api('PUT',`marcas/${id}`, fd, true);
      toast('✅ Marca atualizada!', 'success');
    } else {
      await api('POST','marcas', fd, true);
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
    await api('DELETE',`marcas/${id}`);
    toast('🗑️ Marca excluída.', 'success');
    loadMarcas();
  } catch { toast('❌ Erro ao excluir.','error'); }
}

/* ──────────────────────────────────────────────
   COLUMNS & FORMS for generic pages
────────────────────────────────────────────── */
// Agenda
const agendaCols = [
  {key:'id',label:'#'},{key:'titulo',label:'Título'},{key:'data',label:'Data'},
  {key:'local',label:'Local'},{key:'tipo',label:'Tipo',type:'badge'}
];
function agendaForm(r) { return formHtml(r,[
  {n:'titulo',l:'Título',req:true},{n:'descricao',l:'Descrição',type:'textarea'},
  {n:'data',l:'Data',type:'date',req:true},{n:'horaInicio',l:'Hora Início',type:'time'},
  {n:'horaFim',l:'Hora Fim',type:'time'},{n:'local',l:'Local'},
  {n:'tipo',l:'Tipo',type:'select',opts:['proximo','destaque','passado']}
]); }

// Socios
const sociosCols = [
  {key:'id',label:'#'},{key:'nome',label:'Nome'},{key:'email',label:'E-mail'},
  {key:'cargo',label:'Cargo',type:'badge',badgeClass:'blue'},{key:'ativo',label:'Status',type:'bool'}
];
function sociosForm(r) { return formHtml(r,[
  {n:'nome',l:'Nome',req:true},{n:'email',l:'E-mail',type:'email',req:true},
  {n:'cargo',l:'Cargo',type:'select',opts:['socio','admin','presidente','vice_presidente','dir_financeiro']},
  {n:'senha',l:'Senha (deixe vazio para manter)',type:'password'},
  {n:'ativo',l:'Ativo',type:'bool'}
]); }

// Países
const paisesCols = [{key:'id',label:'#'},{key:'nome',label:'Nome'},{key:'sigla',label:'Sigla',type:'badge'}];
function paisesForm(r) { return formHtml(r,[{n:'nome',l:'Nome',req:true},{n:'sigla',l:'Sigla (ex: BR)',req:true}]); }

// Fábricas
const fabricasCols = [{key:'id',label:'#'},{key:'nome',label:'Nome'},{key:'paisNome',label:'País'},{key:'cidade',label:'Cidade'}];
function fabricasForm(r) { return formHtml(r,[
  {n:'nome',l:'Nome',req:true},{n:'cidade',l:'Cidade'},{n:'paisId',l:'País (ID)'}
]); }

// Dimensão
const dimensaoCols = [{key:'id',label:'#'},{key:'nome',label:'Nome'},{key:'largura',label:'Largura'},{key:'altura',label:'Altura'}];
function dimensaoForm(r) { return formHtml(r,[
  {n:'nome',l:'Nome',req:true},{n:'largura',l:'Largura'},{n:'altura',l:'Altura'}
]); }

// Fase
const faseCols = [{key:'id',label:'#'},{key:'nome',label:'Nome'},{key:'descricao',label:'Descrição'}];
function faseForm(r) { return formHtml(r,[{n:'nome',l:'Nome',req:true},{n:'descricao',l:'Descrição',type:'textarea'}]); }

// Impressora
const impressoraCols = [{key:'id',label:'#'},{key:'nome',label:'Nome'},{key:'modelo',label:'Modelo'}];
function impressoraForm(r) { return formHtml(r,[{n:'nome',l:'Nome',req:true},{n:'modelo',l:'Modelo'}]); }

// Sub-Tipos
const subtiposCols = [{key:'id',label:'#'},{key:'nome',label:'Nome'},{key:'tipoNome',label:'Tipo'}];
function subtiposForm(r) { return formHtml(r,[{n:'nome',l:'Nome',req:true},{n:'tipoId',l:'Tipo (ID)'}]); }

// Tipos
const tiposCols = [{key:'id',label:'#'},{key:'nome',label:'Nome'},{key:'descricao',label:'Descrição'}];
function tiposForm(r) { return formHtml(r,[{n:'nome',l:'Nome',req:true},{n:'descricao',l:'Descrição',type:'textarea'}]); }

/* ──────────────────────────────────────────────
   GENERIC FORM BUILDER
────────────────────────────────────────────── */
function formHtml(row, fields) {
  const cols = fields.length > 4 ? 'form-grid' : 'form-grid';
  const flds = fields.map(f => {
    const v = row?.[f.n]||'';
    if (f.type==='textarea') return `<div class="fg span2"><label>${f.l}${f.req?'<span class="req"> *</span>':''}</label><textarea name="${f.n}" rows="3" ${f.req?'required':''}>${v}</textarea></div>`;
    if (f.type==='bool') return `<div class="fg"><label>${f.l}</label><select name="${f.n}"><option value="true" ${v?'selected':''}>Ativo</option><option value="false" ${!v?'selected':''}>Inativo</option></select></div>`;
    if (f.type==='select') return `<div class="fg"><label>${f.l}${f.req?'<span class="req"> *</span>':''}</label><select name="${f.n}" ${f.req?'required':''}>${(f.opts||[]).map(o=>`<option value="${o}" ${v===o?'selected':''}>${o}</option>`).join('')}</select></div>`;
    return `<div class="fg"><label>${f.l}${f.req?'<span class="req"> *</span>':''}</label><input type="${f.type||'text'}" name="${f.n}" value="${v}" ${f.req?'required':''}></div>`;
  }).join('');
  return `<form id="genericForm" onsubmit="saveForm(event)">
    <input type="hidden" name="id" value="${row?.id||0}">
    <div class="form-grid">${flds}</div>
    <div class="modal-footer" style="margin:16px -22px -22px;padding:14px 22px">
      <button type="button" class="btn btn-secondary" onclick="closeModal()">Cancelar</button>
      <button type="submit" class="btn btn-primary">💾 Salvar</button>
    </div>
  </form>`;
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
   API WRAPPER
────────────────────────────────────────────── */
async function api(method, endpoint, body=null, isFormData=false) {
  const sess = JSON.parse(sessionStorage.getItem('aceca_sessao')||'{}');
  const headers = {};
  if (sess.token && sess.token !== 'demo') headers['Authorization'] = `Bearer ${sess.token}`;
  if (body && !isFormData) { headers['Content-Type']='application/json'; body=JSON.stringify(body); }
  const res = await fetch(`/api/${endpoint}`, { method, headers, body: body||undefined });
  if (!res.ok) throw new Error(await res.text());
  const ct = res.headers.get('Content-Type')||'';
  if (ct.includes('json')) return res.json();
  return {};
}

/* ──────────────────────────────────────────────
   DEMO DATA (fallback when backend offline)
────────────────────────────────────────────── */
function getMarcasDemo() {
  return [
    {id:1,nome:'Hollywood',codigoAceca:'ACECA-001',faseId:1,faseNome:'Fase 1',fabricaId:1,fabricaNome:'Souza Cruz',descricao:'Marca clássica brasileira dos anos 60',incluidoPor:'Admin',imagemUrl:'',imagemDetalheUrl:''},
    {id:2,nome:'Continental',codigoAceca:'ACECA-002',faseId:2,faseNome:'Fase 2',fabricaId:2,fabricaNome:'Philip Morris',descricao:'Embalagem metálica década de 70',incluidoPor:'Admin',imagemUrl:'',imagemDetalheUrl:''},
    {id:3,nome:'Ministers',codigoAceca:'ACECA-003',faseId:1,faseNome:'Fase 1',fabricaId:1,fabricaNome:'Souza Cruz',descricao:'Série especial comemorativa',incluidoPor:'Daniel Oliveira',imagemUrl:'',imagemDetalheUrl:''},
  ];
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
   INIT
────────────────────────────────────────────── */
loadPage('home', document.querySelector('.nav-item[data-page="home"]'));
