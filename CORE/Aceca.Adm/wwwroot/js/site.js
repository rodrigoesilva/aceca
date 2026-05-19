/*
 *  Pages
 */

'use strict';

//#region Declare

let _sessionData = null;
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
            const intervalId = setInterval(() => {
                //console.log("Executando a cada 2 segundos");
                //fn_UpdateClock();
            }, 1000);
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
    console.log(`fn_AuthOut ::`);
    try {

       // $.busyLoadFull("show");

        $.ajax(
            {
                url: '/Auth/Logout',
                type: 'GET',
                success: function (result) {
                    //console.log(`result ::  ${result}`);
                    fn_CleanUser();

                    //$.busyLoadFull("hide");

                    Swal.fire({
                        icon: 'success',
                        title: `At&eacute mais ${_sessionData?.nome?.split(" ")[0]}!`,
                        html: `Nos vemos em breve`,
                        focusConfirm: true,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-success waves-effect'
                        }
                    }).then((resultBye) => {
                        //console.log(`resultBye ::  ${resultBye}`);
                        window.location.href = 'https://www.aceca.com.br/';
                    });
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    console.log(`response XMLHttpRequest ::  ${XMLHttpRequest}`);
                    //$.busyLoadFull("hide");

                    return false;
                }
            });
    }
    catch (ex) {
        console.log(`response ex ::  ${ex}`);
    }
}

function fn_AuthSession() {

    //console.log(`fn_AuthSession sessionStorage ::`, sessionStorage);

    if (sessionStorage?.getItem("aceca_sessao") !== null) {
        _sessionData = JSON.parse(sessionStorage.getItem("aceca_sessao"));

        //console.log(`fn_AuthSession _sessionData ::`, _sessionData);

        if (_sessionData !== null) {
            document.getElementById('hdSocioLogadoId').value = `${_sessionData?.nameIdentifier}`;
            document.getElementById('hdIsPerfil').value = `${_sessionData?.isPerfil}`;
            document.getElementById('tbAvatar').textContent = `${_sessionData?.avatar}`;
            document.getElementById('tbNome').textContent = `${_sessionData?.nome}`;
            document.getElementById('tbCargo').textContent = `${_sessionData?.cargo}`;
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