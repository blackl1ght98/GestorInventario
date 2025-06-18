document.addEventListener("DOMContentLoaded", function () {
    let subscriptionId;

    // Mostrar el modal de confirmación
    document.querySelectorAll(".user-action-button").forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();

            // Obtener el ID de la suscripción
            subscriptionId = button.getAttribute("data-suscription-id");
            console.log("Suscripción ID: " + subscriptionId);

            // Mostrar el modal usando Bootstrap 5
            const confirmModal = new bootstrap.Modal(document.getElementById("confirmModal"));
            document.getElementById("confirmMessage").textContent = `¿Estás seguro de que deseas cancelar la suscripción ${subscriptionId}?`;
            confirmModal.show();
        });
    });

    // Confirmar la acción del usuario
    document.getElementById("confirmActionBtn").addEventListener("click", function () {
        // Ocultar el modal
        const confirmModal = bootstrap.Modal.getInstance(document.getElementById("confirmModal"));
        confirmModal.hide();

        // Llamar a la función para cancelar la suscripción
        cancelarSuscripcion(subscriptionId);
    });

    // Función para cancelar la suscripción
    function cancelarSuscripcion(id) {
        fetch('/Paypal/CancelarSuscripcion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ subscription_id: id })
        })
            .then(response => {
                if (response.ok) {
                    return response.json();
                } else {
                    throw new Error('Error al procesar la solicitud');
                }
            })
            .then(data => {
                if (data.success) {
                    // Recargar la página si la cancelación es exitosa
                    window.location.reload();
                } else {
                    console.error('Error:', data.errorMessage);
                }
            })
            .catch(error => {
                console.error('Error al procesar la solicitud:', error);
            });
    }


});