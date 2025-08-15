$secretJsonPath = "C:\Users\guill\AppData\Roaming\Microsoft\UserSecrets\1e6d9d2a-9d51-467e-b611-b6db2e3b055e\secrets.json"
$secrets = Get-Content $secretJsonPath | ConvertFrom-Json

# Crear o sobrescribir el archivo .env
$envContent = @"
DB_HOST=$($secrets.DataBaseConection.DockerDbHost)
DB_NAME=$($secrets.DataBaseConection.DBName)
DB_SA_PASSWORD=$($secrets.DataBaseConection.DBPassword)
DB_USERNAME=$($secrets.DataBaseConection.DBUserName)
ClaveJWT=$($secrets.ClaveJWT)
REDIS_CONNECTION_STRING=$($secrets.Redis.ConnectionString)
JwtIssuer=$($secrets.JwtIssuer)
JwtAudience=$($secrets.JwtAudience)
PublicKey=$($secrets.JWT.PublicKey)
PrivateKey=$($secrets.JWT.PrivateKey)
LicenseKeyAutoMapper=$($secrets.LicenseKeyAutoMapper)
Paypal_ClientId=$($secrets.Paypal.ClientId)
Paypal_ClientSecret=$($secrets.Paypal.ClientSecret)
Paypal_Mode=$($secrets.Paypal.Mode)
Paypal_returnUrlConDocker=$($secrets.Paypal.returnUrlConDocker)
Paypal_returnUrlSinDocker=$($secrets.Paypal.returnUrlSinDocker)
Email__Host=$($secrets.Email.Host)
Email__Port=$($secrets.Email.Port)
Email__Username=$($secrets.Email.UserName)
Email__Password=$($secrets.Email.PassWord)
"@

# Guardar en el archivo .env
$envContent | Out-File -FilePath .env -Encoding utf8