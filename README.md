# Guía de instalación para usar el proyecto Gestor Inventario

## Requisitos

- **Visual Studio 2022** en su última versión.
- **SQL Server** en su última versión.
- **Azure Data Studio** (para Docker).
- **Redis** (si usas Docker).
- **Docker**.
- **.NET 8.0** instalado.
- **Sistema operativo**: Windows 10 (verificado).

### Notas

- No testeado en **Linux** ni en **Windows 11**.
## Restaurar la copia de seguridad

Primero, restaurar la copia de seguridad **GestorInventarioDB** usando Microsoft SQL Server. Si no disponen de este programa tendrán que descargarlo de la página web de Microsoft. Puedes descargarlo desde [aquí](https://www.microsoft.com/es-es/sql-server/sql-server-downloads). Instalamos la versión **Express** y seguimos los pasos de instalación del instalador. Una vez se complete, tendremos que instalar la interfaz gráfica de SQL Server, que puedes descargar desde [aquí](https://learn.microsoft.com/es-es/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver16).

Una vez instalado, procedemos a abrirlo. Aparecerá una ventana que mostrará el tipo de servidor, nombre del servidor, autenticación. Esto lo dejaremos tal y como viene sin poner contraseña. Luego, hacemos clic en **Conectar**.

Nos dirigimos a la parte izquierda de la pantalla y veremos **Servidores registrados**. Sobre la carpeta **Base de datos**, hacemos clic derecho y seleccionamos **Restaurar base de datos**.

En la ventana que se abre, seleccionamos **Dispositivo** y, al final a la derecha, hay un botón con tres puntos. Hacemos clic ahí, y en la nueva ventana seleccionamos **Agregar** y localizamos la base de datos. Una vez seleccionada, clic en **Aceptar**. Funciona en la última versión de SQL Server, y también en la última versión de Azure Data Studio.

## Scaffold-DbContext

Una vez la base de datos ha sido restaurada, en Visual Studio, si está activo, veremos la **Consola del administrador de paquetes**. Si no está activo, debemos activarlo: `Ver > Otras ventanas > Consola del Administrador de paquetes`.
En la consola del **Consola del Administrador de paquetes**, ejecutamos el siguiente comando:

```sh
Scaffold-DbContext "Data Source=NOMBRESERVIDORBASEDATOS;Initial Catalog=NOMBREBASEDATOS;Integrated Security=True;TrustServerCertificate=True" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Domain/Models -force -project NOMBREPROYECTO
````
### ¿Como se obtienen los parametros del comando anterior?
- **NOMBRESERVIDORBASEDATOS**:  Se obtiene al abrir el programa SQL Server. Lo normal es que sea el nombre del equipo.
- **NOMBREBASEDATOS**:Aquí pondremos el nombre de la base de datos en este caso el nombre de la base de datos es **GestorInventario**.
- **NOMBREPROYECTO**:Aquí pondremos el nombre del proyecto en este caso es **GestorInventario**.
>Ejemplo del comando **Scaffold**:
```sh
Scaffold-DbContext "Data Source=DESKTOP-2TL9C3O\SQLEXPRESS;Initial Catalog=GestorInventario;Integrated Security=True;TrustServerCertificate=True" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Domain/Models -force -project GestorInventario
````
### Scaffold-DbContext con contraseña (uso recomendado)
Aunque la anterior cadena de conexión sigue pudiendose usar, pero lo recomendable es ponerlo con contraseña para evitar que cualquier persona acceda a contenido no autorizado.
```sh
Scaffold-DbContext "Data Source=NOMBRESERVIDORBASEDATOS;Initial Catalog=NOMBREBASEDATOS;User ID=NOMBREUSUARIO;Password=CONTRASEÑAUSUARIO;TrustServerCertificate=True" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Domain/Models -force -project NOMBREPROYECTO
```
### ¿Como se obtienen los parametros del comando anterior?
Parecido al anterior comando pero con dos propiedades nuevas.
- **NOMBREUSUARIO**:Aquí pondremos el nombre del usuario de la base de datos.
- **CONTRASEÑAUSUARIO**:Aquí pondremos la contraseña de base de datos.
Esta cadena de conexión tiene que estar mas protegida que la anterior porque tiene las credenciales de acceso a base de datos. Esto lo pondremos en el archivo de secretos de usuario de visual studio. Dependiendo de como queramos manejar el como almacenarlo podemos poner la cadena de conexión en el **Program.cs** y los datos delicados ponerlos en el archivo de secretos a continuación veremos el como hacerlo.
>Ejemplo del comando **Scaffold** con contraseña
```sh
Scaffold-DbContext "Data Source=DESKTOP-2TL9C3O\SQLEXPRESS;Initial Catalog=GestorInventario;User ID=pepe;Password=pepe1234;TrustServerCertificate=True" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Domain/Models -force -project GestorInventario
````
## Secretos de usuario
Dentro de Visual Studio 2022 para acceder al archivo de **Secretos del usuario** hacemos lo siguiente. `Clic derecho sobre el proyecto > Administrar secretos de usuario`, una vez que le hemos dado ha **Administrar secretos del usuario** tenemos que poner estos valores:
```sh
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
````
### ¿Como obtener cada valor del archivo de secretos?
- **JwtIssuer**: este valor se encarga de verificar el token. El valor que tiene que tener es el que nosotros queramos por ejemplo:
```sh
"JwtIssuer": "GestorInvetarioEmisor"
````
- **JwtAudience**: este valor se encarga de verificar el token. El valor que tiene que tener es el que nosotros queramos por ejemplo:
```sh
"JwtAudience": "GestorInventarioCliente"
````
-**JWT**: para los tokens con valor fijo:
    - **PublicKey y PrivateKey**: para obtener estos valores he dejado en el repositorio el codigo necesario para obtener estos valores, para ello vamos a la carpeta **GeneracionClaves**
- **ClaveJWT**: tiene que ser un valor largo ya que ese valor se usa para cifrar y descifrar minimo una longitud de **38 digitos alfanumericos**. Por ejemplo:
```sh
"ClaveJWT": "Curso@.net#2023_Arelance_MiClaveSecretaMuyLarga"
````
- **DataBaseConection**: aquí pondremos los valores sensibles correspondientes a la **cadena de conexión**, de esta manera como hemos dicho la cadena de conexion se podra poner en el **Program.cs** pero los valores sensibles estan aquí  en el archivo de secretos.
    - **DBHost**: Aquí pondremos el nombre del servidor de nuestra base de datos por ejemplo. ` "DBHost": "DESKTOP-2TL9C3O\\SQLEXPRESS"`.
    - **DockerDbHost**: Aquí ponemos el nombre del contenedor de base de datos de docker por ejemplo. ` "DockerDbHost": "SQL-Server-Local"`
    - **DBName**: Aquí ponemos el nombre de base de datos.
    - **DBUserName**: Aquí ponemos el nombre de usuario para acceder a esa base de datos.
    - **DBPassword**: Aquí ponemos la contraseña del usuario para acceder a base de datos.
-**Paypal**: Agregar función de pago:
    - **ClientId y ClientSecret**: Para obtener estos datos primero tenemos que tener una cuenta de paypal y despues ir a esta dirección [aqui](https://developer.paypal.com/home) una vez que hemos realizado el login en esta pagina podemos obtener estos valores.
    - **Mode**: este modo se quedara tal y como esta en **sandbox** este modo es el modo prueba de paypal para hacer transacciones.
    - **returnUrlSinDocker**: esta url es cuando paypal nos reedirige a nuestra aplicación esta url es para cuando no estamos usando docker.
    - **returnUrlConDocker**: esta url es para cuando estamos usando docker cumple lo mismo que la anterior redirigir de paypal a la aplicación.
- **Email**: Para el envio de correo electronicos
    - **Host**: servidor de correo electronico.
    - **Port**: puerto del servidor de correo electronico.
    - **UserName**: usuario del correo electronico.
    - **Password**: contraseña del correo electronico.
## Modificación del archivo GestorInventarioContext.cs 
Una vez que hemos ejecutado el comando que realiza el scaffold pues tenemos que modificar este archivo agregando lo siguiente lo primero que pondremos en el constructor es:
```sh
 private readonly IConfiguration _configuration;
  public GestorInventarioContext()
  {
  }

  public GestorInventarioContext(DbContextOptions<GestorInventarioContext> options, IConfiguration configuration)
      : base(options)
  {
      _configuration = configuration;
  }
````
Esto es necesario ya que lo usaremos para acceder a los valores que estan en el archivo de secretos de usuario.
Una vez puesto el valor en el constructor vamos a modificar el metodo llamado **OnConfiguring** y lo reemplazamos por esto:
```sh
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
````
## Generar certificado https
Para generar el certificado https ponemos el comando:
```sh
dotnet dev-certs https -ep C:\Users\guill\.aspnet\https\aspnetapp.pfx -p password
````
La ruta solo tendran que cambiar el nombre de usuario.
Para confiar en el certificado generado ponemos el comando:
```sh
dotnet dev-certs https --trust
````
## ¿Como configurar docker?
Para que docker funcione en este proyecto tenemos que hacer estos pasos:
- **Primero**:Si no tenemos un contenedor que contenga una base de datos en docker ejecutamos este comando:
```sh
 docker run --name "SQL-Server-Local" -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=SQL#1234" -p 1433:1433 -d mcr.microsoft.com/mssql/server
`````
este comando creara y arrancara el contenedor de base de datos
- **Segundo**: Creamos el archivo **.back**. Si estamos usando el programa **SQL Server** los pasos para crear este archivo son los siguientes:
    - **Primero**:Abrimos el programa **SQL Server** y nos logueamos.
    - **Segundo**:Una vez que nos hemos logueado veremos en el lado izquierdo el servidor de base de datos pues le hacemos clic y le damos a la carpeta **Base de datos**.
    - **Tercero**:Localizamos la base de datos de la que queremos el archivo **.back** y hacemos clic derecho sobre esta y hacemos clic en `Tareas > Copia de seguridad`.
    - **Cuarto**:Cuando hemos realizado el paso anterior se abrira una ventana y en esa ventana hacemos clic en **Agregar**.
    - **Quinto**:Una vez echo el paso anterior se nos abrira otra ventana esta ventana muestra la ruta donde se guardan las copias de seguridad de base de datos y al lado de la ruta ahi un botón con el aspecto siguiente **...** pues le damos a este botón.
    - **Sexto**: Una vez que le hemos hecho clic al boton anterior se nos abrira una ventana en la que tendremos que poner el nombre con el que se guarda la copia de seguridad ponemos el nombre que queramos lo recomendable es poner el mismo nombre que el de la base de datos y este nombre terminara en .back por ejemplo: `GestorInventario.back`.
    - **Septimo**: Le damos a **Aceptar** y asi hasta que se cierren las ventanas abiertas
- **Tercero**:Una vez realizado el paso anterior y los pasos dentro del paso anterior ejecutamos el comando:
```sh
docker cp "D:\SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\GestorInventario-2024710-18-27-46.bak" SQL-Server-Local:/var/opt/mssql/data
````
la primera parte del comando la tendremos que ajustar porque la primera parte del comando `"D:\SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\GestorInventario-2024710-18-27-46.bak"` es donde se ubica nuestra copia de seguridad y la segunda parte `SQL-Server-Local:/var/opt/mssql/data` se mantiene igual.
- **Cuarto**: Creamos la red en docker para permitir la comunicacion entre la base de datos y el contenedor que contenga nuestra aplicación y los servicios que necesite para ello seguimos los pasos siguientes:
    - **Creación de la red**: Para ello ejecutamos el comando:
    ```sh
      docker network create --attachable <nombre de la red>
    ```
    - **Conexión de contenedores a la red**: Para ello ejecutamos el comando:
    ```sh
    docker network connect <nombre de la red> <nombre del contenedor>
    ````
    - **Comprobar que los contenedores esten en la misma red**: para ello ejecutamos el comando:
    ```sh
    docker network inspect <nombre de la red>
- **Quinto**: Establecemos las variables de entorno con los comandos:
  ```sh
  cd .\GestorInventario
  ./SetEnvironmentVariables.ps1
  ````
## Establecer variables de entorno (opcional si no usas docker)
Para ello ejecutamos estos comandos:
 ```sh
  cd .\GestorInventario
  ./SetEnvironmentVariables.ps1
  ````
  ## Características con las que cuenta el proyecto

El proyecto **Gestor Inventario** ofrece una amplia gama de características para gestionar eficientemente el inventario:

- **Gestión de Datos**: Permite realizar operaciones CRUD (Crear, Leer, Actualizar, Eliminar) en usuarios, proveedores, productos, pedidos, y el historial de productos y pedidos.
- **Autenticación Robusta**: El sistema de autenticación se basa en la generación de tokens y ofrece tres métodos de autenticación: Autenticación simétrica, Autenticación asimétrica con clave pública y privada fija, Autenticación asimétrica con clave pública y privada dinámica.
- **Generación de Informes**: Los usuarios pueden descargar informes en formato PDF del historial de pedidos y productos.
- **Notificaciones por Correo Electrónico**: El sistema envía notificaciones por correo electrónico cuando el stock de un producto está bajo.
- **Registro y Acceso de Usuarios**: Los usuarios pueden registrarse y acceder al sistema. Cuando un nuevo usuario se registra, se le envía un correo electrónico de confirmación.
- **Panel de Administración de Usuarios**: El proyecto incluye un panel de administración de usuarios para gestionar las cuentas de usuario.
- **Sistema Basado en Roles**: El acceso a diferentes niveles del sistema se controla mediante un sistema basado en roles.
- **Pasarela de Pago PayPal**: El proyecto incluye la implementación de una pasarela de pago PayPal.
- **Restablecimiento de Contraseña**: Los usuarios pueden restablecer su contraseña a través del panel de administrador. Se envía un correo electrónico al usuario seleccionado con una contraseña temporal y un enlace para cambiarla.
- **Flexibilidad en la Autenticación**: Los usuarios pueden cambiar entre los modos de autenticación de manera efectiva comentando y descomentando el código correspondiente.
- **Docker**: Configuración necesaria para integrar en Docker.
- **Redis**: Configuración necesaria para que funcione correctamente en Redis.
   
