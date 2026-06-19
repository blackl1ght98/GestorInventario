# Imagen base con el SDK completo de .NET 10 para compilar, la llamamos 'build'
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Creamos y establecemos /src como directorio de trabajo dentro del contenedor
WORKDIR /src

# Copiamos solo el .csproj primero para aprovechar la caché de Docker
# Si el .csproj no cambia, Docker reutiliza esta capa sin redownloadear paquetes
COPY ["GestorInventario/GestorInventario.csproj", "GestorInventario/"]

# Descargamos los paquetes NuGet definidos en el .csproj
RUN dotnet restore "GestorInventario/GestorInventario.csproj"

# Ahora copiamos todo el código fuente al contenedor
COPY . .

# Nos movemos al directorio del proyecto
WORKDIR "/src/GestorInventario"

# Compilamos en modo Release y dejamos el resultado en /app/build
RUN dotnet build "GestorInventario.csproj" -c Release -o /app/build

# Nueva fase basada en la anterior, la llamamos 'publish'
FROM build AS publish

# Publicamos la aplicación optimizada para producción en /app/publish
# El publish incluye todo lo necesario para ejecutar la app
RUN dotnet publish "GestorInventario.csproj" -c Release -o /app/publish

# Imagen final más ligera, solo tiene el runtime de .NET 10, no el SDK completo
# Esto reduce significativamente el tamaño de la imagen final
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Directorio de trabajo en la imagen final
WORKDIR /app

# Copiamos solo los binarios publicados desde la fase 'publish'
# Descartamos el SDK, herramientas de compilación y código fuente
COPY --from=publish /app/publish .

# Documentamos los puertos que usa la aplicación
# Nota: esto no abre los puertos, eso lo hace el docker-compose con 'ports'
EXPOSE 8081 8080

# Comando que se ejecuta cuando arranca el contenedor
ENTRYPOINT ["dotnet", "GestorInventario.dll"]




