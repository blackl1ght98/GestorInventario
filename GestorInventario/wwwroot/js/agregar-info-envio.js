/**
        * 
        * Script para abrir el Modal de agregar información de envio
        */
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".activate-action-button").forEach(boton => {
        boton.addEventListener("click", function () {
            document.getElementById("pedidoIdInput").value = this.getAttribute("data-pedido-id");
            new bootstrap.Modal(document.getElementById("agregarEnvioModal")).show();
        });
    });

    document.getElementById("confirmAgregarEnvioBtn").addEventListener("click", function () {
        if (!document.getElementById("carrierSelect").value) {
            alert("Por favor selecciona un transportista.");
            return;
        }
        if (!document.getElementById("barcodeSelect").value) {
            alert("Por favor selecciona un tipo de codigo de barras");
            return;
        }
        document.getElementById("loadingMessageEnvio").style.display = "block";
        this.disabled = true;
        document.getElementById("agregarEnvioForm").submit();
    });
});