# Carpeta de salida: <carpeta del script>\certs
$outDir = Join-Path $PSScriptRoot 'certs'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

# Crear certificado autofirmado
$cert = New-SelfSignedCertificate `
    -DnsName 'localhost' `
    -CertStoreLocation 'cert:\LocalMachine\My'

# Exportar a PFX dentro de la carpeta creada
$password = ConvertTo-SecureString '0000' -AsPlainText -Force
$pfxPath = Join-Path $outDir 'certificado.pfx'

Export-PfxCertificate `
    -Cert $cert `
    -FilePath $pfxPath `
    -Password $password

Write-Host "Certificado exportado en: $pfxPath"