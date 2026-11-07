document.addEventListener("DOMContentLoaded", function () {
    actualizarBadgeNotificaciones();

    // Polling cada 30 segundos para mantener el badge actualizado en vivo
    setInterval(actualizarBadgeNotificaciones, 30000);
});

function actualizarBadgeNotificaciones() {
    fetch('/Notification/ObtenerConteoNoLeidas')
        .then(response => {
            if (!response.ok) throw new Error("Error al obtener notificaciones");
            return response.json();
        })
        .then(data => {
            const badge = document.getElementById("notificacionesBadge");
            if (!badge) return;

            const nuevoConteo = data.count;

            if (nuevoConteo > 0) {
                badge.textContent = nuevoConteo > 99 ? "99+" : nuevoConteo;
                badge.style.display = "inline-block";
            } else {
                badge.style.display = "none";
            }
        })
        .catch(error => console.error("Error actualizando badge de notificaciones:", error));
}