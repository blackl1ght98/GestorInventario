//Evento que se ejecuta cuando la pagina esta completamente cargada
document.addEventListener("DOMContentLoaded", function () {
    var userId; // Variable va a ha recibir como dato el userId
    var actionType; //Variable que va ha recibir la accion ejecutada por el usuario
    var actionsMap = { //Las acciones que el usuario puede hacer
        'Alta': 'AltaUsuarioPost',
        'Baja': 'BajaUsuarioPost',
    };
    //Se asigna el evento click al boton de baja o alta
    var actionButtons = document.querySelectorAll(".user-action-button");
    /*¿Por que forEach en un boton?
    Porque esto actionButtons devuelve un NodeList "lista" y al ser una lista es iterable y como va ha existir tantos botones como usuarios existan pues
    es nesario recorrerlos para que cada boton tenga su dato especifico, ademas al tratarse de un array de botones se le asigna una funcion anonima con un parametro
    llamado button que dentro de esta funcion puedes configurar que va a pasar si haces click en esos botones y con asignarlo una vez basta y ya no importa
    cuantos botones haya que el evento que se ponga se propaga a todos los botones
    */
    actionButtons.forEach(function (button) {
        //Se agrega el evento click a cada boton del nodelist
        button.addEventListener("click", function (event) {
            //Se evita que se envie al servidor
            event.preventDefault();
            //Se obtiene el id del usuario y la accion que realizo
            userId = button.dataset.userId;
            actionType = button.dataset.userAction;
            console.log('User ID:', userId);
            //La accion se almacena en esta variable y el texto se pasa a minuscula
            var actionText = actionType.toLowerCase();
            //Se obtiene el modal
            var confirmModal = document.getElementById("confirmModal");
            //Se muestra el modal
            confirmModal.classList.add("show");
            confirmModal.style.display = "block";
            //Se obtiene el mensaje que mostrara el modal
            var confirmMessage = document.getElementById("confirmMessage");
            //Se modifica el mensaje que mostrara
            confirmMessage.textContent = `¿Estás seguro de que deseas dar de ${actionText} al usuario con ID ${userId}?`;
        });
    });
    //Se agrega el evento click al boton de confirmacion del modal
    var confirmActionBtn = document.getElementById("confirmActionBtn");
    confirmActionBtn.addEventListener("click", function () {
        //Si se le hace clic se deshabilita el boton para que no se hagan mas clics
        confirmActionBtn.disabled = true;
        //Se modifica el mensaje de carga
        var loadingMessage = document.getElementById("loadingMessage");
        loadingMessage.textContent = "Procesando...";
        //Del array se obtiene la accion escogida por el usuario
        var action = actionsMap[actionType];
        if (action) {
            //Esa accion se pasa al metodo junto al id de usuario
            cambiarEstadoUsuario(userId, action);
        } else {
            //Si se elige una accion no valida se muestra un mensaje de error
            console.log("Acción no definida");
            loadingMessage.textContent = "Error: Acción no definida";
            confirmActionBtn.disabled = false;
        }
    });
    //Funcion que envia al servidor la accion
    function cambiarEstadoUsuario(id, action) {
        var loadingMessage = document.getElementById("loadingMessage");
        var confirmActionBtn = document.getElementById("confirmActionBtn");

        // Mostrar mensaje de carga
        loadingMessage.textContent = "Procesando...";
        confirmActionBtn.disabled = true; // Deshabilitar el botón mientras se procesa

        // Enviar datos en formato JSON
        fetch(`/Admin/${action}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json" // Especificar que los datos son JSON
            },
            body: JSON.stringify({ id: id }) // Convertir el objeto a JSON
        })
            .then(response => response.json()) // Parsear la respuesta como JSON
            .then(data => {
                if (data.success) {
                    location.reload(); // Recargar la página si la acción fue exitosa
                } else {
                    console.log(data.errorMessage);
                    loadingMessage.textContent = "El servidor ha tardado en responder, por favor intente de nuevo";
                    confirmActionBtn.disabled = false; // Habilitar el botón si hay error
                }
            })
            .catch(error => {
                console.error('Error al procesar la solicitud:', error);
                loadingMessage.textContent = "Error al procesar la solicitud";
                confirmActionBtn.disabled = false; // Habilitar el botón si hay error
            });
    }


    //Se cierra el modal
    var modalClose = document.querySelector('.modal');
    //Se agrega el evento click
    modalClose.addEventListener('click', function (event) {
        //Si el evento que se produce es el de cierre lo ejecuta
        if (event.target === modalClose) {
            var confirmModal = document.getElementById("confirmModal");
            confirmModal.classList.remove("show");
            confirmModal.style.display = "none";
        }
    });
});