<!DOCTYPE html>
<html>

<body>
    <h1>Guía de instalación para usar el proyecto Gestor Inventario</h1>
    <h2>Restaurar la copia de seguridad</h2>
    <p>Primero, restaurar la copia de seguridad <strong>GestorInventarioDB</strong> usando Microsoft SQL Server. Si no disponen de este programa tendrán que descargarlo de la página web de Microsoft. Puedes descargarlo desde <a href="https://www.microsoft.com/es-es/sql-server/sql-server-downloads" target="_blank">aquí</a>. Instalamos la versión <strong>Express</strong> y seguimos los pasos de instalación del instalador. Una vez se complete, tendremos que instalar la interfaz gráfica de SQL Server, que puedes descargar desde <a href="https://learn.microsoft.com/es-es/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver16" target="_blank">aquí</a>.</p>
    <p>Una vez instalado, procedemos a abrirlo y nos aparecerá una ventana que pondrá el tipo de servidor, nombre del servidor, autenticación. Esto lo dejaremos tal y como viene sin poner contraseña. Una vez alcanzado este punto le damos a <strong>Conectar</strong>.</p>
    <p>Nos dirigimos a la parte izquierda de la pantalla y veremos que pone algo como esto <strong>Servidores registrados</strong>. Sobre la carpeta <strong>Base de datos</strong> hacemos clic derecho y hacemos clic en <strong>Restaurar base de datos</strong>.</p>
    <p>En la ventana que se abre, seleccionamos <strong>Dispositivo</strong> y en dispositivo al final a la derecha hay un botón que tiene tres puntos. Le damos ahí y se nos abre otra ventana. En esa ventana le damos a agregar y seleccionamos la base de datos. Una vez localizada la base de datos la seleccionamos y le damos a <strong>Aceptar</strong>.</p>
    <h2>Scaffold-DbContext</h2>
    <p>Una vez la base de datos ha sido restaurada, en Visual Studio en la parte inferior si está activo veremos el administrador de paquetes. Si no está activo hay que activarlo. En esta consola ponemos el siguiente comando:</p>
    <pre><code>Scaffold-DbContext "Data Source=NOMBRESERVIDORBASEDATOS;Initial Catalog=NOMBREBASEDATOS;Integrated Security=True;TrustServerCertificate=True" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -force -project NOMBREPROYECTO</code></pre>
    <p>Estos campos se obtienen:
        <br/>
    - <strong>NOMBRESERVIDORBASEDEDATOS</strong>: Se obtiene al abrir el programa SQL Server.
        <br/>
    - <strong>NOMBREBASEDATOS</strong>: En el programa de SQL Server vemos qué nombre tiene nuestra base de datos.
        <br/>
    - <strong>NOMBREPROYECTO</strong>: El nombre que tenga nuestro proyecto en SQL Server.</p>
    <h2>Administrar secretos de usuario</h2>
    <p>Dentro de Visual Studio 2022 hacemos clic derecho en el proyecto nos vamos a donde dice <strong>Administrar secretos de usuario</strong>. Dentro de ese archivo ponemos los valores:</p>
    <pre><code>{
  "ConnectionStrings": {
    "CONNECTION_STRING": "Data Source=NOMBRESERVIDORBASEDEDATOS--;Initial Catalog=GestorInventario;Integrated Security=True;TrustServerCertificate=True"
  },
  "ClaveEncriptacion": "",
  "ClaveJWT": "",
  "JwtIssuer": "",
  "JwtAudience": "",
  "Jwt": {
    "PrivateKey": "",
    "PublicKey": ""
  }
}</code></pre>
    <p><strong>IMPORTANTE</strong>: Las claves de encriptación y JWT tienen que ser largas, mínimo 38 dígitos para que no de fallo. Para obtener la clave privada y pública he dejado en el repositorio un archivo con el código llamado generarpardeclaves. Este código lo ejecutan en una aplicación de consola para que puedan ver las claves, las copian y pegan. Luejo <strong>JwtIssuer y JwtAudience </strong> aqui pueden colocar por ejemplo en JwtIssuer: <strong>EjemploEmisorApp</strong> y en <strong>JwtAudience</strong>
pueden poner EjemploReceptorApp.</p> 
    <p>Una vez hecho todo esto, el proyecto debería arrancar sin problemas.</p>
<h2>Características con las que cuenta el proyecto</h2>
    <p>El proyecto Gestor Inventario ofrece una amplia gama de características para gestionar eficientemente el inventario:</p>
    <ul>
        <li>Gestión de Datos: Permite realizar operaciones CRUD (Crear, Leer, Actualizar, Eliminar) en usuarios, proveedores, productos, pedidos, y el historial de productos y pedidos.</li>
        <li>Autenticación Robusta: El sistema de autenticación se basa en la generación de tokens y ofrece tres métodos de autenticación: Autenticación simétrica, Autenticación asimétrica con clave pública y privada fija, Autenticación asimétrica con clave pública y privada dinámica.</li>
        <li>Generación de Informes: Los usuarios pueden descargar informes en formato PDF del historial de pedidos y productos.</li>
        <li>Notificaciones por Correo Electrónico: El sistema envía notificaciones por correo electrónico cuando el stock de un producto está bajo.</li>
        <li>Registro y Acceso de Usuarios: Los usuarios pueden registrarse y acceder al sistema. Cuando un nuevo usuario se registra, se le envía un correo electrónico de confirmación.</li>
        <li>Panel de Administración de Usuarios: El proyecto incluye un panel de administración de usuarios para gestionar las cuentas de usuario.</li>
        <li>Sistema Basado en Roles: El acceso a diferentes niveles del sistema se controla mediante un sistema basado en roles.</li>
        <li>Pasarela de Pago PayPal: El proyecto incluye la implementación de una pasarela de pago PayPal.</li>
        <li>Restablecimiento de Contraseña: Los usuarios pueden restablecer su contraseña a través del panel de administrador. Se envía un correo electrónico al usuario seleccionado con una contraseña temporal y un enlace para cambiarla.</li>
        <li>Flexibilidad en la Autenticación: Los usuarios pueden cambiar entre los modos de autenticación de manera efectiva comentando y descomentando el código correspondiente.</li>
    </ul>
</body>
</html>