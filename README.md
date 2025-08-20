# Guía de instalación para usar el proyecto Gestor Inventario

## Requisitos

- **Visual Studio 2022** en su última versión.
- **SQL Server** en su última versión.
- **Azure Data Studio** (para Docker).
- **Redis** (si usas Docker).
- **Docker**.(opcional)
- **.NET 9.0** instalado.
- **Sistema operativo**: Windows 10 (verificado), Windows 11 (verificado).
### Notas
No testeado en **Linux** 
## ❌ Problema con Docker y Visual Studio
Si **Docker Desktop no está instalado**, Visual Studio puede dar un error de compilación al intentar interpretar `docker-compose`. Para evitarlo:

1. Abre Visual Studio y ve al **Explorador de Soluciones**.
2. **Haz clic derecho en el proyecto `docker-compose`**.
3. Selecciona **"Descargar proyecto"** (`Unload Project`).
4. Ahora puedes compilar sin errores.

Si en el futuro instalas Docker Desktop, puedes volver a habilitar `docker-compose` haciendo clic derecho en el proyecto y seleccionando **"Volver a cargar" (`Reload Project`)**.

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
 "AuthMode": "Symmetric", // "Symmetric", "AsymmetricFixed", "AsymmetricDynamic"
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
LicenseKeyAutoMapper:
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "UserName": "",
    "PassWord": ""
  }


}
````
### ¿Como obtener cada valor del archivo de secretos?
- **AuthMode**: este valor se encarga de manejar el modo de autenticacion
- **JwtIssuer**: este valor se encarga de verificar el token. El valor que tiene que tener es el que nosotros queramos por ejemplo:
```sh
"JwtIssuer": "GestorInvetarioEmisor"
````
- **JwtAudience**: este valor se encarga de verificar el token. El valor que tiene que tener es el que nosotros queramos por ejemplo:
```sh
"JwtAudience": "GestorInventarioCliente"
````
- **JWT**: 
  - **PublicKey y PrivateKey**: para obtener estos valores he dejado en el repositorio el código necesario para obtener estos valores, para ello vamos a la carpeta **GeneracionClaves**.
**GeneracionClaves**
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

