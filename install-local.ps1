# =================================================================
#  install-local.ps1 — Instalador LOCAL de GestorInventario
#
#  Este script NO usa Docker. Prepara el proyecto para ejecutarlo
#  con SQL Server Express local, regenera el DbContext con EF Core
#  y rellena el archivo de secretos administrados del proyecto.
#
#  Pasos:
#    1. Comprobar prerrequisitos (Visual Studio, SQL Server, SSMS).
#    2. Restaurar la base de datos desde el .bak del directorio.
#    3. Ejecutar Scaffold-DbContext contra la instancia local.
#    4. Rellenar el archivo de secretos administrados (secrets.json).
#
#  El script install.ps1 (modo Docker) NO se ve afectado.
# =================================================================

$ErrorActionPreference = 'Stop'

# ─────────────────────────────────────────────────────────────────
# Constantes del proyecto
# ─────────────────────────────────────────────────────────────────

$script:ProjectRoot  = $PSScriptRoot
$script:HostCsproj   = Join-Path $script:ProjectRoot 'GestorInventario\GestorInventario.csproj'
$script:DomainCsproj = Join-Path $script:ProjectRoot 'GestorInventario.Domain\GestorInventario.Domain.csproj'
$script:ContextPath  = Join-Path $script:ProjectRoot 'GestorInventario.Domain\Domain\Models\GestorInventarioContext.cs'

# UserSecretsId extraído de GestorInventario.csproj (línea 7).
$script:UserSecretsId = '1e6d9d2a-9d51-467e-b611-b6db2e3b055e'
$script:SecretsPath   = Join-Path $env:APPDATA ("Microsoft\UserSecrets\{0}\secrets.json" -f $script:UserSecretsId)

# ─────────────────────────────────────────────────────────────────
# Funciones helper genéricas
# ─────────────────────────────────────────────────────────────────

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

function New-RsaKeyPair {
    param([int]$Bits = 2048)
    $rsa = [System.Security.Cryptography.RSA]::Create($Bits)
    return @{
        Public  = $rsa.ToXmlString($false)
        Private = $rsa.ToXmlString($true)
    }
}

# Pide un valor. Si hay default, lo imprime como fijo y lo usa tal cual.
function Ask {
    param([string]$Prompt, [string]$Default = '', [switch]$AsSecure)
    if (-not [string]::IsNullOrEmpty($Default)) {
        Write-Host ("{0,-60} = {1}  (fijo, no editable)" -f $Prompt, $Default) -ForegroundColor DarkGray
        return $Default
    }
    if ($AsSecure) {
        # Read-Host -AsSecureString falla con "No se puede convertir '' a SecureString"
        # cuando el usuario pulsa ENTER sin escribir nada. Capturamos y devolvemos ''.
        try {
            $secure = Read-Host "$Prompt" -AsSecureString
        } catch {
            return ''
        }
        if ($null -eq $secure -or $secure.Length -eq 0) { return '' }
        $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        try { return [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr) }
        finally { [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) | Out-Null }
    }
    $input = Read-Host "$Prompt (sin valor por defecto)"
    if ([string]::IsNullOrWhiteSpace($input)) { return '' }
    return $input
}

