FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS build
WORKDIR /src
COPY ["GestorInventario/GestorInventario.csproj", "GestorInventario/"]
RUN dotnet restore "GestorInventario/GestorInventario.csproj"
COPY . .
WORKDIR "/src/GestorInventario"
RUN dotnet build "GestorInventario.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GestorInventario.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GestorInventario.dll"]

