# 📦 Gestor Inventario

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean_Architecture-green.svg)]()
[![License](https://img.shields.io/badge/License-MIT-blue.svg)]()

Sistema robusto de gestión de inventarios y suscripciones con integración de pagos PayPal, notificaciones automatizadas y una arquitectura escalable.

---

## 🚀 Características Principales

### 🛠️ Gestión de Inventario y Datos
- **Operaciones CRUD Completas**: Gestión eficiente de productos, proveedores y pedidos.
- **Control de Stock**: Monitoreo automático de niveles bajos con notificaciones en tiempo real.
- **Generación de Códigos de Barras**: Simulación de entorno de tienda con renderizado de códigos de barras profesional.

### 🔐 Seguridad y Autenticación
- **Autenticación Multi-Modo**: Soporte para autenticación Simétrica, Asimétrica (Claves Fijas) y Asimétrica Dinámica.
- **MFA (Multi-Factor Authentication)**: Implementación de doble factor de autenticación global mediante códigos OTP enviados por email.
- **Control de Acceso Basado en Roles (RBAC)**: Gestión granular de permisos para Administradores y Usuarios.

### 💳 Integración con PayPal y Suscripciones
- **Pasarela de Pagos**: Implementación completa de PayPal Sandbox para transacciones seguras.
- **Gestión de Planes**: Creación, edición y suscripción a planes de pago.
- **Gestión de Reembolsos**: Sistema de reembolsos totales y parciales con auditoría.
- **Facturación PDF**: Generación automática de facturas profesionales en formato PDF.

### 🔔 Sistema de Notificaciones
- **Omnicanal**: Notificaciones vía Email y Telegram (CallMeBot).
- **Automatización**: Alertas automáticas de stock bajo y confirmaciones de registro.

---

## 🏗️ Arquitectura del Proyecto

El proyecto ha sido refactorizado bajo los principios de **Clean Architecture**, asegurando un bajo acoplamiento y una alta mantenibilidad.

### Estructura de Capas
- **`Web`**: Capa de presentación. Maneja controladores, vistas y la interfaz de usuario.
- **`Application`**: El "cerebro" del sistema. Contiene la lógica de negocio, orquestadores (Generators), estrategias y los contratos (interfaces).
- **`Infrastructure`**: La implementación técnica. Incluye el acceso a datos (EF Core), servicios de email, renderizado de PDF/Barcodes, caché (Redis) y resiliencia (Polly).
- **`Domain`**: El núcleo. Contiene los modelos de entidad y enumeraciones fundamentales.
- **`Shared`**: Utilidades y constantes transversales utilizadas por todas las capas.

### Stack Tecnológico
- **Framework**: .NET 10
- **Base de Datos**: SQL Server (EF Core)
- **Caché**: Redis
- **Resiliencia**: Polly (Retry, Circuit Breaker, Fallback)
- **Renderizado**: QuestPDF, RazorLight, SkiaSharp, ZXing.Net
- **Pagos**: PayPal REST SDK

---

## 🛠️ Guía de Instalación

### 📋 Requisitos Previos

**Para ejecución con Docker:**
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)

**Para ejecución en Local:**
- [Visual Studio 2022](https://visualstudio.microsoft.com/) o [Rider](https://www.jetbrains.com/es-es/rider/)
- [SQL Server](https://www.microsoft.com/es-es/download/details.aspx?id=104781)
- [Git](https://git-scm.com/)
- **Opcional pero recomendado:** [SSMS](https://aka.ms/ssmsfullsetup) o [DataGrip](https://www.jetbrains.com/es-es/datagrip/)

---

### 🐳 Puesta en Marcha con Docker

1. **Clonar el repositorio:**
   ```sh
   git clone https://github.com/blackl1ght98/GestorInventario
   ```

2. **Ejecutar el instalador guiado:**
   ```powershell
   ./install.ps1
   ```
   *(En Linux usar `sudo ./install-linux.sh`)*. El script creará un archivo `.env` con las variables de entorno necesarias.

3. **Lanzar la aplicación:**
   Reiniciar Docker y ejecutar el proyecto.

---

### 💻 Puesta en Marcha en Local

#### 1. Restaurar Base de Datos
Tienes dos opciones para restaurar la copia de seguridad:

**Opción A: Interfaz Visual (SSMS)**
1. Abrir **SSMS** $\rightarrow$ Conectar con **Certificado de servidor de confianza** activado.
2. Mover el archivo `.bak` a la ruta de backups de SQL Server (ej. `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\`).
3. Clic derecho en **Bases de datos** $\rightarrow$ **Restaurar base de datos** $\rightarrow$ Seleccionar el archivo `.bak` desde el Dispositivo.

**Opción B: Mediante Comandos SQL**
1. Abrir el archivo `restauracionPorConsulta.sql`.
2. Ejecutar la primera consulta (`RESTORE HEADERONLY`) para identificar la posición (`Position`) de la última versión de la base de datos.
````sql
RESTORE HEADERONLY 
FROM DISK = N'C:\Ruta\Al\Archivo\GestorInventario.bak';
````
3. En la segunda consulta, ajustar la ruta en `FROM DISK` y el número en `FILE = [Posicion]` con el valor obtenido.
   ```sql
   RESTORE DATABASE [GestorInventario]
   FROM DISK = N'C:\Ruta\Al\Archivo\GestorInventario.bak'
   WITH FILE = 21, REPLACE, STATS = 5;
   GO
   ```

#### 2. Configurar Secretos de Usuario
En Visual Studio: `Clic derecho sobre el proyecto > Administrar secretos de usuario`.
Agregar el JSON de configuración (ver `appsettings.json` o el archivo `.env` generado por el instalador) incluyendo:
- Credenciales de Redis, PayPal y Email.
- Claves JWT y modos de autenticación (`LoginMode`, `AuthMode`).
- URLs base de la aplicación.
````json
{
  "Redis": {
    "ConnectionString": "redis:6379",
    "ConnectionStringLocal": "127.0.0.1:6379"
  },


  "AuthMode": "AsymmetricDynamic",
  "LoginMode": "MfaLogin",

  "JWT": {
    "PublicKey": "",
    "PrivateKey": "",
    "Issuer": "GestorInvetarioEmisor",
    "Audience": "GestorInventarioCliente",
    "ClaveJWT": "IntroduceClaveLargaergoherofiygkeuidgrf7ieurygf97836trf98egfiuytrf"
  },


  "DataBaseConection": {
    "DBHost": "localhost\\SQLEXPRESS",
    "DockerDbHost": "SQL-Server-Local",
    "DBName": "GestorInventario",
    "DBUserName": "sa",
    "DBPassword": "SQL#1234"
  },
  "App": {
    "BaseUrl": "https://localhost:7056",
    "DockerUrl": "https://localhost:8080"

  },
  "CallMeBot": {
    "TelegramUser": ""
  },
  "Paypal": {
    "ClientId": "",
    "ClientSecret": "",
    "BaseUrl": "https://api-m.sandbox.paypal.com/",
    "ReturnUrls": {
      "Development": "https://localhost:7056/Payment/Success",
      "Docker": "https://localhost:8081/Payment/Success"
    },
    "CancelUrls": {
      "Development": "https://localhost:7056/Payment/Cancel",
      "Docker": "https://localhost:8081/Payment/Cancel"
    }

  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "UserName": "",
    "PassWord": ""
  }
}
````
1. Para obtener el **ClientId y ClientSecret** de paypal registrarse en paypal developer: [PaypalDeveloper](https://developer.paypal.com/community/developer-program/)
2. En email para establecer la contraseña es necesario usar **contraseña de aplicacion** esta la obtenemos en: [ApplicationPassword](https://myaccount.google.com/apppasswords)
**SOLO SI LO EJECUTAS EN LINUX**
1. Modificar el valor de **DBHost** y dejar solo **localhost** ya que en linux funciona de otra forma
#### 3. Sincronización de Modelos (Scaffold)
Si la base de datos cambia, ejecutar en la **Consola del Administrador de Paquetes**:
```powershell
Scaffold-DbContext "Data Source=localhost\SQLEXPRESS;Initial Catalog=GestorInventario;User ID=sa;Password=SQL#1234;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -ContextDir ../GestorInventario.Infrastructure/Data -ContextNamespace "GestorInventario.Infrastructure.Data" -Namespace "GestorInventario.Domain.Models" -force -Project GestorInventario.Domain -NoOnConfiguring
```

---

## ⚠️ Solución de Problemas Comunes

### Docker & Visual Studio
Si no tienes Docker instalado y ves errores en el proyecto `docker-compose`:
`Clic derecho sobre docker-compose > Descargar proyecto (Unload Project)`.

### Instalación de Docker
Si la instalación falla en Windows:
`C:\ProgramData\DockerDesktop` $\rightarrow$ Eliminar la carpeta y reintentar.

### WSL (Windows Subsystem for Linux)
Si Docker no inicia por problemas de WSL:
Ejecutar `wsl --update` en la terminal o descargar la última versión desde el repositorio oficial de Microsoft.

---

## 🧠 Notas Finales
- ✅ Probando en Windows 10/11.
- 💳 PayPal configurado en modo **Sandbox**. Para producción, cambiar `Mode: sandbox` $\rightarrow$ `Mode: live`.
- 🔐 No subir el archivo `appsettings.json` con credenciales reales al repositorio; usar **User Secrets**.
