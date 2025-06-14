// wwwroot/js/create-user.js
document.addEventListener('DOMContentLoaded', function () {
    // Toggle mostrar/ocultar contraseña
    const togglePassword = document.getElementById('togglePassword');
    if (togglePassword) {
        togglePassword.addEventListener('click', function () {
            const passwordInput = document.getElementById('Password');
            const icon = this.querySelector('i');
            if (passwordInput.type === 'password') {
                passwordInput.type = 'text';
                icon.classList.replace('bi-eye', 'bi-eye-slash');
            } else {
                passwordInput.type = 'password';
                icon.classList.replace('bi-eye-slash', 'bi-eye');
            }
        });
    }

    // Validación en tiempo real para email
    const emailInput = document.getElementById('Email');
    if (emailInput) {
        emailInput.addEventListener('input', function () {
            const feedback = this.nextElementSibling;
            const email = this.value;
            if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
                feedback.textContent = 'Por favor, introduce un email válido.';
            } else {
                feedback.textContent = '';
            }
        });
    }

    // Validación en tiempo real para teléfono
    const telefonoInput = document.getElementById('Telefono');
    if (telefonoInput) {
        telefonoInput.addEventListener('input', function () {
            const feedback = this.nextElementSibling;
            const telefono = this.value;
            if (telefono && !/^\d{3}-\d{3}-\d{4}$/.test(telefono)) {
                feedback.textContent = 'Usa el formato 123-456-7890';
            } else {
                feedback.textContent = '';
            }
        });
    }

    // Animación al enviar formulario
    const form = document.getElementById('createUserForm');
    if (form) {
        form.addEventListener('submit', function () {
            const submitBtn = document.getElementById('submitButton');
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="bi bi-arrow-clockwise me-2 spin"></i>Creando...';
        });
    }
});