function Abort-WithMessage {
    param([string]$Message)
    Write-Host ""
    Write-Host "  ABORTANDO: $Message" -ForegroundColor Red
    Write-Host "  Vuelve a ejecutar este script cuando el problema este resuelto." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Wrapper sobre Invoke-Sqlcmd que siempre usa TCP y autenticacion integrada.
# Evita el problema de que 'SQLEXPRESS' resuelva por Named Pipes desde PowerShell
# aunque SSMS (que si resuelve el alias local) pueda entrar.
# Devuelve lo mismo que Invoke-Sqlcmd. Lanza excepcion si la conexion falla.
function Invoke-SqlDb {
    param(
        [Parameter(Mandatory)] [string]$Instance,
        [Parameter(Mandatory)] [string]$Query,
        [int]$Timeout = 5
    )

    # Normalizar el nombre de la instancia:
    #   "SQLEXPRESS"               -> "tcp:localhost\SQLEXPRESS"
    #   "localhost\SQLEXPRESS"     -> "tcp:localhost\SQLEXPRESS"
    #   "DESKTOP-GN4VRAH\SQLEXPRESS"-> "tcp:DESKTOP-GN4VRAH\SQLEXPRESS"
    #   "tcp:loquesea"             -> respeta lo que ya viene
    $ds = $Instance.Trim()
    if ($ds -notmatch '^tcp:') {
        if ($ds -notmatch '[\\\/]') { $ds = "localhost\$ds" }
        $ds = "tcp:$ds"
    }

    $connStr = "Data Source=$ds;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=$Timeout"
    return Invoke-Sqlcmd -ConnectionString $connStr -Query $Query -ErrorAction Stop
}

# ─────────────────────────────────────────────────────────────────
# Paso 1 — Comprobación de prerrequisitos
# ─────────────────────────────────────────────────────────────────

# Devuelve un objeto con la version, edicion y ruta de devenv.exe de la instalacion
# de Visual Studio mas reciente encontrada, o $null si no hay ninguna.
# Considera VS 2017+ en cualquier edicion (Community/Professional/Enterprise/BuildTools/Preview),
# ambas ubicaciones (Program Files 64 bits y Program Files (x86) 32 bits),
# y cualquier ruta custom que cuelgue de "Microsoft Visual Studio\".
function Get-VisualStudioInstallation {
    $editions = @('Community','Professional','Enterprise','BuildTools','Preview')
    $roots    = @($env:ProgramFiles, ${env:ProgramFiles(x86)})

    # Mapa version -> "ano comercial". El "18" es VS 2026; futuras versiones solo hay que
    # anadirlas a esta tabla para que el recomendador las reconozca como "actuales".
    $versionYear = @{
        '18'   = '2026'
        '17'   = '2022'
        '16'   = '2019'
        '15'   = '2017'
    }

    $results = @()

    # 1) Rutas canonicas.
    foreach ($root in $roots) {
        if ([string]::IsNullOrEmpty($root)) { continue }
        $vsRoot = Join-Path $root 'Microsoft Visual Studio'
        if (-not (Test-Path $vsRoot)) { continue }
        foreach ($v in $versionYear.Keys) {
            foreach ($e in $editions) {
                $p = Join-Path $vsRoot "$v\$e\Common7\IDE\devenv.exe"
                if (Test-Path $p) {
                    $results += [pscustomobject]@{
                        Version  = $v
                        Year     = $versionYear[$v]
                        Edition  = $e
                        Path     = $p
                    }
                }
            }
        }
    }

    # 2) Fallback: localizar devenv.exe en "Microsoft Visual Studio\*" sin asumir edicion.
    foreach ($root in $roots) {
        if ([string]::IsNullOrEmpty($root)) { continue }
        $vsRoot = Join-Path $root 'Microsoft Visual Studio'
        if (-not (Test-Path $vsRoot)) { continue }
        Get-ChildItem -Path $vsRoot -Filter devenv.exe -Recurse -ErrorAction SilentlyContinue -Depth 6 |
            ForEach-Object {
                # Ruta esperada: ...\Microsoft Visual Studio\<version>\<edition>\Common7\IDE\devenv.exe
                $rel = $_.FullName.Substring($vsRoot.Length).TrimStart('\').Split('\')
                if ($rel.Count -ge 4) {
                    $v  = $rel[0]
                    $e  = $rel[1]
                    $yr = if ($versionYear.ContainsKey($v)) { $versionYear[$v] } else { $v }
                    $results += [pscustomobject]@{
                        Version = $v
                        Year    = $yr
                        Edition = $e
                        Path    = $_.FullName
                    }
                }
            }
    }

    if ($results.Count -eq 0) { return $null }

    # Orden numerico descendente por Version (Trata '18' > '17' > '16'... aunque Compare-Object los ordenaria mal por string).
    $sorted = $results | Sort-Object -Property @{Expression={[int]$_.Version}; Descending=$true}
    return $sorted | Select-Object -First 1
}

function Test-VisualStudioInstalled {
    return $null -ne (Get-VisualStudioInstallation)
}

function Test-SqlServerInstalled {
    $inst = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL' -ErrorAction SilentlyContinue
    return [bool]$inst
}

function Get-SqlInstances {
    # Devuelve hashtable {NombreVisible = MSSQLxx.INSTANCE_ID}
    $result = @{}
    $reg = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL' -ErrorAction SilentlyContinue
    if ($reg) {
        foreach ($p in $reg.PSObject.Properties) {
            if ($p.Name -notmatch '^PS' -and $null -ne $p.Value) {
                $result[$p.Name] = $p.Value
            }
        }
    }
    return $result
}

function Test-SsmsInstalled {
    $uninstallKeys = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
    )
    foreach ($k in $uninstallKeys) {
        $found = Get-ItemProperty $k -ErrorAction SilentlyContinue |
            Where-Object { $_.DisplayName -like 'SQL Server Management Studio*' }
        if ($found) { return $true }
    }
    return $false
}

# Version que consideramos "actual" para emitir el mensaje adecuado. Subir este
# numero cuando salga una nueva version LTS/recomendada de Visual Studio.
$script:RecommendedVSYear = '2022'

function Invoke-Prerequisites {
    Write-Host "`n[1/3] Comprobando prerrequisitos..." -ForegroundColor Cyan

    $vs       = Get-VisualStudioInstallation
    $hasSQL   = Test-SqlServerInstalled
    $hasSSMS  = Test-SsmsInstalled

    # ── Visual Studio ──────────────────────────────────────────────
    if (-not $vs) {
        Write-Host "  -> Visual Studio NO esta instalado." -ForegroundColor Yellow
        $dockerScript = Join-Path $script:ProjectRoot 'install.ps1'
        $hasDockerScript = Test-Path $dockerScript
        Write-Host ""
        Write-Host "  Opciones:" -ForegroundColor Cyan
        Write-Host "    [1] Abrir la pagina de descarga de Visual Studio 2022 Community" -ForegroundColor DarkCyan
        if ($hasDockerScript) {
            Write-Host "    [2] Lanzar el instalador con Docker (install.ps1) - no necesita VS" -ForegroundColor DarkCyan
        }
        Write-Host "    [3] Cancelar" -ForegroundColor DarkCyan
        $choice = Read-Host "  Elige una opcion (1-3)"
        switch ($choice) {
            '1' { Open-Browser 'https://visualstudio.microsoft.com/es/downloads/'; }
            '2' {
                if ($hasDockerScript) {
                    Write-Host "  -> Lanzando install.ps1 (modo Docker)..." -ForegroundColor Cyan
                    & $dockerScript
                    exit $LASTEXITCODE
                }
                Abort-WithMessage "No se encontro install.ps1 en la raiz del repo."
            }
            default { Abort-WithMessage "Instalacion cancelada por el usuario." }
        }
        # Si el usuario eligio 1 (descargar VS), abortamos para que instale primero.
        Abort-WithMessage "Instala Visual Studio y vuelve a ejecutar este script."
    }

    $vsYear = 0
    [int]::TryParse($vs.Year, [ref]$vsYear) | Out-Null
    $recommended = [int]$script:RecommendedVSYear
    $isOutdated  = $vsYear -lt $recommended

    if ($isOutdated) {
        Write-Host ("  -> Visual Studio {0} {1} detectado en:" -f $vs.Year, $vs.Edition) -ForegroundColor Yellow
        Write-Host ("     {0}" -f $vs.Path) -ForegroundColor DarkGray
        Write-Host ("     Esta version es anterior a la recomendada ({0}+). Puede seguir funcionando," -f $script:RecommendedVSYear) -ForegroundColor Yellow
        Write-Host "     pero el proyecto se testea contra la version actual. Recomendamos actualizar." -ForegroundColor Yellow
        $dockerScript = Join-Path $script:ProjectRoot 'install.ps1'
        $hasDockerScript = Test-Path $dockerScript
        Write-Host ""
        Write-Host "  Opciones:" -ForegroundColor Cyan
        Write-Host ("    [1] Continuar con VS {0} de todos modos" -f $vs.Year) -ForegroundColor DarkCyan
        Write-Host "    [2] Abrir la pagina de descarga de la version actual" -ForegroundColor DarkCyan
        if ($hasDockerScript) {
            Write-Host "    [3] Lanzar el instalador con Docker (install.ps1) - no necesita VS" -ForegroundColor DarkCyan
        }
        $choice = Read-Host "  Elige una opcion (1-3)"
        switch ($choice) {
            '2' { Open-Browser 'https://visualstudio.microsoft.com/es/downloads/'; Abort-WithMessage "Instala la version actual y vuelve a ejecutar este script." }
            '3' {
                if ($hasDockerScript) {
                    Write-Host "  -> Lanzando install.ps1 (modo Docker)..." -ForegroundColor Cyan
                    & $dockerScript
                    exit $LASTEXITCODE
                }
                Abort-WithMessage "No se encontro install.ps1 en la raiz del repo."
            }
            default {
                Write-Host ("  -> Continuando con VS {0} {1} bajo tu responsabilidad." -f $vs.Year, $vs.Edition) -ForegroundColor DarkYellow
            }
        }
    } else {
        Write-Host ("  -> Visual Studio {0} {1} detectado." -f $vs.Year, $vs.Edition) -ForegroundColor Green
    }

    if (-not $hasSQL) {
        Write-Host "  -> SQL Server NO esta instalado." -ForegroundColor Yellow
        Write-Host "     Abriendo pagina de descarga..." -ForegroundColor DarkCyan
        Open-Browser 'https://www.microsoft.com/es-es/download/details.aspx?id=104781'
        Abort-WithMessage "Instala SQL Server 2019 Express y vuelve a ejecutar este script."
    }
    Write-Host "  -> SQL Server detectado." -ForegroundColor Green

    if (-not $hasSSMS) {
        Write-Host "  -> SQL Server Management Studio NO esta instalado." -ForegroundColor Yellow
        Write-Host "     Abriendo pagina de descarga..." -ForegroundColor DarkCyan
        Open-Browser 'https://aka.ms/ssmsfullsetup'
        Abort-WithMessage "Instala SSMS y vuelve a ejecutar este script."
    }
    Write-Host "  -> SSMS detectado." -ForegroundColor Green

    # Listar instancias y proponer la primera.
    $instances = Get-SqlInstances
    if (-not $instances -or $instances.Count -eq 0) {
        Abort-WithMessage "No se encontraron instancias de SQL Server instaladas."
    }
    Write-Host ""
    Write-Host "  Instancias detectadas:" -ForegroundColor Yellow
    $i = 0
    $ordered = $instances.Keys | Sort-Object
    foreach ($name in $ordered) {
        $i++
        Write-Host ("    [{0}] {1}" -f $i, $name) -ForegroundColor DarkCyan
    }
    $firstName = $ordered | Select-Object -First 1

    $sel = Read-Host "  Elige el numero de instancia (ENTER para usar '$firstName')"
    if ([string]::IsNullOrWhiteSpace($sel)) { $sel = $firstName }
    elseif ($sel -match '^\d+$') {
        $idx = [int]$sel
        if ($idx -ge 1 -and $idx -le $ordered.Count) { $sel = $ordered[$idx - 1] }
        else { Abort-WithMessage "Opcion fuera de rango." }
    }
    return $sel
}

# ─────────────────────────────────────────────────────────────────
# Paso 2 — Restauración del .bak
# ─────────────────────────────────────────────────────────────────

function Get-BakFiles {
    Get-ChildItem -Path $script:ProjectRoot -Filter '*.bak' -File |
        Sort-Object LastWriteTime -Descending
}

# Devuelve $true si la base 'GestorInventario' existe en la instancia dada.
function Test-DatabaseExists {
    param([string]$Instance)
    $q = "SELECT DB_ID('GestorInventario') AS Id"
    $row = Invoke-SqlDb -Instance $Instance -Query $q -ErrorAction Stop
    return ($null -ne $row -and $null -ne $row[0].Id)
}

# Lista los LogicalName Data/Log del .bak sin restaurarlo.
function Get-BakLogicalNames {
    param([string]$Instance, [string]$BakPath)
    $filelist = Invoke-SqlDb -Instance $Instance -Query "RESTORE FILELISTONLY FROM DISK = N'$BakPath'" -ErrorAction Stop
    $data = $filelist | Where-Object { $_.Type -eq 'D' } | Select-Object -First 1
    $log  = $filelist | Where-Object { $_.Type -eq 'L' } | Select-Object -First 1
    return @{ Data = $data.LogicalName; Log = $log.LogicalName }
}

# Compara el conjunto de tablas del .bak con las que ya tiene la base existente.
# Devuelve un objeto con {Match, BakTables, DbTables, OnlyInBak, OnlyInDb}.
# La comparacion es insensible a mayusculas/minusculas y se hace sobre nombres base
# (sin esquema) porque el .bak puede haberse generado con un usuario concreto (ej. dbo).
function Compare-DatabaseSchema {
    param([string]$Instance, [string]$BakPath)

    # Tablas en la base actual.
    $dbQ = @"
SELECT t.TABLE_NAME
FROM   GestorInventario.INFORMATION_SCHEMA.TABLES t
WHERE  t.TABLE_TYPE = 'BASE TABLE'
"@
    $dbTables = @(Invoke-SqlDb -Instance $Instance -Query $dbQ -ErrorAction Stop |
        ForEach-Object { $_.TABLE_NAME })

    # Tablas declaradas en el .bak via RESTORE FILELISTONLY no da tablas;
    # hay que leerlas de la propia base: usamos una conexion temporal con
    # RESTORE ... WITH STANDBY sobre un nombre unico y la consultamos.
    # Para evitar el lio, montamos un restore "header only" + lectura de
    # las tablas de la base origen del backup a traves de msdb... no,
    # lo mas fiable: leer la base temporal con REPLACE a un nombre de prueba
    # y volver a tirarla. Es caro, pero solo se ejecuta cuando la base YA existe.
    $tempDb = "GestorInventario_bakcheck_$([guid]::NewGuid().ToString('N').Substring(0,8))"
    try {
        $names = Get-BakLogicalNames -Instance $Instance -BakPath $BakPath
        if (-not $names.Data -or -not $names.Log) {
            return [pscustomobject]@{ Match = $false; Reason = 'No se pudieron leer los logical names del .bak.' }
        }
        $mdf = Join-Path (Get-DefaultDataPath -Instance $Instance) "$tempDb.mdf"
        $ldf = Join-Path (Get-DefaultDataPath -Instance $Instance) "$tempDb.ldf"
        $restore = @"
IF DB_ID('$tempDb') IS NOT NULL
    ALTER DATABASE [$tempDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$tempDb]
    FROM DISK = N'$BakPath'
    WITH REPLACE, RECOVERY,
         MOVE N'$($names.Data)' TO N'$mdf',
         MOVE N'$($names.Log)'  TO N'$ldf';
"@
        Invoke-SqlDb -Instance $Instance -Query $restore -ErrorAction Stop | Out-Null
        $bakQ = "SELECT TABLE_NAME FROM $tempDb.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
        $bakTables = @(Invoke-SqlDb -Instance $Instance -Query $bakQ -ErrorAction Stop |
            ForEach-Object { $_.TABLE_NAME })
    } finally {
        # Limpiar la base temporal.
        try {
            Invoke-SqlDb -Instance $Instance -Query @"
IF DB_ID('$tempDb') IS NOT NULL
BEGIN
    ALTER DATABASE [$tempDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$tempDb];
END
"@ -ErrorAction Stop | Out-Null
        } catch { }
    }

    $dbSet  = @($dbTables  | ForEach-Object { $_.ToLowerInvariant() } | Sort-Object -Unique)
    $bakSet = @($bakTables | ForEach-Object { $_.ToLowerInvariant() } | Sort-Object -Unique)
    $onlyBak = @($bakSet | Where-Object { $_ -notin $dbSet })
    $onlyDb  = @($dbSet  | Where-Object { $_ -notin $bakSet })

    # Coincidencia "razonable": el .bak no aporta tablas nuevas y la base tiene
    # al menos un porcentaje minimo de las del .bak. Si la base esta vacia
    # (0 tablas) tratamos como no coincidente para forzar el restore.
    $overlap = @($bakSet | Where-Object { $_ -in $dbSet }).Count
    $ratio   = if ($bakSet.Count -eq 0) { 0 } else { $overlap / $bakSet.Count }
    $match   = ($dbSet.Count -gt 0) -and ($ratio -ge 0.8) -and ($onlyBak.Count -eq 0)

    return [pscustomobject]@{
        Match      = $match
        Ratio      = [math]::Round($ratio, 2)
        BakTables  = $bakSet.Count
        DbTables   = $dbSet.Count
        OnlyInBak  = $onlyBak
        OnlyInDb   = $onlyDb
    }
}

function Get-DefaultDataPath {
    param([string]$Instance)

    $attempts = @()

    # 1) SERVERPROPERTY (SQL 2022+ lo rellena si se ha ejecutado al menos una vez;
    #    en 2019 normalmente viene NULL, pero lo intentamos por si acaso).
    try {
        $query = "SELECT SERVERPROPERTY('InstanceDefaultDataPath') AS DataPath"
        $path = Invoke-SqlDb -Instance $Instance -Query $query -ErrorAction Stop |
            Select-Object -ExpandProperty DataPath -ErrorAction SilentlyContinue
        if (-not [string]::IsNullOrWhiteSpace($path)) { return $path }
        $attempts += "SERVERPROPERTY devolvio NULL"
    } catch {
        $attempts += "SERVERPROPERTY fallo: $($_.Exception.Message)"
    }

    # 2) Registro: HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\<InstanceId>\Setup\DefaultData
    $instances = Get-SqlInstances
    if (-not $instances.ContainsKey($Instance)) {
        Abort-WithMessage "No se reconoce la instancia '$Instance' en el registro de SQL Server."
    }
    $instanceId = $instances[$Instance]
    $setup = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceId\Setup" -ErrorAction SilentlyContinue
    if ($setup -and $setup.DefaultData) {
        return $setup.DefaultData
    }
    $attempts += "Setup\DefaultData vacio o inexistente"

    # 3) Derivar de SQLDataRoot\DATA (patron 2017/2019 Express y Developer).
    if ($setup -and $setup.SQLDataRoot) {
        $derived = Join-Path $setup.SQLDataRoot 'DATA'
        if (Test-Path $derived) { return $derived }
        $attempts += "SQLDataRoot no apuntaba a un directorio valido ($derived)"
    } else {
        $attempts += "Setup\SQLDataRoot vacio o inexistente"
    }

    # 4) Fallback hardcoded a la ruta 2019/2022 Express mas comun.
    $guess = "C:\Program Files\Microsoft SQL Server\$instanceId\MSSQL\DATA"
    if (Test-Path $guess) { return $guess }
    $attempts += "Fallback $guess no existe"

    # Si llegamos aqui, no pudimos deducir el data path. Mostramos todos los intentos
    # para que sea facil arreglarlo.
    Abort-WithMessage ("No se pudo determinar el data path por defecto de la instancia '{0}'. " -f $Instance) +
                       "Intentos: " + ($attempts -join ' | ')
}

function Invoke-RestoreBak {
    param([string]$BakPath, [string]$Instance, [string]$DataPath)

    # El servicio SQL Server corre como NT Service\MSSQL$SQLEXPRESS y por
    # defecto NO puede leer ficheros bajo C:\Users\* (restriccion de Windows).
    # El script (tu shell) si puede, pero el servicio que ejecuta el RESTORE
    # NO. Cualquier intento de leer el .bak desde SQL falla con
    # "Cannot open backup device ... Operating system error 5".
    #
    # Solucion: copiar el .bak a la propia carpeta DATA del servicio, que
    # SI es legible. Hacemos la copia con tu shell (admin) y luego el RESTORE
    # opera sobre la copia. La copia se elimina al final (exito o error).
    $stageBak = Join-Path $DataPath ("GestorInventario_stage_{0}.bak" -f [guid]::NewGuid().ToString('N').Substring(0,8))
    Write-Host "  -> Copiando .bak a una ruta accesible para el servicio SQL..." -ForegroundColor DarkCyan
    Write-Host "     origen:   $BakPath" -ForegroundColor DarkGray
    Write-Host "     destino:  $stageBak" -ForegroundColor DarkGray
    try {
        Copy-Item -Path $BakPath -Destination $stageBak -Force -ErrorAction Stop
    } catch {
        Abort-WithMessage ("No se pudo copiar el .bak a la carpeta del servicio SQL. " +
                           "Detalle: $($_.Exception.Message)")
    }
    $cleanupStage = {
        if ($stageBak -and (Test-Path $stageBak)) {
            try { Remove-Item $stageBak -Force -ErrorAction SilentlyContinue } catch { }
        }
    }

    # Listar nombres lógicos desde la COPIA (el servicio SQL ya puede leerla).
    $filelistQuery = "RESTORE FILELISTONLY FROM DISK = N'$stageBak'"
    try {
        $filelist = Invoke-SqlDb -Instance $Instance -Query $filelistQuery -ErrorAction Stop
    } catch {
        & $cleanupStage
        Abort-WithMessage ("RESTORE FILELISTONLY fallo sobre la copia staged. " +
                           "El servicio SQL no puede leer su propia carpeta DATA. " +
                           "Detalle: $($_.Exception.Message)")
    }
    $dataLogical = ($filelist | Where-Object { $_.Type -eq 'D' } | Select-Object -First 1).LogicalName
    $logLogical  = ($filelist | Where-Object { $_.Type -eq 'L' } | Select-Object -First 1).LogicalName
    if (-not $dataLogical -or -not $logLogical) {
        & $cleanupStage
        Abort-WithMessage "No se pudieron identificar los ficheros Data/Log dentro de '$BakPath'."
    }
    Write-Host "  -> Logical Data: $dataLogical" -ForegroundColor DarkGray
    Write-Host "  -> Logical Log:  $logLogical"  -ForegroundColor DarkGray

    $mdf = Join-Path $DataPath 'GestorInventario.mdf'
    $ldf = Join-Path $DataPath 'GestorInventario_log.ldf'

    # Comprobacion previa de permisos de escritura en $DataPath: el servicio
    # SQL (no la shell) necesita poder escribir el .mdf/.ldf ahi.
    if (-not (Test-Path $DataPath)) {
        & $cleanupStage
        Abort-WithMessage "El data path '$DataPath' no existe. El servicio SQL no podra restaurar ahi."
    }
    $testFile = Join-Path $DataPath "_gestor_permtest_$([guid]::NewGuid().ToString('N').Substring(0,8)).tmp"
    try {
        [System.IO.File]::WriteAllText($testFile, 'ok')
        Remove-Item $testFile -Force -ErrorAction SilentlyContinue
    } catch {
        & $cleanupStage
        Abort-WithMessage ("No se puede escribir en el data path '{0}'. " -f $DataPath) +
                           "Detalle: $($_.Exception.Message)"
    }

    $restore = @"
IF DB_ID('GestorInventario') IS NOT NULL
    ALTER DATABASE [GestorInventario] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [GestorInventario]
    FROM DISK = N'$stageBak'
    WITH REPLACE, RECOVERY,
         MOVE N'$dataLogical' TO N'$mdf',
         MOVE N'$logLogical'  TO N'$ldf';
ALTER DATABASE [GestorInventario] SET MULTI_USER;
"@
    try {
        Invoke-SqlDb -Instance $Instance -Query $restore -ErrorAction Stop | Out-Null
        & $cleanupStage
    } catch {
        & $cleanupStage
        $msg = $_.Exception.Message
        if ($msg -match 'Operating system error 5|Access is denied|Cannot open backup device') {
            Abort-WithMessage ("Acceso denegado al restaurar. Aunque hayas copiado el .bak a la " +
                               "carpeta del servicio, este aun no puede escribir el .mdf/.ldf ahi. " +
                               "Soluciones: " +
                               "(1) Otorgar manualmente: icacls `"$DataPath`" /grant `"NT Service\MSSQL`$SQLEXPRESS`":(OI)(CI)F /T " +
                               "y vuelve a ejecutar este script. " +
                               "Data path: $DataPath. Detalle original: $msg")
        }
        Abort-WithMessage "La restauracion fallo. Detalle: $msg"
    }
}

function Invoke-BakRestoreStep {
    param(
        [string]$Instance,
        [switch]$AlwaysAsk
    )
    Write-Host "`n[2/3] Comprobando base de datos 'GestorInventario'..." -ForegroundColor Cyan

    # Asegurar que el módulo SqlServer está disponible (necesario para Invoke-Sqlcmd).
    if (-not (Get-Module -ListAvailable -Name SqlServer)) {
        Write-Host "  -> Instalando modulo SqlServer de PowerShell..." -ForegroundColor DarkCyan
        try {
            # PSGallery puede no estar registrado o no ser de confianza; nos aseguramos.
            $repo = Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue
            if (-not $repo) {
                Register-PSRepository -Name PSGallery -SourceLocation 'https://www.powershellgallery.com/api/v2' -InstallationPolicy Trusted -ErrorAction Stop
            } elseif ($repo.InstallationPolicy -ne 'Trusted') {
                Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction Stop
            }
            # -Force evita la confirmación interactiva, -SkipPublisherCheck evita otro prompt
            # cuando el publisher no está aún en la lista de confianza.
            Install-Module -Name SqlServer -Scope CurrentUser -Force -AllowClobber -SkipPublisherCheck -ErrorAction Stop
        } catch {
            Abort-WithMessage "No se pudo instalar el modulo SqlServer. Detalle: $($_.Exception.Message)"
        }
    }
    Import-Module SqlServer -ErrorAction Stop | Out-Null

    $exists = $false
    try {
        $exists = Test-DatabaseExists -Instance $Instance
    } catch {
        Abort-WithMessage "No se pudo comprobar la existencia de la base. Detalle: $($_.Exception.Message)"
    }

    if ($exists) {
        Write-Host "  -> La base 'GestorInventario' YA EXISTE en $Instance." -ForegroundColor Yellow
        $cmp = $null
        try {
            $baksForCheck = @(Get-BakFiles)
            if ($baksForCheck.Count -gt 0) {
                $cmp = Compare-DatabaseSchema -Instance $Instance -BakPath $baksForCheck[0].FullName
            }
        } catch {
            Write-Host "  -> No se pudo comparar el esquema (se preguntara igualmente). Detalle: $($_.Exception.Message)" -ForegroundColor DarkYellow
        }
        if ($cmp) {
            Write-Host ("     Tablas en la base actual: {0}  |  en el .bak: {1}  |  coincidencia: {2:P0}" -f $cmp.DbTables, $cmp.BakTables, $cmp.Ratio) -ForegroundColor DarkCyan
            if ($cmp.Match) {
                Write-Host "     El esquema parece coherente con el .bak." -ForegroundColor DarkGray
            } else {
                Write-Host "     El esquema actual NO encaja con el .bak (faltan: $($cmp.OnlyInBak -join ', '))." -ForegroundColor DarkYellow
            }
        }
    } else {
        Write-Host "  -> La base 'GestorInventario' no existe en $Instance." -ForegroundColor Yellow
    }

    # Preguntar SIEMPRE (con -AlwaysAsk) o solo si la BD existe (comportamiento legacy).
    if ($exists -or $AlwaysAsk) {
        Write-Host ""
        Write-Host "  ¿Deseas restaurar la base desde el .bak?" -ForegroundColor Cyan
        Write-Host "    [1] NO restaurar. Mantener la base actual (o dejarla como esta si no existe) y continuar" -ForegroundColor DarkCyan
        Write-Host "    [2] SI restaurar. SOBREESCRIBIR la base actual con el .bak" -ForegroundColor DarkCyan
        Write-Host "    [3] Cancelar" -ForegroundColor DarkCyan
        $sel = Read-Host "  Elige una opcion (1-3)"
        switch ($sel) {
            '1' {
                if ($exists) {
                    Write-Host "  -> Manteniendo la base actual. Saltando restauracion." -ForegroundColor Green
                } else {
                    Write-Host "  -> No se restauro. La base 'GestorInventario' sigue sin existir." -ForegroundColor Yellow
                }
                return
            }
            '2' { Write-Host "  -> Se restaurara el .bak sobre la base actual." -ForegroundColor Yellow }
            default { Abort-WithMessage "Instalacion cancelada por el usuario." }
        }
    } else {
        Write-Host "  -> La base 'GestorInventario' no existe. Se restaurara desde el .bak." -ForegroundColor Green
    }

    $baks = @(Get-BakFiles)
    if ($baks.Count -eq 0) {
        Abort-WithMessage "No se encontro ningun archivo .bak en '$($script:ProjectRoot)'."
    }

    $bakPath = if ($baks.Count -eq 1) { $baks[0].FullName }
               else {
                   Write-Host "  Archivos .bak encontrados:" -ForegroundColor Yellow
                   for ($i = 0; $i -lt $baks.Count; $i++) {
                       Write-Host ("    [{0}] {1}  ({2:N1} MB)" -f ($i+1), $baks[$i].Name, ($baks[$i].Length/1MB)) -ForegroundColor DarkCyan
                   }
                   $sel = Read-Host "  Elige el numero de .bak a restaurar (ENTER = 1)"
                   if ([string]::IsNullOrWhiteSpace($sel)) { $sel = 1 }
                   $baks[[int]$sel - 1].FullName
               }
    Write-Host "  -> Restaurando desde: $bakPath" -ForegroundColor Green

    $dataPath = Get-DefaultDataPath -Instance $Instance
    if (-not $dataPath) {
        Abort-WithMessage "No se pudo determinar el data path por defecto de la instancia '$Instance'."
    }
    Write-Host "  -> Data path: $dataPath" -ForegroundColor DarkGray

    try {
        Invoke-RestoreBak -BakPath $bakPath -Instance $Instance -DataPath $dataPath
        Write-Host "  -> Base de datos restaurada correctamente." -ForegroundColor Green
    } catch {
        Abort-WithMessage "La restauracion fallo. Detalle: $($_.Exception.Message)"
    }
}

# ─────────────────────────────────────────────────────────────────
# Paso 3 — Scaffold-DbContext
# ─────────────────────────────────────────────────────────────────

function Get-DotNetSdk {
    # Devuelve la version MAYOR del SDK mas alto instalado, o 0 si no hay ninguno.
    # No impone minimo: el instalador solo valida que exista.
    try {
        $sdks = & dotnet --list-sdks 2>$null
        $max = 0
        foreach ($line in $sdks) {
            if ($line -match '^(\d+)\.') {
                $major = [int]$matches[1]
                if ($major -gt $max) { $max = $major }
            }
        }
        return $max
    } catch {
        return 0
    }
}

function Remove-OnConfiguring {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return }
    $content = Get-Content $Path -Raw
    # Buscar la firma del metodo (con posibles saltos de linea entre los tokens).
    $signature = [regex]::Escape('protected override void OnConfiguring(')
    $rx = [regex]"(?s)$signature[^)]*\)\s*\{"
    $m = $rx.Match($content)
    if (-not $m.Success) { return }

    # Localizar el cuerpo contando llaves: empieza en la '{' de la firma.
    $start = $m.Index
    $braceIdx = $content.IndexOf('{', $m.Index)
    if ($braceIdx -lt 0) { return }
    $depth = 0
    $i = $braceIdx
    while ($i -lt $content.Length) {
        $c = $content[$i]
        if     ($c -eq '{') { $depth++ }
        elseif ($c -eq '}') {
            $depth--
            if ($depth -eq 0) { break }
        }
        $i++
    }
    if ($depth -ne 0) { return }   # Metodo mal formado, no tocar.
    $end = $i                       # incluye la '}' final.

    # Eliminar tambien espacios/saltos de linea previos para no dejar lineas en blanco raras.
    $cutStart = $start
    while ($cutStart -gt 0 -and ($content[$cutStart - 1] -match '[\s]')) { $cutStart-- }

    $new = $content.Substring(0, $cutStart) + $content.Substring($end + 1)
    Set-Content -Path $Path -Value $new -Encoding UTF8
    Write-Host "  -> OnConfiguring eliminado de $Path" -ForegroundColor DarkGray
}

