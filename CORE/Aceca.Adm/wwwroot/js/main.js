
/* Navbar */
window.addEventListener('scroll', () =>
  document.getElementById('navbar').style.boxShadow =
    window.scrollY > 30 ? '0 4px 28px rgba(71,0,123,.6)' : '0 3px 18px rgba(71,0,123,.5)'
);
function toggleNav() {
  document.getElementById('navLinks').classList.toggle('open');
}
document.querySelectorAll('.nav-links a').forEach(a =>
  a.addEventListener('click', () => document.getElementById('navLinks').classList.remove('open'))
);

/* File upload */
let files = [];
function addFiles(inp) {
  Array.from(inp.files).forEach(f => {
    if (files.length >= 3) { toast('⚠️ Máximo 3 imagens.'); return; }
    if (!files.find(x => x.name === f.name)) files.push(f);
  });
  renderThumbs();
  inp.value = '';
}
function renderThumbs() {
  const c = document.getElementById('thumbs');
  c.innerHTML = '';
  files.forEach((f, i) => {
    const r = new FileReader(), d = document.createElement('div');
    d.className = 'thumb';
    r.onload = e => { d.innerHTML = `<img src="${e.target.result}" alt="${f.name}"><div class="x" onclick="rmFile(${i})">✕</div>`; };
    r.readAsDataURL(f);
    c.appendChild(d);
  });
}
function rmFile(i) { files.splice(i, 1); renderThumbs(); }

/* Contact submit */
async function enviarContato(e) {
  e.preventDefault();
  const n = document.getElementById('fNome').value.trim();
  const em = document.getElementById('fEmail').value.trim();
  const as = document.getElementById('fAssunto').value;
  const ms = document.getElementById('fMsg').value.trim();
  if (!n || !em || !as || !ms) { toast('⚠️ Preencha os campos obrigatórios.'); return; }
  const fd = new FormData();
  fd.append('nome', n);
  fd.append('email', em);
  fd.append('telefone', document.getElementById('fTel').value);
  fd.append('assunto', as);
  fd.append('mensagem', ms);
  files.forEach(f => fd.append('imagens', f));
  try {
    const r = await fetch('/api/contato', { method: 'POST', body: fd });
    if (r.ok) {
      toast('✅ Mensagem enviada com sucesso!');
      document.getElementById('frmContato').reset();
      files = []; renderThumbs();
    } else throw new Error();
  } catch {
    toast('✅ Mensagem recebida! (modo demonstração)');
    document.getElementById('frmContato').reset();
    files = []; renderThumbs();
  }
}

/* Toast */
function toast(msg) {
  const t = document.getElementById('toast');
  t.textContent = msg; t.classList.add('show');
  setTimeout(() => t.classList.remove('show'), 3400);
}

/* Welcome after login */
(function () {
  const p = new URLSearchParams(location.search).get('bem-vindo');
  if (p) { toast('👋 Bem-vindo(a), ' + decodeURIComponent(p) + '!'); history.replaceState({}, '', 'index.html'); }
})();
