'use strict';

document.addEventListener('DOMContentLoaded', function () {

    const passoEscolha = document.getElementById('passoEscolha');
    const frmCadastro = document.getElementById('frmCadastroTeste');
    const err = document.getElementById('registerErr');
    const btnContinuarEmail = document.getElementById('btnContinuarEmail');
    const btnVoltarEscolha = document.getElementById('btnVoltarEscolha');
    const inputCpf = document.getElementById('rCpf');
    const inputEmail = document.getElementById('rEmail');
    const cpfErro = document.getElementById('rCpfErro');
    const emailErro = document.getElementById('rEmailErro');
    const btn = document.getElementById('btnContinuarCadastro');

    if (!frmCadastro) return;

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
            const coords = await obterCoordenadas();

            const body = {
                cpf: digitosCpf,
                email: inputEmail.value.trim(),
                latitude: coords ? String(coords.latitude) : null,
                longitude: coords ? String(coords.longitude) : null,
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
                // Swal próprio (não swalWithBootstrapButtons, que é pra confirmação
                // destrutiva com o botão de ação apagado) - aqui "Sim, fazer login" é a
                // ação mais provável, então é ela que fica em destaque.
                Swal.fire({
                    title: 'E-mail já cadastrado',
                    icon: 'info',
                    html: 'Este e-mail já pertence a um sócio. Deseja ir para a tela de login?',
                    showCancelButton: true,
                    confirmButtonText: '<i class="ri-login-box-line"></i> &nbsp;Sim, fazer login',
                    cancelButtonText: 'Não',
                    buttonsStyling: false,
                    customClass: { confirmButton: 'swal-btn-confirmar', cancelButton: 'swal-btn-cancelar' }
                }).then((swalResult) => {
                    if (swalResult.isConfirmed) {
                        window.location.href = '/Auth/Index';
                    } else {
                        inputCpf.value = '';
                        inputEmail.value = '';
                        cpfErro.textContent = '';
                        emailErro.textContent = '';
                        inputCpf.focus();
                    }
                });
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
});
