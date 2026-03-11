document.addEventListener('DOMContentLoaded', () => {
    const actionsMap = {
        'Alta': 'AltaUsuarioPost',
        'Baja': 'BajaUsuarioPost'
    };

    // Inicializar el modal de Bootstrap una sola vez
    const confirmModalElement = document.getElementById('confirmModal');
    const confirmModal = new bootstrap.Modal(confirmModalElement); 

    // Abrir modal
    document.querySelectorAll('.user-action-button').forEach(button => {
        button.addEventListener('click', e => {
            e.preventDefault();

            const userId = button.dataset.userId;
            const actionType = button.dataset.userAction;

            if (!userId || !actionType) {
                console.error('Botón sin data-user-id o data-user-action');
                return;
            }

            openConfirmModal(userId, actionType);
        });
    });

    const openConfirmModal = (userId, actionType) => {
        const actionText = actionType.toLowerCase();
        const message = document.getElementById('confirmMessage');
        message.textContent = `¿Estás seguro de que deseas dar de ${actionText} al usuario con ID ${userId}?`;

        const confirmBtn = document.getElementById('confirmActionBtn');
        confirmBtn.dataset.userId = userId;
        confirmBtn.dataset.actionType = actionType;

        // Mostrar modal con Bootstrap API
        confirmModal.show();
    };

    // Resetear botón
    const resetButton = () => {
        const confirmBtn = document.getElementById('confirmActionBtn');
        confirmBtn.innerHTML = 'Confirmar';
        confirmBtn.disabled = false;
    };

    // Confirmar acción
    const confirmBtn = document.getElementById('confirmActionBtn');
    const loadingMessage = document.getElementById('loadingMessage');

    confirmBtn.addEventListener('click', async () => {
        const userId = confirmBtn.dataset.userId;
        const actionType = confirmBtn.dataset.actionType;

        if (!userId || !actionType) {
            loadingMessage.textContent = 'Error: Datos no disponibles';
            resetButton();
            return;
        }

        confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Procesando...';
        confirmBtn.disabled = true;
        loadingMessage.textContent = 'Procesando...';

        const action = actionsMap[actionType];
        if (!action) {
            loadingMessage.textContent = 'Error: Acción no definida';
            resetButton();
            return;
        }
        const tokenElement = document.querySelector('meta[name="csrf-token"]');
        if (!tokenElement) {
            console.error("Error: No se encontró el token CSRF");
            alert("Error: No se pudo encontrar el token de seguridad.");
            return Promise.reject(new Error("Token CSRF no encontrado"));
        }
        const token = tokenElement.getAttribute("content");
        try {
            const response = await fetch(`/Admin/${action}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ id: userId })
            });

            const data = await response.json();

            if (data.success) {
                location.reload();
            } else {
                loadingMessage.textContent = data.errorMessage || 'El servidor tardó en responder';
                resetButton();
            }
        } catch (error) {
            console.error('Error:', error);
            loadingMessage.textContent = 'Error al procesar la solicitud';
            resetButton();
        }
    });

    // Opcional: resetear modal al cerrarse (por si acaso)
    confirmModalElement.addEventListener('hidden.bs.modal', () => {
        loadingMessage.textContent = '';
        resetButton();
    });
});