// planes.js

// Mostrar el modal de actualización de precio del plan
document.querySelectorAll(".update-plan-price-button").forEach(function (button) {
    button.addEventListener("click", function (event) {
        event.preventDefault();
        const planId = button.getAttribute("data-plan-id");
        const currency = button.getAttribute("data-currency"); // Sin fallback a USD
        if (!planId) {
            console.error("Error: No se encontró el ID del plan");
            alert("Error: No se pudo obtener el ID del plan.");
            return;
        }
        if (!currency) {
            console.error("Error: No se encontró la moneda del plan");
            alert("Error: No se pudo determinar la moneda del plan. Contacta al administrador.");
            return;
        }
        console.log("Plan ID:", planId, "Currency:", currency);
        document.getElementById("updatePlanPriceModal").setAttribute("data-plan-id", planId);
        document.getElementById("updatePlanPriceModal").setAttribute("data-currency", currency);
        document.getElementById("updatePlanPricePlanId").textContent = planId;
        document.getElementById("newTrialPrice").value = "";
        document.getElementById("newRegularPrice").value = "";
        document.getElementById("currencyDisplay").value = currency;
        document.getElementById("currencySelect").value = currency;

        const updatePlanPriceModal = new bootstrap.Modal(document.getElementById("updatePlanPriceModal"));
        updatePlanPriceModal.show();
    });
});

// Confirmar la actualización de precio del plan
document.getElementById("confirmUpdatePlanPriceBtn").addEventListener("click", function () {
    const confirmButton = document.getElementById("confirmUpdatePlanPriceBtn");
    const trialPriceInput = document.getElementById("newTrialPrice");
    const regularPriceInput = document.getElementById("newRegularPrice");
    const currencySelect = document.getElementById("currencySelect");
    const planId = document.getElementById("updatePlanPriceModal").getAttribute("data-plan-id");
    const trialPrice = trialPriceInput.value ? parseFloat(trialPriceInput.value) : null;
    const regularPrice = parseFloat(regularPriceInput.value);
    const currency = currencySelect.value;

    // Validar precios
    if (isNaN(regularPrice) || regularPrice <= 0) {
        regularPriceInput.classList.add("is-invalid");
        return;
    }
    regularPriceInput.classList.remove("is-invalid");

    if (trialPrice !== null && (isNaN(trialPrice) || trialPrice < 0)) {
        trialPriceInput.classList.add("is-invalid");
        return;
    }
    trialPriceInput.classList.remove("is-invalid");

    if (!currency) {
        document.getElementById("currencyDisplay").classList.add("is-invalid");
        alert("Error: La moneda del plan no está definida.");
        return;
    }
    document.getElementById("currencyDisplay").classList.remove("is-invalid");

    console.log("Enviando solicitud de actualización de precio para Plan ID:", planId, "Trial Price:", trialPrice, "Regular Price:", regularPrice, "Currency:", currency);

    confirmButton.disabled = true;
    confirmButton.textContent = "Procesando...";
    const updatePlanPriceModal = bootstrap.Modal.getInstance(document.getElementById("updatePlanPriceModal"));
    updatePlanPriceModal.hide();

    actualizarPrecioPlan(planId, trialPrice, regularPrice, currency)
        .finally(() => {
            confirmButton.disabled = false;
            confirmButton.textContent = "Actualizar";
        });
});

// Función para actualizar el precio del plan
function actualizarPrecioPlan(planId, trialPrice, regularPrice, currency) {
    const tokenElement = document.querySelector('meta[name="csrf-token"]');
    if (!tokenElement) {
        console.error("Error: No se encontró el token CSRF");
        alert("Error: No se pudo encontrar el token de seguridad.");
        return Promise.reject(new Error("Token CSRF no encontrado"));
    }
    const token = tokenElement.getAttribute("content");

    const requestBody = {
        planId: planId,
        trialAmount: trialPrice,
        regularAmount: regularPrice,
        currency: currency
    };

    console.log("Cuerpo de la solicitud:", JSON.stringify(requestBody));

    return fetch('/Paypal/ActualizarPrecioPlan', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(requestBody)
    })
        .then(response => {
            if (response.ok) {
                return response.json();
            }
            return response.json().then(data => {
                throw new Error(data.errorMessage || `Error al procesar la solicitud: ${response.statusText}`);
            });
        })
        .then(data => {
            if (data.success) {
                alert(data.message || "Precio del plan actualizado con éxito.");
                window.location.reload();
            } else {
                console.error('Error:', data.errorMessage);
                alert(`Error al actualizar el precio del plan: ${data.errorMessage}`);
            }
        })
        .catch(error => {
            console.error('Error al procesar la solicitud:', error);
            alert(`Error al procesar la solicitud: ${error.message}`);
        });
}