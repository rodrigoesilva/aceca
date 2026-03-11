
const USUARIOS = [
  { email:'admin@aceca.com.br',    senha:'Aceca@2025!',   nome:'Admin ACECA',     cargo:'admin' },
  { email:'marcos@aceca.com.br',   senha:'Marcos#123',    nome:'Marcos Vieira',   cargo:'presidente' },
  { email:'alberto@aceca.com.br',  senha:'Alberto@01',    nome:'Alberto Souza',   cargo:'presidente' },
  { email:'carlos@aceca.com.br',   senha:'Carlos@02',     nome:'Carlos Lima',     cargo:'vice_presidente' },
  { email:'ricardo@aceca.com.br',  senha:'Ricardo@03',    nome:'Ricardo Santos',  cargo:'dir_financeiro' },
  { email:'pedro@aceca.com.br',    senha:'Pedro@04',      nome:'Pedro Martins',   cargo:'dir_extraordinario' },
  { email:'camila@aceca.com.br',   senha:'Camila@05',     nome:'Camila Alves',    cargo:'gestao_ti' },
  { email:'daniel@aceca.com.br',   senha:'Daniel@321',    nome:'Daniel Oliveira', cargo:'socio' },
  { email:'fernanda@aceca.com.br', senha:'Fer@2023',      nome:'Fernanda Costa',  cargo:'socio' },
  { email:'lucas@aceca.com.br',    senha:'Lucas#456',     nome:'Lucas Mendes',    cargo:'socio' },
  { email:'julia@aceca.com.br',    senha:'Julia@789',     nome:'Julia Martins',   cargo:'socio' },
  { email:'gustavo@aceca.com.br',  senha:'Gust@000',      nome:'Gustavo Lima',    cargo:'socio' },
];

function verSenha() {
  const i = document.getElementById('lSenha');
  i.type = i.type === 'password' ? 'text' : 'password';
}

async function fazerLogin(e) {
    console.log("fazerLogin - e :::", e);

  e.preventDefault();
  const email = document.getElementById('lEmail').value.trim().toLowerCase();
  const senha = document.getElementById('lSenha').value;
  const btn   = document.getElementById('btnEntrar');
  const err   = document.getElementById('loginErr');
  err.style.display = 'none';
  btn.disabled = true; btn.textContent = 'Verificando…';

  let user = null;

    try {
        //const res = await fetch('/api/auth/login', {

        const response = await fetch('/Auth/Login', {
            method: 'POST',
            headers: {
                "Content-Type": 'application/json',
            },
            body: JSON.stringify({ email, senha }),
        });

        if (response.ok)
            user = await response.json();
  }
  catch {
  }

  if (!user) {
      const u = USUARIOS.find(x => x.email === email && x.senha === senha);

      if (u)
          user = {
              nome: u.nome,
              email: u.email,
              cargo: u.cargo,
              token: 'demo'
          };
  }

  btn.disabled = false; btn.textContent = 'Entrar';

  if (user) {
      sessionStorage.setItem('aceca_sessao', JSON.stringify(user));

      console.log("fazerLogin - user :::", user);
      //console.log("fazerLogin - sessionStorage :::", sessionStorage);

      //window.location.href = 'dashboard.html';
      window.location.href = '/Marcas/Index';
  } else {
    err.textContent = '❌ E-mail ou senha incorretos. Verifique suas credenciais.';
      err.style.display = 'block';

      console.log("fazerLogin - err :::", err);

    document.getElementById('lSenha').value = '';
  }
}
