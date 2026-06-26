document.addEventListener('DOMContentLoaded', function () {

    const dropdown = document.getElementById('notificacionesDropdown');
    const list = document.getElementById('notificacionesList');

    if (!dropdown || !list) return;

    dropdown.addEventListener('show.bs.dropdown', function () {
        list.innerHTML =
            '<div class="text-center text-muted py-3">' +
            '<span class="spinner-border spinner-border-sm"></span> Cargando…' +
            '</div>';

        fetch('/Notification/ListadoParcial')
            .then(function (r) {
                if (!r.ok) throw new Error('HTTP ' + r.status);
                return r.text();
            })
            .then(function (html) {
                list.innerHTML = html;
            })
            .catch(function () {
                list.innerHTML =
                    '<div class="text-center text-danger py-3">' +
                    'Error al cargar las notificaciones.' +
                    '</div>';
            });
    });
});