function Invoke-Scaffold {
    param([string]$Instance)

    # Validar que hay un SDK instalado (no imponemos version minima).
    $sdk = Get-DotNetSdk
    if ($sdk -eq 0) {
        Abort-WithMessage "No hay ningun .NET SDK instalado. Instala el SDK desde https://dotnet.microsoft.com/download"
    }
    Write-Host "  -> .NET SDK $sdk detectado." -ForegroundColor Green

    # Instalar dotnet-ef alineado con la version del SDK que esta en la maquina.
    $efVersion = "$sdk.0.0"
    $efPath = Join-Path $env:USERPROFILE '.dotnet\tools\dotnet-ef.exe'
    if (-not (Test-Path $efPath)) {
        Write-Host "  -> Instalando dotnet-ef $efVersion..." -ForegroundColor DarkCyan
        & dotnet tool install --global dotnet-ef --version $efVersion
        if ($LASTEXITCODE -ne 0) { Abort-WithMessage "No se pudo instalar dotnet-ef." }
    } else {
        Write-Host "  -> dotnet-ef ya instalado (verificando version $efVersion)..." -ForegroundColor DarkCyan
        & dotnet tool update --global dotnet-ef --version $efVersion | Out-Null
    }

    # Asegurar que las tools globales están en PATH para esta sesión.
    $env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"

    $conn = "Data Source=$Instance;Initial Catalog=GestorInventario;Integrated Security=True;TrustServerCertificate=True"
    Write-Host "  -> Cadena de conexion: $conn" -ForegroundColor DarkGray

    # Borrar OnConfiguring antes del scaffold.
    Remove-OnConfiguring -Path $script:ContextPath

    $scaffoldArgs = @(
        'ef', 'dbcontext', 'scaffold', $conn,
        'Microsoft.EntityFrameworkCore.SqlServer',
        '--output-dir', 'Domain/Models',
        '--force',
        '--project',       $script:DomainCsproj,
        '--startup-project', $script:HostCsproj
    )
    Write-Host "  -> Ejecutando dotnet ef dbcontext scaffold..." -ForegroundColor DarkCyan
    & dotnet @scaffoldArgs
    if ($LASTEXITCODE -ne 0) { Abort-WithMessage "Scaffold-DbContext fallo (exit $LASTEXITCODE)." }

    # Volver a borrar OnConfiguring por si la plantilla lo reintroduce.
    Remove-OnConfiguring -Path $script:ContextPath

    Write-Host "  -> Scaffold completado." -ForegroundColor Green
}

