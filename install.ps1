# =================================================================
#  install.ps1 — Instalador de GestorInventario
#
#  Pasos:
#    1. Crear carpeta certs\ y generar certificado autofirmado (PFX).
#    2. Generar el archivo .env (orden exacto, valores no editables).
#    3. Verificar si Docker esta disponible.
#    4. Arrancar Docker Desktop si esta instalado pero apagado.
#    5. Ejecutar "docker compose up -d --build".
#
#  NOTA SOBRE VARIABLES DE ENTORNO:
#  El docker-compose.yml lee las variables en MAYUSCULAS (DB_HOST,
#  CLAVE_JWT, JWT_ISSUER, PUBLIC_KEY, etc.), que es como el codigo
#  C# las busca via Environment.GetEnvironmentVariable(...). Por eso
#  el .env generado aqui esta en MAYUSCULAS tambien. El bloque
#  AuthenticationConfigurationExtensions ya no depende de la casing
#  porque consulta directamente las variables, no appsettings.json.
# =================================================================

$ErrorActionPreference = 'Stop'

# ─────────────────────────────────────────────────────────────────
# 1. Certificado autofirmado en certs\
# ─────────────────────────────────────────────────────────────────
Write-Host "`n[1/4] Generando certificado autofirmado..." -ForegroundColor Cyan

$outDir = Join-Path $PSScriptRoot 'certs'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$pfxPath = Join-Path $outDir 'certificado.pfx'
$certPassword = ConvertTo-SecureString '0000' -AsPlainText -Force

