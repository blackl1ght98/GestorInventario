document.querySelectorAll('.reasignar-btn').forEach(btn => {
    btn.addEventListener('click', async function () {
        const userId = this.dataset.userId;
        const userName = this.dataset.userName;

        document.getElementById('nombreUsuario').textContent = userName;

        const response = await fetch(`/Admin/ObtenerInfoReasignacion?id=${userId}`);
        const data = await response.json();

        const contenido = document.getElementById('contenidoModal');

        if (!data.success) {
            contenido.innerHTML = `<div class="alert alert-info">${data.message}</div>`;
            document.getElementById('confirmarReasignacion').disabled = true;
        } else {
            document.getElementById('confirmarReasignacion').disabled = false;
            contenido.innerHTML = `
                <label class="form-label fw-bold">Reasignar proveedores a:</label>
                <select id="usuarioDestino" class="form-select">
                    <option value="">-- Selecciona un usuario --</option>
                    ${data.usuarios.map(u => `<option value="${u.id}">${u.nombreCompleto}</option>`).join('')}
                </select>`;
        }

        document.getElementById('confirmarReasignacion').dataset.userId = userId;
        new bootstrap.Modal(document.getElementById('reasignarModal')).show();
    });
});

document.getElementById('confirmarReasignacion').addEventListener('click', async function () {
    const userId = this.dataset.userId;
    const usuarioDestino = document.getElementById('usuarioDestino')?.value;

    if (!usuarioDestino) {
        alert('Selecciona un usuario destino');
        return;
    }

    const response = await fetch('/Admin/ReasignarProveedores', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ usuarioOrigenId: parseInt(userId), usuarioDestinoId: parseInt(usuarioDestino) })
    });

    const data = await response.json();
    if (data.success) {
        bootstrap.Modal.getInstance(document.getElementById('reasignarModal')).hide();
        location.reload();
    } else {
        alert(data.message);
    }
});