<h1>Guia de intalacion para usar el proyecto Gestor Inventario</h1>
<p>Primero  restaurar la copia de seguridad <strong>GestorInventarioDB</strong> usando Microsoft SQL Server si no disponen de este programa tendran que descargarlo de la pagina
  web de Microsoft, descargaremos este <a href="https://www.microsoft.com/es-es/sql-server/sql-server-downloads" target="_blank">Ir a enlace</a> si bajamos hacia abajo en esta pagina veremos
  dos versiones Developer y Express pues instalamos la versión <strong>Express</strong> seguiremos los pasos de instalacion del instalador y una vez se complete tendremos que
  intalar esto otro que es la interfaz grafica de SQL Server <a href="https://learn.microsoft.com/es-es/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver16" target="_blank">Ir a enlace</a>
  una vez instalado procedemos ha habrirlo y nos aparecera una ventana que podra el tipo de servidor, nombre del servidor, autenticacion esto lo dejaremos tal y como viene sin
  poner contraseña. Una vez alcanzado este punto le damos a <strong>Conectar</strong> una vez que le hemos dado nos dirigimos a la parte izquierda de la pantalla y veremos
  que pone al como esto <strong>Servidores registrados</strong> pues le damos clic ahi, una vez que le hemos dado nos aparecera un solo servidor nuestro equipo si no esta 
  desplegado le damos en el simbolito de + y una vez que le hemos dado nos apareceran varias carpetas pues la que nos interesa es la que pone <strong>Base de datos</strong>
  sobre esa carpeta hacemos clic derecho y hacemos clic en <strong>Restaurar base de datos</strong> una vez que hemos hecho clic ahi se nos abre una ventana, en dicha ventana
  seleccionamos <strong>Dispositivo</strong> y en dispositivo al final a la derecha hay un boton que tiene tres puntos pues le damos ahi una vez que le hemos echo clic se nos
  abre otra ventana en esa ventana le damos a agregar y aparecera a que carpetas puedes acceder desde ahi pues tienes que poner la base de datos en una de las carpetas que puedas
  acceder desde ahi el motivo por el cual no puedes acceder a todo es porque hay bastantes carpetas protegidas. Una vez localizada la base de datos la seleccionamos y le damos a 
  <strong>Aceptar</strong> le damos a las otras 2 ventanas al mismo boton.
</p>
<p>Una vez la base de datos ha sido restaurada  en visual studio en la parte inferior si esta activo veremos el administrador de paquetes si no esta activo hay que activarlo
para que podamos verlo le damos a Ver-->Otras ventanas-->Consola de Administrador de paquetes en esta consola ponemos el siguiente comando:
Scaffold-DbContext "Data Source=NOMBRESERVIDORBASEDATOS;Initial Catalog=NOMBREBASEDATOS;Integrated Security=True;TrustServerCertificate=True" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -force -project NOMBREPROYECTO
Estos campos se obtienen:
  NOMBRESERVIDORBASEDEDATOS-->Se obtiene al abrir el programa sql server.
  NOMBREBASEDATOS-->En el programa de SQL Server vemos que nombre tiene nuestra base de datos que esta base de datos nuestra se encuentra en la carpeta <strong>Base de datos</strong>
  de SQL Server pues ponemos el nombre de la base de datos
  NOMBREPROYECTO-->El nombre que tenga nuestro proyecto en sql server
</p>
<p>Una vez echo el scaffold estamos un paso mas cerca de poder poner en marcha el proyecto pero falta un paso mas, dentro de visual studio 2022 hacemos clic derecho en 
el proyecto nos vamos a donde dice <strong>Administrar secretos de usuario</strong> dentro de ese archivo ponemos los valores:
{
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
  

}
 IMPORTANTE
 Las claves de ecriptacion y jwt tienen que ser largas minimo 38 digitos para que no de fallo, para obtener la clave privada y publica he dejado en el repositorio un archivo con el 
 codigo llamado generarpardeclaves este codigo lo ejecutan en una aplicacion de consola para que puedan ver las claves las copian y pegan.
 Una vez echo todo esto el proyecto deberia arrancar sin problemas
</p>
