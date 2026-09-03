'use strict';

document.addEventListener('DOMContentLoaded', function () {

    const passoEscolha = document.getElementById('passoEscolha');
    const frmCadastro = document.getElementById('frmCadastroTeste');
    const frmCadastroGoogle = document.getElementById('frmCadastroGoogle');
    const err = document.getElementById('registerErr');
    const btnContinuarEmail = document.getElementById('btnContinuarEmail');
    const btnVoltarEscolha = document.getElementById('btnVoltarEscolha');
    const inputCpf = document.getElementById('rCpf');
    const inputEmail = document.getElementById('rEmail');
    const cpfErro = document.getElementById('rCpfErro');
    const emailErro = document.getElementById('rEmailErro');
    const btn = document.getElementById('btnContinuarCadastro');

    if (!frmCadastro) return;

    // Swal reaproveitado nos dois fluxos (e-mail e Google) pra quando o e-mail já
    // pertence a um sócio - "Sim, fazer login" é a ação mais provável, por isso fica em
    // destaque (não usa swalWithBootstrapButtons, que é pensado pra confirmação
    // destrutiva com o botão de ação apagado).
    function mostrarSwalEmailJaCadastrado(aoRecusar) {
        Swal.fire({
            title: 'E-mail já cadastrado',
            icon: 'info',
            html: `Este e-mail já pertence a um sócio. <br/> Deseja realizar o login?`,
            showCancelButton: true,
            confirmButtonText: '<i class="ri-login-box-line"></i> &nbsp;Sim, fazer login',
            cancelButtonText: `<i class="ri-check-double-line"></i> &nbsp; N&atilde;o, cancelar!`,
            buttonsStyling: false,
            customClass: { confirmButton: 'swal-btn-confirmar', cancelButton: 'swal-btn-cancelar' }
        }).then((swalResult) => {
            if (swalResult.isConfirmed) {
                window.location.href = '/Auth/Index';
            } else if (aoRecusar) {
                aoRecusar();
            }
        });
    }

    // CPF já usado no teste grátis (trava antifraude - ver AuthController.CadastroTesteIniciar/
    // CadastroTesteGoogleFinalizar) - Swal com link clicável pra Solicitar Associação, em vez
    // do banner de erro comum (onde a URL aparecia como texto puro, sem poder ser clicada).
    function mostrarSwalCpfJaUtilizouTeste() {
        Swal.fire({
            title: 'Teste grátis já utilizado',
            icon: 'info',
            html: 'Este CPF já utilizou o período de teste grátis.<br/>Solicite sua associação na <a href="https://www.aceca.com.br/#contato" target="_blank" rel="noopener">ACECA</a>.',
            confirmButtonText: 'Entendi',
            buttonsStyling: false,
            customClass: { confirmButton: 'swal-btn-confirmar' }
        });
    }

    // Flags vindas do servidor (GoogleCallback/RegisterCover) - ver comentário no cshtml.
    const flags = document.getElementById('registerServerFlags');
    if (flags?.dataset.emailJaCadastrado === '1') {
        mostrarSwalEmailJaCadastrado();
    } else if (flags?.dataset.googleTokenExpirado === '1') {
        err.textContent = '❌ A sessão do Google expirou. Clique em "Continuar com o Google" novamente.';
        err.style.display = 'block';
    }

    btnContinuarEmail?.addEventListener('click', function () {
        passoEscolha.style.display = 'none';
        frmCadastro.style.display = 'block';
        inputCpf?.focus();
    });

    btnVoltarEscolha?.addEventListener('click', function () {
        frmCadastro.style.display = 'none';
        passoEscolha.style.display = 'block';
        err.style.display = 'none';
    });

    // Dígito verificador de CPF - mesmo algoritmo de Helper/CpfHelper.cs. Só dá feedback
    // imediato pro usuário (evita ida e volta ao servidor pra descobrir um CPF óbvio errado);
    // quem garante de verdade é a validação no servidor, que não confia no que veio do cliente.
    function cpfValido(digitos) {
        if (digitos.length !== 11) return false;
        if (new Set(digitos).size === 1) return false;

        const nums = digitos.split('').map(Number);
        function calcularDigito(qtd) {
            let soma = 0, peso = qtd + 1;
            for (let i = 0; i < qtd; i++) soma += nums[i] * peso--;
            const resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        return calcularDigito(9) === nums[9] && calcularDigito(10) === nums[10];
    }

    // Máscara de CPF (000.000.000-00) + checa dígito verificador ao completar os 11 dígitos.
    inputCpf?.addEventListener('input', function () {
        let digitos = this.value.replace(/\D/g, '').slice(0, 11);
        let formatado = digitos;
        if (digitos.length > 9) formatado = `${digitos.slice(0, 3)}.${digitos.slice(3, 6)}.${digitos.slice(6, 9)}-${digitos.slice(9)}`;
        else if (digitos.length > 6) formatado = `${digitos.slice(0, 3)}.${digitos.slice(3, 6)}.${digitos.slice(6)}`;
        else if (digitos.length > 3) formatado = `${digitos.slice(0, 3)}.${digitos.slice(3)}`;
        this.value = formatado;

        if (digitos.length < 11) { cpfErro.textContent = ''; return; }
        cpfErro.textContent = cpfValido(digitos) ? '' : 'CPF inválido - confira os números digitados.';
    });

    // Feedback de e-mail visivelmente mal formado ao sair do campo (o servidor ainda faz a
    // validação real - formato + domínio existente + bloqueio de e-mail descartável).
    inputEmail?.addEventListener('blur', function () {
        const valor = this.value.trim();
        emailErro.textContent = (valor && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(valor))
            ? 'E-mail inválido - confira se digitou corretamente.'
            : '';
    });

    // IP real do visitante via ipify.org (mesmo padrão de fn_LoginAuthGeo em pages-auth.js) -
    // HttpContext.Connection.RemoteIpAddress no servidor fica preso a loopback/IP interno
    // atrás de proxy/IIS em produção, então o IP de verdade precisa vir do próprio navegador.
    // Best-effort: se a chamada falhar (rede, bloqueio de terceiros), segue sem IP - o
    // servidor cai de volta pro RemoteIpAddress como antes.
    async function obterIpReal() {
        try {
            const response = await fetch('https://api.ipify.org?format=json');
            const data = await response.json();
            return data?.ip || null;
        } catch (e) {
            return null;
        }
    }

    // Geolocalização best-effort (mesmo espírito do login - fn_LoginAuthGeo em
    // pages-auth.js): timeout curto, nunca trava o envio do formulário.
    function obterCoordenadas() {
        return new Promise(function (resolve) {
            if (!navigator.geolocation) { resolve(null); return; }
            let resolvido = false;
            const timer = setTimeout(function () { if (!resolvido) { resolvido = true; resolve(null); } }, 4000);
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
                { timeout: 4000 }
            );
        });
    }

    function setLoading(isLoading) {
        btn.disabled = isLoading;
        btn.querySelector('.btn-text').style.display = isLoading ? 'none' : 'inline';
        btn.querySelector('.btn-spinner').style.display = isLoading ? 'inline-flex' : 'none';
    }

    frmCadastro.addEventListener('submit', async function (e) {
        e.preventDefault();
        err.style.display = 'none';

        // <form novalidate> desliga a validação nativa do HTML (mesmo padrão do login em
        // pages-auth.js) - então o "required" do checkbox de Termos não bloqueia nada por
        // conta própria; a checagem precisa ser explícita aqui.
        const chkTermos = document.getElementById('rTermos');
        if (!chkTermos.checked) {
            err.textContent = '❌ É obrigatório concordar com os Termos de Uso e a Política de Privacidade para continuar.';
            err.style.display = 'block';
            chkTermos.focus();
            return;
        }

        const digitosCpf = inputCpf.value.replace(/\D/g, '');
        if (!cpfValido(digitosCpf)) {
            cpfErro.textContent = 'CPF inválido - confira os números digitados.';
            inputCpf.focus();
            return;
        }

        setLoading(true);

        try {
            const [coords, ipReal] = await Promise.all([obterCoordenadas(), obterIpReal()]);

            const body = {
                cpf: digitosCpf,
                email: inputEmail.value.trim(),
                latitude: coords ? String(coords.latitude) : null,
                longitude: coords ? String(coords.longitude) : null,
                ip: ipReal,
            };

            const response = await fetch('/Auth/CadastroTesteIniciar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });

            const result = await response.json();

            if (response.ok && result.bResult) {
                window.location.href = `/Auth/VerifyEmailCover?email=${encodeURIComponent(result.email)}`;
                return;
            }

            setLoading(false);

            // E-mail já pertence a um sócio - em vez do banner de erro comum, pergunta se
            // a pessoa quer ir direto pro login (Sim) ou só limpar os campos e continuar
            // tentando outro e-mail (Não).
            if (result?.type === 'EMAIL_JA_CADASTRADO') {
                mostrarSwalEmailJaCadastrado(function () {
                    inputCpf.value = '';
                    inputEmail.value = '';
                    cpfErro.textContent = '';
                    emailErro.textContent = '';
                    inputCpf.focus();
                });
                return;
            }

            if (result?.type === 'CPF_JA_UTILIZOU_TESTE') {
                mostrarSwalCpfJaUtilizouTeste();
                return;
            }

            err.textContent = `❌ ${result?.message ?? 'Não foi possível concluir o cadastro.'}`;
            err.style.display = 'block';
        } catch (ex) {
            console.error('CadastroTesteIniciar:', ex);
            err.textContent = '❌ Não foi possível concluir o cadastro. Tente novamente.';
            err.style.display = 'block';
            setLoading(false);
        }
    });

    // ── Continuação via Google (CPF-only, e-mail já verificado pelo Google) ──
    if (frmCadastroGoogle) {
        const inputGgCpf = document.getElementById('ggCpf');
        const ggCpfErro = document.getElementById('ggCpfErro');
        const ggToken = document.getElementById('ggToken');
        const btnGg = document.getElementById('btnContinuarCadastroGoogle');

        inputGgCpf?.addEventListener('input', function () {
            let digitos = this.value.replace(/\D/g, '').slice(0, 11);
            let formatado = digitos;
            if (digitos.length > 9) formatado = `${digitos.slice(0, 3)}.${digitos.slice(3, 6)}.${digitos.slice(6, 9)}-${digitos.slice(9)}`;
            else if (digitos.length > 6) formatado = `${digitos.slice(0, 3)}.${digitos.slice(3, 6)}.${digitos.slice(6)}`;
            else if (digitos.length > 3) formatado = `${digitos.slice(0, 3)}.${digitos.slice(3)}`;
            this.value = formatado;

            if (digitos.length < 11) { ggCpfErro.textContent = ''; return; }
            ggCpfErro.textContent = cpfValido(digitos) ? '' : 'CPF inválido - confira os números digitados.';
        });

        function setLoadingGg(isLoading) {
            btnGg.disabled = isLoading;
            btnGg.querySelector('.btn-text').style.display = isLoading ? 'none' : 'inline';
            btnGg.querySelector('.btn-spinner').style.display = isLoading ? 'inline-flex' : 'none';
        }

        frmCadastroGoogle.addEventListener('submit', async function (e) {
            e.preventDefault();
            err.style.display = 'none';

            const chkTermosGg = document.getElementById('ggTermos');
            if (!chkTermosGg.checked) {
                err.textContent = '❌ É obrigatório concordar com os Termos de Uso e a Política de Privacidade para continuar.';
                err.style.display = 'block';
                chkTermosGg.focus();
                return;
            }

            const digitosCpf = inputGgCpf.value.replace(/\D/g, '');
            if (!cpfValido(digitosCpf)) {
                ggCpfErro.textContent = 'CPF inválido - confira os números digitados.';
                inputGgCpf.focus();
                return;
            }

            setLoadingGg(true);

            try {
                const [coords, ipReal] = await Promise.all([obterCoordenadas(), obterIpReal()]);

                const body = {
                    cpf: digitosCpf,
                    googleToken: ggToken.value,
                    latitude: coords ? String(coords.latitude) : null,
                    longitude: coords ? String(coords.longitude) : null,
                    ip: ipReal,
                };

                const response = await fetch('/Auth/CadastroTesteGoogleFinalizar', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body),
                });

                const result = await response.json();

                if (response.ok && result.bResult) {
                    window.location.href = result.redirectUrl;
                    return;
                }

                setLoadingGg(false);

                if (result?.type === 'EMAIL_JA_CADASTRADO') {
                    mostrarSwalEmailJaCadastrado();
                    return;
                }

                if (result?.type === 'CPF_JA_UTILIZOU_TESTE') {
                    mostrarSwalCpfJaUtilizouTeste();
                    return;
                }

                err.textContent = `❌ ${result?.message ?? 'Não foi possível concluir o cadastro.'}`;
                err.style.display = 'block';
            } catch (ex) {
                console.error('CadastroTesteGoogleFinalizar:', ex);
                err.textContent = '❌ Não foi possível concluir o cadastro. Tente novamente.';
                err.style.display = 'block';
                setLoadingGg(false);
            }
        });
    }
});
