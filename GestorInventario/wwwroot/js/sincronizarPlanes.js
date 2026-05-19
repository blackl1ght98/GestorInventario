document.getElementById('btnSincronizar')?.addEventListener('click', async function () {
    // Valores de Razor
    const totalPaginas = @Model.TotalPaginas;
    const paginaActual = @Model.PaginaActual;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const modalEl = document.getElementById('syncModal');
    const modal = new bootstrap.Modal(modalEl);

    const texto = document.getElementById('syncTexto');
    const barra = document.getElementById('syncBarra');
    const detalle = document.getElementById('syncDetalle');

    modal.show();

    let totalActualizados = 0;
    let errores = 0;

    // Iterar desde página 1 hasta TotalPaginas
    for (let pagina = 1; pagina <= totalPaginas; pagina++) {
        const porcentaje = Math.round(((pagina - 1) / totalPaginas) * 100);
        barra.style.width = porcentaje + '%';
        texto.textContent = `Sincronizando ${pagina} de ${totalPaginas}`;
        detalle.textContent = 'Conectando con PayPal...';

        try {
            const response = await fetch('/PaypalPlan/SincronizarPagina', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': token
                },
                body: `pagina=${pagina}`
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const data = await response.json();

            if (data.success) {
                totalActualizados += (data.actualizados || 0);
                detalle.textContent = data.message || `Página ${pagina} completada`;
            } else {
                errores++;
                detalle.textContent = `Error en página ${pagina}: ${data.message}`;
                // Opcional: detener o continuar
                // break;
            }

        } catch (error) {
            errores++;
            detalle.textContent = `Error de red en página ${pagina}: ${error.message}`;
        }

        // Pausa para que la UI respire y no saturar PayPal
        if (pagina < totalPaginas) {
            await new Promise(r => setTimeout(r, 400));
        }
    }

    // Completado
    barra.style.width = '100%';
    barra.classList.remove('progress-bar-animated');

    if (errores === 0) {
        barra.classList.remove('bg-primary');
        barra.classList.add('bg-success');
        texto.textContent = '¡Sincronización completada!';
        detalle.textContent = `${totalPaginas} página(s) procesadas. ${totalActualizados} plan(es) actualizado(s).`;
    } else {
        barra.classList.remove('bg-primary');
        barra.classList.add('bg-warning');
        texto.textContent = 'Completado con advertencias';
        detalle.textContent = `${totalPaginas - errores} exitosas, ${errores} fallidas. ${totalActualizados} plan(es) actualizado(s).`;
    }

    // Recargar página tras 2 segundos
    setTimeout(() => {
        modal.hide();
        location.reload();
    }, 2000);
});