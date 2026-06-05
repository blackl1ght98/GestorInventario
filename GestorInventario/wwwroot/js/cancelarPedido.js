
document.addEventListener('DOMContentLoaded', () => {

    const modalEl = document.getElementById('confirmCancelarModal');
    const modal = new bootstrap.Modal(modalEl);

    const resultBox = document.getElementById('cancelResult');
    const confirmBtn = document.getElementById('confirmCancelBtn');

    let pedidoId,  currentButton;

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
            return Promise.reject(new Error("Token CSRF no encontrado"));
        }
        const token = tokenElement.getAttribute("content");
        try {
            const response = await fetch(`/Pedidos/CancelarPedido?pedidoId=${pedidoId}`, {
                method: 'POST',
                headers: {
                  
                    'RequestVerificationToken': token
                }
               
            });

            if (!response.ok) throw new Error();

            // ✅ ÉXITO
            resultBox.className = 'alert alert-success mt-3';
            resultBox.textContent = '✔️ Pedido cancelado con exito';

            currentButton.disabled = true;
            currentButton.classList.replace('btn-outline-warning', 'btn-outline-secondary');
            currentButton.textContent = 'Cancelado';

            setTimeout(() => {
                modal.hide();
                location.reload();
            }, 1200);

        } catch {
            // ❌ ERROR
            resultBox.className = 'alert alert-danger mt-3';
            resultBox.textContent = '❌ Error al cancelar el pedido';

            confirmBtn.disabled = false;
            confirmBtn.textContent = 'Cancelar';
        }
    });
});
