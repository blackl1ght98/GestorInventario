document.addEventListener("DOMContentLoaded", function () {
    var userId;

    // Abrir el modal al hacer clic en "Cambiar Rol"
    var changeRoleButtons = document.querySelectorAll(".change-role-button");
    changeRoleButtons.forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            userId = button.dataset.userId;

            var changeRoleModal = document.getElementById("changeRoleModal");
            changeRoleModal.classList.add("show");
            changeRoleModal.style.display = "block";
        });
    });

    // Confirmar el cambio de rol
    var confirmChangeRoleBtn = document.getElementById("confirmChangeRoleBtn");
    confirmChangeRoleBtn.addEventListener("click", function () {
        confirmChangeRoleBtn.disabled = true;
        var loadingMessage = document.getElementById("loadingMessage");
        loadingMessage.classList.remove("d-none");
        loadingMessage.textContent = "Procesando...";

        var roleId = document.getElementById("roleSelect").value;

        fetch(`/Admin/CambiarRol`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ id: parseInt(userId), rolId: parseInt(roleId) })
        })
            .then(response => response.json())
            .then(data => {
                loadingMessage.classList.add("d-none");
                if (data.success) {
                    //alert(data.message); // Mostrar mensaje de éxito
                    location.reload(); // Recargar la página para reflejar los cambios
                } else {
                    alert(data.errorMessage || "El servidor ha tardado en responder, por favor intenta de nuevo");
                    confirmChangeRoleBtn.disabled = false;
                }
            })
            .catch(error => {
                console.error('Error al procesar la solicitud:', error);
                loadingMessage.classList.add("d-none");
                loadingMessage.textContent = "Error al procesar la solicitud";
                confirmChangeRoleBtn.disabled = false;
            });
    });

    // Cerrar el modal
    var modalClose = document.querySelector('.modal');
    modalClose.addEventListener('click', function (event) {
        if (event.target === modalClose) {
            var changeRoleModal = document.getElementById("changeRoleModal");
            changeRoleModal.classList.remove("show");
            changeRoleModal.style.display = "none";
        }
    });

    var closeButton = document.querySelector('.btn-close');
    closeButton.addEventListener('click', function () {
        var changeRoleModal = document.getElementById("changeRoleModal");
        changeRoleModal.classList.remove("show");
        changeRoleModal.style.display = "none";
    });

    var cancelButton = document.querySelector('.btn-secondary');
    cancelButton.addEventListener('click', function () {
        var changeRoleModal = document.getElementById("changeRoleModal");
        changeRoleModal.classList.remove("show");
        changeRoleModal.style.display = "none";
    });
});
