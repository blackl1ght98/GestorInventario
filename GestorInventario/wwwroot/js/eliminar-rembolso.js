document.addEventListener("DOMContentLoaded", function () {
    let rembolsoId;
    let actionType;

    const actionsMap = {
        "Eliminar": "EliminarRembolso"
    };

    const actionButtons = document.querySelectorAll(".user-action-button");

    actionButtons.forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();

            // obtener datos del botón
            rembolsoId = button.dataset.rembolsoId;
            actionType = button.dataset.userAction;

            console.log("Rembolso ID:", rembolsoId, "Acción:", actionType);

            // abrir modal
            const confirmModal = document.getElementById("confirmModal");
            confirmModal.classList.add("show");
            confirmModal.style.display = "block";

            // actualizar mensaje
            const confirmMessage = document.getElementById("confirmMessage");
            confirmMessage.textContent = `¿Estás seguro de que deseas ${actionType.toLowerCase()} el reembolso con ID ${rembolsoId}?`;

            // resetear loading message
            const loadingMessage = document.getElementById("loadingMessage");
            loadingMessage.classList.add("d-none");
            loadingMessage.textContent = "";
        });
    });

    const confirmActionBtn = document.getElementById("confirmActionBtn");
    confirmActionBtn.addEventListener("click", function () {
        confirmActionBtn.disabled = true;

        const loadingMessage = document.getElementById("loadingMessage");
        loadingMessage.classList.remove("d-none");
        loadingMessage.textContent = "Procesando...";

        const action = actionsMap[actionType];
        if (action) {
            cambiarEstadoUsuario(rembolsoId, action);
        } else {
            console.error("Acción no definida");
            loadingMessage.textContent = "Error: Acción no definida";
            confirmActionBtn.disabled = false;
        }
    });

    function cambiarEstadoUsuario(id, action) {
        const loadingMessage = document.getElementById("loadingMessage");
        const confirmActionBtn = document.getElementById("confirmActionBtn");

        fetch(`/Rembolso/${action}`, {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ id: id })
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    location.reload();
                } else {
                    console.error(data.errorMessage);
                    loadingMessage.textContent = "El servidor ha tardado en responder, por favor intente de nuevo";
                    confirmActionBtn.disabled = false;
                }
            })
            .catch(error => {
                console.error("Error al procesar la solicitud:", error);
                loadingMessage.textContent = "Error al procesar la solicitud";
                confirmActionBtn.disabled = false;
            });
    }

    // cerrar modal al hacer clic fuera
    const modal = document.querySelector(".modal");
    modal.addEventListener("click", function (event) {
        if (event.target === modal) {
            modal.classList.remove("show");
            modal.style.display = "none";
        }
    });
});
