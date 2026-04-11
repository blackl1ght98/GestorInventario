document.addEventListener('DOMContentLoaded', () => {

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

        // Guardar datos en el botón Confirmar
        const confirmBtn = document.getElementById('confirmActionBtn');
        confirmBtn.dataset.rembolsoId = rembolsoId;
        confirmBtn.dataset.userAction = actionType;
    };

    // Abrir modal al hacer clic en el botón Eliminar
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

    // Confirmar acción (Eliminar)
    document.getElementById('confirmActionBtn').addEventListener('click', async () => {
        const btn = document.getElementById('confirmActionBtn');
        const loading = document.getElementById('loadingMessage');

        btn.disabled = true;
        loading.classList.remove('d-none');
        loading.textContent = 'Procesando...';

        const rembolsoId = btn.dataset.rembolsoId;

        if (!rembolsoId) {
            loading.textContent = 'Error: ID no disponible';
            btn.disabled = false;
            return;
        }

        const tokenElement = document.querySelector('meta[name="csrf-token"]');
        const token = tokenElement ? tokenElement.getAttribute("content") : null;

        try {
            const response = await fetch(`/Rembolso/${rembolsoId}`, {
                method: 'DELETE',
                headers: {
                    'RequestVerificationToken': token
                }
            });

            const data = await response.json();

            if (data.success) {
                location.reload();
            } else {
                loading.textContent = data.errorMessage || 'Error al procesar la solicitud';
                btn.disabled = false;
            }
        } catch (error) {
            console.error('Error:', error);
            loading.textContent = 'Error al procesar la solicitud';
            btn.disabled = false;
        }
    });

    // Cerrar modal al hacer clic fuera
    document.querySelector('.modal').addEventListener('click', e => {
        const modal = document.getElementById('confirmModal');
        if (e.target === modal) {
            modal.classList.remove('show');
            modal.style.display = 'none';
            resetModal();
        }
    });

    // Botón Cancelar del modal
    document.querySelector('#confirmModal .btn-outline-secondary').addEventListener('click', () => {
        const modal = document.getElementById('confirmModal');
        modal.classList.remove('show');
        modal.style.display = 'none';
        resetModal();
    });

    // Función para resetear el modal
    function resetModal() {
        const btn = document.getElementById('confirmActionBtn');
        btn.disabled = false;
        btn.innerHTML = 'Confirmar';
        const loading = document.getElementById('loadingMessage');
        loading.classList.add('d-none');
        loading.textContent = '';
    }
});