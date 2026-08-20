'use strict';

document.addEventListener('DOMContentLoaded', function () {

    const passoEscolha = document.getElementById('passoEscolha');
    const frmCadastro = document.getElementById('frmCadastroTeste');
    const err = document.getElementById('registerErr');
    const btnContinuarEmail = document.getElementById('btnContinuarEmail');
    const btnVoltarEscolha = document.getElementById('btnVoltarEscolha');
    const inputCpf = document.getElementById('rCpf');
    const btn = document.getElementById('btnContinuarCadastro');

    if (!frmCadastro) return;

    btnContinuarEmail?.addEventListener('click', function () {
        passoEscolha.style.display = 'none';
        frmCadastro.style.display = 'block';
        document.getElementById('rNome')?.focus();
    });

    btnVoltarEscolha?.addEventListener('click', function () {
        frmCadastro.style.display = 'none';
        passoEscolha.style.display = 'block';
        err.style.display = 'none';
    });

    // Máscara simples de CPF (000.000.000-00) - só formatação visual, a validação real
    // (dígito verificador) é feita no servidor (Helper/CpfHelper.cs).
    inputCpf?.addEventListener('input', function () {
        let digitos = this.value.replace(/\D/g, '').slice(0, 11);
        let formatado = digitos;
        if (digitos.length > 9) formatado = `${digitos.slice(0, 3)}.${digitos.slice(3, 6)}.${digitos.slice(6, 9)}-${digitos.slice(9)}`;
        else if (digitos.length > 6) formatado = `${digitos.slice(0, 3)}.${digitos.slice(3, 6)}.${digitos.slice(6)}`;
        else if (digitos.length > 3) formatado = `${digitos.slice(0, 3)}.${digitos.slice(3)}`;
        this.value = formatado;
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
        setLoading(true);

        try {
            const coords = await obterCoordenadas();

            const body = {
                nome: document.getElementById('rNome').value.trim(),
                cpf: inputCpf.value.trim(),
                email: document.getElementById('rEmail').value.trim(),
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

            err.textContent = `❌ ${result?.message ?? 'Não foi possível concluir o cadastro.'}`;
            err.style.display = 'block';
            setLoading(false);
        } catch (ex) {
            console.error('CadastroTesteIniciar:', ex);
            err.textContent = '❌ Não foi possível concluir o cadastro. Tente novamente.';
            err.style.display = 'block';
            setLoading(false);
        }
    });
});
