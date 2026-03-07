/**
 * Script para controlar las acciones de las suscripciones
 */
document.addEventListener('DOMContentLoaded', function () {
    // Configuración de todas las acciones (puedes añadir más fácilmente)
    const actionConfigs = {
        'activar': {
            buttonClass: '.btn-activar-suscripcion',
            modalId: 'activateModal',
            idInputId: 'activateSubscriptionId',
            displayId: 'activateSubscriptionIdDisplay',
            reasonId: 'activateReason',
            actionText: 'activar'
        },
        'suspender': {
            buttonClass: '.btn-suspend-suscripcion',
            modalId: 'suspendModal',
            idInputId: 'suspendSubscriptionId',
            displayId: 'suspendSubscriptionIdDisplay',
            reasonId: 'suspendReason',
            actionText: 'suspender'
        },
        'cancelar': {
            buttonClass: '.btn-cancel-suscripcion',
            modalId: 'cancelModal',
            idInputId: 'cancelSubscriptionId',
            displayId: 'cancelSubscriptionIdDisplay',
            reasonId: 'cancelReason',
            actionText: 'cancelar'
        }
    };

    // Función reutilizable para abrir modal y rellenar datos
    function openActionModal(button, config) {
        const subscriptionId = button.getAttribute('data-id');

        // Rellenar campos
        document.getElementById(config.idInputId).value = subscriptionId;
        document.getElementById(config.displayId).textContent = subscriptionId;

        // Limpiar textarea y feedback
        const reasonInput = document.getElementById(config.reasonId);
        reasonInput.value = '';
        reasonInput.classList.remove('is-invalid');

        // Abrir modal
        const modal = new bootstrap.Modal(document.getElementById(config.modalId));
        modal.show();
    }

    // Asignar evento a todos los botones de acción
    Object.values(actionConfigs).forEach(config => {
        document.querySelectorAll(config.buttonClass).forEach(button => {
            button.addEventListener('click', function () {
                openActionModal(this, config);
            });
        });
    });
});