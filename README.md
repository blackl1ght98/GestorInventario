<!DOCTYPE html>
<html>

<body>
    <h1>Guía de instalación para usar el proyecto Gestor Inventario</h1>
    <h2>Restaurar la copia de seguridad</h2>
    <p>Primero, restaurar la copia de seguridad <strong>GestorInventarioDB</strong> usando Microsoft SQL Server. Si no disponen de este programa tendrán que descargarlo de la página web de Microsoft. Puedes descargarlo desde <a href="https://www.microsoft.com/es-es/sql-server/sql-server-downloads" target="_blank">aquí</a>. Instalamos la versión <strong>Express</strong> y seguimos los pasos de instalación del instalador. Una vez se complete, tendremos que instalar la interfaz gráfica de SQL Server, que puedes descargar desde <a href="https://learn.microsoft.com/es-es/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver16" target="_blank">aquí</a>.</p>
    <p>Una vez instalado, procedemos a abrirlo y nos aparecerá una ventana que pondrá el tipo de servidor, nombre del servidor, autenticación. Esto lo dejaremos tal y como viene sin poner contraseña. Una vez alcanzado este punto le damos a <strong>Conectar</strong>.</p>
    <p>Nos dirigimos a la parte izquierda de la pantalla y veremos que pone algo como esto <strong>Servidores registrados</strong>. Sobre la carpeta <strong>Base de datos</strong> hacemos clic derecho y hacemos clic en <strong>Restaurar base de datos</strong>.</p>
    <p>En la ventana que se abre, seleccionamos <strong>Dispositivo</strong> y en dispositivo al final a la derecha hay un botón que tiene tres puntos. Le damos ahí y se nos abre otra ventana. En esa ventana le damos a agregar y seleccionamos la base de datos. Una vez localizada la base de datos la seleccionamos y le damos a <strong>Aceptar</strong>. Funciona en la ultima versión de SQL Server, tambien funcion en Azure Data Studio su ultima versión.
    </p>
    <h2>Scaffold-DbContext</h2>
    <p>Una vez la base de datos ha sido restaurada, en Visual Studio en la parte inferior si está activo veremos el administrador de paquetes. Si no está activo hay que activarlo. Para proceder a la activación le damos a: Ver-->Otras ventanas--><strong>Consola del Administrador de paquetes</strong> 
        En esta consola ponemos el siguiente comando:</p>
    <pre><code>Scaffold-DbContext "Data Source=NOMBRESERVIDORBASEDATOS;Initial Catalog=NOMBREBASEDATOS;Integrated Security=True;TrustServerCertificate=True" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -force -project NOMBREPROYECTO</code></pre>
    <p>Estos campos se obtienen:
        <br/>
    - <strong>NOMBRESERVIDORBASEDEDATOS</strong>: Se obtiene al abrir el programa SQL Server. Lo normal es que sea el nombre del equipo.
        <br/>
    - <strong>NOMBREBASEDATOS</strong>: En el programa de SQL Server vemos qué nombre tiene nuestra base de datos. 
        <br/>
    - <strong>NOMBREPROYECTO</strong>: El nombre que tenga nuestro proyecto en visual studio.
        <h2>Cadena de Conexion con usuario y contraseña en base datos (uso recomendado)</h2>
        <pre><code>Scaffold-DbContext "Data Source=NOMBRESERVIDORBASEDATOS;Initial Catalog=NOMBREBASEDATOS;User ID=NOMBREUSUARIO;Password=CONTRASEÑAUSUARIO;TrustServerCertificate=True" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Domain/Models -force -project NOMBREPROYECTO</code></pre>
    <p>Aquí los unicos campos nuevos que se agregan es:
    <br/>
        -<strong>NOMBREUSUARIO</strong>: aquí ponemos el nombre de usuario que tengamos para acceder al sistema de base de datos. 
        -<strong>CONTRASEÑAUSUARIO</strong>: aquí ponemos la contraseña correspondiente a ese usuario
    </p>
    </p>
    <h2>Secretos de usuario</h2>
    <p>Dentro de Visual Studio 2022 hacemos clic derecho en el proyecto nos vamos a donde dice <strong>Administrar secretos de usuario</strong>. Dentro de ese archivo ponemos los valores:</p><pre>
    <code>
{
  "Redis": {
    "ConnectionString": "redis:6379",
    "ConnectionStringLocal": "127.0.0.1:6379"

  },

  "JwtIssuer": "",
  "JwtAudience": "",
  "JWT": {
    "PublicKey": "",
    "PrivateKey": ""

  },
   "ClaveJWT": "",
  "DataBaseConection": {
    "DBHost": "",
    "DockerDbHost": "",
    "DBName": "",
    "DBUserName": "",
    "DBPassword": "SQL#1234"
  },
 
  "Paypal": {
    "ClientId": "",
    "ClientSecret": "",
    "Mode": "sandbox",
    "returnUrlSinDocker": "https://localhost:7056/Payment/Success",
    "returnUrlConDocker": "https://localhost:8081/Payment/Success"
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "UserName": "",
    "PassWord": ""
  }


}
   </code>
    </pre>
