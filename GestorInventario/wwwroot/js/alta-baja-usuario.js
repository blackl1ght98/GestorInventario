
 document.addEventListener('DOMContentLoaded', () => {
    const actionsMap = {
        'Alta': 'AltaUsuarioPost',
        'Baja': 'BajaUsuarioPost'
    };

    // Abrir modal
    document.querySelectorAll('.user-action-button').forEach(button => {
        button.addEventListener('click', e => {
            e.preventDefault();
            /**
             * Aqui recuperamos del dataset el userId y la accion que realice el usuario
             * el navegador realiza una transformacion quitado los - y sustitullendo por . usando
             * la notacion camelCase
             */
            const userId = button.dataset.userId;
            const actionType = button.dataset.userAction;

            if (!userId || !actionType) {
                console.error('Botón sin data-user-id o data-user-action');
                return;
            }
            //Esta informacion la pasamos al metodo que abrira el modal
            openConfirmModal(userId, actionType);
        });
    });
    const openConfirmModal = (userId, actionType) => {
        const actionText = actionType.toLowerCase(); // Aqui llega "Alta": "Baja"

        const message = document.getElementById('confirmMessage'); 
        message.textContent = `¿Estás seguro de que deseas dar de ${actionText} al usuario con ID ${userId}?`; 

        /** 
         *  Llegados aqui recuperamos la informacion que le habimos pasado y creamos 2 dataset de forma dinamica para
         * el boton confirmar y en el guardamos los datos 
         */
        const confirmBtn = document.getElementById('confirmActionBtn');
        confirmBtn.dataset.userId = userId;
        confirmBtn.dataset.actionType = actionType;
        
        const modal = document.getElementById('confirmModal');
        modal.classList.add('show');
        modal.style.display = 'block';
    };

    

    // Obtenemos el boton de confirmacion del modal, y un texto que hemos puesto nostros
    const confirmBtn = document.getElementById('confirmActionBtn');
    const loadingMessage = document.getElementById('loadingMessage');

    const resetButton = () => {
        confirmBtn.innerHTML = 'Confirmar';
        confirmBtn.disabled = false;
    };

     // Dentro del DOMContentLoaded
     confirmBtn.addEventListener('click', async () => {
         const userId = confirmBtn.dataset.userId;
         const actionType = confirmBtn.dataset.actionType;

         if (!userId || !actionType) {
             loadingMessage.textContent = 'Error: Datos no disponibles';
             resetButton();
             return;
         }

         // Preparar UI antes de la petición
         confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Procesando...';
         confirmBtn.disabled = true;
         loadingMessage.textContent = 'Procesando...';

         const action = actionsMap[actionType];
         if (!action) {
             loadingMessage.textContent = 'Error: Acción no definida';
             resetButton();
             return;
         }

         try {
             const response = await fetch(`/Admin/${action}`, {
                 method: 'POST',
                 headers: { 'Content-Type': 'application/json' },
                 body: JSON.stringify({ id: userId })
             });

             const data = await response.json();

             if (data.success) {
                 location.reload();
             } else {
                 loadingMessage.textContent = data.errorMessage || 'El servidor tardó en responder';
                 resetButton();
             }
         } catch (error) {
             console.error('Error:', error);
             loadingMessage.textContent = 'Error al procesar la solicitud';
             resetButton();
         }
     });
});