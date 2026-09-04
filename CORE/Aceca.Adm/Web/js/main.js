//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`Todos os recursos terminaram o carregamento!`);

		const btn = document.getElementById('btn-proximos');
		//console.log("addEventListener btn ::", btn);

		filterEvents('upcoming',btn);

		document.querySelectorAll('.nav-links a').forEach(a=>{
			a.addEventListener('click',()=>
				document.getElementById('navLinks').classList.remove('open')
			);
		});

		// Fecha o menu mobile clicando em qualquer lugar da tela (fora do hambúrguer,
		// que já tem seu próprio toggle e re-abriria o menu no mesmo clique).
		document.addEventListener('click', function (e) {
			var navLinks = document.getElementById('navLinks');
			var hamburger = document.getElementById('hamburger');
			if (!navLinks.classList.contains('open')) return;
			if (hamburger.contains(e.target)) return;
			navLinks.classList.remove('open');
		});

		ajustarLinksLoginParaDominioLocal();

		const statsBar = document.querySelector('.stats-bar');
		if (statsBar) {
			const obsContadores = new IntersectionObserver(entries => {
				entries.forEach(e => { if (e.isIntersecting) { animateCounters(); obsContadores.disconnect(); } });
			}, { threshold: 0.3 });
			obsContadores.observe(statsBar);
		}

		// Política/Termos viraram modal (não mais seção fixa na página) - o Google exige,
		// na tela de consentimento OAuth, um link de política num domínio autorizado
		// (aceca.tryasp.net não é aceito), então quem visita direto
		// aceca.com.br/#politica-privacidade (sem clicar em nada) precisa ver o conteúdo
		// mesmo assim - abre a modal já na carga da página nesse caso.
		if (window.location.hash === '#politica-privacidade') openLegalModal(null, 'politica');
		else if (window.location.hash === '#termos-de-uso') openLegalModal(null, 'termos');

		fn_AppInstall();

    })();
});

// Este mesmo index.html serve 2 contextos: (1) site estático standalone em
// www.aceca.com.br, onde "Login"/"Área do Sócio" precisam apontar pro domínio
// externo aceca.tryasp.net (é outro site de verdade); (2) servido de DENTRO do
// próprio aceca.tryasp.net (rota /Web/index.html), onde esses mesmos links devem
// apontar pra rota interna /Auth/Index - um href fixo não acerta os dois ao mesmo
// tempo. Detecta em qual domínio a página está rodando, usado aqui e em
// fn_AppInstall (instalação de PWA também só funciona na mesma origem do app).
function fnEstaDentroDoApp(){
	return /(^|\.)aceca\.tryasp\.net$/i.test(window.location.hostname)
		|| window.location.hostname === 'localhost'
		|| window.location.hostname === '127.0.0.1';
}

function ajustarLinksLoginParaDominioLocal(){
	if (!fnEstaDentroDoApp()) return;

	document.querySelectorAll('a[href="https://aceca.tryasp.net"]').forEach(function(a){
		var texto = a.textContent.trim();
		if (texto === 'Login' || texto === 'Área do Sócio') {
			a.setAttribute('href', '/Auth/Index');
		}
	});
}

//#endregion

//#region Page

window.addEventListener('scroll',()=>{
  document.getElementById('navbar').classList.toggle('scrolled',window.scrollY>50);
});

function showToast(msg){
	//console.log("showToast msg ::", msg);
	
  const t = document.getElementById('toast');
	t.textContent = msg;
	t.classList.add('show');
	
  setTimeout(()=>t.classList.remove('show'),3200);
}

function filterEvents(type,btn){
	//console.log("filterEvents type ::", type);
	//console.log("filterEvents btn ::", btn);
	
	document.querySelectorAll('.tab-btn').forEach(b=>b.classList.remove('active'));
	btn.classList.add('active');
  
	document.querySelectorAll('.event-card').forEach(c=>{
		c.style.display=(type==='all'||c.dataset.type===type)?'':'none';
	});
}

function animateCounters(){
  document.querySelectorAll('[data-target]').forEach(el=>{
    const tgt=+el.dataset.target,step=tgt/110;
	let cur=0;
    const iv = setInterval(()=>{
		cur=Math.min(cur+step,tgt);
		el.textContent=tgt>=1000?Math.floor(cur).toLocaleString('pt-BR'):Math.floor(cur);if(cur>=tgt)clearInterval(iv);
	},16);
  });
}

