# ruta del .env junto al script
$envPath = Join-Path $PSScriptRoot '.env'

# cadena de referencia para ClaveJWT (solo se usa para medir la longitud)
$jwtReference = 'IntroduceClaveLargaergoherofiygkeuidgrf7ieurygf97836trf98egfiuytrf'
$jwtLength = $jwtReference.Length   # 79

# orden exacto en que se escribiran las lineas en el .env
$order = @(
    'DB_HOST'
    'DB_NAME'
    'DB_SA_PASSWORD'
    'DB_SA_USERNAME'
    'DB_SQLUSER'
    'DB_SQLUSER_PASSWORD'
    'IsMfaEnabled'
    'ClaveJWT'
    'REDIS_CONNECTION_STRING'
    'JwtIssuer'
    'JwtAudience'
    'PublicKey'
    'PrivateKey'
    'Paypal_ClientId'
    'Paypal_ClientSecret'
    'Paypal_Mode'
    'Paypal_returnUrlConDocker'
    'Email__Host'
    'Email__Port'
    'Email__Username'
    'Email__Password'
    'CertificatePassword'
    'AuthMode'
    'LicenseKeyAutoMapper'
    'CallMeBotUser'
)

# valores por defecto (en este hashtable el orden NO importa,
# solo se usan para los defaults que muestra Ask)
$defaults = @{
    DB_HOST                    = 'SQL-Server-Local'
    DB_NAME                    = 'GestorInventario'
    DB_SA_PASSWORD             = 'SQL#1234'
    DB_SA_USERNAME             = 'sa'
    DB_SQLUSER                 = 'sqluser'
    DB_SQLUSER_PASSWORD        = '12345678SQL#1234'
    IsMfaEnabled               = 'true'
    ClaveJWT                   = ''
    REDIS_CONNECTION_STRING    = 'redis:6379'
    JwtIssuer                  = 'GestorInvetarioEmisor'
    JwtAudience                = 'GestorInventarioCliente'
    PublicKey                  = ''
    PrivateKey                 = ''
    Paypal_ClientId            = ''
    Paypal_ClientSecret        = ''
    Paypal_Mode                = 'sandbox'
    Paypal_returnUrlConDocker  = 'https://localhost:8081/Payment/Success'
    Email__Host                = 'smtp.gmail.com'
    Email__Port                = '587'
    Email__Username            = ''
    Email__Password            = ''
    CertificatePassword        = '0000'
    AuthMode                   = 'AsymmetricDynamic'
    LicenseKeyAutoMapper       = ''
    CallMeBotUser              = ''
}

# abre una URL en el navegador por defecto del usuario
function Open-Browser {
    param([string]$Url)
    try {
        Start-Process $Url
        Write-Host "  -> Abriendo $Url en tu navegador" -ForegroundColor DarkCyan
    } catch {
        Write-Host "  -> No se pudo abrir el navegador. Ve manualmente a: $Url" -ForegroundColor DarkYellow
    }
}

