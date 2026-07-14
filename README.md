# Guía de instalación para usar el proyecto Gestor Inventario

# 📑 Índice
- ⚠️ Requisitos para ejecutarlo con docker
- ⚠️ Requisitos para ejecutarlo en local
- 🔑 Generación de certificado HTTPS
- 🐳 Puesta en marcha para ejecutacion con docker
- 📝 Credenciales de prueba 
- Puesta en marcha en local
   - 📂 Restaurar base de datos
   - ⚙️ Scaffold-DbContext
       - 🔑 Scaffold-DbContext con usuario y contraseña (recomendado)
   - 🔐 Configurar Secretos de usuario
   - ⚙️ Modificación GestorInventarioContext.cs    
- 🐳 Problemas comunes (Docker / Visual Studio / WSL)
- ✨ Características
- 🆕 Novedades
- 🧠 Notas importantes

# ⚠️ Requisitos para ejecutarlo con docker
Antes de comenzar asegúrate de tener instalado lo siguiente:
-  [Docker Desktop](https://www.docker.com/products/docker-desktop/)
-  [Git](https://git-scm.com/)

# ⚠️ Requisitos para ejecutarlo en local
Antes de comenzar asegúrate de tener instalado lo siguiente:
Tener instalado lo siguiente:
  - [Visual Studio 2022](https://visualstudio.microsoft.com/) 
  - [Git](https://git-scm.com/)  
  - [SQL Server](https://www.microsoft.com/es-es/download/details.aspx?id=104781)
  - [SQL Server Management Studio (SSMS)](https://aka.ms/ssmsfullsetup)  para gestionar la BD
    
# 🔑 Configuración común (Docker y entorno local)
Para que todo quede configurado ejecutar el script **install.ps1** este script es un instalador guiado que te indicara que valores poner.
```powershell
./install.ps1
````
Una vez termine script se generara un archivo **.env** con las variables de entorno el cual nos ayudara a la hora del despliegue sin el uso de docker en lo que ayuda es a rellenar los valores del archivo de secretos.
PROXIMAMENTE PONDRE SCRIPT SEPARADOS PARA CERTIFICADO Y VARIABLES DE ENTORNO
Al ejecutarlo nos pedira  que pongamos ciertos valores lo cual los pondremos, el proceso de poner los valores es guiado asi que no habra perdida en donde conseguir cada valor.
NOTA: si no quieres usar doker y quieres desplegarlo en visual studio generaremos este archivo para rellenar el archivo de secretos.
# 🐳 Puesta en marcha para ejecutacion con docker
1. Clonar el repositorio con el comando:
```sh
git clone https://github.com/blackl1ght98/GestorInventario
````
2. Ejecutar el Instalador
Para que todo quede configurado ejecutar el script **install.ps1** este script es un instalador guiado que te indicara que valores poner.
```powershell
./install.ps1
````


# Credenciales para probar
- **Email**: keupa@yopmail.com
- **Contraseña**: 1a2a3a4a5
- Estas credenciales para probar son del usuario administrador.
 Una vez instalado reiniciamos docker y ya dejaria iniciarlo.
# Puesta en marcha en local:
## 📂 Restaurar la copia de seguridad
El primer paso que tendremos que realizar es la restauración de la base de datos para ello hacemos lo siguiente:
1. Abrir **SSMS**
2. Marcar la casilla de **Certificado de servidor de confianza**
3. Darle a conectar
4. En el **Explorador de objetos**, haz clic derecho en **Bases de datos** → **Restaurar base de datos** en caso de tener el programa en ingles la opción para restaurar la base de datos se llama **Database**->**Restore database**.
5. Para no tener problema con la restauración de la base de datos mover el archivo `.back` a la  ruta `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\` la carpeta   `MSSQL17.SQLEXPRESS` puede variar dependiendo de la version que tengamos pero esta es la ruta tipica.
6. Una vez que estemos en la ventana de restauración hacemos lo siguiente:
      - Selecciona **Dispositivo**.
      - Haz clic en el botón `...`
      -  Pulsa **Agregar** y busca el archivo `.bak` en la carpeta `Backup`.
      -  Confirma con **Aceptar**.
      -  Hacer nuevamente en **Aceptar** para completar la restauración




## 🔐 Configurar Secretos de usuario

Para acceder al archivo de **Secretos del usuario** en Visual Studio 2022:  
`Clic derecho sobre el proyecto > Administrar secretos de usuario`.

Luego, agrega los siguientes valores en formato JSON:

```json
{
  "Redis": {
    "ConnectionString": "redis:6379",
    "ConnectionStringLocal": "127.0.0.1:6379"
  },
  "AuthMode": "AsymmetricDynamic",
  "JwtIssuer": "GestorInvetarioEmisor",
  "JwtAudience": "GestorInventarioCliente",
  "JWT": {
    "PublicKey": "",
    "PrivateKey": ""
  },
  "ClaveJWT": "IntroduceClaveLarga",
  "IsMfaEnabled": true,
 "CallMeBot": {
   "user": ""
 },
  "DataBaseConection": {
    "DBHost": "",
    "DockerDbHost": "SQL-Server-Local",
    "DBName": "GestorInventario",
    "DBUserName": "sa",
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
**DBHost**: esto ya lo mencionamos en el comando scaffold pero esto nos lo dice el motor de base de datos a la hora de loguearnos tiene este aspecto: `DESKTOP-XXXX\SQLEXPRESS`
**CallMeBot: user**: Este valor sera tu usuario de telegram 
**AuthMode**: Admite estos valores: Symmetric, AsymmetricFixed, AsymmetricDynamic. De estos tres modos el mas aconsejado es **AsymmetricDynamic** por su seguridad
## Modificación del archivo GestorInventarioContext.cs 
Una vez que hemos ejecutado el comando que realiza el scaffold tenemos  que borrar el metodo **OnConfiguring**
```csharp
   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {}
````
## Configuración de launchSettings.json
En este archivo ajustaremos las variables de entorno con los valores:
```json
 "https": {
   "commandName": "Project",
   "launchBrowser": true,
   "environmentVariables": {
     "ASPNETCORE_ENVIRONMENT": "Development",   
     "USE_REDIS": "false",
     "IS_DOCKER": "false"
   },
   "dotnetRunMessages": true,
   "applicationUrl": "https://localhost:7056;http://localhost:5000;https://localhost:7057"
 },
````
## ⚙️ Scaffold-DbContext
El scaffold solo se ejecutara si la base de datos cambia mientras que no cambie la base de datos no se ejecutara el scaffold
Para ejecutar este comando hacemos lo siguiente:


1. Abrimos **Visual Studio**
2. Activa la consola desde: `Ver > Otras ventanas > Consola del Administrador de paquetes`.  
3. Ejecutar el comando:

```sh
Scaffold-DbContext "Data Source=localhost\SQLEXPRESS;Initial Catalog=GestorInventario;User ID=sa;Password=SQL#1234;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -ContextDir ../GestorInventario.Infrastructure/Data -ContextNamespace "GestorInventario.Infrastructure.Data" -Namespace "GestorInventario.Domain.Models" -force -Project GestorInventario.Domain -NoOnConfiguring 
````



# 🐳 Problemas comunes (Docker / Visual Studio / WSL)
## Visual Studio y Docker
Si **no tienes instalado Docker Desktop**, Visual Studio puede mostrar un error de compilación al intentar interpretar el archivo `docker-compose`.

Para solucionarlo haremos lo siguiente:

1. Abre **Visual Studio** y ve al **Explorador de Soluciones**.  
2. Haz **clic derecho** sobre el proyecto `docker-compose`.  
3. Selecciona **“Descargar proyecto”** (*Unload Project*).  
4. Vuelve a compilar el proyecto → ya no tendrás el error.
 
Si más adelante instalas **Docker Desktop**, puedes volver a habilitar `docker-compose` haciendo clic derecho en el proyecto y seleccionando **“Volver a cargar”** (*Reload Project*).  
# Problema al instalar Docker
Para solucionar este problema hacemos lo siguiente:
1. Vamos a la unidad C en Windows y una vez que estemos habilitamos la opción para ver archivos ocultos.
2. Vamos a la carpeta llamada **ProgramData**
3. Dentro de esa carpeta veremos una carpeta llamada **DockerDescktop**
4. Eliminamos dicha carpeta
Con estos pasos realizados la instalación se completara.
# Problema al iniciar docker (WSL)
El mismo docker nos dice que ejecutemos en la terminal el comando:
```sh
wsl --update
```
pero si esto no lo soluciona lo que haremos es descargar la ultima versión de wsl del repositorio de microsoft: [WSL](https://github.com/microsoft/WSL/releases) instalamos la ultima version del programa y el problema se soluciona


# ✨ Características

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

# 🆕 Novedades
- **Rembolsos parciales**: esta nueva funcion permite devolver parte de los productos de un pedido, esta funcion la veremos siempre y cuando el pedido que se realice tenga mas de un producto
- **Generacion de codigos de barras**: nueva funcion que permite simular como si fuese una tienda.
- **Agregar informacion de envio**: con esta nueva funcionalidad  podemos agregar informacion sobre que empresa se encarga de repartir el pedido
-  **Activacion de subscripcion**: El administrador puede activar una subscripcion cancelada o suspendida
-  **Suspender subscripcion**: El usuario puede suspender su propia subscripcion, y el administrador puede suspender las de todos
-  **Cancelar subscripcion**: El usuario puede cancelar su propia subscripcion, y el administrador puede cancelar cualquier susbscripcion
-  **Agregar informacion de seguimiento a pedidos**: El administrador puede agregar informacion de seguimiento a los pedidos
-  **MFA Implementado**: Autenticacion de doble factor implementado a nivel global
-  **Notificaciones via telegram**: Notifica al usuario de eventos importantes
 
  # 🧠 Notas importantes

- ✅ Proyecto probado en **Windows 10** y **Windows 11**.  
- ⚠️ **No testeado en Linux ni MacOS** (puede requerir ajustes adicionales).  
- 🔧 Se recomienda instalar y usar **SQL Server Express** con **SQL Server Management Studio** (SSMS) .  
- 🔑 Mantener credenciales y claves JWT en **User Secrets** o variables de entorno (no en el código fuente) en caso de integrar nuevas.  
- 💳 La integración con PayPal funciona en **modo sandbox** por defecto.  
- 🌐 Si quieres pasar a producción, recuerda cambiar `Mode: sandbox` → `Mode: live` y registrar tus credenciales reales en PayPal Developer.
 