//#endregion

//#region Menu

function toggleMenu(){ 
	//console.log("toggleMenu ::");
	document.getElementById('navLinks').classList.toggle('open'); 
}

//#endregion

//#region Form Page

let selectedFiles=[];

// Usado em todo alerta de erro do formulário - antes não existia em lugar nenhum do
// código, então toda chamada Swal.fire(`${swalTitleError}`,...) quebrava com
// ReferenceError antes de mostrar qualquer coisa pro usuário (erro engolido, sem
// feedback nenhum na tela).
const swalTitleError = 'Ops!';

const TAMANHO_MAXIMO_IMAGEM_MB = 5;
const TAMANHO_MAXIMO_IMAGEM_BYTES = TAMANHO_MAXIMO_IMAGEM_MB * 1024 * 1024;

function handleFiles(input) {

    const novos = Array.from(input.files || []);

    if (novos.length === 0) {
        console.warn("⚠️ Nenhum arquivo selecionado");
        return;
    }

    for (const file of novos) {

        if (selectedFiles.length >= 3) {
            Swal.fire(`${swalTitleError}`,`Máximo de 3 imagens`,'warning');
            break;
        }

        if (!file.type.startsWith("image/")) {
			Swal.fire(`${swalTitleError}`,`❌ Apenas imagens são permitidas`,'error');
            continue;
        }

        // Rótulo do formulário promete "até 5 MB cada", mas nada validava isso - o
        // arquivo ia até o servidor (que também não valida tamanho) e só falhava lá,
        // sem mensagem clara.
        if (file.size > TAMANHO_MAXIMO_IMAGEM_BYTES) {
            Swal.fire(`${swalTitleError}`,`❌ "${file.name}" tem mais de ${TAMANHO_MAXIMO_IMAGEM_MB}MB`,'error');
            continue;
        }

        selectedFiles.push(file);
    }

    // Limpa o <input> nativo - a seleção de verdade passa a viver em selectedFiles,
    // permitindo remover 1 imagem sem perder as outras (o FileList de um <input> é
    // somente-leitura, não dá pra tirar 1 item dele direto) e deixando escolher o
    // mesmo arquivo de novo depois de removê-lo.
    input.value = "";
    renderPreviews();
}

function renderPreviews(){
	//console.log("renderPreviews ::");
  const c = document.getElementById('filePreview');
	c.innerHTML='';

	//console.log("renderPreviews selectedFiles ::", selectedFiles);

  selectedFiles.forEach((f,i)=>{
    const r = new FileReader(),th=document.createElement('div');
		th.className='file-thumb';

    r.onload=ev=>{
		th.innerHTML =`<img src="${ev.target.result}" alt="${f.name}"><div class="rm" onclick="removeFile(${i})">✕</div>`;
	}

    r.readAsDataURL(f);
	c.appendChild(th);
  });
}

function removeFile(i){
  selectedFiles.splice(i,1);renderPreviews();
}

// ===============================
// 🚀 ENVIO FORMULÁRIO CONTATO
// ===============================

