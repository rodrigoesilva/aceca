/**
 * Proposta -> Editar
 **/

//#region Declare

let varSessionDataSite = 1;
const _ck = "aceca_cookie";

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`SITE - Todos os recursos terminaram o carregamento!`);

        /*
        const successCallback = (position) => {
            console.log("Latitude: ", position.coords.latitude);
            console.log("Longitude: ", position.coords.longitude);
        };

        const errorCallback = (error) => {
            console.error("Error Code: " + error.code + " - " + error.message);
        };

        // Optional configuration
        const options = {
            enableHighAccuracy: true, // Use GPS if available for better precision
            timeout: 5000,            // Wait up to 5 seconds for a response
            maximumAge: 0             // Do not use a cached position
        };

        navigator.geolocation.getCurrentPosition(successCallback, errorCallback, options);
        */

        // Update the clock immediately on load, and then every second
        var pgLogin = document.querySelector(".pg-login");

        if (pgLogin === null) {
            fn_AuthSession();
            setInterval(fn_CheckSession, 1000);
            fn_ImageProtect();
        }

        $('.btn-logout').on('click', function () {
            //console.log("cclick logout ::: ");
            fn_AuthOut();
        });

        $('.btn-voltar-home').on('click', function () {
            window.location.href = 'https://www.aceca.com.br/';
        });

    })();
});

//#endregion

//#region CLOCK DATE
function fn_UpdateClock() {
    console.log(`::`);
    const timeString = new Date().toLocaleTimeString();
    const dateString = new Date().toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });

    if(document.getElementById('date-time') !== null)
        document.getElementById('date-time').textContent = `${dateString} - ${timeString}`;

    //AUTH
    typeof Storage !== "undefined" ? fn_AuthSession() : window.location.href = 'https://www.aceca.com.br/';
}

//#endregion

//#region AUTH

