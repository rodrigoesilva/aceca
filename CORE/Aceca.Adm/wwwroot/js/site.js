//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`SITE - Todos os recursos terminaram o carregamento!`);

        // Update the clock immediately on load, and then every second
        fn_UpdateClock();
        setInterval(fn_UpdateClock, 1000); // Updates every 1000 milliseconds
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
}

//#endregion