function Invoke-ScaffoldStep {
    param([string]$Instance)
    Write-Host "`n[3/4] Regenerando entidades con Scaffold-DbContext..." -ForegroundColor Cyan
    Push-Location $script:ProjectRoot
    try {
        Invoke-Scaffold -Instance $Instance
    } finally {
        Pop-Location
    }
}

# ─────────────────────────────────────────────────────────────────
# Paso 4 — Archivo de secretos administrados
# ─────────────────────────────────────────────────────────────────

function Read-ExistingSecrets {
    if (-not (Test-Path $script:SecretsPath)) { return [pscustomobject]@{} }
    $raw = Get-Content $script:SecretsPath -Raw
    if ([string]::IsNullOrWhiteSpace($raw)) { return [pscustomobject]@{} }
    try { return $raw | ConvertFrom-Json }
    catch { return [pscustomobject]@{} }
}

function Merge-SecretsObject {
    # Fusiona recursivamente: una clave del objeto deseado SOBREESCRIBE solo si
    # la clave en $base no existe o tiene un valor "vacio" (null o cadena en blanco).
    # Las claves no vacias en $base se respetan siempre.
    param(
        [Parameter(Mandatory)] $Base,
        [Parameter(Mandatory)] $Extra
    )
    $result = if ($null -ne $Base) { $Base } else { [pscustomobject]@{} }
    if ($null -eq $Extra) { return $result }
    foreach ($key in $Extra.Keys) {
        $newVal = $Extra[$key]
        $existingProp = $result.PSObject.Properties[$key]
        $hasExisting  = $null -ne $existingProp
        $existingVal  = if ($hasExisting) { $existingProp.Value } else { $null }
        $existingEmpty = -not $hasExisting -or $null -eq $existingVal -or
                         ($existingVal -is [string] -and [string]::IsNullOrWhiteSpace($existingVal))
        if ($newVal -is [hashtable]) {
            $child = Merge-SecretsObject -Base $existingVal -Extra $newVal
            if ($child.PSObject.Properties.Count -gt 0) {
                if ($hasExisting) { $existingProp.Value = $child }
                else { $result | Add-Member -NotePropertyName $key -NotePropertyValue $child -Force }
            }
        } elseif ($existingEmpty) {
            if ($hasExisting) { $existingProp.Value = $newVal }
            else { $result | Add-Member -NotePropertyName $key -NotePropertyValue $newVal -Force }
        }
    }
    return $result
}

