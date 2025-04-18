$secretJsonPath = "C:\Users\guill\AppData\Roaming\Microsoft\UserSecrets\1e6d9d2a-9d51-467e-b611-b6db2e3b055e\secrets.json"
$secrets = Get-Content $secretJsonPath | ConvertFrom-Json
$env:ClaveJWT=$secrets.ClaveJWT
$env:REDIS_CONNECTION_STRING=$secrets.Redis.ConnectionString
$env:JwtIssuer=$secrets.JwtIssuer
$env:JwtAudience=$secrets.JwtAudience
$env:PublicKey=$secrets.JWT.PublicKey
$env:PrivateKey=$secrets.JWT.PrivateKey
$env:DB_USERNAME=$secrets.DataBaseConection.DBUserName
$env:DB_SA_PASSWORD=$secrets.DataBaseConection.DBPassword
$env:DB_NAME=$secrets.DataBaseConection.DBName
$env:DB_HOST=$secrets.DataBaseConection.DockerDbHost
$env:Paypal_ClientId=$secrets.Paypal.ClientId
$env:Paypal_ClientSecret=$secrets.Paypal.ClientSecret
$env:Paypal_Mode=$secrets.Paypal.Mode
$env:Paypal_returnUrlConDocker=$secrets.Paypal.returnUrlConDocker
$env:Paypal_returnUrlSinDocker=$secrets.Paypal.returnUrlSinDocker
$env:Email__Host=$secrets.Email.Host
$env:Email__Port=$secrets.Email.Port
$env:Email__Username=$secrets.Email.UserName
$env:Email__Password=$secrets.Email.PassWord