# genera una cadena aleatoria de la longitud indicada (A-Z, a-z, 0-9)
function New-RandomString {
    param([int]$Length)
    $chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
    $bytes = New-Object byte[] $Length
    (New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
    $sb = New-Object System.Text.StringBuilder ($Length)
    foreach ($b in $bytes) { [void]$sb.Append($chars[$b % $chars.Length]) }
    return $sb.ToString()
}

# genera un par RSA y lo devuelve como XML
function New-RsaKeyPair {
    param([int]$Bits = 2048)
    $rsa = [System.Security.Cryptography.RSA]::Create($Bits)
    return @{
        Public  = $rsa.ToXmlString($false)
        Private = $rsa.ToXmlString($true)
    }
}

# pregunta un valor (usa el default si el usuario pulsa ENTER sin escribir)
function Ask {
    param([string]$Key, [string]$Prompt, [string]$Default = '')
    if ([string]::IsNullOrEmpty($Default)) { $Default = '' }
    $label = if ($Default) { "$Prompt [$Default]" } else { $Prompt }
    $input = Read-Host $label
    if ([string]::IsNullOrWhiteSpace($input)) { $input = $Default }
    $script:values[$Key] = $input
    return $input
}

Write-Host '=== Configuracion del archivo .env ===' -ForegroundColor Cyan

$values = @{}
foreach ($k in $order) { $values[$k] = $defaults[$k] }

# ─── 1. Base de datos ──────────────────────────────────────────────
Write-Host "`n[1/6] Base de datos SQL Server" -ForegroundColor Yellow
Ask 'DB_HOST'              'Host de SQL Server'             $values['DB_HOST']
Ask 'DB_NAME'              'Nombre de la base de datos'     $values['DB_NAME']
Ask 'DB_SA_USERNAME'       'Usuario SA'                     $values['DB_SA_USERNAME']
Ask 'DB_SA_PASSWORD'       'Contrasena SA'                  $values['DB_SA_PASSWORD']
Ask 'DB_SQLUSER'           'Usuario SQL'                    $values['DB_SQLUSER']
Ask 'DB_SQLUSER_PASSWORD'  'Contrasena del usuario SQL'     $values['DB_SQLUSER_PASSWORD']

# ─── 2. Seguridad ─────────────────────────────────────────────────
Write-Host "`n[2/6] Seguridad y autenticacion" -ForegroundColor Yellow
$jwtInput = Ask 'ClaveJWT' "Clave JWT (secreto). Si la dejas vacia se generara una automatica de $jwtLength caracteres" ''
if ([string]::IsNullOrWhiteSpace($jwtInput)) {
    $values['ClaveJWT'] = New-RandomString -Length $jwtLength
    Write-Host "  -> Clave JWT generada automaticamente." -ForegroundColor Green
} else {
    $values['ClaveJWT'] = $jwtInput
}

# ─── 3. PayPal ─────────────────────────────────────────────────────
Write-Host "`n[3/6] PayPal" -ForegroundColor Yellow
Write-Host "A continuacion se abrira el portal de PayPal Developers en tu navegador." -ForegroundColor DarkCyan
Write-Host "Inicia sesion, crea una app en sandbox y copia aqui ClientId y ClientSecret." -ForegroundColor DarkCyan
$null = Read-Host "Pulsa ENTER para abrir la pagina de PayPal"
Open-Browser 'https://developer.paypal.com/home/'
Ask 'Paypal_ClientId'     'PayPal ClientId'     ''
Ask 'Paypal_ClientSecret' 'PayPal ClientSecret' ''

# ─── 4. Email ──────────────────────────────────────────────────────
Write-Host "`n[4/6] Correo electronico (contrasena de aplicacion)" -ForegroundColor Yellow
Ask 'Email__Username'     'Email (remitente)'   ''
if (-not [string]::IsNullOrWhiteSpace($values['Email__Username'])) {
    Write-Host "A continuacion se abrira la pagina de contrasenas de aplicacion de Google." -ForegroundColor DarkCyan
    Write-Host "Genera una contrasena para 'Correo' (o 'Mail') y pegala aqui." -ForegroundColor DarkCyan
    $null = Read-Host "Pulsa ENTER para abrir la pagina de Google"
    Open-Browser 'https://myaccount.google.com/apppasswords'
}
Ask 'Email__Password'     'Contrasena de aplicacion de Google' ''

# ─── 5. Licencia AutoMapper ───────────────────────────────────────
Write-Host "`n[5/6] Licencia de AutoMapper (LuckyPennySoftware)" -ForegroundColor Yellow
Write-Host "A continuacion se abrira la pagina de LuckyPennySoftware." -ForegroundColor DarkCyan
Write-Host "Elige el plan GRATUITO, genera tu clave de licencia y pegala aqui abajo." -ForegroundColor DarkCyan
$null = Read-Host "Pulsa ENTER para abrir la pagina de LuckyPennySoftware"
Open-Browser 'https://luckypennysoftware.com/'
Ask 'LicenseKeyAutoMapper' 'Licencia AutoMapper (pega aqui la clave)' ''

# ─── 6. Resto de campos ───────────────────────────────────────────
Write-Host "`n[6/6] Otros" -ForegroundColor Yellow
Ask 'CertificatePassword'  'Contrasena del certificado'        $values['CertificatePassword']
Ask 'CallMeBotUser'        'Usuario CallMeBot (Telegram)'      ''

# ─── Generacion automatica de claves RSA (XML) ────────────────────
Write-Host "`nGenerando par de claves RSA (2048 bits)..." -ForegroundColor Yellow
$keys = New-RsaKeyPair -Bits 2048
$values['PublicKey']  = $keys.Public
$values['PrivateKey'] = $keys.Private
Write-Host 'Claves RSA generadas correctamente.' -ForegroundColor Green

# ─── Construir y guardar el .env (en el orden definido por $order) ──
$lines = foreach ($k in $order) {
    $v = $values[$k]
    if ($v -match "`r?`n") { "$k=`"$v`"" } else { "$k=$v" }
}
$envContent = ($lines -join "`r`n") + "`r`n"
Set-Content -Path $envPath -Value $envContent -Encoding UTF8

Write-Host "`nArchivo .env generado en: $envPath" -ForegroundColor Green
