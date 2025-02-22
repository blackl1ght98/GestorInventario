
# Explicación de eventos de jQuery:
### ¿Que es JQuery?
jQuery es una popular biblioteca de JavaScript que facilita tareas comunes como la manipulación del DOM, el manejo de eventos, la creación de animaciones y 
las solicitudes AJAX, simplificando el código y mejorando la compatibilidad entre navegadores.
### Ejemplo practico de uso de esta función:
@section Scripts { 
      <script>
        $(document).ready(function () {
            var userId;
            var actionType;
            var actionsMap = {
                'Alta': 'AltaUsuarioPost',
                'Baja': 'BajaUsuarioPost',             
            };
            $(".user-action-button").click(function (event) {
                event.preventDefault();
                userId = $(this).data("user-id");
                actionType = $(this).data("user-action");             
                var actionText = actionType.toLowerCase();             
                $("#confirmModal").modal("show");               
                $("#confirmMessage").text(`¿Estás seguro de que deseas dar de  ${actionText} al usuario con ID ${userId}?`);
            });        
            $("#confirmActionBtn").click(function () {             
                $('#confirmModal').modal({
                    backdrop: true, 
                    keyboard: true                    
                });                            
                $(this).prop('disabled', true);             
                $("#loadingMessage").text("Procesando...").show();               
                var action = actionsMap[actionType];
                if (action) {
                    cambiarEstadoUsuario(userId, action); 
                } else {
                    console.log("Acción no definida");
                    $("#loadingMessage").text("Error: Acción no definida").show();
                    $("#confirmActionBtn").prop('disabled', false);
                }
            });         
            function cambiarEstadoUsuario(id, action) {
                $.ajax({
                    type: "POST",
                    url: `/Admin/${action}`,
                    data: { id: id },
                    success: function (response) {
                        if (response.success) {                         
                            location.reload();
                        } else {
                            console.log(response.errorMessage);
                            $("#loadingMessage").text("El servidor ha tardado en responder por favor intentelo de nuevo").show();                          
                            $("#confirmActionBtn").prop('disabled', false);
                        }
                    },
                    error: function () {
                        console.log('Error al procesar la solicitud');
                        $("#loadingMessage").text("Error al procesar la solicitud").show();                   
                        $("#confirmActionBtn").prop('disabled', false);
                    }
                });
            }
        });
    </script>
}
El script anterior muestra como se tiene que hacer una operación con jQuery en este caso estamos usando JQuery y JavaScript para alterar el comportamiento del modal y realizar
una operación con este, a continuacion vamos a explicar en detalle que es lo que hace cada linea:
## 1. $(document).ready(function () {});
Este evento de **JQuery** se ejecuta cuando el **DOM** (Document Object Module) esta completamente cargado, esto evita que las acciones que contiene se ejecuten antes de tiempo.
## 2.var userId; var actionType;
Estas variables son necesarias ya que **userId** identifica al usuario y **actionType** la acción que el usuario realiza.
## 3. var actionsMap = {'Alta': 'AltaUsuarioPost','Baja': 'BajaUsuarioPost'};
Esta variable almacena un conjunto de acciones que permite cambiar entre una u otra en base a la acción del usuario a esta tecnica de tener varias acciones dentro de una variable y
recorrerla se le llama `mapear`.
## 4. $(".user-action-button").click(function (event) {});
Este es un evento dinamico que llama al evento click el evento es dinamico porque en el boton se llama asi: `data-user-action` cuando en JQuery se quiere hacer un evento dinamico
se llama de esta manera y a este evento que inicialmente no es un evento con esta linea hacemos que sea un evento y que el evento que ejecute sea el evento click.
## 5. event.preventDefault();
Esta linea contenida en el evento anterior previene el comportamiento por defecto del boton en este caso.
## 6. userId = $(this).data("user-id"); actionType = $(this).data("user-action");
Estas lineas extraen de manera dinamica los datos del usuario y la accion que ha realizado el usuario,  la palabra clave `$(this)` llama al propio modal  y seguido a esto
`.data("user-action")` extrae el valor correspondiente a la accion del usuario para que esta extraccion sea exitosa en el boton hay que poner esto `data-user-action`, y de la
misma manera se hace para extraer el valor de la id del usuario: `data-user-id`.
## 7.  var actionText = actionType.toLowerCase(); 
Esta variable guarda la acción que realiza el usuario de manera dinamica si la accion que realiza el usuario es un `Alta` si realiza una baja pues guarda `Baja`
## 8.$("#confirmModal").modal("show");
Este evento hace uso de JQuery y el evento `show` de bootstrap para mostra el modal, la manera que llama al modal es por la id `#confirmModal`
## 9.$("#confirmMessage").text(`¿Estás seguro de que deseas dar de  ${actionText} al usuario con ID ${userId}?`);});
Este evento modifica el texto por defecto que tiene el modal de bootrap para ello se llama al evento `.text()` y para modificar un mensaje especifico se nombra el elemento html
mediante su id que es `#confirmMessage`. Y en esta linea termina la apertura del modal y lo que se muestra.
## 10.$("#confirmActionBtn").click(function () {});
Con el evento click manipulamos el comportamiento del boton para hacer referencia a un boton especifico lo llamamos por su id que es `#confirmActionBtn`.
## 11.$('#confirmModal').modal({backdrop: true, keyboard: true});
Este evento llama al modal con id `#confirmModal``y dicho evento controla que es lo que va ha ocurrir cuando se hace click fuera del modal o se presiona la tecla Esc, en este caso
este evento cierra el modal.
## 12. $(this).prop('disabled', true);
Este evento llama al modal y previene que el usuario pueda hacer multiples clicks.
## 13.$("#loadingMessage").text("Procesando...").show();
Este evento esta asociado a una etiqueta `<p></p>` con id `#loadingMessage` que muestra el mesanje `Procesando...` cuando se ha hecho click en el boton dentro del modal.
## 14.  var action = actionsMap[actionType];
Esta variable guarda todas las acciones que puede hacer el usuario pero solo se ejecuta una accion que es la que haya escojido el usuario, se pone asi `actionsMap[actionType]` porque
esta variable lo que hace es mapear las acciones que puede hacer el usuario. A continuacion vamos a ver como se pasa la accion a la función que hemos creado:
**if (action) {
                    cambiarEstadoUsuario(userId, action); 
                } else {
                    console.log("Acción no definida");
                    $("#loadingMessage").text("Error: Acción no definida").show();
                    $("#confirmActionBtn").prop('disabled', false);
                }** Este if comprueba si le ha llegado la accion y si es asi lo pasa a la funcion `cambiarEstadoUsuario(userId, action);` que recibe el id de usuario y la accion que este
                realiza. Si la accion no se ejecuta porque el servidor haya tardado en responder se muestra este mensaje en el modal ` $("#loadingMessage").text("Error: Acción no definida").show();`
                y se vuelve a habilitar el boton para reintentarlo `$("#confirmActionBtn").prop('disabled', false);`
