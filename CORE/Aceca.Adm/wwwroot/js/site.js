/**
 *  Pages
 */

'use strict';

//#region Declare

let sessionData;
const _ck = "aceca_cookie";

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`SITE - Todos os recursos terminaram o carregamento!`);

        // Update the clock immediately on load, and then every second
        var pgLogin = document.querySelector(".pg-login");

        if (pgLogin === null) {
            fn_UpdateClock();
            setInterval(fn_UpdateClock, 1000); // Updates every 1000 milliseconds
        }

        $('.btn-logout').on('click', function () {
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

    try {

        $.busyLoadFull("show");

        $.ajax(
            {
                url: '/Auth/Logout',
                type: 'POST',
                success: function (result) {
                    console.log(`result ::  ${result}`);
                    fn_CleanUser();

                    $.busyLoadFull("hide");

                    Swal.fire({
                        icon: 'success',
                        title: `At&eacute mais ${sessionData?.nome?.split(" ")[0]}!`,
                        html: `Nos vemos em breve`,
                        focusConfirm: true,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-success waves-effect'
                        }
                    }).then((resultBye) => {
                        window.location.href = 'https://www.aceca.com.br/';
                    });
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    console.log(`response XMLHttpRequest ::  ${XMLHttpRequest}`);
                    $.busyLoadFull("hide");

                    return false;
                }
            });
    }
    catch (ex) {
        console.log(`response ex ::  ${ex}`);
    }
}

function fn_AuthSession() {
    if (sessionStorage?.getItem("aceca_sessao") !== null) {
        sessionData = JSON.parse(sessionStorage.getItem("aceca_sessao"));
        
        if (sessionData !== null) {
            document.getElementById('hdSocioId').value = `${sessionData?.nameIdentifier}`;
            document.getElementById('tbNome').textContent = `${sessionData?.nome}`;
            document.getElementById('tbCargo').textContent = `${sessionData?.cargo}`;
        } else {
            fn_CleanUser();
        }
    }else {
        fn_CleanUser();
    }  
}
function fn_CleanUser() {
    document.cookie = `${_ck}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
    sessionStorage.removeItem('aceca_sessao');
    window.location.href = 'https://www.aceca.com.br/';
}
function fn_CkRemove(_ck) {
    document.cookie = `${_ck}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
}
//#endregion