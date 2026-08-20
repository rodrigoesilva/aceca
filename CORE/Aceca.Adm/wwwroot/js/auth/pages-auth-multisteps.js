'use strict';

document.addEventListener('DOMContentLoaded', function () {

    const frm = document.getElementById('frmCadastroCompleto');
    if (!frm) return;

    const err = document.getElementById('msErr');
    const btn = document.getElementById('btnCadastroCompleto');
    const inputNascimento = document.getElementById('msNascimento');
    const inputTelefone = document.getElementById('msTelefone');
    const inputCep = document.getElementById('msCep');

    // Máscaras simples, sem depender de lib externa - só formatação visual, o
    // servidor não confia em nada disso pra validar (ver AuthController.
    // FinalizarCadastroCompleto/ParseDataNascimento/ParseTelefone).
    inputNascimento?.addEventListener('input', function () {
        let d = this.value.replace(/\D/g, '').slice(0, 8);
        let f = d;
        if (d.length > 4) f = `${d.slice(0, 2)}/${d.slice(2, 4)}/${d.slice(4)}`;
        else if (d.length > 2) f = `${d.slice(0, 2)}/${d.slice(2)}`;
        this.value = f;
    });

    inputTelefone?.addEventListener('input', function () {
        let d = this.value.replace(/\D/g, '').slice(0, 11);
        let f = d;
        if (d.length > 6) f = `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
        else if (d.length > 2) f = `(${d.slice(0, 2)}) ${d.slice(2)}`;
        this.value = f;
    });

    inputCep?.addEventListener('input', function () {
        let d = this.value.replace(/\D/g, '').slice(0, 8);
        this.value = d.length > 5 ? `${d.slice(0, 5)}-${d.slice(5)}` : d;
    });

    function setLoading(isLoading) {
        btn.disabled = isLoading;
        btn.querySelector('.btn-text').style.display = isLoading ? 'none' : 'inline';
        btn.querySelector('.btn-spinner').style.display = isLoading ? 'inline-flex' : 'none';
    }

    frm.addEventListener('submit', async function (e) {
        e.preventDefault();
        err.style.display = 'none';

        const nome = document.getElementById('msNome').value.trim();
        if (!nome) {
            err.textContent = '❌ Informe seu nome completo.';
            err.style.display = 'block';
            return;
        }

        setLoading(true);

        try {
            const body = {
                token: document.getElementById('msToken').value,
                email: document.getElementById('msEmail').value,
                nome,
                dataNascimento: inputNascimento.value.trim() || null,
                telefone: inputTelefone.value.trim() || null,
                endereco: document.getElementById('msEndereco').value.trim() || null,
                numero: document.getElementById('msNumero').value.trim() || null,
                complemento: document.getElementById('msComplemento').value.trim() || null,
                bairro: document.getElementById('msBairro').value.trim() || null,
                cidade: document.getElementById('msCidade').value.trim() || null,
                estado: document.getElementById('msEstado').value || null,
                cep: inputCep.value.trim() || null,
            };

            const response = await fetch('/Auth/FinalizarCadastroCompleto', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });

            const result = await response.json();

            if (response.ok && result.bResult) {
                Swal.fire({
                    icon: 'success',
                    title: 'Cadastro concluído!',
                    html: 'Seu acesso de teste está pronto. Faça login com o e-mail e a senha que você definiu.',
                    focusConfirm: true,
                    confirmButtonText: '<i class="ri-check-double-line"></i>&nbsp;Ok!',
                    customClass: { confirmButton: 'swal-btn-confirmar' },
                    buttonsStyling: false
                }).then(() => {
                    window.location.href = result.redirectUrl || '/Auth/Index';
                });
                return;
            }

            err.textContent = `❌ ${result?.message ?? 'Não foi possível concluir o cadastro.'}`;
            err.style.display = 'block';
            setLoading(false);
        } catch (ex) {
            console.error('FinalizarCadastroCompleto:', ex);
            err.textContent = '❌ Não foi possível concluir o cadastro. Tente novamente.';
            err.style.display = 'block';
            setLoading(false);
        }
    });
});