## 15.  function cambiarEstadoUsuario(id, action) {}
Esta funcion es la que hemos creado para manejar el envio al servidor
## 16  $.ajax({});
Dentro de la funcion que hemos creado llamamos a este evento `$.ajax({});` que lo que hace es controlar el comportamiento de una petición HTTP, pudiendo manipular la información que se
manda, a donde se manda, manejo del exito de la peticion y manejo de errores de la peticion a continuacion vamos a ver cada una de las propiedades que esta tiene:
    - **type: "POST",** Esta propiedad especifica el tipo de petición que se va a mandar al servidor.
    - **url: `/Admin/${action}`** Esta propiedad controla a que endpoint se va a mandar esa peticion.
    - ** data: { id: id },** Esta propiedad controla que datos va a llevar esa peticion al servidor.
    - ** success: function (response) {}** Esta propiedad controla el exito de esa peticion dentro del exito se puede controlar de esta manera:
        - **if (response.success) {
                            location.reload();
                        } else {
                            console.log(response.errorMessage);
                            $("#loadingMessage").text("Error: " + response.errorMessage).show();
                            $("#confirmActionBtn").prop('disabled', false);
                        }** Aqui hacemos uso del condicional if y si la respuesta es exitosa se ejecuta el evento `location.reload();` que lo que hace es recargar la pagina, si algo ha
                        fallado muestra el mensaje de error en el modal `$("#loadingMessage").text("Error: " + response.errorMessage).show();` y vuelve a habilitar el boton 
                        ` $("#confirmActionBtn").prop('disabled', false);` para reintentar la operacion.
    - **error: function () {
                        console.log('Error al procesar la solicitud');
                        $("#loadingMessage").text("Error al procesar la solicitud").show();
                        $("#confirmActionBtn").prop('disabled', false);
                    }** Si lo anterior ha fallado o ocurre un error inesperado entra aqui y muesta este mensaje en el modal ` $("#loadingMessage").text("Error al procesar la solicitud").show();`
                    y habilita el boton para reintentar la operacion `$("#confirmActionBtn").prop('disabled', false);`

