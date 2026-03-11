/**
 * Script encargado de realizar el rembolso
 */
document.addEventListener('DOMContentLoaded', () => {

    const modalEl = document.getElementById('confirmRefundTotalModal');
    const modal = new bootstrap.Modal(modalEl);

    const resultBox = document.getElementById('refundResult');
    const confirmBtn = document.getElementById('confirmRefundTotalBtn');

    let pedidoId, currency, currentButton;

    document.querySelectorAll('.refund-total-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            pedidoId = btn.dataset.pedidoId;
            currency = btn.dataset.currency;
            currentButton = btn;

            // Reset estado del modal
            resultBox.className = 'alert d-none';
            resultBox.textContent = '';
            confirmBtn.disabled = false;
            confirmBtn.textContent = 'Confirmar reembolso';

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
            const response = await fetch('/Payment/RefundSale', {
                method: 'POST',
                headers: {
                    'Content-Type':
                        'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    PedidoId: pedidoId,
                    Currency: currency
                })
            });

            if (!response.ok) throw new Error();

            // ✅ ÉXITO
            resultBox.className = 'alert alert-success mt-3';
            resultBox.textContent = '✔️ Reembolso realizado con éxito';

            currentButton.disabled = true;
            currentButton.classList.replace('btn-outline-warning', 'btn-outline-secondary');
            currentButton.textContent = 'Reembolsado';

            setTimeout(() => {
                modal.hide();
                location.reload();
            }, 1200);

        } catch {
            // ❌ ERROR
            resultBox.className = 'alert alert-danger mt-3';
            resultBox.textContent = '❌ Error al realizar el reembolso';

            confirmBtn.disabled = false;
            confirmBtn.textContent = 'Confirmar reembolso';
        }
    });
});