function Get-MergedSecrets {
    param(
        $Existing,
        [hashtable]$Desired
    )
    return Merge-SecretsObject -Base $Existing -Extra $Desired
}

function Write-SecretsWithConfirmation {
    param([pscustomobject]$Merged, [bool]$HasChanges)
    if (-not $HasChanges) {
        Write-Host "  -> Sin cambios: el archivo de secretos ya contiene los valores necesarios." -ForegroundColor DarkGray
        return
    }
    $json = $Merged | ConvertTo-Json -Depth 10
    Write-Host ""
    Write-Host "  El siguiente JSON se escribira en:" -ForegroundColor Yellow
    Write-Host "    $script:SecretsPath" -ForegroundColor DarkCyan
    Write-Host ""
    Write-Host $json -ForegroundColor DarkGray
    Write-Host ""
    $null = Read-Host "  Pulsa ENTER para escribir el archivo (Ctrl+C para abortar)"
    $dir = Split-Path $script:SecretsPath -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Set-Content -Path $script:SecretsPath -Value $json -Encoding UTF8
    Write-Host "  -> Archivo de secretos escrito." -ForegroundColor Green
}

# Devuelve $true si el valor (string o PSCustomObject) esta "relleno":
# no es null, no es vacio, y (si es PSCustomObject) tiene al menos una
# propiedad no vacia. Lo usamos para decidir si un secreto YA esta
# configurado y por tanto debe respetarse.
function Test-HasValue {
    param($Value)
    if ($null -eq $Value) { return $false }
    if ($Value -is [string]) { return -not [string]::IsNullOrWhiteSpace($Value) }
    if ($Value -is [pscustomobject]) {
        foreach ($p in $Value.PSObject.Properties) {
            if (Test-HasValue $p.Value) { return $true }
        }
        return $false
    }
    if ($Value -is [hashtable]) {
        foreach ($k in $Value.Keys) { if (Test-HasValue $Value[$k]) { return $true } }
        return $false
    }
    return $true
}

