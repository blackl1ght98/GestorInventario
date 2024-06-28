FROM  mcr.microsoft.com/dotnet/sdk:5 AS build
WORKDIR webapp
EXPOSE 80
EXPOSE 5000

COPY ./*.csproj ./
RUN dotnet restore 
COPY . .
RUN dotnet publish -c Relese -o out

FROM  mcr.microsoft.com/dotnet/sdk:5 
WORKDIR /webapp
copy --from=build /webapp/out
ENTRYPOINT ["dotnet", "GestorInventario.dll"]

