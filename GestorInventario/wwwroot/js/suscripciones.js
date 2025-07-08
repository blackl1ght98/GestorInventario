document.addEventListener("DOMContentLoaded", function () {
    let subscriptionId;

    // Mostrar el modal de confirmación
    document.querySelectorAll(".user-action-button").forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();

            // Obtener el ID de la suscripción
            subscriptionId = button.getAttribute("data-subscription-id"); // Corregido el typo
            if (!subscriptionId) {
                console.error("Error: No se encontró el ID de la suscripción");
                alert("Error: No se pudo obtener el ID de la suscripción.");
                return;
            }
            console.log("Suscripción ID: " + subscriptionId);

            // Mostrar el modal usando Bootstrap 5
            const confirmModal = new bootstrap.Modal(document.getElementById("confirmModal"));
            document.getElementById("confirmMessage").textContent = `¿Estás seguro de que deseas cancelar la suscripción ${subscriptionId}?`;
            confirmModal.show();
        });
    });

    // Confirmar la acción del usuario
    document.getElementById("confirmActionBtn").addEventListener("click", function () {
        // Deshabilitar el botón para evitar múltiples clics
        const confirmButton = document.getElementById("confirmActionBtn");
        confirmButton.disabled = true;
        confirmButton.textContent = "Procesando...";

        // Ocultar el modal
        const confirmModal = bootstrap.Modal.getInstance(document.getElementById("confirmModal"));
        confirmModal.hide();

        // Llamar a la función para cancelar la suscripción
        cancelarSuscripcion(subscriptionId)
            .finally(() => {
                // Rehabilitar el botón después de la solicitud
                confirmButton.disabled = false;
                confirmButton.textContent = "Confirmar";
            });
    });

    // Función para cancelar la suscripción
    function cancelarSuscripcion(id) {
        const tokenElement = document.querySelector('meta[name="csrf-token"]');
        if (!tokenElement) {
            console.error("Error: No se encontró el token CSRF");
            alert("Error: No se pudo encontrar el token de seguridad.");
            return Promise.reject(new Error("Token CSRF no encontrado"));
        }
        const token = tokenElement.getAttribute("content");

        return fetch('/Paypal/CancelarSuscripcion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ subscription_id: id })
        })
            .then(response => {
                if (response.ok) {
                    return response.json();
                }
                return response.text().then(text => {
                    throw new Error(`Error al procesar la solicitud: ${text}`);
                });
            })
            .then(data => {
                if (data.success) {
                    // Mostrar mensaje de éxito antes de recargar
                    alert(data.message || "Suscripción cancelada con éxito.");
                    window.location.reload();
                } else {
                    console.error('Error:', data.errorMessage);
                    alert(`Error al cancelar la suscripción: ${data.errorMessage}`);
                }
            })
            .catch(error => {
                console.error('Error al procesar la solicitud:', error);
                alert(`Error al procesar la solicitud: ${error.message}`);
            });
    }
});