# Lee un valor del JSON existente recorriendo un path de claves separadas
# por punto: "JWT.PrivateKey", "DataBaseConection.DBHost", etc.
# Devuelve $null si alguna clave intermedia no existe.
function Get-NestedValue {
    param($Obj, [string]$Path)
    if ($null -eq $Obj -or [string]::IsNullOrEmpty($Path)) { return $null }
    $parts = $Path.Split('.')
    $cur = $Obj
    foreach ($p in $parts) {
        if ($null -eq $cur) { return $null }
        if ($cur -is [pscustomobject]) {
            $prop = $cur.PSObject.Properties[$p]
            if ($null -eq $prop) { return $null }
            $cur = $prop.Value
        } elseif ($cur -is [hashtable]) {
            if (-not $cur.ContainsKey($p)) { return $null }
            $cur = $cur[$p]
        } else {
            return $null
        }
    }
    return $cur
}

function Invoke-SecretsStep {
    param([string]$Instance)
    Write-Host "`n[3/3] Auditando archivo de secretos administrados..." -ForegroundColor Cyan
    Write-Host "  Ruta: $script:SecretsPath" -ForegroundColor DarkGray

    $existing = Read-ExistingSecrets
    if (-not (Test-HasValue $existing)) {
        Write-Host "  -> No hay archivo de secretos (o esta vacio). Se creara desde cero." -ForegroundColor Yellow
    } else {
        Write-Host ""
        Write-Host "  Valores actuales:" -ForegroundColor Yellow
        $existingJson = $existing | ConvertTo-Json -Depth 10
        Write-Host $existingJson -ForegroundColor DarkGray
        Write-Host ""
    }

    # Catalogo de claves que el script sabe manejar. Para cada una indicamos
    # la ruta en el JSON y si es "secreto" (no debe imprimirse en pantalla).
    $keys = @(
        @{ Path = 'DataBaseConection.DBHost';       Secret = $false; AskIfEmpty = $true  }
        @{ Path = 'DataBaseConection.DBName';       Secret = $false; AskIfEmpty = $true  }
        @{ Path = 'DataBaseConection.DBUserName';   Secret = $false; AskIfEmpty = $false }
        @{ Path = 'DataBaseConection.DBPassword';   Secret = $true;  AskIfEmpty = $true  }
        @{ Path = 'DataBaseConection.DockerDbHost'; Secret = $false; AskIfEmpty = $false }
        @{ Path = 'CallMeBot.user';                 Secret = $false; AskIfEmpty = $false }
        @{ Path = 'Paypal.ClientId';                Secret = $false; AskIfEmpty = $false }
        @{ Path = 'Paypal.ClientSecret';            Secret = $true;  AskIfEmpty = $false }
        @{ Path = 'Paypal.BaseUrl';                 Secret = $false; AskIfEmpty = $false }
        @{ Path = 'Email.UserName';                 Secret = $false; AskIfEmpty = $false }
        @{ Path = 'Email.PassWord';                 Secret = $true;  AskIfEmpty = $false }
        @{ Path = 'Email.Host';                     Secret = $false; AskIfEmpty = $false }
        @{ Path = 'Email.Port';                     Secret = $false; AskIfEmpty = $false }
        @{ Path = 'LicenseKeyAutoMapper';           Secret = $false; AskIfEmpty = $false }
        @{ Path = 'App.BaseUrl';                    Secret = $false; AskIfEmpty = $false }
        @{ Path = 'App.DockerUrl';                  Secret = $false; AskIfEmpty = $false }
        @{ Path = 'App.JwtIssuer';                  Secret = $false; AskIfEmpty = $false }
        @{ Path = 'App.JwtAudience';                Secret = $false; AskIfEmpty = $false }
        @{ Path = 'AuthMode';                       Secret = $false; AskIfEmpty = $false }
        @{ Path = 'LoginMode';                      Secret = $false; AskIfEmpty = $false }
        @{ Path = 'IsMfaEnabled';                   Secret = $false; AskIfEmpty = $false }
        # IMPORTANTE: en este secrets.json las claves viven en la RAIZ y bajo "JWT",
        # no bajo "App". Apuntamos a las rutas reales para que se detecten como
        # existentes y no se pidan de nuevo en cada ejecucion.
        @{ Path = 'ClaveJWT';                       Secret = $true;  AskIfEmpty = $false }
        @{ Path = 'JWT.PublicKey';                  Secret = $true;  AskIfEmpty = $false }
        @{ Path = 'JWT.PrivateKey';                 Secret = $true;  AskIfEmpty = $false }
        @{ Path = 'Redis.ConnectionString';         Secret = $false; AskIfEmpty = $false }
        @{ Path = 'Redis.ConnectionStringLocal';    Secret = $false; AskIfEmpty = $false }
    )

    # Defaults sensibles a $Instance (solo se usan si la clave esta vacia).
    $dbHostDefault = if ($Instance -match '[\\\/]') { $Instance } else { "localhost\$Instance" }

    $changes = @{}
    $askedCount = 0
    foreach ($k in $keys) {
        $current    = Get-NestedValue $existing $k.Path
        $hasCurrent = Test-HasValue $current

        # Reglas:
        #   - Si la clave YA tiene valor y NO es secreta: la respetamos sin preguntar.
        #   - Si es secreta: la pedimos siempre que exista (para que el usuario
        #     pueda cambiarla ENTER-a-ENTER), salvo que el flag de arriba lo desactive.
        #     Pero para que coincida con lo que pides ("ENTER = mantener, escribir = cambiar"),
        #     pedimos siempre con -AsSecure y solo actualizamos si el usuario escribio algo.
        #   - Si esta vacia y AskIfEmpty: preguntamos.
        $mustAsk = $false
        if ($k.Secret -and $hasCurrent) { $mustAsk = $true }
        elseif (-not $hasCurrent -and $k.AskIfEmpty) { $mustAsk = $true }

        if (-not $mustAsk) { continue }

        $label = $k.Path
        if ($k.Secret) { $label = "$($k.Path)  (secreto, ENTER para mantener)" }

        # Para secretos, Ask con ExistingValue mostrara "[actual: <valor>]" pero el valor
        # NO debe aparecer real. Usamos un marcador para que la rama "tiene existing" del
        # Ask funcione, pero sin pasar el valor real.
        $existingForAsk = $(if ($k.Secret -and $hasCurrent) { '[OCULTO]' } elseif ($hasCurrent) { [string]$current } else { '' })
        $defaultForAsk  = ''
        if (-not $hasCurrent) {
            if ($k.Path -eq 'DataBaseConection.DBHost') { $defaultForAsk = $dbHostDefault }
            elseif ($k.Path -eq 'DataBaseConection.DBName') { $defaultForAsk = 'GestorInventario' }
        }
        $value = Ask $label $defaultForAsk -AsSecure:($k.Secret) -ExistingValue $existingForAsk

        if ($hasCurrent) {
            # Mantener valor: si el usuario pulso ENTER, Ask devuelve el existing (string o '[OCULTO]'
            # si era secreto). Si era secreto, NO escribimos nada en $changes (mantenemos el valor
            # original del JSON tal cual).
            if ($k.Secret) {
                if ($value -ne '' -and $value -ne '[OCULTO]') {
                    Set-NestedValue -Obj $changes -Path $k.Path -Value $value
                    $askedCount++
                }
            } else {
                if ($value -ne ([string]$current)) {
                    Set-NestedValue -Obj $changes -Path $k.Path -Value $value
                    $askedCount++
                }
            }
        } else {
            if (-not [string]::IsNullOrEmpty($value)) {
                Set-NestedValue -Obj $changes -Path $k.Path -Value $value
                $askedCount++
            }
        }
    }

    if ($askedCount -eq 0) {
        Write-Host "  -> No hay huecos que rellenar y ningun secreto presente. Nada que escribir." -ForegroundColor Green
        return
    }

    if ($changes.Keys.Count -eq 0 -and $changes.PSObject.Properties.Count -eq 0) {
        Write-Host "  -> No se introdujeron valores nuevos. No se modifica el archivo." -ForegroundColor DarkGray
        return
    }

    # Mostrar vista previa con secretos como [OCULTO].
    $redactedPreview = [pscustomobject]@{}
    foreach ($k in $keys) {
        if ($k.Secret) {
            $cur = Get-NestedValue $changes $k.Path
            if ($null -ne $cur) { Set-NestedValue -Obj $redactedPreview -Path $k.Path -Value '[OCULTO]' }
        }
    }
    $merged = Get-MergedSecrets -Existing $existing -Desired $changes
    Write-Host ""
    Write-Host "  Cambios a aplicar (vista previa, secretos como [OCULTO]):" -ForegroundColor Yellow
    $previewJson = $redactedPreview | ConvertTo-Json -Depth 10
    if ($previewJson.Trim() -ne '{}' -and $previewJson.Trim() -ne 'null') {
        Write-Host $previewJson -ForegroundColor DarkGray
    } else {
        Write-Host "  (sin cambios que mostrar)" -ForegroundColor DarkGray
    }
    Write-Host ""
    $null = Read-Host "  Pulsa ENTER para escribir el archivo (Ctrl+C para abortar)"
    $dir = Split-Path $script:SecretsPath -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $json = $merged | ConvertTo-Json -Depth 10
    Set-Content -Path $script:SecretsPath -Value $json -Encoding UTF8
    Write-Host "  -> Archivo de secretos escrito." -ForegroundColor Green
}