function fn_AuthOut() {
    //console.log(`fn_AuthOut ::`);
    try {
        $.ajax({
            url: '/Auth/Logout',
            type: 'GET',
            success: function (result) {
                // 1. Limpa estado do cliente (sem redirect aqui)
                document.cookie = `${_ck}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
                sessionStorage.removeItem('aceca_sessao');
                try {
                    localStorage.removeItem('aceca_last_activity');
                    localStorage.removeItem('aceca_abs_exp');
                } catch (e) { /* ignore */ }

                // 2. Exibe Swal e só então redireciona
                Swal.fire({
                    icon: 'success',
                    title: `At&eacute; mais ${varSessionDataSite?.nome?.split(" ")[0] ?? ''}!`,
                    html: `Nos vemos em breve`,
                    focusConfirm: true,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-success waves-effect'
                    }
                }).then(() => {
                    window.location.href = '/Auth/Index';
                });
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                console.log(`response XMLHttpRequest :: ${XMLHttpRequest}`);
                return false;
            }
        });
    }
    catch (ex) {
        console.log(`response ex :: ${ex}`);
    }
}

let _sessionInvalidating = false;

function fn_CheckSession() {
    if (_sessionInvalidating) return;
    // Aguarda a primeira confirmação de sessão (varSessionDataSite sai de 1 após fn_SetSessionData)
    if (!varSessionDataSite || varSessionDataSite === 1) return;

    var raw = sessionStorage.getItem("aceca_sessao");
    if (!raw) { fn_SessionInvalid(); return; }

    try {
        var d = JSON.parse(raw);
        if (!d || !d.nameIdentifier) fn_SessionInvalid();
    } catch (e) {
        fn_SessionInvalid();
    }
}

function fn_SessionInvalid() {
    if (_sessionInvalidating) return;
    _sessionInvalidating = true;
    document.cookie = `${_ck}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
    sessionStorage.removeItem('aceca_sessao');
    try {
        localStorage.removeItem('aceca_last_activity');
        localStorage.removeItem('aceca_abs_exp');
    } catch (e) { /* ignore */ }
    window.location.href = '/Auth/AccessDenied';
}

function fn_AuthSession(callback) {
    if (sessionStorage?.getItem("aceca_sessao") !== null) {
        var sessionData = JSON.parse(sessionStorage.getItem("aceca_sessao"));
        if (sessionData !== null) {
            fn_SetSessionData(sessionData);
            if (callback) callback();
        } else {
            fn_RestoreSession(callback);
        }
    } else {
        fn_RestoreSession(callback);
    }
}
function fn_RestoreSession(callback) {
    $.ajax({
        url: '/Auth/GetSessionData',
        type: 'GET',
        success: function (data) {
            sessionStorage.setItem('aceca_sessao', JSON.stringify(data));
            fn_SetSessionData(data);
            if (callback) callback();
        },
        error: function () {
            fn_CleanUser();
        }
    });
}
function fn_SetSessionData(data) {
    varSessionDataSite = data;
    document.getElementById('hdSocioLogadoId').value = `${data?.nameIdentifier}`;
    document.getElementById('hdIsPerfil').value = `${data?.isPerfil}`;
    if (document.getElementById('tbAvatar')) document.getElementById('tbAvatar').textContent = `${data?.avatar}`;
    document.getElementById('tbNome').textContent = `${data?.nome}`;
    document.getElementById('tbCargo').textContent = `${data?.cargo}`;
}
function fn_CleanUser() {
    document.cookie = `${_ck}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
    sessionStorage.removeItem('aceca_sessao');
    try {
        localStorage.removeItem('aceca_last_activity');
        localStorage.removeItem('aceca_abs_exp');
    } catch (e) { /* ignore */ }
    window.location.href = '/Auth/Index';
}
function fn_CkRemove(_ck) {
    document.cookie = `${_ck}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
}
//#endregion

//#region PROTEÇÃO DE IMAGEM

function fn_ImageProtect() {

    // Seletor unificado: imagens do acervo na grid e no modal de zoom
    var SEL = '.cmyImg, #imgZoomTarget';

    // ── helpers ──────────────────────────────────────────────────────────────

    function _imgMeta(img) {
        return {
            codigoAceca: img?.getAttribute('data-codigo') || img?.getAttribute('data-id') || img?.alt || '',
            src:         img?.src || img?.getAttribute('data-src') || ''
        };
    }

    function _swalAviso() {
        Swal.fire({
            icon: 'warning',
            title: 'Ação não permitida',
            html: 'As imagens do acervo ACECA são protegidas por direitos autorais.<br><b>Cópia, download e captura de tela são proibidos.</b>',
            confirmButtonText: '<i class="ri-check-line"></i>&nbsp;Entendi',
            customClass: { confirmButton: 'btn btn-label-warning waves-effect' }
        });
    }

    function _reportar(img, acao) {
        var meta = _imgMeta(img);
        var ts   = new Date().toISOString();
        var fd   = new FormData();
        fd.append('codigoAceca', meta.codigoAceca || '');
        fd.append('imagemSrc',   meta.src         || '');
        fd.append('acao',        acao);
        fd.append('timestamp',   ts);
        fetch('/Auth/ReportImageAccess', { method: 'POST', body: fd }).catch(function () {});
    }

    // Cria overlay com selo 'PLÁGIO PROIBIDO' (aceca_plagio.jpeg) em mosaico diagonal
    // sobre a imagem alvo. Retorna a função de remoção para limpeza posterior.
    function _watermark(img) {
        var rect = img.getBoundingClientRect();
        var w    = rect.width  || img.naturalWidth  || 300;
        var h    = rect.height || img.naturalHeight || 300;
        var diag = Math.ceil(Math.sqrt(w * w + h * h) * 1.2);

        var overlay = document.createElement('div');
        overlay.style.cssText = [
            'position:fixed',
            'left:' + (rect.left + window.scrollX) + 'px',
            'top:'  + (rect.top  + window.scrollY) + 'px',
            'width:'  + w + 'px',
            'height:' + h + 'px',
            'overflow:hidden',
            'z-index:2147483647',
            'pointer-events:none',
            'background-color:rgba(255,255,255,0.45)'
        ].join(';');

        var stampSize = Math.max(90, Math.min(w / 2.2, 240));
        var stamp = document.createElement('div');
        stamp.style.cssText = [
            'position:absolute',
            'top:50%',
            'left:50%',
            'width:'  + diag + 'px',
            'height:' + diag + 'px',
            'transform:translate(-50%,-50%) rotate(-45deg)',
            'background-image:url(/img/aceca_plagio.jpeg)',
            'background-repeat:repeat',
            'background-size:' + stampSize + 'px auto',
            'opacity:0.6'
        ].join(';');

        overlay.appendChild(stamp);
        document.body.appendChild(overlay);
        return function () { overlay.remove(); };
    }

    // ── CSS base: impede seleção e arraste nativo em todas as imagens protegidas ──

    (function () {
        var s = document.createElement('style');
        s.textContent = SEL + ' { -webkit-user-select:none!important; user-select:none!important; -webkit-touch-callout:none!important; -webkit-user-drag:none!important; }';
        document.head.appendChild(s);
    })();

    // ── detecção de DevTools aberto ───────────────────────────────────────────
    // Compara outerWidth/Height vs innerWidth/Height. Quando DevTools está
    // encaixado (docked), ele reduz o innerWidth ou innerHeight. Threshold de
    // 160px é conservador: a UI do browser (barra de endereços + abas) ocupa
    // ~70–90px; DevTools ocupa no mínimo 200px.

    var _devOpen = false;

    function _devCheck() {
        var wDiff = window.outerWidth  - window.innerWidth;
        var hDiff = window.outerHeight - window.innerHeight;
        var open  = wDiff > 160 || hDiff > 160;

        if (open && !_devOpen) {
            _devOpen = true;
            _reportar(null, 'devtools-open');
            document.querySelectorAll(SEL).forEach(function (el) {
                el.style.filter = 'blur(14px)';
            });
        } else if (!open && _devOpen) {
            _devOpen = false;
            document.querySelectorAll(SEL).forEach(function (el) {
                el.style.filter = '';
            });
        }
    }

    setInterval(_devCheck, 800);

    // ── variáveis de controle ─────────────────────────────────────────────────

    var _removeWatermark = null;
    var _lastImg         = null;

    // ── rastrear última imagem clicada ────────────────────────────────────────

    $(document).on('click', SEL, function () {
        _lastImg = this;
    });

    // ── contextmenu: bloqueia botão direito em TODA a página ─────────────────
    // Exceções: input, textarea, select e <a> — preserva colar e abrir link.
    // Isso impede "Salvar como", "Imprimir" e "Salvar imagem como" do browser.

    document.addEventListener('contextmenu', function (e) {
        var tag = (e.target.tagName || '').toUpperCase();
        if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || tag === 'A') return;
        e.preventDefault();
        e.stopPropagation();
        if (tag === 'IMG' && $(e.target).is(SEL)) {
            _reportar(e.target, 'contextmenu');
            _swalAviso();
        }
    }, true);

    // ── drag: bloqueia arraste em TODAS as imagens ────────────────────────────

    $(document).on('dragstart', 'img', function (e) {
        e.preventDefault();
        if ($(this).is(SEL)) _reportar(this, 'dragstart');
    });

    // ── copy ─────────────────────────────────────────────────────────────────

    document.addEventListener('copy', function (e) {
        var active = document.activeElement;
        if (active && active.matches && active.matches(SEL)) {
            e.preventDefault();
            _reportar(active, 'copy');
            _swalAviso();
        }
    }, true);

    // ── teclas proibidas ──────────────────────────────────────────────────────

    document.addEventListener('keydown', function (e) {
        var key  = e.key  || '';
        var code = e.code || '';

        var isPrint      = (key === 'PrintScreen' || code === 'PrintScreen');
        var isF12        = (key === 'F12'  || code === 'F12');
        var isCtrlShiftI = e.ctrlKey && e.shiftKey && (key === 'I' || key === 'i');
        var isCtrlShiftJ = e.ctrlKey && e.shiftKey && (key === 'J' || key === 'j');
        var isCtrlShiftC = e.ctrlKey && e.shiftKey && (key === 'C' || key === 'c');
        var isCtrlShiftK = e.ctrlKey && e.shiftKey && (key === 'K' || key === 'k');
        var isCtrlSave   = e.ctrlKey && !e.shiftKey && (key === 's' || key === 'S');
        var isCtrlSrc    = e.ctrlKey && !e.shiftKey && (key === 'u' || key === 'U');
        var isCtrlPrint  = e.ctrlKey && !e.shiftKey && (key === 'p' || key === 'P');

        var isDevTools = false;// isF12 || isCtrlShiftI || isCtrlShiftJ || isCtrlShiftC || isCtrlShiftK;
        var isBlocked  = isPrint || isDevTools || isCtrlSave || isCtrlSrc || isCtrlPrint;

        if (!isBlocked) return;

        e.preventDefault();
        e.stopImmediatePropagation();

        var img   = _lastImg;
        var label = isPrint       ? 'printscreen'
                  : isF12         ? 'f12'
                  : isCtrlShiftI  ? 'ctrl+shift+i'
                  : isCtrlShiftJ  ? 'ctrl+shift+j'
                  : isCtrlShiftC  ? 'ctrl+shift+c'
                  : isCtrlShiftK  ? 'ctrl+shift+k'
                  : isCtrlSave    ? 'ctrl+s'
                  : isCtrlSrc     ? 'ctrl+u'
                  :                 'ctrl+p';

        if (img) _reportar(img, label);

        if (isDevTools) {
            _swalAviso();
        } else if (img) {
            if (_removeWatermark) _removeWatermark();
            _removeWatermark = _watermark(img);
            setTimeout(function () {
                if (_removeWatermark) { _removeWatermark(); _removeWatermark = null; }
            }, 4000);
            _swalAviso();
        }
    }, true);

    // ── beforeprint / afterprint ──────────────────────────────────────────────

    window.addEventListener('beforeprint', function () {
        if (_lastImg) _reportar(_lastImg, 'print');
        document.querySelectorAll(SEL).forEach(function (el) {
            el.style.visibility = 'hidden';
        });
    });

    window.addEventListener('afterprint', function () {
        document.querySelectorAll(SEL).forEach(function (el) {
            el.style.visibility = '';
        });
    });
}

//#endregion