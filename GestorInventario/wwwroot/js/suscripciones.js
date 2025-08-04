document.addEventListener("DOMContentLoaded", function () {
    let subscriptionId;

    /* Mostrar el modal de cancelación 
    El motivo por el cual se usa forEach en lugar de un solo querySelector es para que se pueda aplicar a múltiples botones
    que pueden existir en la página, permitiendo que cada botón tenga su propio evento de clic. 
    */
    document.querySelectorAll(".user-action-button").forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            subscriptionId = button.getAttribute("data-subscription-id");
            if (!subscriptionId) {
                console.error("Error: No se encontró el ID de la suscripción");
                alert("Error: No se pudo obtener el ID de la suscripción.");
                return;
            }
            console.log("Suscripción ID: " + subscriptionId);
            const confirmModal = new bootstrap.Modal(document.getElementById("confirmModal"));
            document.getElementById("confirmMessage").textContent = `¿Estás seguro de que deseas cancelar la suscripción ${subscriptionId}?`;
            confirmModal.show();
        });
    });

    /*Confirmar la cancelación 
    Como solo hay un botón de confirmación, no es necesario usar forEach aquí.
    */
    document.getElementById("confirmActionBtn").addEventListener("click", function () {
        const confirmButton = document.getElementById("confirmActionBtn");
        confirmButton.disabled = true;
        confirmButton.textContent = "Procesando...";
        const confirmModal = bootstrap.Modal.getInstance(document.getElementById("confirmModal"));
        confirmModal.hide();
        cancelarSuscripcion(subscriptionId)
            .finally(() => {
                confirmButton.disabled = false;
                confirmButton.textContent = "Confirmar";
            });
    });

    // Mostrar el modal de suspensión
    document.querySelectorAll(".suspend-action-button").forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            subscriptionId = button.getAttribute("data-subscription-id");
            if (!subscriptionId) {
                console.error("Error: No se encontró el ID de la suscripción");
                alert("Error: No se pudo obtener el ID de la suscripción.");
                return;
            }
            console.log("Suscripción ID: " + subscriptionId);
            const suspendModal = new bootstrap.Modal(document.getElementById("suspendModal"));
            document.getElementById("suspendSubscriptionId").textContent = subscriptionId;
            document.getElementById("suspendReason").value = ""; // Limpiar el campo
            suspendModal.show();
        });
    });
    // Mostrar el modal de activación
    document.querySelectorAll(".activate-action-button").forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            subscriptionId = button.getAttribute("data-subscription-id");
            if (!subscriptionId) {
                console.error("Error: No se encontró el ID de la suscripción");
                alert("Error: No se pudo obtener el ID de la suscripción.");
                return;
            }
            const activateModal = new bootstrap.Modal(document.getElementById("activateModal"));
            document.getElementById("activateSubscriptionId").textContent = subscriptionId;
            document.getElementById("activateReason").value = ""; // Limpiar el campo
            activateModal.show();
        });
    });

    // Confirmar la suspensión
    document.getElementById("confirmSuspendBtn").addEventListener("click", function () {
        const confirmButton = document.getElementById("confirmSuspendBtn");
        const reasonInput = document.getElementById("suspendReason");
        const reason = reasonInput.value.trim();

        // Validar el motivo
        if (!reason) {
            reasonInput.classList.add("is-invalid");
            return;
        }
        reasonInput.classList.remove("is-invalid");

        confirmButton.disabled = true;
        confirmButton.textContent = "Procesando...";
        const suspendModal = bootstrap.Modal.getInstance(document.getElementById("suspendModal"));
        suspendModal.hide();

        suspenderSuscripcion(subscriptionId, reason)
            .finally(() => {
                confirmButton.disabled = false;
                confirmButton.textContent = "Suspender";
            });
    });
    // Confirmar la activación
    document.getElementById("confirmActivateBtn").addEventListener("click", function () {
        const confirmButton = document.getElementById("confirmActivateBtn");
        const reasonInput = document.getElementById("activateReason");
        const reason = reasonInput.value.trim();
        if (!reason) {
            reasonInput.classList.add("is-invalid");
            return;
        }
        reasonInput.classList.remove("is-invalid");
        confirmButton.disabled = true;
        confirmButton.textContent = "Procesando...";
        const activateModal = bootstrap.Modal.getInstance(document.getElementById("activateModal"));
        activateModal.hide();
        activarSubscripcion(subscriptionId, reason).finally(() => {
            confirmButton.disabled = false;
            confirmButton.textContent = "Activar";
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

    // Función para suspender la suscripción
    function suspenderSuscripcion(id, reason) {
        const tokenElement = document.querySelector('meta[name="csrf-token"]');
        if (!tokenElement) {
            console.error("Error: No se encontró el token CSRF");
            alert("Error: No se pudo encontrar el token de seguridad.");
            return Promise.reject(new Error("Token CSRF no encontrado"));
        }
        const token = tokenElement.getAttribute("content");

        return fetch('/Paypal/SuspenderSuscripcion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ id: id, reason: reason })
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
                    alert(data.message || "Suscripción suspendida con éxito.");
                    window.location.reload();
                } else {
                    console.error('Error:', data.errorMessage);
                    alert(`Error al suspender la suscripción: ${data.errorMessage}`);
                }
            })
            .catch(error => {
                console.error('Error al procesar la solicitud:', error);
                alert(`Error al procesar la solicitud: ${error.message}`);
            });
    }
    // Función para activar la suscripción
    function activarSubscripcion(id, reason) {
        const tokenElement = document.querySelector('meta[name="csrf-token"]');
        if (!tokenElement) {
            console.error("Error: No se encontró el token CSRF");
            alert("Error: No se pudo encontrar el token de seguridad.");
            return Promise.reject(new Error("Token CSRF no encontrado"));
        }
        const token = tokenElement.getAttribute("content");
        return fetch('/Paypal/ActivarSuscripcion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ id: id, reason: reason })
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
                    alert(data.message || "Suscripción activada con éxito.");
                    window.location.reload();
                } else {
                    console.error('Error:', data.errorMessage);
                    alert(`Error al activar la suscripción: ${data.errorMessage}`);
                }
            })
            .catch(error => {
                console.error('Error al procesar la solicitud:', error);
                alert(`Error al procesar la solicitud: ${error.message}`);
            });
    }

});