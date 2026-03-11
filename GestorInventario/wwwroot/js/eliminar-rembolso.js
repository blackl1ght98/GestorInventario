
document.addEventListener('DOMContentLoaded', () => {
    const actionsMap = {
        'Eliminar': 'EliminarRembolso'
    };

    const openConfirmModal = (rembolsoId, actionType) => {
        const actionText = actionType.toLowerCase();
        document.getElementById('confirmMessage').textContent =
            `¿Estás seguro de que deseas ${actionText} el reembolso con ID ${rembolsoId}?`;

        const modal = document.getElementById('confirmModal');
        modal.classList.add('show');
        modal.style.display = 'block';

        // Resetear loading
        const loading = document.getElementById('loadingMessage');
        loading.classList.add('d-none');
        loading.textContent = '';

        // Guardar datos en el botón Confirmar ← ESTO FALTABA
        const confirmBtn = document.getElementById('confirmActionBtn');
        confirmBtn.dataset.rembolsoId = rembolsoId;
        confirmBtn.dataset.userAction = actionType;
    };

    document.querySelectorAll('.user-action-button').forEach(button => {
        button.addEventListener('click', e => {
            e.preventDefault();
            const rembolsoId = button.dataset.rembolsoId;
            const actionType = button.dataset.userAction;
            if (!rembolsoId || !actionType) {
                console.error('Botón sin data-rembolso-id o data-user-action');
                return;
            }
            openConfirmModal(rembolsoId, actionType);
        });
    });

    document.getElementById('confirmActionBtn').addEventListener('click', async () => {
        const btn = document.getElementById('confirmActionBtn');
        const loading = document.getElementById('loadingMessage');

        btn.disabled = true;
        loading.classList.remove('d-none');
        loading.textContent = 'Procesando...';

        const rembolsoId = btn.dataset.rembolsoId;
        const actionType = btn.dataset.userAction;

        if (!rembolsoId || !actionType) {
            loading.textContent = 'Error: Datos no disponibles';
            btn.disabled = false;
            return;
        }

        const action = actionsMap[actionType];
        if (!action) {
            loading.textContent = 'Error: Acción no definida';
            btn.disabled = false;
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
            const response = await fetch(`/Rembolso/${action}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ id: rembolsoId })
            });

            const data = await response.json();

            if (data.success) {
                location.reload();
            } else {
                loading.textContent = data.errorMessage || 'El servidor tardó en responder';
                btn.disabled = false;
            }
        } catch (error) {
            console.error('Error:', error);
            loading.textContent = 'Error al procesar la solicitud';
            btn.disabled = false;
        }
    });

    document.querySelector('.modal').addEventListener('click', e => {
        const modal = document.getElementById('confirmModal');
        if (e.target === modal) {
            modal.classList.remove('show');
            modal.style.display = 'none';
            const btn = document.getElementById('confirmActionBtn');
            btn.disabled = false;
            btn.innerHTML = 'Confirmar';
            document.getElementById('loadingMessage').classList.add('d-none');
            document.getElementById('loadingMessage').textContent = '';
        }
    });
    // Al final del DOMContentLoaded
    document.querySelector('#confirmModal .btn-outline-secondary').addEventListener('click', () => {
        const modal = document.getElementById('confirmModal');
        modal.classList.remove('show');
        modal.style.display = 'none';

        // Reset completo
        const btn = document.getElementById('confirmActionBtn');
        btn.disabled = false;
        btn.innerHTML = 'Confirmar';
        document.getElementById('loadingMessage').classList.add('d-none');
        document.getElementById('loadingMessage').textContent = '';
    });
});