async function handleContactSubmit(event) {
    event.preventDefault();

    console.log("📤 Iniciando envio...");

    // 🔥 pegar form corretamente
    const form = document.getElementById("formContact");

    if (!form) {
        console.error("❌ Formulário não encontrado");
        return;
    }
	
	const btn   = document.getElementById('btn-send');
	btn.disabled = true; btn.textContent = 'Enviando dados …';

    // 🔥 montar FormData corretamente
    const formData = new FormData(form);

    // O <input type="file"> nativo é sempre esvaziado em handleFiles (permite remover
    // 1 imagem sem perder as outras) - a seleção real vive em selectedFiles, então é
    // ela que precisa ir pro envio, com o mesmo nome de campo que o PHP espera.
    formData.delete('cf-files[]');
    selectedFiles.forEach(f => formData.append('cf-files[]', f));

    console.log("📦 Dados coletados:");

    for (let pair of formData.entries()) {
        //console.log(pair[0] + ":", pair[1]);
    }

    try {

        const response = await fetch("contact-form.php", {
            method: "POST",
            body: formData
        });

        const text = await response.text();

        //console.log("📥 Resposta RAW:", text);

        let data;

        try {
            data = JSON.parse(text);
        } catch (e) {
            //console.error("❌ Não é JSON válido");
			//alert("Erro no servidor (ver console)");
			Swal.fire(`${swalTitleError}`,`Erro ao enviar <br><br> ❌ Não é JSON válido`,'error');
			  
			btn.disabled = false; btn.textContent = 'Enviar Mensagem';
			return;
        }

        //console.log("📥 JSON:", data);


        if (data.ok) {
			//alert("Mensagem enviada com sucesso");
			Swal.fire('Aceca','Mensagem enviada com sucesso! <br><br> Responderemos em breve.','success');
			
			showToast('✓ Mensagem enviada! Responderemos em breve.');
			
			['cf-name','cf-email','cf-phone','cf-message'].forEach(id=>document.getElementById(id).value='');
			
			document.getElementById('cf-motivo').value='';
			document.getElementById('filePreview').innerHTML='';
			
			selectedFiles=[];
			
			btn.disabled = false; btn.textContent = 'Enviar Mensagem';
			
            console.log("✅ Email enviado com sucesso");

            form.reset();

        } else {

            console.error("❌ Erro:", data.error);
			
			 //alert(data.error || "Erro ao enviar");
			Swal.fire(`${swalTitleError}`,`Erro ao enviar <br><br> ${data.error}`,'error');
			
			btn.disabled = false; btn.textContent = 'Enviar Mensagem';
        }

    } catch (error) {
        console.error("❌ Erro na requisição:", error);
        //console.error("🔥 ERRO FETCH:", error);
		Swal.fire(`${swalTitleError}`,`🔥 ERRO FETCH: ${error}`,'error');

		btn.disabled = false; btn.textContent = 'Enviar Mensagem';
    }
}

//#endregion

//#region Modal Login
//
// Mesmo fluxo de autenticação usado hoje em /Auth/Login (ver pages-auth.js::fn_LoginAuth
// no projeto principal) - POST em /Auth/Login, mesmo tratamento de sucesso/erro, mesmo
// registro de geolocalização do acesso (/Auth/LoginLog). Reimplementado aqui (em vez de
// simplesmente incluir pages-auth.js) porque aquele script depende de jQuery e da lib
// FormValidation, que essa página institucional não carrega - script próprio evita
// puxar essas dependências só por causa do modal. Qualquer mudança no fluxo de login
// de /Auth/Login precisa ser replicada aqui também.

function openModal(e){
  if (e) e.preventDefault();
  document.getElementById('loginModal').classList.add('open');
  document.body.style.overflow = 'hidden';
}

function closeModal(){
  document.getElementById('loginModal').classList.remove('open');
  document.body.style.overflow = '';
}

function closeModalOutside(e){
  if (e.target === document.getElementById('loginModal')) closeModal();
}

//#region MODAL POLÍTICA / TERMOS

function openLegalModal(e, tab){
  if (e) e.preventDefault();
  showLegalTab(tab || 'politica');
  document.getElementById('legalModal').classList.add('open');
  document.body.style.overflow = 'hidden';
}

function closeLegalModal(){
  document.getElementById('legalModal').classList.remove('open');
  document.body.style.overflow = '';
  // Limpa o hash (#politica-privacidade/#termos-de-uso) pra fechar não deixar a modal
  // reabrindo sozinha numa atualização de página futura.
  history.replaceState(null, '', window.location.pathname + window.location.search);
}

function closeLegalModalOutside(e){
  if (e.target === document.getElementById('legalModal')) closeLegalModal();
}

function showLegalTab(tab){
  var ehPolitica = tab !== 'termos';
  document.getElementById('painelPolitica').style.display = ehPolitica ? '' : 'none';
  document.getElementById('painelTermos').style.display = ehPolitica ? 'none' : '';
  document.getElementById('tabPolitica').classList.toggle('active', ehPolitica);
  document.getElementById('tabTermos').classList.toggle('active', !ehPolitica);
  document.querySelector('.legal-modal-body').scrollTop = 0;
}

//#endregion

function verSenhaModal(){
  const i = document.getElementById('loginPass');
  i.type = i.type === 'password' ? 'text' : 'password';
}

function fn_BtnLoadingModal(btn){
  btn.disabled = true;
  const txtSpan  = btn.querySelector('.btn-text');
  const spinSpan = btn.querySelector('.btn-spinner');
  if (txtSpan)  txtSpan.style.display  = 'none';
  if (spinSpan) spinSpan.style.display = 'inline-flex';
}

