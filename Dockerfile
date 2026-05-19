FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY CrudWithAuth/*.csproj ./CrudWithAuth/
RUN dotnet restore CrudWithAuth/CrudWithAuth.csproj

# Copy everything else and build app
COPY CrudWithAuth/. ./CrudWithAuth/
WORKDIR /app/CrudWithAuth
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/CrudWithAuth/out ./
ENTRYPOINT ["dotnet", "CrudWithAuth.dll"]
