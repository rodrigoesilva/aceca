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
      console.log("user nao localizado:::");
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