**LicenseKeyAutoMapper**: aquí ponemos la clave de licencia de AutoMapper para ello vamos aqui [obtener licencia](https://luckypennysoftware.com/#automapper) en esta pagina nos registramos y la licencia a escoger es la community
## Modificación del archivo GestorInventarioContext.cs 
Una vez que hemos ejecutado el comando que realiza el scaffold pues tenemos que modificar este archivo agregando lo siguiente al metodo **OnConfiguring**
```csharp
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
 {
     var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";

     if (isDocker)
     {
         var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
         var dbName = Environment.GetEnvironmentVariable("DB_NAME");
         var dbPassword = Environment.GetEnvironmentVariable("DB_SA_PASSWORD");

         var connectionString = $"Data Source={dbHost};Initial Catalog={dbName};User ID=sa;Password={dbPassword};TrustServerCertificate=True";
         optionsBuilder.UseSqlServer(connectionString);
     }
     else
     {
         // Cadena de conexión en duro para entorno local
         var connectionString = "Data Source=GUILLERMO\\SQLEXPRESS;Initial Catalog=GestorInventario;User ID=sa;Password=SQL#1234;TrustServerCertificate=True";
         optionsBuilder.UseSqlServer(connectionString);
     }
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
## Docker compose
````sh
networks:
  gestor:
    driver: bridge
services:
  sql-server:
    image: mcr.microsoft.com/mssql/server
    container_name: SQL-Server-Local
    user: root  # Forzamos ejecución como root para evitar problemas de permisos
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=SQL#1234
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sql-data:/var/opt/mssql
      - ./GestorInventario-2025629-10-14-552.bak:/var/opt/mssql/data/GestorInventario-2025629-10-14-552.bak
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P SQL#1234 -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 5s
      retries: 10
    command: 
      - /bin/bash
      - -c 
      - |
        # Iniciar SQL Server en segundo plano
        /opt/mssql/bin/sqlservr &
        
        # Esperar a que SQL Server esté listo
        sleep 30
        
        # Instalar herramientas necesarias
        apt-get update && apt-get install -y curl gnupg
        curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add -
        curl https://packages.microsoft.com/config/ubuntu/20.04/prod.list > /etc/apt/sources.list.d/mssql-release.list
        apt-get update
        ACCEPT_EULA=Y apt-get install -y mssql-tools
        
        # Configurar PATH para mssql-tools
        echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc
        source ~/.bashrc
        
        # Restaurar la base de datos
        /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P SQL#1234 -Q "RESTORE DATABASE GestorInventario FROM DISK = '/var/opt/mssql/data/GestorInventario-2025629-10-14-552.bak' WITH MOVE 'GestorInventario' TO '/var/opt/mssql/data/GestorInventario.mdf', MOVE 'GestorInventario_log' TO '/var/opt/mssql/data/GestorInventario_log.ldf', REPLACE;"
        
        # Mantener el contenedor en ejecución
        wait
    networks:
      - gestor

  gestorinventario:
    container_name: gestor-inventario
    image: ${DOCKER_REGISTRY-}gestorinventario
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
      - "8081:8081"
    environment:
      - DB_HOST=sql-server
      - DB_NAME=GestorInventario
      - DB_SA_PASSWORD=SQL#1234
      - IS_DOCKER=true
      - USE_REDIS=true
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/GestorInventario.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=password
      - ClaveJWT=${ClaveJWT}
      - REDIS_CONNECTION_STRING=redis:6379
      - JwtIssuer=${JwtIssuer}
      - JwtAudience=${JwtAudience}
      - PublicKey=${PublicKey}
      - PrivateKey=${PrivateKey}
      - DB_USERNAME=${DB_USERNAME}
      - Paypal_ClientId=${Paypal_ClientId}
      - Paypal_ClientSecret=${Paypal_ClientSecret}
      - Paypal_Mode=${Paypal_Mode}
      - Paypal_returnUrlConDocker=${Paypal_returnUrlConDocker}
      - Paypal_returnUrlSinDocker=${Paypal_returnUrlSinDocker}
      - Email__Host=${Email__Host}
      - Email__Port=${Email__Port}
      - Email__Username=${Email__Username}
      - Email__Password=${Email__Password}
    volumes:
      - C:/Users/guill/AppData/Roaming/ASP.NET/Https/GestorInventario.pfx:/https/GestorInventario.pfx:ro
    depends_on:
      sql-server:
        condition: service_healthy
      redis:
        condition: service_started
    networks:
      - gestor

  redis:
    image: redis:latest
    container_name: redis
    ports:
      - "6379:6379"
    volumes:
      - redisdata:/data
    networks:
      - gestor

volumes:
  sql-data:
    driver: local
  redisdata:
    driver: local
  appdata:
    driver: local
````
**¿Como arrancarlo en docker?**
Para arrancar este proyecto en docker nos saldremos de visual studio y abriremos la terminal en la carpeta raiz y pondremos el comando 
````sh
docker-compose up -d --build
````
  ## Características con las que cuenta el proyecto

El proyecto **Gestor Inventario** ofrece una amplia gama de características para gestionar eficientemente el inventario:

- **Gestión de Datos**: Permite realizar operaciones CRUD (Crear, Leer, Actualizar, Eliminar).
- **Autenticación Robusta**: El sistema de autenticación se basa en la generación de tokens y ofrece tres métodos de autenticación: Autenticación simétrica, Autenticación asimétrica con clave pública y privada fija, Autenticación asimétrica con clave pública y privada dinámica.
- **Generación de Informes**: Los usuarios pueden descargar informes en formato PDF del historial de pedidos y productos.
- **Notificaciones por Correo Electrónico**: El sistema envía notificaciones por correo electrónico cuando el stock de un producto está bajo.
- **Registro y Acceso de Usuarios**: Los usuarios pueden registrarse y acceder al sistema. Cuando un nuevo usuario se registra, se le envía un correo electrónico de confirmación.
- **Panel de Administración de Usuarios**: El proyecto incluye un panel de administración de usuarios para gestionar las cuentas de usuario.
- **Sistema Basado en Roles**: El acceso a diferentes niveles del sistema se controla mediante un sistema basado en roles.
- **Pasarela de Pago PayPal**: El proyecto incluye la implementación de una pasarela de pago PayPal.
- **Restablecimiento de Contraseña**: El usuario como el administrador puede restablecer la constraseña si es un usuario solo puede restablecer la suya y un administrador puede restablecer la de todos.
- **Flexibilidad en la Autenticación**: Los usuarios pueden cambiar entre los modos de autenticación de manera efectiva comentando y descomentando el código correspondiente.
- **Alta y baja de usuarios**: El administrador puede dar de alta o baja a un usuario o varios usuarios.
- **Docker**: Configuración necesaria para integrar en Docker.
- **Redis**: Configuración necesaria para que funcione correctamente en Redis.
- **Función de reembolso**: Ahora cuenta con la función de reembolsar un pedido
-  **Creacion de planes y productos con paypal**: Actualmente cuenta con la funcionalidad de crear productos y planes en paypal 
- **Función de suscripcion a planes**: Actualmente cuenta con la posibilidad de suscribirse a planes.
- **Ver planes**: Los usuarios pueden ver los planes a los cuales pueden suscribirse
- **Ver subscriptores**: El administrador puede ver cuantos subscriptores ahi subscriptos
- **Ver Productos**: El administrador puede ver los productos que estan asociados a los planes
- ** Cambio de precio en los planes** El administrador puede cambiar el precio de los planes

## Novedades
- **Rembolsos parciales**: esta nueva funcion permite devolver parte de los productos de un pedido, esta funcion la veremos siempre y cuando el pedido que se realice tenga mas de un producto
- **Generacion de codigos de barras**: nueva funcion que permite simular como si fuese una tienda.
- **Agregar informacion de envio**: con esta nueva funcionalidad  podemos agregar informacion sobre que empresa se encarga de repartir el pedido
-  **Activacion de subscripcion**: El administrador puede activar una subscripcion cancelada o suspendida
-  **Suspender subscripcion**: El usuario puede suspender su propia subscripcion, y el administrador puede suspender las de todos
-  **Cancelar subscripcion**: El usuario puede cancelar su propia subscripcion, y el administrador puede cancelar cualquier susbscripcion
-  **Agregar informacion de seguimiento a pedidos**: El administrador puede agregar informacion de seguimiento a los pedidos