<p>El valor de <strong>JwtIssuer: </strong>este valor es para verificar el token. Por ejemplo <pre><code>"JwtIssuer": "GestorInvetarioEmisor"</code></pre></p>
<p>El valor de <strong>JwtAudience:</strong> este valor es para verificar el token. Por ejemplo <pre><code>"JwtAudience": "GestorInventarioCliente",</code></pre></p>
<p>Los valores de <strong>PublicKey y PrivateKey</strong> dejare en este mismo respositorio el codigo necesario para obtener estos valores, esta en la carpeta llamada GeneracionClaves</p>
<p>El valor de <strong>ClaveJWT</strong> tiene que ser un valor largo ya que es el valor que se usa para cifrar y descifrar minimo una logitud de 38 digitos. Por ejemplo
<pre><code> "ClaveJWT": "Curso@.net#2023_Arelance_MiClaveSecretaMuyLarga"</code></pre>
<p>Los valores de <strong>DataBaseConection</strong> tienen que establecerse de esta manera:
  <ul>
      <li><strong>DBHost</strong>: Esta linea tendra como valor la conexión a nuestra base de datos, este dato ya hemos dicho como obtenerlo en donde se explica como obtener los valores de la cadena de conexión, una vez que tengamos puesto el valor en esta linea queda asi <pre><code>"DBHost":"DESKTOP-2TL9C3O\\SQLEXPRESS",</code></pre> este es un valor de ejemplo, tendremos que colocar el valor correspondiente. Si nuestra base de datos esta en Azure Data Studio el valor de este campo y de el primer valor de la cadena de conexión sera <strong>localhost</strong> si por el contrario usamos SQL Server tendra un aspecto similar al ejemplo.</li>
      <li><strong>DockerDbHost</strong>: Como el nombre indica esta es la cadena de conexión para docker este valor sera el nombre que tenga nuestro contenedor que contenga la base de datos en este caso es: <pre><code>"DockerDbHost": "SQL-Server-Local",</code></pre></li>.
      <li><strong>DBName</strong>: como el nombre indica esta linea contendra el nombre de nuestra base de datos.</li>
      <li><strong>DBUserName</strong>: como el nombre indica aqui pondremos el nombre de usuario de nuestra base de datos.</li>
      <li><strong>DBPassword</strong>: como el nombre indica aqui pondremos la contraseña de nuestra base de datos.</li>
      <li><strong>ClientId y ClientSecret</strong>: Para obtener estos valores primero tendremos que tener una cuenta de paypal una vez que la tengamos nos dirigimos a esta pagina <a href="https://developer.paypal.com/home" target="_blank">Ir a paypal developer</a> en esta pagina obtendremos estos 2 datos</li>
      <li><strong>Mode</strong>: esto lo dejaremos tal y como esta. <pre><code>"Mode": "sandbox",</code></pre></li>
      <li><strong>returnUrlSinDocker y returnUrlConDocker</strong>: aqui manejamos las url de retorno de paypal a nuestra pagina web  aqui el motivo por el cual tenemos 2 es por los puertos ya que visual studio le asigna un puerto y docker otro puerto pues con ajustarlo basta. <pre><code>
   "returnUrlSinDocker": "https://localhost:7056/Payment/Success",
   "returnUrlConDocker": "https://localhost:8081/Payment/Success"</code></pre></li>
      <li><strong>UserName y PassWord </strong>: aqui pondremos el usuario y contraseña del correo electronico que vayamos a usar</li>
  </ul>
</p>
</p>
<h2>Modificación del archivo GestorInventarioContext.cs </h2>
<p>Una vez que hemos ejecutado el comando que realiza el scaffold pues tenemos que modificar este archivo agregando lo siguiente lo primero que pondremos en el constructor es:
    <pre><code>  private readonly IConfiguration _configuration;
  public GestorInventarioContext()
  {
  }

  public GestorInventarioContext(DbContextOptions<GestorInventarioContext> options, IConfiguration configuration)
      : base(options)
  {
      _configuration = configuration;
  }</code></pre>
  Esto es necesario ya que lo usaremos para acceder a los valores que estan en el archivo de secretos de usuario.
</p>
<p>Una vez puesto el valor en el constructor vamos a modificar el metodo llamado <strong>OnConfiguring</strong> y lo reemplazamos por esto:
<pre><code>
 protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
 {
     var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
 var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? _configuration["DataBaseConection:DBHost"];
 var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? _configuration["DataBaseConection:DBName"];
 var dbUserName = Environment.GetEnvironmentVariable("DB_USERNAME") ?? _configuration["DataBaseConection:DBUserName"];
 var dbPassword = Environment.GetEnvironmentVariable("DB_SA_PASSWORD") ?? _configuration["DataBaseConection:DBPassword"];

 string connectionString;

 if (isDocker)
 {
     connectionString = $"Data Source={dbHost};Initial Catalog={dbName};User ID=sa;Password={dbPassword};TrustServerCertificate=True";
 }
 else
 {
     // connectionString = $"Data Source={dbHost};Initial Catalog={dbName};Integrated Security=True;TrustServerCertificate=True";
     connectionString = $"Data Source={dbHost};Initial Catalog={dbName};User ID={dbUserName};Password={dbPassword};TrustServerCertificate=True";
 }

 optionsBuilder.UseSqlServer(connectionString);
 }
    
