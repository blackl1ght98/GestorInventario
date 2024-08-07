
# Explicaciones detalladas de jQuery en el script

## 1. $(document).ready(function() { ... });
La función \`$(document).ready\` se ejecuta cuando el DOM está completamente cargado y listo para ser manipulado. Esto asegura que el código jQuery dentro 
de esta función no intente acceder a elementos que aún no existen en el DOM.

### Uso en el código

$(document).ready(function () {
    // Código para manejar eventos y lógicas cuando la página está lista
});


### Explicación
Dentro de esta función, se configura el comportamiento del modal y las acciones que se ejecutarán en función de la interacción del usuario. Esto garantiza 
que todas las referencias a elementos del DOM funcionen correctamente.

## 2. $('#confirmModal').on('show.bs.modal', function(event) { ... });
Este método se utiliza para agregar un evento personalizado a un elemento seleccionado. En este caso, el evento es \`show.bs.modal\`, que se activa cuando el modal con el ID \`confirmModal\` está a punto de ser mostrado.

### Uso en el código

$('#confirmModal').on('show.bs.modal', function (event) {
    var button = $(event.relatedTarget);
    var userId = button.data('user-id');
    var actionType = button.data('user-action');
    var modal = $(this);
    var actionText = actionType === 'Alta' ? 'reactivar' : 'desactivar';
    modal.find('.modal-title').text('Confirmación');
    modal.find('#confirmMessage').text(\`¿Estás seguro de que deseas \${actionText} al usuario con ID \${userId}?\`);
});


### Explicación
- \`event.relatedTarget\` es una referencia al elemento que activó la apertura del modal (el botón en este caso).
- \`var button = $(event.relatedTarget);\` selecciona el botón que disparó el evento.
- \`button.data('user-id');\` y \`button.data('user-action');\` obtienen los valores de los atributos \`data-user-id\` y \`data-user-action\` del botón, 
respectivamente.
- \`var modal = $(this);\` selecciona el modal que se está mostrando.
- Se actualizan los textos dentro del modal para reflejar la acción que se va a confirmar (reactivar o desactivar un usuario).

## 3. $('#confirmActionBtn').click(function () { ... });
Este método añade una función que se ejecutará cuando el elemento con el ID \`confirmActionBtn\` sea clicado.

### Uso en el código

$('#confirmActionBtn').click(function () {
    $('#confirmModal').modal('hide');
    if (actionType === 'Alta') {
        cambiarEstadoUsuario(userId, "AltaUsuarioPost");
    } else {
        cambiarEstadoUsuario(userId, "BajaUsuarioPost");
    }
});


### Explicación
- \`$('#confirmModal').modal('hide');\` cierra el modal cuando el usuario hace clic en el botón de confirmación.
- La función determina qué acción tomar (\`Alta\` o \`Baja\`) en función del valor de \`actionType\` y llama a la función \`cambiarEstadoUsuario\` con los 
- parámetros correspondientes.

## 4. $.ajax({ ... });
La función \`$.ajax\` realiza una llamada AJAX al servidor. Permite enviar y recibir datos del servidor de forma asíncrona sin recargar la página.

### Uso en el código

function cambiarEstadoUsuario(id, action) {
    $.ajax({
        type: "POST",
        url: `/Admin/${action}`,
        data: { id: id },
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.errorMessage);
            }
        },
        error: function () {
            console.log('Error al procesar la solicitud');
        }
    });
}


### Explicación
- \`type: "POST",\` especifica el tipo de petición HTTP que se realizará.
- \`url: \`/Admin/\${action}\`,\` es la URL a la que se envía la petición, donde \`action\` es dinámico y puede ser "AltaUsuarioPost" o "BajaUsuarioPost".
- \`data: { id: id },\` envía el ID del usuario como dato.
- \`success: function(response) { ... }\` define la función que se ejecutará si la petición es exitosa. Si la operación en el servidor fue exitosa, se recarga la página; si no, se muestra un mensaje de error.
- \`error: function() { ... }\` define la función que se ejecutará si hay un error en la petición AJAX.

### Explicacion
1. $('#confirmModal').on('show.bs.modal', function (event) {...})
   - Se utiliza para adjuntar un controlador de eventos al modal con el ID `confirmModal`.
   - El evento `show.bs.modal` es desencadenado por Bootstrap cuando el modal está a punto de mostrarse.
   - Dentro de la función del controlador, `event.relatedTarget` se refiere al elemento que activó el modal, que en este caso sería el botón que el usuario clicó.
   - Se extraen datos como `userId` y `actionType` desde los atributos `data-user-id` y `data-user-action` del botón.

2. var button = $(event.relatedTarget);
   - Este código selecciona el botón que activó el evento del modal. `event.relatedTarget` es el elemento (botón) que fue clicado para abrir el modal.

3. userId = button.data('user-id');
   - Extrae el ID del usuario desde el atributo `data-user-id` del botón.

4. actionType = button.data('user-action');
   - Extrae el tipo de acción (ya sea "Alta" o "Baja") desde el atributo `data-user-action` del botón.

5. modal.find('.modal-title').text('Confirmación');
   - Cambia el texto del título del modal a "Confirmación". Utiliza `modal.find` para buscar elementos dentro del modal actual.

6. modal.find('#confirmMessage').text(`¿Estás seguro de que deseas ${actionText} al usuario con ID ${userId}?`);
   - Cambia el mensaje dentro del modal para reflejar la acción a realizar (reactivar o desactivar) y el ID del usuario.

7. $('#confirmActionBtn').click(function () {...});
   - Se adjunta un controlador de eventos al botón de confirmación dentro del modal. Cuando se hace clic en este botón, se ejecuta la función proporcionada.
   - Dentro de esta función, el modal se oculta (`$('#confirmModal').modal('hide')`), y dependiendo de `actionType`, se llama a la función `cambiarEstadoUsuario` con la acción correspondiente.

8. cambiarEstadoUsuario(id, action)
   - Esta es una función personalizada que envía una solicitud AJAX para cambiar el estado de un usuario (ya sea activándolo o desactivándolo).
   - Utiliza `$.ajax` de jQuery para enviar una solicitud POST al servidor con el ID del usuario y la acción a realizar.
   - Si la solicitud es exitosa (`success`), se recarga la página para reflejar los cambios. Si ocurre un error (`error`), se registra un mensaje de error en la consola y, si el servidor responde con un error, se muestra un mensaje de alerta con el `errorMessage`.