<button class="btn btn-danger user-action-button mt-2" data-user-id="@user.Id" data-user-action="@((user.BajaUsuario) ? "Alta" : "Baja")" >
                                    @((user.BajaUsuario) ? "Alta" : "Baja")
</button>
-----------------------------------------------------------------------------OTRA MANERA DE HACER EL SCRIPT----------------------------------------------------------------------------------------------
<script>
       $(document).ready(function () {
            var pedidoId;           
            $(".refund-button").click(function (event) {
                event.preventDefault();
                pedidoId = $(this).data("pedido-id");               
                $("#confirmModal").modal("show");
                $("#confirmMessage").text(`¿Estás seguro de que deseas reembolsar el pedido ${pedidoId}?`);
            });         
            $("#confirmActionBtn").click(function () {                           
               
                $('#confirmModal').modal({
                    backdrop: true,
                    keyboard: true  
                });
                $("#loadingMessage").text("Procesando...").show();
                $(this).prop('disabled', true);
                $.ajax({
                    url: '@Url.Action("RefundSale", "Payment")',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({
                        PedidoId: pedidoId,                                        
                    }),
                    success: function (response) {
                        console.log("Reembolso realizado con éxito");
                        location.reload(); 
                    },
                    error: function (xhr, status, error) {
                        console.log("Error al realizar el reembolso: " + xhr.responseText);
                    }
                });
                $("#confirmModal").modal("hide");
            });
        });
</script>
Como vemos este es otro script que podemos ver que se hace lo mismo de de una manera diferente, diferente en el sentido porque lo que ejecuta es una sola accion.
## 1.  var pedidoId; 
Como solo se necesita 1 dato que es el pedidoId pues el valor se gurda en esta variable y se hace como el anterior de manera dinamica ` pedidoId = $(this).data("pedido-id");`,
llamando primero al modal y en el modal mostrar la id.
## 2.  $(".refund-button").click(function (event) {});
Aqui en vez de usar algo dinamico usamos algo fijo llamamos al boton directamente por la clase del boton que es `.refund-button` y hacemos lo mismo que el anterior 
agregamos el evento click.
## 3.¿Como llama el boton al modal de este script?
Lo llama de esta manera ` data-toggle="modal"` pero claro eso indica que tiene que llamar a un modal pero ¿a cual modal? para ello lo indicamos asi `data-target="#confirmModal"` haciendo
uso de la id del modal que queremos llamar.
## 4. pedidoId = $(this).data("pedido-id");   
Como ya sabe el boton a que modal llamar pues del `data-pedido-id="@pedido.Id"` que es un elemento dinamico extraemos el id del pedido a reembolsar.
## 5. $.ajax({});
Aqui vemos algo nuevo que es:
**data: JSON.stringify({
                        PedidoId: pedidoId,                                        
                    }),** Esta propiedad sigue haciendo lo mismo enviar datos al servidor, pero ¿como manejamos este envio cuando el servidor lo que devuelve no es un json y lo que espera
                    es un objeto que en nuestro caso es `RefundRequestModel`, para ello JQuery dispone de `JSON.stringify({})` que esto lo que hace es convertir las propiedades que tenga
                    que recibir ese objeto a un JSON que es lo que el servidor espera.
**success: function (response) {
                        console.log("Reembolso realizado con éxito");
                        location.reload(); 
                    },**: Cuando en el servidor devolvemos un Ok() la manera de manejar el exito o fracaso de la peticion cambia ligeramente, no se pone if y lo que se pone es directamente
                    la accion.
La diferencia que tenemos en este script es que en ves de tener un metodo a parte que llame al servidor se hace todo de un golpe, ademas de solo tener una accion y llamar de manera
distinta al modal.
 <button type="button" class="btn btn-info mt-2 refund-button"
                                    data-toggle="modal"
                                    data-target="#confirmModal"
                                    data-pedido-id="@pedido.Id"
                            @if (isRefunded)
                            {
                                <text>disabled</text>
                            }>
                    Reembolsar
                </button>