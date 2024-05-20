// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//Si el usuario cierra el navegador sin cerrar sesion llama al metodo logout
//Esta variable almacena la hora actual
var lastAction = Date.now();
/*Aquí se añade un controlador de eventos al objeto window para el evento click. Cada vez que el usuario hace clic en cualquier parte de 
la ventana, la función se activa y actualiza lastAction con el tiempo actual. */
window.addEventListener('click', function (event) {
    lastAction = Date.now();
});

window.addEventListener('beforeunload', function (event) {
    /*Esta parte del código añade otro controlador de eventos al objeto window para el evento beforeunload, que se dispara justo antes de 
    que la página se vaya a descargar. Cuando se dispara este evento, comprueba si han pasado más de 1000 milisegundos (1 segundo) desde 
    la última acción del usuario.
    */
    if (Date.now() - lastAction > 1000) { 
        /*Si han pasado más de 1000 milisegundos desde la última acción del usuario, entonces se crea un nuevo objeto FormData y 
        se envía un beacon al servidor en la ruta ‘/Auth/Logout’. Esto se hace para informar al servidor de que el usuario está saliendo 
        de la página. */
        var data = new FormData();
        navigator.sendBeacon('/Auth/LogoutScript', data);
    }
});




