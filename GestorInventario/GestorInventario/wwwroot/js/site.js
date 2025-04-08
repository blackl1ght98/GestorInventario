var tiempoDeInactividad = 200000; // ~3.33 minutos (en milisegundos)
var temporizador;
var contadorIntervalo;
var tiempoRestante = tiempoDeInactividad;

window.addEventListener('mousemove', reiniciarTemporizador, false);
window.addEventListener('mousedown', reiniciarTemporizador, false);
window.addEventListener('keypress', reiniciarTemporizador, false);
window.addEventListener('DOMMouseScroll', reiniciarTemporizador, false);
window.addEventListener('mousewheel', reiniciarTemporizador, false);
window.addEventListener('touchmove', reiniciarTemporizador, false);
window.addEventListener('MSPointerMove', reiniciarTemporizador, false);

function reiniciarTemporizador() {
    clearTimeout(temporizador);
    clearInterval(contadorIntervalo);
    tiempoRestante = tiempoDeInactividad;
    temporizador = setTimeout(cerrarSesion, tiempoDeInactividad);

    contadorIntervalo = setInterval(() => {
        tiempoRestante -= 1000;
        if (tiempoRestante >= 0) {
            console.log(`Tiempo restante para cerrar sesión: ${Math.floor(tiempoRestante / 1000)} segundos`);
        } else {
            clearInterval(contadorIntervalo);
        }
    }, 1000);
}

async function cerrarSesion() {
    clearInterval(contadorIntervalo);
    console.log('Cerrando sesión por inactividad...');

    try {
        const response = await fetch('/Auth/Logout', {
            method: 'GET',
            credentials: 'include'
        });

        const result = await response.json();
        console.log("El resultado es: " + result.message);

        if (response.ok) {
            console.log("La respuesta es: " + response);
            window.location.href = 'https://localhost:7056/Auth/Login'; // Redirige a /Auth/Login
        } else {
            console.error('Error al cerrar sesión:', result.message);
            window.location.href = 'https://localhost:7056/Auth/Login'; // Redirige a /Auth/Login incluso si falla
        }
    } catch (error) {
        console.error('Error en la solicitud de logout:', error);
        window.location.href = 'https://localhost:7056/Auth/Login'; // Fallback a /Auth/Login
    }
}

reiniciarTemporizador();