# Helper: escribe un valor en un path anidado de un objeto/hashtable
# ("App.ClaveJWT" -> $obj.App.ClaveJWT), creando los niveles intermedios.
# Se usa desde Invoke-SecretsStep para que las claves nuevas se
# fusionen con la estructura existente del secrets.json.
function Set-NestedValue {
    param($Obj, [string]$Path, $Value)
    $parts = $Path.Split('.')
    $cur = $Obj
    for ($i = 0; $i -lt $parts.Count - 1; $i++) {
        $p = $parts[$i]
        $next = $parts[$i + 1]
        $prop = $cur.PSObject.Properties[$p]
        if ($null -eq $prop) {
            $child = [pscustomobject]@{}
            $cur | Add-Member -NotePropertyName $p -NotePropertyValue $child -Force
            $cur = $child
        } else {
            $cur = $prop.Value
        }
    }
    $leaf = $parts[-1]
    $leafProp = $cur.PSObject.Properties[$leaf]
    if ($null -eq $leafProp) {
        $cur | Add-Member -NotePropertyName $leaf -NotePropertyValue $Value -Force
    } else {
        $leafProp.Value = $Value
    }
}

# ─────────────────────────────────────────────────────────────────
# Orquestador
# ─────────────────────────────────────────────────────────────────

