document.addEventListener("DOMContentLoaded", function () {
    var checkbox = document.getElementById("HasTrialPeriodCheckbox");
    var trialPeriodFields = document.getElementById("trialPeriodFields");

    // Función para manejar el cambio del checkbox
    checkbox.addEventListener("change", function () {
        trialPeriodFields.style.display = checkbox.checked ? "block" : "none";
    });

    // Verificar el estado inicial
    if (checkbox.checked) {
        trialPeriodFields.style.display = "block";
    }
});