try {
    $cert = New-SelfSignedCertificate `
        -DnsName 'localhost' `
        -CertStoreLocation 'cert:\LocalMachine\My' `
        -ErrorAction Stop

    Export-PfxCertificate `
        -Cert $cert `
        -FilePath $pfxPath `
        -Password $certPassword `
        -ErrorAction Stop | Out-Null

    # ─── Agregar el certificado a los certificados de confianza del equipo ───
    #   LocalMachine\Root  = "Entidades de certificación raíz de confianza"
    #   Solo se añade si aún no está presente para evitar duplicados.
    $rootStore = Get-ChildItem 'Cert:\LocalMachine\Root' -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $cert.Thumbprint }

    if (-not $rootStore) {
        $sourceStore = New-Object System.Security.Cryptography.X509Certificates.X509Store(
            'My', 'LocalMachine')
        $sourceStore.Open('ReadOnly')
        $sourceStore.Close()

        $destStore = New-Object System.Security.Cryptography.X509Certificates.X509Store(
            'Root', 'LocalMachine')
        $destStore.Open('ReadWrite')
        try {
            $destStore.Add($cert)
            Write-Host "  -> Certificado agregado a 'Entidades de certificacion raiz de confianza' (LocalMachine\Root)." -ForegroundColor Green
        } finally {
            $destStore.Close()
        }
    } else {
        Write-Host "  -> El certificado ya estaba en LocalMachine\Root." -ForegroundColor DarkGray
    }

    Write-Host "  -> Certificado exportado en: $pfxPath" -ForegroundColor Green
} catch {
    Write-Host "  -> No se pudo crear o confiar en el certificado (¿PowerShell sin permisos de Administrador?)." -ForegroundColor Yellow
    Write-Host "     Detalle: $($_.Exception.Message)" -ForegroundColor DarkYellow
    Write-Host "     Continuando sin certificado (lo necesitaras si sirves HTTPS en IIS)." -ForegroundColor DarkYellow
}

# ─────────────────────────────────────────────────────────────────
# 2. Generar .env
# ─────────────────────────────────────────────────────────────────
Write-Host "`n[2/4] Generando archivo .env..." -ForegroundColor Cyan

$envPath = Join-Path $PSScriptRoot '.env'
$jwtReference = 'IntroduceClaveLargaergoherofiygkeuidgrf7ieurygf97836trf98egfiuytrf'
$jwtLength = $jwtReference.Length   # 79

# Orden EXACTO en que apareceran las variables en .env.
# Coincide con las variables que docker-compose.yml consume
# y con las que el codigo C# lee de Environment.GetEnvironmentVariable.
$order = @(
    'DB_HOST'
    'DB_NAME'
    'DB_SA_PASSWORD'
    'DB_SA_USERNAME'
    'DB_SQLUSER'
    'DB_SQLUSER_PASSWORD'
    'IS_MFA_ENABLED'
    'CLAVE_JWT'
    'REDIS_CONNECTION_STRING'
    'JWT_ISSUER'
    'JWT_AUDIENCE'
    'PUBLIC_KEY'
    'PRIVATE_KEY'
    'PAYPAL_BASEURL'
    'PAYPAL_CLIENTID'
    'PAYPAL_CLIENTSECRET'
    'PAYPAL_RETURN_URL'
    'PAYPAL_CANCEL_URL'
    'EMAIL_HOST'
    'EMAIL_PORT'
    'EMAIL_USERNAME'
    'EMAIL_PASSWORD'
    'CertificatePassword'
    'LOGIN_MODE'
    'AUTH_MODE'
    'LICENSE_AUTOMAPPER'
    'TELEGRAM_USER'
    'APP_DOCKER_URL'
)

# Defaults en MAYUSCULAS. Los vacios ("") se pediran al usuario.
# Los que tienen valor fijo se imprimen como "(fijo, no editable)".
$defaults = @{
    DB_HOST                  = 'SQL-Server-Local'
    DB_NAME                  = 'GestorInventario'
    DB_SA_PASSWORD           = 'SQL#1234'
    DB_SA_USERNAME           = 'sa'
    DB_SQLUSER               = 'sqluser'
    DB_SQLUSER_PASSWORD      = '12345678SQL#1234'
    IS_MFA_ENABLED           = 'true'
    CLAVE_JWT                = ''
    REDIS_CONNECTION_STRING  = 'redis:6379'
    JWT_ISSUER               = 'GestorInvetarioEmisor'
    JWT_AUDIENCE             = 'GestorInventarioCliente'
    PUBLIC_KEY               = ''
    PRIVATE_KEY              = ''
    PAYPAL_BASEURL           = 'https://api-m.sandbox.paypal.com/'
    PAYPAL_CLIENTID          = ''
    PAYPAL_CLIENTSECRET      = ''
    PAYPAL_RETURN_URL        = 'https://localhost:8081/Payment/Success'
    PAYPAL_CANCEL_URL        = 'https://localhost:8081/Payment/Cancel'
    EMAIL_HOST               = 'smtp.gmail.com'
    EMAIL_PORT               = '587'
    EMAIL_USERNAME           = ''
    EMAIL_PASSWORD           = ''
    CertificatePassword      = '0000'
    LOGIN_MODE               = 'MfaLogin'
    AUTH_MODE                = 'AsymmetricFixed'
    LICENSE_AUTOMAPPER       = ''
    TELEGRAM_USER            = ''
    APP_DOCKER_URL           = 'https://localhost:8081'
}

function Open-Browser {
    param([string]$Url)
    try {
        Start-Process $Url
        Write-Host "  -> Abriendo $Url en tu navegador" -ForegroundColor DarkCyan
    } catch {
        Write-Host "  -> No se pudo abrir el navegador. Ve manualmente a: $Url" -ForegroundColor DarkYellow
    }
}

function New-RandomString {
    param([int]$Length)
    $chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
    $bytes = New-Object byte[] $Length
    (New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
    $sb = New-Object System.Text.StringBuilder ($Length)
    foreach ($b in $bytes) { [void]$sb.Append($chars[$b % $chars.Length]) }
    return $sb.ToString()
}

# Genera un par RSA en el MISMO formato XML que rsa.FromXmlString() espera
# en C# (.NET XML legacy, no PEM). Por eso usamos ToXmlString:
#   - $false => solo Modulus + Exponent         (clave publica)
#   - $true  => + P, Q, DP, DQ, InverseQ, D    (clave privada con CRT)
function New-RsaKeyPair {
    param([int]$Bits = 2048)
    $rsa = [System.Security.Cryptography.RSA]::Create($Bits)
    return @{
        Public  = $rsa.ToXmlString($false)
        Private = $rsa.ToXmlString($true)
    }
}

# Pide un valor SOLO si no hay default. Si hay default, lo imprime como fijo y lo usa tal cual.
function Ask {
    param([string]$Key, [string]$Prompt, [string]$Default = '')
    if (-not [string]::IsNullOrEmpty($Default)) {
        Write-Host ("{0,-55} = {1}  (fijo, no editable)" -f $Prompt, $Default) -ForegroundColor DarkGray
        $script:values[$Key] = $Default
        return $Default
    }
    $input = Read-Host "$Prompt (sin valor por defecto)"
    if ([string]::IsNullOrWhiteSpace($input)) { $input = '' }
    $script:values[$Key] = $input
    return $input
}

Write-Host '   Los valores con texto por defecto NO se pueden editar; los vacios se piden.' -ForegroundColor DarkGray

$values = @{}
foreach ($k in $order) { $values[$k] = $defaults[$k] }

# [2.1] SQL Server
Write-Host "`n   [2.1] Base de datos SQL Server" -ForegroundColor Yellow
Ask 'DB_HOST'              'Host de SQL Server'             $values['DB_HOST']
Ask 'DB_NAME'              'Nombre de la base de datos'     $values['DB_NAME']
Ask 'DB_SA_USERNAME'       'Usuario SA'                     $values['DB_SA_USERNAME']
Ask 'DB_SA_PASSWORD'       'Contrasena SA'                  $values['DB_SA_PASSWORD']
Ask 'DB_SQLUSER'           'Usuario SQL'                    $values['DB_SQLUSER']
Ask 'DB_SQLUSER_PASSWORD'  'Contrasena del usuario SQL'     $values['DB_SQLUSER_PASSWORD']

# [2.2] Seguridad
Write-Host "`n   [2.2] Seguridad y autenticacion" -ForegroundColor Yellow
$jwtInput = Ask 'CLAVE_JWT' "Clave JWT (secreto). Si la dejas vacia se generara una automatica de $jwtLength caracteres" ''
if ([string]::IsNullOrWhiteSpace($jwtInput)) {
    $values['CLAVE_JWT'] = New-RandomString -Length $jwtLength
    Write-Host "   -> Clave JWT generada automaticamente." -ForegroundColor Green
} else {
    $values['CLAVE_JWT'] = $jwtInput
}

# [2.3] PayPal
Write-Host "`n   [2.3] PayPal" -ForegroundColor Yellow
Write-Host "   A continuacion se abrira el portal de PayPal Developers en tu navegador." -ForegroundColor DarkCyan
Write-Host "   Inicia sesion, crea una app en sandbox y copia aqui ClientId y ClientSecret." -ForegroundColor DarkCyan
$null = Read-Host "   Pulsa ENTER para abrir la pagina de PayPal"
Open-Browser 'https://developer.paypal.com/home/'
Ask 'PAYPAL_CLIENTID'     'PayPal ClientId'     ''
Ask 'PAYPAL_CLIENTSECRET' 'PayPal ClientSecret' ''

# [2.4] Email
Write-Host "`n   [2.4] Correo electronico (contrasena de aplicacion)" -ForegroundColor Yellow
Ask 'EMAIL_USERNAME'     'Email (remitente)'   ''
if (-not [string]::IsNullOrWhiteSpace($values['EMAIL_USERNAME'])) {
    Write-Host "   A continuacion se abrira la pagina de contrasenas de aplicacion de Google." -ForegroundColor DarkCyan
    Write-Host "   Genera una contrasena para 'Correo' (o 'Mail') y pegala aqui." -ForegroundColor DarkCyan
    $null = Read-Host "   Pulsa ENTER para abrir la pagina de Google"
    Open-Browser 'https://myaccount.google.com/apppasswords'
}
Ask 'EMAIL_PASSWORD'     'Contrasena de aplicacion de Google' ''

# [2.5] Licencia AutoMapper
Write-Host "`n   [2.5] Licencia de AutoMapper (LuckyPennySoftware)" -ForegroundColor Yellow
Write-Host "   A continuacion se abrira la pagina de LuckyPennySoftware." -ForegroundColor DarkCyan
Write-Host "   Elige el plan GRATUITO, genera tu clave de licencia y pegala aqui abajo." -ForegroundColor DarkCyan
$null = Read-Host "   Pulsa ENTER para abrir la pagina de LuckyPennySoftware"
Open-Browser 'https://luckypennysoftware.com/'
Ask 'LICENSE_AUTOMAPPER' 'Licencia AutoMapper (pega aqui la clave)' ''

# [2.6] Otros
Write-Host "`n   [2.6] Otros" -ForegroundColor Yellow
Ask 'CertificatePassword'  'Contrasena del certificado'        $values['CertificatePassword']
Ask 'TELEGRAM_USER'        'Usuario CallMeBot (Telegram)'      ''

# Generar claves RSA en XML (.NET legacy: <Modulus><Exponent>...</Exponent></Modulus>
# para la publica y +P, Q, DP, DQ, InverseQ, D para la privada)
Write-Host "`n   Generando par de claves RSA (2048 bits)..." -ForegroundColor Yellow
$keys = New-RsaKeyPair -Bits 2048
$values['PUBLIC_KEY']  = $keys.Public
$values['PRIVATE_KEY'] = $keys.Private
Write-Host "   Claves RSA generadas correctamente." -ForegroundColor Green

# Volcar .env. Si un valor contiene saltos de linea, lo entrecomillamos.
$lines = foreach ($k in $order) {
    $v = $values[$k]
    if ($v -match "`r?`n") { "$k=`"$v`"" } else { "$k=$v" }
}
$envContent = ($lines -join "`r`n") + "`r`n"
Set-Content -Path $envPath -Value $envContent -Encoding UTF8
Write-Host "`n   -> .env generado en: $envPath" -ForegroundColor Green

# ─────────────────────────────────────────────────────────────────
# 3. Detectar Docker
# ─────────────────────────────────────────────────────────────────
Write-Host "`n[3/4] Comprobando Docker..." -ForegroundColor Cyan

function Test-DockerInstalled {
    $cmd = Get-Command docker -ErrorAction SilentlyContinue
    return [bool]$cmd
}

function Test-DockerRunning {
    try {
        $info = docker info 2>$null
        return ($LASTEXITCODE -eq 0)
    } catch {
        return $false
    }
}

$dockerInstalled = Test-DockerInstalled

if (-not $dockerInstalled) {
    Write-Host "  -> Docker NO esta instalado en este equipo." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "     Instala Docker Desktop desde:" -ForegroundColor DarkCyan
    Write-Host "       https://www.docker.com/products/docker-desktop/" -ForegroundColor DarkCyan
    Write-Host ""
    Write-Host "     Tras instalarlo, vuelve a ejecutar este script." -ForegroundColor DarkCyan
    Write-Host "     El archivo .env ya esta generado y listo para usar." -ForegroundColor DarkCyan
    return
}

Write-Host "  -> Docker esta instalado." -ForegroundColor Green

# ─────────────────────────────────────────────────────────────────
# 4. Arrancar Docker si esta parado y desplegar
# ─────────────────────────────────────────────────────────────────
Write-Host "`n[4/4] Desplegando con docker compose..." -ForegroundColor Cyan

if (-not (Test-DockerRunning)) {
    Write-Host "  -> Docker no esta en ejecucion. Intentando arrancar Docker Desktop..." -ForegroundColor Yellow

    # Rutas habituales del ejecutable de Docker Desktop en Windows
    $candidates = @(
        "$env:ProgramFiles\Docker\Docker\Docker Desktop.exe"
        "${env:ProgramFiles(x86)}\Docker\Docker\Docker Desktop.exe"
        "$env:LOCALAPPDATA\Programs\Docker\Docker\Docker Desktop.exe"
    )
    $dockerExe = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($dockerExe) {
        Write-Host "  -> Iniciando: $dockerExe" -ForegroundColor DarkCyan
        Start-Process -FilePath $dockerExe -ErrorAction SilentlyContinue
    } else {
        Write-Host "  -> No se encontro el ejecutable de Docker Desktop. Arrancalo manualmente." -ForegroundColor Yellow
    }

    # Esperar hasta 90 segundos a que Docker responda
    $deadline = (Get-Date).AddSeconds(90)
    $ready = $false
    while ((Get-Date) -lt $deadline) {
        if (Test-DockerRunning) { $ready = $true; break }
        Write-Host "  ... esperando a que Docker responda" -ForegroundColor DarkGray
        Start-Sleep -Seconds 5
    }

    if (-not $ready) {
        Write-Host "  -> Docker no arranco a tiempo. Vuelve a ejecutar este script cuando este listo." -ForegroundColor Yellow
        return
    }
}

Write-Host "  -> Docker en ejecucion. Construyendo y levantando contenedores..." -ForegroundColor Green

# Ejecutar docker compose desde la carpeta del script
Push-Location $PSScriptRoot
try {
    docker compose up -d --build
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n  -> Despliegue completado." -ForegroundColor Green
        Write-Host ""
        docker compose ps
    } else {
        Write-Host "`n  -> 'docker compose' finalizo con errores (codigo $LASTEXITCODE). Revisa los logs." -ForegroundColor Yellow
    }
} finally {
    Pop-Location
}

Write-Host "`n=== Instalador finalizado ===" -ForegroundColor Cyan