# En PS 5.1, la primera vez que se toca PSGallery (Install-Module / Set-PSRepository)
# aparece un prompt de "Se necesita el proveedor de NuGet...". Lo pre-instalamos
# para que no se quede colgado en mitad de la ejecucion.
function Initialize-PowerShellGet {
    $nuget = Get-PackageProvider -Name NuGet -ErrorAction SilentlyContinue
    if (-not $nuget -or $nuget.Version -lt [version]'2.8.5.201') {
        Write-Host "  -> Pre-instalando proveedor de NuGet (evita prompts durante la instalacion del modulo SqlServer)..." -ForegroundColor DarkCyan
        try {
            Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -Confirm:$false -Scope CurrentUser -ErrorAction Stop
        } catch {
            # No es bloqueante: si falla, el prompt aparecera abajo y el usuario lo aceptara manualmente.
            Write-Host "  -> Aviso: no se pudo pre-instalar NuGet ($($_.Exception.Message)). Se pedira confirmacion interactiva mas adelante." -ForegroundColor DarkYellow
        }
    }
    # Asegurar que PSGallery esta registrado y es de confianza.
    $repo = Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue
    if (-not $repo) {
        try { Register-PSRepository -Name PSGallery -SourceLocation 'https://www.powershellgallery.com/api/v2' -InstallationPolicy Trusted -ErrorAction Stop }
        catch { Write-Host "  -> Aviso: no se pudo registrar PSGallery ($($_.Exception.Message))." -ForegroundColor DarkYellow }
    } elseif ($repo.InstallationPolicy -ne 'Trusted') {
        try { Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction Stop }
        catch { Write-Host "  -> Aviso: no se pudo marcar PSGallery como Trusted ($($_.Exception.Message))." -ForegroundColor DarkYellow }
    }
}

Write-Host ""
Write-Host "=== Instalador LOCAL de GestorInventario ===" -ForegroundColor Cyan
Write-Host "  (no usa Docker; modifica la base de datos y los secretos del proyecto)" -ForegroundColor DarkGray
Write-Host ""

if (-not (Test-Path $script:HostCsproj)) {
    Abort-WithMessage "No se encuentra el proyecto web en '$($script:HostCsproj)'. Ejecuta este script desde la raiz del repo."
}
if (-not (Test-Path $script:DomainCsproj)) {
    Abort-WithMessage "No se encuentra GestorInventario.Domain en '$($script:DomainCsproj)'."
}

Initialize-PowerShellGet

$instance = Invoke-Prerequisites
Invoke-BakRestoreStep     -Instance $instance -AlwaysAsk
Invoke-SecretsStep        -Instance $instance

Write-Host ""
Write-Host "=== Instalador LOCAL finalizado ===" -ForegroundColor Cyan
Write-Host "  Puedes arrancar el proyecto con:" -ForegroundColor DarkGray
Write-Host "    dotnet run --project GestorInventario" -ForegroundColor DarkGray
