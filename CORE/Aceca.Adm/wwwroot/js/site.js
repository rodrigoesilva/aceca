/**
 *  Pages
 */

'use strict';

//#region Declare

let sessionData;

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
        
    })();
});

//#endregion

//#region CLOCK DATE
function fn_UpdateClock() {
    const now = new Date();
    // Format the date and time for display
    const timeString = now.toLocaleTimeString();
    const dateString = now.toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });

    document.getElementById('date-time').textContent = `${dateString} - ${timeString}`;

    //AUTH
    typeof Storage !== "undefined" ? fn_AuthSession() : window.location.href = 'https://www.aceca.com.br/';
}

//#endregion

//#region AUTH

function fn_AuthOut() {

    setTimeout(function () {
        Swal.fire({
            icon: 'success',
            title: `At&eacute mais ${sessionData?.nome?.split(" ")[0]}!`,
            html: `Nos vemos em breve`,
            focusConfirm: true,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-success waves-effect'
            }
        }).then((result) => {
            sessionStorage.removeItem('aceca_sessao');
            window.location.href = 'https://www.aceca.com.br/';
        });
    }, 500);
}

function fn_AuthSession() {
   sessionData = JSON.parse(sessionStorage.getItem("aceca_sessao"));
    //console.log("sessionData ::: ", sessionData.nome);
    if (sessionData !== null) {
        document.getElementById('tbNome').textContent = `${sessionData?.nome}`;
        document.getElementById('tbCargo').textContent = `${sessionData?.cargo}`;
    } else {
        sessionStorage.removeItem('aceca_sessao');
        window.location.href = 'https://www.aceca.com.br/';
    }
   
}

//#endregion