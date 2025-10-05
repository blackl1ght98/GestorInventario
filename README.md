# Guía de instalación para usar el proyecto Gestor Inventario

## 📑 Índice
1. [Requisitos](#-requisitos)
2. [Notas](#-notas)
3. [Instalación](#instalación)
   - [Problema común: Docker y Visual Studio](#-problema-común-docker-y-visual-studio)
   - [Restaurar la copia de seguridad](#-restaurar-la-copia-de-seguridad)
   - [Scaffold-DbContext](#%EF%B8%8F-scaffold-dbcontext)
   - [Secretos de usuario](#secretos-de-usuario)
   - [Modificación del archivo GestorInventarioContext.cs](#modificación-del-archivo-gestorinventariocontextcs)
   - [Generar certificado HTTPS](#generar-certificado-https)
   - [Docker](#docker)
4. [Credenciales de prueba](#credenciales-para-probar)
5. [Características](#características)
6. [Novedades](#novedades)


## ✅ Requisitos

Antes de comenzar asegúrate de tener instalado lo siguiente:

- 💻 **Sistema operativo**:  
  - Windows 10 (verificado)  
  - Windows 11 (verificado)  
  > ⚠️ No testeado en Linux ni MacOS  

- 🛠️ **Herramientas de desarrollo**:  
  - [Visual Studio 2022](https://visualstudio.microsoft.com/) (última versión, con carga de trabajo **ASP.NET y desarrollo web**)  
  - [.NET 9.0 SDK](https://dotnet.microsoft.com/)  

- 🗄️ **Base de datos**:  
  - [SQL Server](https://www.microsoft.com/es-es/sql-server/sql-server-downloads) (última versión)  
  - [SQL Server Management Studio (SSMS)](https://aka.ms/ssmsfullsetup)  para gestionar la BD  

- ⚡ **Servicios adicionales**:  
  - [Redis](https://redis.io/) → solo si vas a usar Docker  
  - [Docker Desktop](https://www.docker.com/products/docker-desktop/) *(opcional, para despliegues en contenedor)*  


## 📝 Notas

- ✅ Proyecto probado en **Windows 10** y **Windows 11**.  
- ⚠️ **No testeado en Linux ni MacOS** (puede requerir ajustes adicionales).  
- 🔧 Se recomienda instalar y usar **SQL Server Express** con **SQL Server Management Studio** (SSMS) .  
- 🔑 Mantener credenciales y claves JWT en **User Secrets** o variables de entorno (no en el código fuente) en caso de integrar nuevas.  
- 💳 La integración con PayPal funciona en **modo sandbox** por defecto.  
- 🌐 Si quieres pasar a producción, recuerda cambiar `Mode: sandbox` → `Mode: live` y registrar tus credenciales reales en PayPal Developer.

# Instalación
## 🐳 Problema común: Docker y Visual Studio

Si **no tienes instalado Docker Desktop**, Visual Studio puede mostrar un error de compilación al intentar interpretar el archivo `docker-compose`.

### 🔧 Solución rápida

1. Abre **Visual Studio** y ve al **Explorador de Soluciones**.  
2. Haz **clic derecho** sobre el proyecto `docker-compose`.  
3. Selecciona **“Descargar proyecto”** (*Unload Project*).  
4. Vuelve a compilar el proyecto → ya no tendrás el error. ✅  

### ➕ Nota adicional
- Si más adelante instalas **Docker Desktop**, puedes volver a habilitar `docker-compose` haciendo clic derecho en el proyecto y seleccionando **“Volver a cargar”** (*Reload Project*).  



## 📂 Restaurar la copia de seguridad

Para usar la base de datos del proyecto, primero debes restaurar la copia de seguridad **`GestorInventarioDB.bak`** en **SQL Server**.  

### 🔧 Pasos en SQL Server Management Studio (SSMS)

1. Descarga e instala **SQL Server Express** desde [aquí](https://www.microsoft.com/es-es/sql-server/sql-server-downloads).  
2. Descarga e instala **SQL Server Management Studio (SSMS)** desde [aquí](https://aka.ms/ssmsfullsetup).  
3. Abre **SSMS** e inicia sesión con la configuración predeterminada:  
   - **Servidor**: Nombre del equipo (ejemplo: `DESKTOP-XXXX\SQLEXPRESS`)  
   - **Autenticación**: Windows Authentication (no requiere contraseña).  
4. En el **Explorador de objetos**, haz clic derecho en **Bases de datos** → **Restaurar base de datos**.  
5. Antes de continuar, copia el archivo de respaldo **`GestorInventarioDB.bak`** a la carpeta de backups de SQL Server, ya que el explorador de SSMS no muestra todas las rutas del sistema.  
   - Ruta típica:  
     ```
     E:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup
     ```  
   - Si tu instalación está en otra ubicación, copia el archivo en la carpeta **Backup** equivalente.  
6. En la ventana de restauración:  
   - Selecciona **Dispositivo**.  
   - Haz clic en el botón `...` (a la derecha).  
   - Pulsa **Agregar** y busca el archivo `GestorInventarioDB.bak` en la carpeta `Backup`.  
   - Confirma con **Aceptar**.  
7. Haz clic en **Aceptar** nuevamente para iniciar la restauración ✅. 

## ⚙️ Scaffold-DbContext

Una vez restaurada la base de datos, necesitamos generar las clases de modelo en el proyecto con **Entity Framework Core** mediante el comando `Scaffold-DbContext`.

---

### 📌 Abrir la Consola del Administrador de Paquetes
En **Visual Studio**:  
1. Activa la consola desde: `Ver > Otras ventanas > Consola del Administrador de paquetes`.  
2. Ejecuta el siguiente comando (ajustando los parámetros a tu entorno):

```sh
Scaffold-DbContext "Data Source=NOMBRESERVIDORBASEDATOS;Initial Catalog=NOMBREBASEDATOS;Integrated Security=True;TrustServerCertificate=True" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Domain/Models -Force -Project NOMBREPROYECTO
````
**NOMBRESERVIDORBASEDATOS**: Nombre del servidor de SQL Server. Suele ser el nombre del equipo `DESKTOP-XXXX\SQLEXPRESS`
**NOMBREBASEDATOS**: Nombre de la base de datos. En este caso: `GestorInventario`.
**NOMBREPROYECTO**: Nombre del proyecto de Visual Studio. En este caso: `GestorInventario` 
## 🔑 Scaffold-DbContext con usuario y contraseña (recomendado)
```sh
Scaffold-DbContext "Data Source=NOMBRESERVIDORBASEDATOS;Initial Catalog=NOMBREBASEDATOS;User ID=NOMBREUSUARIO;Password=CONTRASEÑAUSUARIO;TrustServerCertificate=True" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Domain/Models -Force -Project NOMBREPROYECTO
````
**NOMBREUSUARIO**: Usuario de la base de datos por ejemplo `sa`  
**CONTRASEÑAUSUARIO**: Contraseña de ese usuario  
En este proyecto se ha empleado la segunda opcion del comando scaffold
## 🔐 Secretos de usuario

Para acceder al archivo de **Secretos del usuario** en Visual Studio 2022:  
`Clic derecho sobre el proyecto > Administrar secretos de usuario`.

Luego, agrega los siguientes valores en formato JSON:

```json
{
  "Redis": {
    "ConnectionString": "redis:6379",
    "ConnectionStringLocal": "127.0.0.1:6379"
  },
  "AuthMode": "Symmetric",
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
  "LicenseKeyAutoMapper": "",
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "UserName": "",
    "PassWord": ""
  }
}
````
### Significado de cada valor
- **AuthMode**: este valor se encarga de almacenar el modo de autenticacion tenemos 3 modos: `Symmetric`,`AsymmetricFixed` y `AsymmetricDynamic`
- **JwtIssuer**: este valor sirve para verificar el token, por ejemplo:
```sh
"JwtIssuer": "GestorInvetarioEmisor"
````
- **JwtAudience**: este sirve para verificar el token,  por ejemplo:
```sh
"JwtAudience": "GestorInventarioCliente"
````
- **JWT**: 
  - **PublicKey y PrivateKey**: para obtener estos valores vamos al siguiente al siguiente enlace: .
**GeneracionClaves**
- **ClaveJWT**: Cadena larga para cifrar/descifrar tokens (mínimo 38 caracteres). Por ejemplo:
```sh
"ClaveJWT": "Curso@.net#2023_Arelance_MiClaveSecretaMuyLarga"
````
- **DataBaseConection**: Información sensible de la base de datos  
    - **DBHost**: Nombre del servidor SQL. Por ejemplo. ` "DBHost": "DESKTOP-2TL9C3O\\SQLEXPRESS"`.  
    - **DockerDbHost**: Nombre del contenedor Docker de la base de datos. Por ejemplo. `SQL-Server-Local`  
    - **DBName**: Nombre de la base de datos.  
    - **DBUserName**: Usuario de la base de datos.  
    - **DBPassword**: Contraseña del usuario.  
-**Paypal**:Configuración para la pasarela de pagos.  
    - **ClientId y ClientSecret**: Se obtienen creando una cuenta en [aqui](https://developer.paypal.com/home)   
    - **Mode**: `sandbox` para pruebas, `live`  para producción.    
    - **returnUrlSinDocker**: URL de retorno cuando no se usa Docker.  
    - **returnUrlConDocker**: URL de retorno cuando se usa Docker.  
- **Email**: Configuración para envío de correos.  
    - **Host**: Servidor SMTP. Por ejemplo `smtp.gmail.com`  
    - **Port**: Puerto del servidor. Por ejemplo `587`  
    - **UserName y Password**: Credenciales de la cuenta de correo.
   

**LicenseKeyAutoMapper**: Clave de licencia de AutoMapper. Se obtiene registrándose en [obtener licencia](https://luckypennysoftware.com/#automapper) y usando la licencia Community.
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
## Generar certificado HTTPS en caso de no tenerlo
### Generar certificado https
Para generar el certificado https ponemos el comando:
```sh
dotnet dev-certs https -ep C:\Users\<TU USUARIO>\.aspnet\https\aspnetapp.pfx -p password
````
En la ruta tendremos que poner el nombre de usuario de nuestro pc por ejemplo
```sh
dotnet dev-certs https -ep C:\Users\guillermo\.aspnet\https\aspnetapp.pfx -p password

````
Para confiar en el certificado generado ponemos el comando:
```sh
dotnet dev-certs https --trust
````
# Docker
**¿Como arrancar el proyecto en docker?**
Para arrancar este proyecto en docker nos saldremos de visual studio y abriremos la terminal en la carpeta raiz del proyecto y pondremos el comando 
````sh
docker-compose up -d --build
````
# Credenciales para probar
- **Email**: keuppa@yopmail.com
- **Contraseña**: 1A2a3A4a5@
- Estas credenciales para probar son del usuario administrador.

# Características 

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
- **Cambio de precio en los planes**: El administrador puede cambiar el precio de los planes

## Novedades
- **Rembolsos parciales**: esta nueva funcion permite devolver parte de los productos de un pedido, esta funcion la veremos siempre y cuando el pedido que se realice tenga mas de un producto
- **Generacion de codigos de barras**: nueva funcion que permite simular como si fuese una tienda.
- **Agregar informacion de envio**: con esta nueva funcionalidad  podemos agregar informacion sobre que empresa se encarga de repartir el pedido
-  **Activacion de subscripcion**: El administrador puede activar una subscripcion cancelada o suspendida
-  **Suspender subscripcion**: El usuario puede suspender su propia subscripcion, y el administrador puede suspender las de todos
-  **Cancelar subscripcion**: El usuario puede cancelar su propia subscripcion, y el administrador puede cancelar cualquier susbscripcion
-  **Agregar informacion de seguimiento a pedidos**: El administrador puede agregar informacion de seguimiento a los pedidos
