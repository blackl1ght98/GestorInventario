document.addEventListener("DOMContentLoaded", function () {
    const modalEl = document.getElementById('agregarEnvioModal');
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

    const resultBox = document.getElementById('envioResult');
    const confirmBtn = document.getElementById('confirmAgregarEnvioBtn');
    const pedidoIdInput = document.getElementById('pedidoIdInput');
    const carrierSelect = document.getElementById('carrierSelect');
    
    const loadingMessage = document.getElementById('loadingMessageEnvio');

    let currentPedidoId = null;

    // Helper para mostrar mensajes en la caja del modal
    function showMessage(message, type) {
        if (!resultBox) return;
        resultBox.textContent = message;
        resultBox.className = `alert alert-${type}`;
        resultBox.classList.remove('d-none');
    }

    // Limpiar mensajes al abrir el modal
    function clearMessage() {
        if (!resultBox) return;
        resultBox.className = 'alert d-none';
        resultBox.textContent = '';
    }

    // 1. Abrir modal al hacer click en "Agregar Envío"
    document.querySelectorAll('.envio-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            currentPedidoId = parseInt(this.dataset.pedidoId, 10);

            if (pedidoIdInput) {
                pedidoIdInput.value = currentPedidoId;
            }

            clearMessage();
            confirmBtn.disabled = false;
            confirmBtn.textContent = 'Guardar Información';

            modal.show();
        });
    });

    // 2. Confirmar envío
    confirmBtn.addEventListener('click', async function () {
        const carrier = carrierSelect ? carrierSelect.value : '';
      

        // Validaciones locales
        if (!carrier) {
            showMessage("Por favor selecciona un transportista.", "warning");
            return;
        }
     
        if (!currentPedidoId || isNaN(currentPedidoId)) {
            showMessage("Error: No se pudo identificar el pedido. Cierra el modal e inténtalo de nuevo.", "danger");
            return;
        }

        // UI de carga
        loadingMessage.classList.remove('d-none');
        this.disabled = true;
        clearMessage();

        try {
            const response = await fetch('/Envios/AgregarInfoEnvio', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify({
                    PedidoId: currentPedidoId,
                    Carrier: carrier,
                   
                })
            });

            const result = await response.json();

            if (!response.ok || !result.success) {
                throw new Error(result.message || "Error desconocido en el servidor");
            }

            // Éxito: mostrar mensaje verde, esperar 1.5s y recargar
            showMessage(result.message, "success");
            loadingMessage.classList.add('d-none');

            setTimeout(() => {
                location.reload();
            }, 1500);

        } catch (error) {
            // Error: mostrar en rojo dentro del modal, mantenerlo abierto
            showMessage("Error: " + error.message, "danger");
            loadingMessage.classList.add('d-none');
            this.disabled = false;
        }
    });
});