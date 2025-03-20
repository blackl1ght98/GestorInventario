FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GestorInventario/GestorInventario.csproj", "GestorInventario/"]
RUN dotnet restore "GestorInventario/GestorInventario.csproj"
COPY . .
WORKDIR "/src/GestorInventario"
RUN dotnet build "GestorInventario.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GestorInventario.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
CMD printenv
ENTRYPOINT ["dotnet", "GestorInventario.dll"]
CMD printenv
EXPOSE  8080 8081 28908 28907
# Primero ejecutar dotnet publish -c Release -o out

#Comando para crear la imagen de la base de datos en docker: docker pull mcr.microsoft.com/mssql/server
#Comando para crear y acceder a la base de datos en docker docker run --name "SQL-Server-Local" -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=SQL#1234" -p 1433:1433 -d mcr.microsoft.com/mssql/server
#Comando para copiar los archivos .bak a docker: docker cp "D:\SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\GestorInventario-2024710-18-27-46.bak" SQL-Server-Local:/var/opt/mssql/data
#Para poner un contenedor o varios contenedores en la misma red primero se pone el siguiente comando:
		#docker network create --attachable <nombre de la red>
	    #docker network connect <nombre de la red> <nombre del contenedor> --> Para obtener el nombre de un contenedor se pone el comando docker ps
#Para ver la información de la red ponemos el comando docker network inspect <nombre de la red> --> Para obtener las redes existentes ponemos el comando docker network ls
#Para testear la conexion con base de datos ponemos docker exec -it SQL-Server-Local /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P SQL#1234 -d GestorInventario -Q "SELECT * FROM Usuarios;"
#Ademas el comando anterior permite hacer consultas desde la terminal docker exec -it SQL-Server-Local /opt/mssql-tools/bin/sqlcmd -S localhost -U pepe -P pepe#1234  -d GestorInventario -Q "SELECT * FROM Usuarios;"
#Para generar el certificado https y ponerlo en un directorio en concreto se usa el comando: dotnet dev-certs https -ep C:\Users\guill\.aspnet\https\aspnetapp.pfx -p password
#Para confiar en el certificado generado se pone el comando: dotnet dev-certs https --trust
#Para que nuestra app de .net tenga acceso a ese certificado ponemos en el app.development.json:
#{
 # "Kestrel": {
  #  "Certificates": {
   #   "Development": {
    #    "Password": "password",
     #   "Path": "/https/aspnetapp.pfx"
      #}
    #}
 # }
#}
#Para construir un proyecto de docker se pone el comando docker-compose build
#Para levantar el contenedor docker-compose up
###Esto se pone en el launchseting.json
 #"IIS Express": {
      #"commandName": "IISExpress",
      #"launchBrowser": true,
      #"environmentVariables": {
        #"ASPNETCORE_ENVIRONMENT": "Development",
        #"DB_HOST": "DESKTOP-2TL9C3O\\SQLEXPRESS",
        #"DB_NAME": "Pub",
        #"DB_SA_PASSWORD": "SQL#1234",
        #"IS_DOCKER": "false"
      #}
    #},
#
    #"Container (Dockerfile)": {
      #"commandName": "Docker",
      #"launchBrowser": true,
      #"launchUrl": "{Scheme}://{ServiceHost}:{ServicePort}",
      #"environmentVariables": {
        #"ASPNETCORE_HTTPS_PORTS": "443", en esta linea y la siguiente para saber por que puerto esta escuchando el contenedor primero se pone el comando docker ps y luego el comando docker inspect <nombre contenedor>
        #"ASPNETCORE_HTTP_PORTS": "80",
        #"DB_HOST": "sql-server-local",
        #"DB_NAME": "Pub",
        #"DB_SA_PASSWORD": "SQL#1234",
        #"IS_DOCKER": "true"
      #},
      #"publishAllPorts": true,
      #"useSSL": true
    #}
  #},
###
#Tratar valores que se encuentran en el archivo secrets.json para que docker pueda acceder a esos valores
#Primero ejecutamos este comando $secretJsonPath = "C:\Users\guill\AppData\Roaming\Microsoft\UserSecrets\1e6d9d2a-9d51-467e-b611-b6db2e3b055e\secrets.json"
#en este primer comando decimos donde esta nuestro archivo de secretos
# Una vez que tenemos la ruta ejecutamos este comando $secrets = Get-Content $secretJsonPath | ConvertFrom-Json que convierte los valores a json
# y por ultimo del valor que queramos convertir a variable de entorno ponemos este comando $env:ClaveJWT = $secrets.ClaveJWT
#Por ultimo para comprobar que la variable de entorno se ha creado correctamente ponemos este comando Write-Host "ClaveJWT: $env:ClaveJWT"

#Luego en el Program.cs ponemos esto
#// Agregar variables de entorno a la configuración
#builder.Configuration.AddEnvironmentVariables();
#
#// Imprimir todas las variables de entorno para depuración
#foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
#{
    #Console.WriteLine($"{env.Key}: {env.Value}");
#}
#
#string secret = builder.Configuration["ClaveJWT"];
#Console.WriteLine($"ClaveJWT from Configuration: {secret}");
#Estas lineas no solo agregan y configuran para que docker pueda acceder al valor de la clave si no que tambien comprobamos que la variable
#de entorno creada se haya creado de manera correcta