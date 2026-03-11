//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`Todos os recursos terminaram o carregamento!`);
		
      const btn = document.getElementById('btn-proximos');
      //console.log("addEventListener btn ::", btn);
		
        filterEvents('upcoming',btn);
    })();
});

//#endregion

//#region Page

window.addEventListener('scroll',()=>{
  document.getElementById('navbar').classList.toggle('scrolled',window.scrollY>50);
});

function showToast(msg){
	//console.log("showToast msg ::", msg);
	
  const t = document.getElementById('toast');
	t.textContent=msg;
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

document.querySelectorAll('.nav-links a').forEach(a=>{
  a.addEventListener('click',()=> document.getElementById('navLinks').classList.remove('open'));
});

function toggleMenu(){ 
	document.getElementById('navLinks').classList.toggle('open'); 
}
//#endregion

//#region Modal Login

function openModal(e){
  if(e)e.preventDefault();
  document.getElementById('loginModal').classList.add('open');
  document.body.style.overflow='hidden';
}

function closeModal(){
  document.getElementById('loginModal').classList.remove('open');
  document.body.style.overflow='';
}

function closeModalOutside(e){
  if(e.target===document.getElementById('loginModal'))closeModal();
}

function handleLogin(e){
	console.log("handleLogin click - e :::", e);
  
	e.preventDefault();
	
	let user = null;
	let url = 'http://aceca.tryasp.net/Auth/Index';
	
	const em=document.getElementById('loginEmail').value.trim().toLowerCase();
	const pw=document.getElementById('loginPass').value;
	const btn=document.getElementById('loginBtn');
	const msg=document.getElementById('toast');
	
	msg.classList.remove('show');	

	if(!em||!pw){
		showToast('⚠️ Preencha e-mail e senha.');
		return;
	}
  console.log("handleLogin em ::", em);
  console.log("handleLogin pw ::", pw);
  
	btn.disabled = true; 
	btn.textContent = 'Verificando…';
	
	showToast('✓ Autenticando… (conectar ao backend MySQL)');
  
	setTimeout(closeModal,1500);
}

function verSenha() {
	console.log("verSenha :::");
  	const i = document.getElementById('loginPass');

  	i.type = i.type === 'password' ? 'text' : 'password';
}

async function fazerLogin(e) {
    console.log("fazerLogin - e :::", e);

	e.preventDefault();

	let user = null;
	let url = 'http://aceca.tryasp.net';

	const email = document.getElementById('loginEmail').value.trim().toLowerCase();
	const senha = document.getElementById('loginPass').value;
	const btn   = document.getElementById('loginBtn');
 	const msg 	= document.getElementById('toast');
	
	msg.classList.remove('show');	

	if(!email||!senha){
		showToast('⚠️ Preencha e-mail e senha.');
		return;
	}

  	console.log("handleLogin email ::", email);
  	console.log("handleLogin senha ::", senha);
  
	btn.disabled = true; 
	btn.textContent = 'Verificando…';

	try {
		//const res = await fetch('/api/auth/login', {

		const response = await fetch(`${url}/Auth/Login`, {
			method: 'POST',
			headers: {
				"Content-Type": 'application/json',
			},
			body: JSON.stringify({ email, senha }),
		});
		
		if (response.ok)
			user = await response.json();
	}catch {
	}

	if (!user) {
		console.log("user nao localizado:::");
		document.getElementById('loginEmail').value = '';
    	document.getElementById('loginPass').value = '';
	}

	btn.disabled = false; 
	btn.textContent = 'Entrar';

	if (user) {
		sessionStorage.setItem('aceca_sessao', JSON.stringify(user));

      	console.log("fazerLogin - user :::", user);
      	//console.log("fazerLogin - sessionStorage :::", sessionStorage);

      	//window.location.href = 'dashboard.html';
      	window.location.href = '/Marcas/Index';
  	} else {
		let msgErr = '❌ E-mail ou senha incorretos. Verifique suas credenciais.';
    	showToast(msgErr);

      	console.log("fazerLogin ::: ", msgErr);

		document.getElementById('loginEmail').value = '';
    	document.getElementById('loginPass').value = '';
  	}
}

//#endregion

//#region Form Page

const form = document.getElementById('contactForm');
const backdrop = document.getElementById('modalBackdrop');
const modalText = document.getElementById('modalText');
const modalOk = document.getElementById('modalOk');
let redirectTimer = null;


let selectedFiles=[];

function handleContactSubmit1(){
  const n=document.getElementById('cf-name').value.trim();
  const e=document.getElementById('cf-email').value.trim();
  const s=document.getElementById('cf-motivo').value;
  const m=document.getElementById('cf-message').value.trim();
  
  if(!n||!e||!s||!m){
	  showToast('⚠️ Preencha todos os campos obrigatórios.');
	  return;
  }
  
  showToast('✓ Mensagem enviada! Responderemos em breve.');
  
  ['cf-name','cf-email','cf-phone','cf-message'].forEach(id=>document.getElementById(id).value='');
  
  document.getElementById('cf-motivo').value='';
  document.getElementById('filePreview').innerHTML='';
  
  selectedFiles=[];
}

function handleFiles(input){
	//console.log("handleFiles input ::", input);
  Array.from(input.files).forEach(f=>{
  
    if(selectedFiles.length>=3){
        showToast('⚠️ Máximo de 3 imagens.')
      ;return;
    }
	
    if(!selectedFiles.find(x=>x.name===f.name))
      selectedFiles.push(f);
  });
  
  renderPreviews();
  input.value='';
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
// Fomulario Envio
async function handleContactSubmit(e){
	e.preventDefault();

	const formElement = document.getElementById('contact-form');
	
	if (formElement) {
        const formData = new FormData(formElement);
       
		const n=document.getElementById('cf-name').value.trim();
		const i=document.getElementById('cf-email').value.trim();
		const s=document.getElementById('cf-motivo').value;
		const m=document.getElementById('cf-message').value.trim();
		
		if(!n||!i||!s||!m){
		  showToast('⚠️ Preencha todos os campos obrigatórios.');
		  return;
		}

		// Funcao Envio
		await fetchData(formElement, formData);
	
    } else {
		showToast('⚠️ Form element não encontrado!');
		return;
    }
}


async function fetchData(formElement, formData) {
	try {
	  
		const jsonData = Object.fromEntries(formData.entries());
		//console.log('fetchData jsonData:: ' , jsonData);
		  
			fetch(formElement.action, {
				method: formElement.method,
				headers: {
				  'Content-Type': 'application/json'
				},
				body: JSON.stringify(jsonData)
			})
			.then(response => response.json())
			.then(result  => {
				//console.log('Result :: ' , result );
				
				if (!result .ok) {
					alert('Não foi possível enviar agora. Tente novamente.');
					throw new Error(`Response status: ${result .status}`);
					return;
				}else{
					//console.log('Result message:: ' , result .message);

					showToast('✓ Mensagem enviada! Responderemos em breve.');
	  
					['cf-name','cf-email','cf-phone','cf-message'].forEach(id=>document.getElementById(id).value='');
				  
					document.getElementById('cf-motivo').value='';
					document.getElementById('filePreview').innerHTML='';
				  
					selectedFiles=[];
				}
			})
			.catch(error => {
				//console.error('catch error:', error);
			
				//const dataErr = JSON.parse(error);
				//console.error('catch dataErr:', dataErr);
			});
	} catch (err) {
		//console.error('catch err:', err);
		
		const data = JSON.parse(err);
		//console.error('catch data:', data);
	}
}

//#endregion