function fn_BtnResetModal(btn){
  btn.disabled = false;
  const txtSpan  = btn.querySelector('.btn-text');
  const spinSpan = btn.querySelector('.btn-spinner');
  if (txtSpan)  txtSpan.style.display  = '';
  if (spinSpan) spinSpan.style.display = 'none';
}

async function fn_LoginAuthModal(event){
  if (event) event.preventDefault();

  const err   = document.getElementById('loginErr');
  const email = document.getElementById('loginEmail').value.trim().toLowerCase();
  const senha = document.getElementById('loginPass').value;
  const btn   = document.getElementById('btnEntrarModal');

  err.style.display = 'none';

  if (!email || !senha) {
    err.textContent = '⚠️ Preencha e-mail e senha.';
    err.style.display = 'block';
    return;
  }

  fn_BtnLoadingModal(btn);

  try {
    const response = await fetch('/Auth/Login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, senha }),
    });

    const user = await response.json();

    if (response.ok && user.bResult) {

      // Geo: fire-and-forget - não bloqueia o redirecionamento
      fn_LoginAuthGeoModal(user.nameIdentifier);

      fn_LoginCkSetModal('aceca_cookie', user.nome?.split(' ')[0], 1440); // 24h (teto absoluto da sessão)
      sessionStorage.setItem('aceca_sessao', JSON.stringify(user));

      // Fecha o modal antes do Swal - o modal usa z-index alto (9999) pra ficar por
      // cima do resto da página, e isso também cobria o próprio Swal de boas-vindas.
      closeModal();

      if (user.pswuptd === false) {
        Swal.fire({
          title: `Olá ${user.nome?.split(' ')[0]}!`,
          html: `Identificamos que a sua senha expirou!!<br><br>Faça a atualização para realizar seu acesso.`,
          confirmButtonText: 'Ok!',
        }).then(() => { window.location.href = '/Auth/UpdatePass'; });
      } else {
        Swal.fire({
          icon: 'success',
          title: `Olá ${user.nome?.split(' ')[0]}!`,
          html: `Seja bem-vindo`,
          confirmButtonText: 'Ok!',
        }).then(() => { window.location.href = '/Auth/Access'; });
      }

    } else {
      const msg = user?.message ?? 'Não foi possível realizar o acesso.';

      err.textContent = `❌ ${msg}`; // textContent evita XSS com conteúdo do servidor
      err.style.display = 'block';

      fn_BtnResetModal(btn);
      document.getElementById('loginPass').value = ''; // limpa só a senha, mantém o e-mail
    }

  } catch (ex) {
    console.error('fn_LoginAuthModal:', ex);
    fn_BtnResetModal(btn);
    Swal.fire({
      title: 'Ops!',
      icon: 'error',
      html: `<b>Não foi possível realizar o acesso.<br>Tente novamente.</b>`,
    });
  }
}

// Grava o mesmo cookie client-side que /Auth/Login grava hoje (usado pela tela de
// login pra exibir "Seja bem-vindo novamente" em visitas futuras) - mantém
// compatibilidade se o usuário mais tarde acessar /Auth/Index diretamente.
function fn_LoginCkSetModal(cname, cvalue, exmins){
  const d = new Date();
  let hash = 0;

  for (const char of cvalue) {
    hash = (hash << 5) - hash + char.charCodeAt(0);
    hash |= 0;
  }

  hash = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    const r = Math.random() * 16 | 0,
        v = c === 'x' ? r : (r & 0x3 | 0x8);
    return `${cvalue}|${v.toString(16)}${hash}`;
  });

  d.setTime(d.getTime() + (exmins * 60 * 1000));
  let expires = `expires=${d.toUTCString()}`;
  document.cookie = `${cname}= ${hash};${expires};path=/`;
}

// Mesmo registro de geolocalização do acesso feito em /Auth/LoginLog hoje (ver
// pages-auth.js::fn_LoginAuthGeo) - best-effort, nunca bloqueia o login.
async function fn_LoginAuthGeoModal(userId){
  try {
    const response = await fetch('https://api.ipify.org?format=json');
    const data = await response.json();

    if (!data?.ip) return;

    const coords = await fn_ObterCoordenadasModal();
    const winPlatformVersion = await fn_ObterWinPlatformVersionModal();

    const params = { strIp: data.ip, srtId: userId };
    if (coords) {
      params.latitude  = coords.latitude;
      params.longitude = coords.longitude;
    }
    if (winPlatformVersion) {
      params.winPlatformVersion = winPlatformVersion;
    }

    await fetch('/Auth/LoginLog', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams(params),
    });
  } catch (error) {
    console.error('fn_LoginAuthGeoModal:', error);
  }
}

