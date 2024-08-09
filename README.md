# Guía de instalación para usar el proyecto Gestor Inventario

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

