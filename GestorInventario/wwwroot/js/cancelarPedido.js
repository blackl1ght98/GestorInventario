/**
 * - Script encargado de cancelar el pedido
 */
document.addEventListener('DOMContentLoaded', () => {

    const modalEl = document.getElementById('confirmCancelarModal');
    const modal = new bootstrap.Modal(modalEl);

    const resultBox = document.getElementById('cancelResult');
    const confirmBtn = document.getElementById('confirmCancelBtn');

    let pedidoId, currentButton;

    document.querySelectorAll('.cancel-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            pedidoId = btn.dataset.pedidoId;
            currentButton = btn;

            // Reset estado del modal
            resultBox.className = 'alert d-none';
            resultBox.textContent = '';
            confirmBtn.disabled = false;
            confirmBtn.textContent = 'Cancelar';

            modal.show();
        });
    });

    confirmBtn.addEventListener('click', async () => {
        confirmBtn.disabled = true;
        confirmBtn.textContent = 'Procesando...';

        const tokenElement = document.querySelector('meta[name="csrf-token"]');
        if (!tokenElement) {
            console.error("Error: No se encontró el token CSRF");
            alert("Error: No se pudo encontrar el token de seguridad.");
            confirmBtn.disabled = false;
            confirmBtn.textContent = 'Cancelar';
            return;
        }
        const token = tokenElement.getAttribute("content");

        try {
            const response = await fetch(`/Pedidos/CancelarPedido?pedidoId=${pedidoId}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                }
            });

            // 1. Intentamos leer el JSON devuelto por el servidor (sea OK o error)
            let data = {};
            try {
                data = await response.json();
            } catch {
                // Por si la respuesta no viene en formato JSON
            }

            // 2. Si la respuesta no es OK, disparamos el error con el mensaje del servidor
            if (!response.ok) {
                const errorMsg = data.errorMessage || data.message || 'Error al cancelar el pedido';
                throw new Error(errorMsg);
            }

            // ✅ ÉXITO
            resultBox.className = 'alert alert-success mt-3';
            resultBox.textContent = '✔️ Pedido cancelado con éxito';

            currentButton.disabled = true;
            currentButton.classList.replace('btn-outline-warning', 'btn-outline-secondary');
            currentButton.textContent = 'Cancelado';

            setTimeout(() => {
                modal.hide();
                location.reload();
            }, 1200);

        } catch (error) {
            // ❌ ERROR (Muestra el mensaje exacto que provenga del backend)
            resultBox.className = 'alert alert-danger mt-3';
            resultBox.textContent = `❌ ${error.message || 'Error al cancelar el pedido'}`;

            confirmBtn.disabled = false;
            confirmBtn.textContent = 'Cancelar';
        }
    });
});