function fn_ObterCoordenadasModal(){
  return new Promise(function (resolve) {
    if (!navigator.geolocation) { resolve(null); return; }

    var resolvido = false;
    var timer = setTimeout(function () {
      if (!resolvido) { resolvido = true; resolve(null); }
    }, 4000);

    navigator.geolocation.getCurrentPosition(
      function (position) {
        if (resolvido) return;
        resolvido = true;
        clearTimeout(timer);
        resolve({ latitude: position.coords.latitude, longitude: position.coords.longitude });
      },
      function () {
        if (resolvido) return;
        resolvido = true;
        clearTimeout(timer);
        resolve(null);
      },
      { enableHighAccuracy: true, timeout: 3500, maximumAge: 300000 }
    );
  });
}

async function fn_ObterWinPlatformVersionModal(){
  try {
    if (!navigator.userAgentData?.getHighEntropyValues) return null;
    var info = await navigator.userAgentData.getHighEntropyValues(['platformVersion']);
    return info?.platformVersion || null;
  } catch (e) {
    return null;
  }
}

//#endregion

//#region app

// "Baixe nosso APP" instala o PWA que JÁ EXISTE no projeto (wwwroot/manifest.json +
// wwwroot/sw.js, o mesmo app autenticado) - não é um PWA à parte pro site institucional.
// Instalação de PWA nunca funciona entre origens diferentes, então isso só é possível
// quando esta página está servida do MESMO domínio desse manifest (aceca.tryasp.net,
// rota /Home/Web - ver <link rel="manifest"> no <head> de Web/Index.cshtml). Na versão
// estática standalone (Web/index.html, publicada em www.aceca.com.br), clicar no card
// simplesmente leva pro app de verdade, onde o Chrome oferece a instalação por lá.
function fn_AppInstall() {
    var botoes = document.querySelectorAll('.btn-app-install');
    if (!botoes.length) return;

    var estaDentroDoApp = fnEstaDentroDoApp();
    var eventoInstalacao = null;

    // Só registra o sw.js aqui dentro do domínio do app - sem isso (mesmo com o
    // <link rel="manifest">), o Chrome não considera a página instalável.
    if (estaDentroDoApp && 'serviceWorker' in navigator) {
        navigator.serviceWorker.register('/sw.js').catch(function (err) {
            console.warn('Falha ao registrar Service Worker do app:', err);
        });
    }

    // 1. Escuta o evento que o navegador dispara se o PWA for instalável (só existe no
    // Chrome/Edge - Android ou desktop; Safari/iOS nunca dispara isso, ver item 2).
    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        eventoInstalacao = e;
    });

    window.addEventListener('appinstalled', function () {
        console.log('PWA ACECA instalado com sucesso!');
        eventoInstalacao = null;
    });

    // 2. Um clique por cartão (Android/iOS) - antes só existia um único
    // document.querySelector (singular), que pegava só o 1º cartão e deixava o 2º
    // sem nenhum comportamento.
    botoes.forEach(function (botao) {
        botao.style.cursor = 'pointer';

        botao.addEventListener('click', function () {
            var ehCartaoIOS = (botao.querySelector('img')?.src || '').includes('store-apple');

            if (ehCartaoIOS) {
                Swal.fire({
                    title: 'Instalar no iPhone/iPad',
                    icon: 'info',
                    html: 'O iOS n&atilde;o permite instalar o app automaticamente. No Safari, toque em <b>Compartilhar</b> e depois em <b>"Adicionar &agrave; Tela de In&iacute;cio"</b>.',
                    confirmButtonText: 'Entendi'
                });
                return;
            }

            if (!estaDentroDoApp) {
                window.location.href = 'https://aceca.tryasp.net';
                return;
            }

            if (!eventoInstalacao) {
                Swal.fire({
                    title: 'Instalação indisponível',
                    icon: 'info',
                    html: 'Seu navegador já instalou o app ACECA, ou não oferece suporte à instalação neste momento.',
                    confirmButtonText: 'Entendi'
                });
                return;
            }

            eventoInstalacao.prompt();
            eventoInstalacao.userChoice.then(function (resultado) {
                console.log('Usuário escolheu: ' + resultado.outcome);
                eventoInstalacao = null;
            });
        });
    });
}

//#endregion