</code></pre>
</p>
<h2>Generar certificado https</h2>
<p>Para ello ponemos el comando: <pre><code> dotnet dev-certs https -ep C:\Users\guill\.aspnet\https\aspnetapp.pfx -p password</code></pre></p>
<p>La ruta la tendran que adaptar a como tengan el nombre de usuario en el pc</p>
<p>Para confiar en el certificado se usa el comando: <pre><code>dotnet dev-certs https --trust</code></pre></p>
<h2>¿Cómo hacer que funcione en docker?</h2>
<p>Para que este proyecto funcione en docker vamos a seguir unos pasos previos antes:
<ul>
    <li>Primero: Si no tenemos un contenedor que contenga una base de datos en docker ejecutamos este comando. <pre><code>
    docker run --name "SQL-Server-Local" -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=SQL#1234" -p 1433:1433 -d mcr.microsoft.com/mssql/server</code></pre> este comando creara y accedera a la base de datos de docker</li>
    <li>Segundo: Creamos el archivo .back en donde tengamos nuestra base de datos si la tenemos en SQL server, los pasos para crear este archivo son:
        <p>
        - Primero: Nos logueamos y desplegamos la carpeta llamada base de datos.
        - Segundo: En esta carpeta tendremos nuestras bases de datos, pues localizamos de la que queremos hacer el archivo .back
        - Tercero: Una vez localizada la base de datos hacemos clic derecho sobre ella y vamos a <strong>Tareas</strong> y dentro de Tareas le hacemos clic en 
        <strong>Copia de seguridad</strong>
        - Cuarto: Se nos abrira una ventana en esa ventana le damos al botón que dice <strong>Agregar</strong>
        - Quinto: Se nos abrira una ventana mas para seleccionar el destino de donde se almacenara nuestra copia de seguridad, tiene este aspecto <strong>
            D:\SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\</strong> en nuestro caso se va ha almacenar en esta ruta en esta ventana al lado de la ruta aparecera un boton con 
        <strong>...</strong> pues le damos a este boton.
        - Sexto: Se abrira otra ventana mas que nos mostrara los directorios que puede "ver" el programa lo recomendable es no cambiar el directorio y dejarlo en el que pone las copias de seguridad por defecto, en esta ventana nos pedira que pongamos el nombre del archivo pues lo ponemos y una vez puesto le ponemos .back esto es importante para poder pasarlo a la base de datos de docker. Una vez puesto el nombre le damos a <strong>Aceptar</strong> esa ventana se cerrara y nos mostrara la ventana que esta debajo de esa ventana pues nuevamente le damos a <strong>Aceptar</strong> y por ultimo le damos otra vez a <strong>Aceptar</strong> con esto habremos creado nuestro archivo .back.
            </p></li>
        <li>Tercero: Una vez creado el archivo .back ejecutamos el comando: <pre><code>
            docker cp "D:\SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\GestorInventario-2024710-18-27-46.bak" SQL-Server-Local:/var/opt/mssql/data</code></pre> la primera parte de este comando es donde esta nuestro archivo .back, la segunda parte la dejamos tal y como esta que es <strong>SQL-Server-Local:/var/opt/mssql/data</strong></li>
    <li>Cuarto: Creamos una red en docker para que base de datos y el contenedor que este nuesta aplicacion se puedan comunicar, para ello ejecutamos el comando:
    <pre><code>docker network create --attachable <nombre de la red></code></pre> siendo nombre de la red el nombre que nosotros pongamos a esa red. </li>
    <li>Quinto: Nos conectamos a esa red con este comando: <pre><code>docker network connect <nombre de la red> <nombre del contenedor></code></pre> </li>
  <li>Sexto: Para ver si el contenedor esta conectado a la red creada ponemos el comando: <pre><code>docker network inspect <nombre de la red></code></pre></li>
    <li>Septimo: generamos el certificado https si no lo hemos generado con los comandos: <pre><code>
        dotnet dev-certs https -ep C:\Users\guill\.aspnet\https\aspnetapp.pfx -p password
    dotnet dev-certs https --trust
    </code></pre> </li>
    <li>Octavo: Establecemos las variables de entorno ejecutando los comandos: <pre><code>
        cd .\GestorInventario
        ./SetEnvironmentVariables.ps1</code></pre></li>
</ul>
</p>
<h2>Establecer las variables de entorno </h2>
<p>Es necesario solo si usamos docker. Si no usamos docker se puede establecer para observar el como trabaja una variable de entorno</p>
<p>Para ello ejecutamos el comando:<pre><code> 
    cd .\GestorInventario  
    ./SetEnvironmentVariables.ps1</code></pre> </p>


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
        <li>Docker: configuración necesaria para integrar en docker</li>
        <li>Redis: configuración necesaria para que funcione correctamente en redis.</li>
    </ul>

</body>
</html>
