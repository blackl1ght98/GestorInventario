document.addEventListener('DOMContentLoaded', function () {
    const modal = new bootstrap.Modal(document.getElementById('confirmRefundModal'));
    const refundReasonSelect = document.getElementById('refundReason');
    const otherReasonContainer = document.getElementById('otherReasonContainer');
    const otherReasonInput = document.getElementById('otherReason');
    let currentPedidoId, currentCurrency, currentButton;

    // Mostrar/ocultar campo "Otro" según selección
    refundReasonSelect.addEventListener('change', function () {
        if (this.value === 'Otro') {
            otherReasonContainer.style.display = 'block';
            otherReasonInput.setAttribute('required', 'required');
        } else {
            otherReasonContainer.style.display = 'none';
            otherReasonInput.removeAttribute('required');
            otherReasonInput.value = '';
        }
    });

    // Evento para todos los botones de reembolso
    document.querySelectorAll('.refund-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            currentPedidoId = this.dataset.pedidoId;
            currentCurrency = this.dataset.currency;
            currentButton = this;
            // Reiniciar el formulario al abrir el modal
            refundReasonSelect.value = '';
            otherReasonContainer.style.display = 'none';
            otherReasonInput.value = '';
            otherReasonInput.removeAttribute('required');
        });
    });

    // Confirmación del modal
    document.getElementById('confirmRefundBtn').addEventListener('click', async function () {
        const refundReason = refundReasonSelect.value;
        const otherReason = otherReasonInput.value.trim();

        // Validar motivo
        if (!refundReason) {
            alert('Por favor, selecciona un motivo para el reembolso.');
            return;
        }
        if (refundReason === 'Otro' && !otherReason) {
            alert('Por favor, especifica el motivo en el campo de texto.');
            otherReasonInput.focus();
            return;
        }

        // Combinar motivo (usar el texto de "Otro" si aplica)
        const finalReason = refundReason === 'Otro' ? otherReason : refundReason;
        const tokenElement = document.querySelector('meta[name="csrf-token"]');
        if (!tokenElement) {
            console.error("Error: No se encontró el token CSRF");
            alert("Error: No se pudo encontrar el token de seguridad.");
            return Promise.reject(new Error("Token CSRF no encontrado"));
        }
        const token = tokenElement.getAttribute("content");
        try {
            const response = await fetch('/Payment/RefundPartial', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    PedidoId: currentPedidoId,
                    currency: currentCurrency,
                    motivo: finalReason
                })
            });

            const result = await response.json();

            if (result.success) {
                modal.hide();
                currentButton.disabled = true;
                currentButton.classList.replace('btn-outline-warning', 'btn-outline-secondary');
                currentButton.textContent = 'Reembolsado';
                alert('Reembolso procesado con éxito.');
            } else {
                throw new Error(result.message);
            }
        } catch (error) {
            modal.hide();
            alert(`Error al procesar el reembolso: ${error.message}`);
        }
    });
});